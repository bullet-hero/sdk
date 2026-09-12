using System;
using System.Collections.Generic;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Generators.Modifiers;
using BH.SDK.Models.Data;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;

namespace BH.SDK.Interop.AfterBeat.Import
{
    // A .vgp holds template objects; a .vgd prefab_objects entry PLACES one. Both concepts exist
    // here too, so this is one of the few parts of the format that crosses structurally rather than
    // by approximation.
    //
    // What does NOT happen here is materialization - but the ID TABLE IS STILL FILLED, and the
    // difference matters. A placement's copies stopped being written to the file: they are rebuilt
    // from pfid + ids + mod every time a level is read (BH.SDK.Utils.PrefabVirtualizationUtils), so
    // an imported level with a filled table is a COMPLETE level, drawn correctly by anything that
    // opens it, with or without a host. It used to arrive with an empty table and wait for the
    // editor to resync it once; an empty table now means a placement that expands to nothing, which
    // is the state of a placement created by hand and not yet pointed at a template.
    //
    // Minting the ids is all that is needed here, and it is deliberately all that happens: whether
    // the copies exist as objects is the reader's business at every other entry point too.
    //
    // The template gets its OWN ObjectId namespace, minted by the Prefab itself: ids are
    // scope-relative in this format, so a template object and a level object may share the number 1
    // without being related.

    /// <summary> Afterbeat prefabs and their placements. </summary>
    public static class ABPrefabImporter
    {
        /// <summary> One .vgp - or one entry of .vgd prefabs[] - into a template.
        /// <paramref name="placements"/> is where this document's placements sit on the timeline,
        /// which a Song Time autokill inside the template cannot be resolved without - see
        /// <see cref="ResolveAbsoluteBase"/>. </summary>
        public static Prefab ImportTemplate(VgpPrefab source, ABOptions options,
            InteropReport report, IDictionary<ShapeId, CompositeShape> shapes,
            ThemeData referenceTheme, string path,
            IDictionary<EffectId, EffectData> effects = null,
            ABLayerMap.Plan layerPlan = null,
            IReadOnlyDictionary<string, PlacementWindow> placements = null)
        {
            if (source == null) return null;

            var prefab = new Prefab
            {
                PrefabId = ABIdMap.ToPrefabId(string.IsNullOrEmpty(source.Id) ? source.Name : source.Id),
            };
            prefab.Root.Name = string.IsNullOrEmpty(source.Name) ? source.Id ?? string.Empty : source.Name;

            // A Prefab is one of the two places in this format where the scope and the id counter
            // are the SAME object - at level scope they are Level.Game and Level.Settings.
            var context = new ABImportContext(options, report, prefab, prefab, shapes, effects)
            {
                ReferenceTheme = referenceTheme,
                LayerPlan = layerPlan,
                AbsoluteTimeBase = ResolveAbsoluteBase(source, placements, report, path),
            };

            ABObjectImporter.ImportAll(source.Objects, context, $"{path}.objs");
            PrefabRootUtils.SetTemplateLength(prefab, MeasureDuration(prefab));

            if (!string.IsNullOrEmpty(source.Preview))
                report?.Dropped("prefab_preview",
                    "Afterbeat prefabs carry a preview image; this format has no field for one and it is not imported.",
                    path);

            // A template's own placements. Not imported: materializing a placement is the consuming
            // host's job (see this file's header), and a nested one needs it to recurse into a
            // template it has already materialized. Reported rather than dropped silently, because
            // a template carrying one draws visibly less than it should.
            if (source.Placements is { Count: > 0 })
                report?.Deferred("prefab_nested_placements",
                    "Some Afterbeat prefabs place other prefabs inside themselves; nested placements are not imported yet, so those templates arrive missing that content.",
                    path);

            return prefab;
        }

        /// <summary> Where a template's placements sit on the timeline, in SECONDS. Both ends of
        /// the window, because one placement resolves a Song Time autokill exactly and several
        /// spread apart cannot resolve it at all. </summary>
        public readonly struct PlacementWindow
        {
            /// <summary> The stretch of time a template's placements span, which is what its own timeline has to cover. </summary>
            public PlacementWindow(float earliest, float latest, int count)
            {
                Earliest = earliest;
                Latest = latest;
                Count = count;
            }

            /// <summary> Start of the earliest placement, in seconds. </summary>
            public float Earliest { get; }

            /// <summary> Start of the latest one. </summary>
            public float Latest { get; }

            /// <summary> How many placements of this template the document carries. </summary>
            public int Count { get; }

            /// <summary> True when every placement of this template starts on the same time, so one
            /// resolution is right for all of them. </summary>
            public bool IsSingular => Latest <= Earliest;
        }

        /// <summary> Every template's placement window, keyed by the source document's own prefab
        /// id. Null when the document places nothing. </summary>
        public static Dictionary<string, PlacementWindow> MeasurePlacements(
            IReadOnlyList<VgdPrefabPlacement> placements)
        {
            if (placements == null || placements.Count == 0) return null;

            var windows = new Dictionary<string, PlacementWindow>();
            foreach (var placement in placements)
            {
                if (placement == null || string.IsNullOrEmpty(placement.PrefabId)) continue;

                var time = placement.StartTime;
                windows[placement.PrefabId] = windows.TryGetValue(placement.PrefabId, out var window)
                    ? new PlacementWindow(Math.Min(window.Earliest, time),
                        Math.Max(window.Latest, time), window.Count + 1)
                    : new PlacementWindow(time, time, 1);
            }

            return windows.Count > 0 ? windows : null;
        }

        // WHERE A TEMPLATE WILL BE PLACED IS PART OF ITS CONTENT, for one autokill rule out of five.
        // Song Time names an absolute moment in the song, the source game keeps it absolute through
        // a prefab, and its objects therefore live `ak_o` minus where the placement put them -
        // ABTimeMap.ResolveEndTime's own header carries the source-side citation. A template read on
        // its own resolves those objects as living the whole of ak_o, which on real content is
        // minutes, and the materializer then adds the placement's start to that.
        //
        // The EARLIEST placement, not the latest or the mean: a lifetime is `ak_o - start`, so the
        // earliest start is the longest lifetime, and erring long keeps content the source game drew
        // rather than cutting content it did not. It is EXACT for a template placed once, which is
        // most of them - an author who wants a group at several times usually ends up with several
        // prefabs, since saving a prefab over there makes a new one rather than reusing it.
        private static float ResolveAbsoluteBase(VgpPrefab source,
            IReadOnlyDictionary<string, PlacementWindow> placements, InteropReport report, string path)
        {
            if (placements == null || string.IsNullOrEmpty(source.Id)) return 0f;
            if (!placements.TryGetValue(source.Id, out var window)) return 0f;

            if (!window.IsSingular && HasSongTimeAutokill(source))
                report?.Approximated("autokill_songtime_in_prefab",
                    "Some prefabs contain objects that die at a fixed moment of the SONG and are placed at several different times; every copy was resolved against the earliest of those placements, so the later ones stay on screen longer than they did.",
                    path);

            return window.Earliest - source.Offset;
        }

        private static bool HasSongTimeAutokill(VgpPrefab source)
        {
            if (source.Objects == null) return false;

            foreach (var obj in source.Objects)
                if (obj != null && (ABAutokillType)obj.AutokillType == ABAutokillType.SongTime)
                    return true;
            return false;
        }

        // A PLACEMENT IS NAMED AFTER WHAT IT PLACES. Its source id is a bare Guid, and naming the
        // placement after it put a few hundred rows of hex in the timeline of a prefab-heavy level -
        // unreadable, and unsearchable, since the one string an author would look for is the
        // template's name. Nothing is lost by it: the placement is MINTED from that id, so a second
        // import lands on the same object, and which template it places is still PrefabObject.PrefabId.
        private static string ResolveName(VgdPrefabPlacement source, ABImportContext context,
            PrefabId prefabId, IReadOnlyDictionary<PrefabId, Prefab> templates)
        {
            if (!context.Options.KeepObjectNames) return string.Empty;

            if (templates != null && templates.TryGetValue(prefabId, out var template)
                                  && !string.IsNullOrEmpty(template?.Root?.Name))
                return template.Root.Name;

            return source.Id ?? string.Empty;
        }

        /// <summary> Records a template's lead time so the placements can apply it. Separate from
        /// <see cref="ImportTemplate"/> because a template imported on its own - a bare .vgp - has
        /// no placement to apply it to and no context to record it in. </summary>
        public static void RegisterLeadTime(VgpPrefab source, Prefab prefab, ABImportContext context)
        {
            if (source == null || prefab == null || context == null) return;
            if (source.Offset != 0f) context.PrefabLeadTimes[prefab.PrefabId] = source.Offset;
        }

        /// <summary> One .vgd prefab_objects entry into a placement in the hosting scope.
        /// <paramref name="placementIndex"/> is its position in the source list, which is what gives
        /// it a timeline row of its own. </summary>
        public static PrefabObject ImportPlacement(VgdPrefabPlacement source,
            ABImportContext context, int levelFrameDuration, string path,
            IReadOnlyDictionary<PrefabId, Prefab> templates = null, int placementIndex = 0)
        {
            if (source == null) return null;

            var prefabId = string.IsNullOrEmpty(source.PrefabId)
                ? PrefabId.Null
                : ABIdMap.ToPrefabId(source.PrefabId);

            var placement = new PrefabObject
            {
                ObjectId = context.Mint(source.Id),
                PrefabId = prefabId,
                Name = ResolveName(source, context, prefabId, templates),
                Active = true,
                Span = ResolveSpan(source, context, prefabId, levelFrameDuration, templates),
                Layer = ResolveLayer(context, placementIndex, path),
                ParentObjectId = context.ResolveParent(source.ParentId, path),
            };

            // THE ID TABLE IS FILLED HERE, and that is what makes an imported level complete on its
            // own rather than complete once a host has resynced it. A placement's copies are not in
            // the file any more - they are rebuilt from pfid + ids + mod on the way in - so an empty
            // table is no longer "not materialized yet", it is a placement that expands to nothing.
            // Minting is all that is needed: the ids are what the table means, and the copies
            // themselves are BH.SDK.Utils.PrefabVirtualizationUtils' work, here as everywhere else.
            Prefab placed = null;
            templates?.TryGetValue(prefabId, out placed);

            if (placed?.Objects != null)
                foreach (var innerId in placed.Objects.Keys)
                    placement.ObjectIds[innerId] = context.Mint(null);

            // See VgdPrefabPlacement.RepeatCount: the source game stores it and draws nothing from
            // it, so neither does this - but a level author who set it meant something by it.
            if (source.RepeatCount > 0)
                context.Report.Deferred("placement_repeat",
                    "Some prefab placements are marked to repeat; the source game does not draw the repetitions either, so they were imported as the single placement they render as.",
                    path);

            var x = source.GetValue(VgdPrefabPlacement.TrackIndex.Position, 0);
            var y = source.GetValue(VgdPrefabPlacement.TrackIndex.Position, 1);
            var width = source.GetValue(VgdPrefabPlacement.TrackIndex.Scale, 0, 1f);
            var height = source.GetValue(VgdPrefabPlacement.TrackIndex.Scale, 1, 1f);
            var rotation = source.GetValue(VgdPrefabPlacement.TrackIndex.Rotation, 0);

            placement.Positions.Add(new PosKey(new Vector2Value(x, y), FrameRules.MinFrame));
            placement.Scales.Add(new ScaKey(new Vector2Value(width, height), FrameRules.MinFrame));
            placement.Rotations.Add(new AngleKey(
                new FloatValue(rotation * ABValueMap.DegreesToRadians), FrameRules.MinFrame));

            RecordRootOverrides(placement, placed);

            if (string.IsNullOrEmpty(source.PrefabId))
                context.Report.Approximated("placement_without_prefab",
                    "Some prefab placements name no prefab; Afterbeat removes those on load, and they were imported as empty placements.",
                    path);

            return placement;
        }

        // A PLACEMENT'S OWN TRANSFORM IS AN OVERRIDE NOW, not just a field, and without this every
        // imported placement snapped to the origin the first time the level was opened. A template
        // owns its root and a placement is that root's materialization (Prefab.Root), so
        // PrefabRootUtils.ApplyRoot rewrites Name and all seven positional tracks from the template
        // on every load and every resync - and an Afterbeat template's root is identity, the source
        // format having no such object at all. Three tracks written onto the placement alone were
        // therefore three tracks discarded by the very next expansion.
        //
        // THE VALUES STAY ON THE PLACEMENT AS WELL, deliberately: a reader that never expands (an
        // export, a stats pass) sees the level the source described, and for one that does expand
        // ApplyModifications puts the same numbers back afterwards. The override carries a COPY,
        // since ApplyModifications assigns the stored list straight onto the object and a shared
        // instance would make a later edit rewrite the override it came from.
        //
        // Only what DIFFERS from the root is recorded. For an Afterbeat import that is every
        // non-empty track, since the root is identity - but the rule is the general one, so a
        // template that gains a real root later records only the real divergence.
        private static void RecordRootOverrides(PrefabObject placement, Prefab template)
        {
            var root = template?.Root;
            if (root == null) return;

            if (placement.Name != root.Name)
                Record(ModificationFields.Name, placement.Name);

            if (!placement.Positions.ListEquals(root.Positions))
                Record(ModificationFields.Positions, placement.Positions.CopyList());
            if (!placement.Rotations.ListEquals(root.Rotations))
                Record(ModificationFields.Rotations, placement.Rotations.CopyList());
            if (!placement.Scales.ListEquals(root.Scales))
                Record(ModificationFields.Scales, placement.Scales.CopyList());

            void Record(int field, object value)
            {
                var key = new ModificationKey(ObjectId.PrefabRoot, field);
                placement.Modifications[key] = new Modification(key, value);
            }
        }

        // A placement's own start is what times everything inside it: the template's objects are
        // authored relative to the placement, and the materializer shifts every copy by this span's
        // start frame. Reading it as zero puts an entire prefab library in the first second of the
        // level, which is what happened for as long as the placement had no time field at all.
        //
        // The template's LEAD TIME is part of that start rather than something lost with the
        // template: the source game spawns every copied object at `placement.t - prefab.Offset +
        // its own st` (ObjectManager.AddPrefabToLevel), so a lead time of half a second means the
        // whole placement plays half a second EARLIER, and nothing else about it changes. Which is
        // exactly a shift of this span - so it crosses, and only a placement pushed before frame
        // zero by one loses anything (the clamp in FromFrames, matching the source game's own).
        //
        // The length is the TEMPLATE's, not the level's. Spanning the level instead would work
        // (a child must lie inside its parent, and a level-long parent cuts nothing short), but it
        // makes every placement's clip cover the whole timeline, which is unreadable in the editor
        // and useless to Trim/Blade. A placement whose template is missing keeps the old behaviour,
        // since there is no length to take.
        private static FrameSpan ResolveSpan(VgdPrefabPlacement source, ABImportContext context,
            PrefabId prefabId, int levelFrameDuration, IReadOnlyDictionary<PrefabId, Prefab> templates)
        {
            context.PrefabLeadTimes.TryGetValue(prefabId, out var leadTime);
            var startFrame = ABTimeMap.ToFrame(source.StartTime - leadTime, context.Options.Framerate);

            var duration = levelFrameDuration;
            if (templates != null && templates.TryGetValue(prefabId, out var template) && template != null)
                duration = template.Root.Span.FrameDuration;

            return ABTimeMap.FromFrames(startFrame, startFrame + duration);
        }

        // A PLACEMENT IS NOT A RENDER CONCEPT over there - it is a group of ordinary objects, each
        // carrying its own depth, drawn against the level's own objects by that depth and nothing
        // else. So a placement takes no draw order of its own: it sits on layer 0 and its
        // materialized children keep the layers the level-wide plan already gave them (Layer is
        // parent-relative here, so a placement at 0 adds nothing to the subtree hanging off it).
        //
        // Giving each placement a layer of its own is what this replaced, and the cost was the
        // level's whole positive range: one real level's 386 placements reached layer +395, on top
        // of templates ranked independently underneath. The motive was timeline rows for the
        // several thousand objects a prefab-heavy level materializes into - a real problem, and one
        // that belongs to the editor's own grouping rather than to draw order.
        //
        // The offset therefore now defaults to 0 and the old stacking is what an author opts INTO
        // by raising it, in which case a level with more placements than there is room above the
        // base wraps rather than piling the remainder on the last one.
        private static int ResolveLayer(ABImportContext context, int placementIndex, string path)
        {
            var offset = context.Options.PlacementLayerOffset;
            if (offset <= 0) return ValueRules.DefaultLayer;

            var baseLayer = context.HighestContentLayer + offset;

            var room = ValueRules.MaxLayer - baseLayer + 1;
            if (room <= 0) return ValueRules.MaxLayer;

            if (placementIndex >= room)
                context.Report.Approximated("placement_layers_wrapped",
                    $"This level has more prefab placements than the {room} draw-order layers left above their base, so some of them share a layer.",
                    path);

            return baseLayer + placementIndex % room;
        }

        // A template's own length is not stored in a .vgp, so it is measured: the last frame any of
        // its objects reaches. A template shorter than its content would clip the content.
        //
        // KEYFRAMES count, not just spans, and that is the part that is easy to miss. A keyframe's
        // frame is local to its object's span and may sit past the span's own end - under the source
        // format's "die on the last keyframe" rule the final key lands exactly ON the end boundary,
        // and an object cut short by a Fixed Time autokill can carry keys well beyond it. Measuring
        // spans alone produced templates whose own keyframes were out of range the moment the level
        // was validated, on real content, in numbers (32 findings on one level).
        private static int MeasureDuration(Prefab prefab)
        {
            var end = FrameRules.MinFrameDuration;
            foreach (var pair in prefab.Objects)
            {
                var obj = pair.Value;
                if (obj == null) continue;

                // A COUNT is what this measures, and a timeline of N frames holds frames 1..N, so
                // the count a frame demands is that frame's own number. EndFrame is the exclusive
                // boundary AFTER the span, hence its LastFrame here.
                if (obj.Span.LastFrame > end) end = obj.Span.LastFrame;

                foreach (var track in ObjectTracks.Of(obj, ObjectTrackMask.All))
                    for (var i = 0; i < track.Count; i++)
                    {
                        var reach = FrameRules.CountOf(obj.Span.ToGlobalFrame(track.FrameAt(i)));
                        if (reach > end) end = reach;
                    }
            }

            return Math.Clamp(end, FrameRules.MinFrameDuration, FrameRules.MaxFrameDuration);
        }
    }
}
