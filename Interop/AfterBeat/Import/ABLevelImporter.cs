using System;
using System.Collections.Generic;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Interop.AfterBeat.Import
{
    // The whole level, in the order the pieces depend on each other:
    //
    //   themes first, because every colour anywhere in the level is resolved against one;
    //   then the level's own length, because parallax and prefab placements are spanned by it;
    //   then prefab templates, because a placement references one;
    //   then objects, parallax and placements, which all write into the same scope;
    //   then the level-global event tracks, which reference themes by the ids step one derived.
    //
    // The length itself is MEASURED rather than read: Afterbeat stores no level length at all - a
    // level is as long as its song, and the song is a file this library cannot open. So the
    // timeline is stretched to the last frame anything in the file reaches, and the host is free to
    // stretch it further once it knows the clip's real duration.

    /// <summary> A whole .vgd (plus its .vgm) into a <see cref="Level"/> and a <see cref="LevelMeta"/>. </summary>
    public static class ABLevelImporter
    {
        /// <summary> Padding added past the last frame anything reaches, so the level does not end
        /// on the exact frame its final object dies. </summary>
        public const int TailFrames = 60;

        /// <summary> The result of one import: the two documents plus everything given up on the
        /// way. </summary>
        public readonly struct Result
        {
            /// <summary> The level that came out. </summary>
            public Level Level { get; }

            /// <summary> Its metadata, which the source keeps in a separate document. </summary>
            public LevelMeta Meta { get; }

            /// <summary> What the import had to say about itself. </summary>
            public InteropReport Report { get; }

            /// <summary> All three at once; nothing mutates them afterwards. </summary>
            public Result(Level level, LevelMeta meta, InteropReport report)
            {
                Level = level;
                Meta = meta;
                Report = report;
            }
        }

        /// <summary> Reads the two documents as text. A document that is not JSON at all is
        /// reported as a failure rather than thrown, so one unreadable metadata file does not cost
        /// the level. </summary>
        public static Result ImportJson(string levelJson, string metaJson,
            ABOptions options = null, InteropReport report = null)
        {
            report ??= new InteropReport();

            if (!ABSerialization.TryDeserialize<VgdLevel>(levelJson, out var source, out var error))
            {
                report.Failed("level_unreadable", $"The .vgd could not be read: {error}", "level");
                return new Result(null, null, report);
            }

            VgmMeta meta = null;
            if (!string.IsNullOrWhiteSpace(metaJson)
                && !ABSerialization.TryDeserialize<VgmMeta>(metaJson, out meta, out var metaError))
                report.Failed("meta_unreadable",
                    $"The metadata document could not be read: {metaError}. The level was imported without it.",
                    "metadata");

            return Import(source, meta, options, report);
        }

        /// <summary> Reads two already-parsed documents. </summary>
        public static Result Import(VgdLevel source, VgmMeta meta,
            ABOptions options = null, InteropReport report = null)
        {
            report ??= new InteropReport();
            options = (options ?? new ABOptions()).Sanitized();

            if (source == null)
            {
                report.Failed("level_missing", "There was no level document to import.", "level");
                return new Result(null, null, report);
            }

            // Structure first: which objects end up behind a placement decides what the layout
            // below has to lay out. A copy when anything changes - the caller's document is theirs.
            source = ABPrefabExtractor.Apply(source, options, report);

            var level = new Level();
            level.Settings.Fps = options.Framerate;

            var context = new ABImportContext(options, report,
                level.Game, level.Settings, level.Resources.CompositeShapes,
                level.Resources.Effects);

            ResolveLayers(source, options, context);

            ImportThemes(source, level, context);
            level.Settings.FrameDuration = ResolveDuration(source, meta, options, report);

            if (options.ImportPrefabs) ImportPrefabs(source, level, context);
            ImportCameraScaleRoot(source, level, context);
            ABObjectImporter.ImportAll(source.Objects, context, "objects");
            if (options.ImportParallax)
                ABParallaxImporter.ImportAll(source.Parallax, context,
                    level.Settings.FrameDuration, "parallax_settings");
            if (options.ImportPrefabs) ImportPlacements(source, level, context);

            ABEventsImporter.ImportAll(source, level, context, "events");
            ABEventsImporter.ImportEvents(source, level, context, "events");
            ABEventsImporter.ImportBeats(source, level, context);

            var levelMeta = ABMetaImporter.Import(meta, report);
            return new Result(level, levelMeta, report);
        }

        #region Steps

        // BEFORE ANYTHING IS IMPORTED, because everything that is imported reads the answer: the
        // level's own objects and every template's are ordered against ONE table of depths, since
        // that is what the source game does with them - a template's objects are copied into the
        // level and sorted against its own by depth alone. Resolving each list separately and then
        // stacking the results is what this replaced; see ABLayerMap's header for what it cost.
        //
        // The templates are read here even though they are imported later, and the parallax
        // deliberately is not: its objects are drawn by a different camera over there, so they
        // belong below the whole band rather than inside it (ABParallaxImporter places them off
        // LowestContentLayer).
        private static void ResolveLayers(VgdLevel source, ABOptions options, ABImportContext context)
        {
            var lists = new List<IReadOnlyList<VgdObject>> { source.Objects };

            if (options.ImportPrefabs && source.Prefabs != null)
                foreach (var prefab in source.Prefabs)
                    if (prefab?.Objects != null)
                        lists.Add(prefab.Objects);

            context.LayerPlan = ABLayerMap.Build(lists, options, context.Report, "objects");

            if (options.LayerImport == ABLayerImport.Packed)
            {
                PackLayers(source, options, context);
                return;
            }

            context.RegisterContentLayers(context.LayerPlan.Lowest, context.LayerPlan.Highest);
        }

        // Themes are read first because every literal colour anywhere in the level is resolved
        // against one, and the theme the LEVEL STARTS ON becomes that reference - a colour is
        // resolved once at import while the active theme changes over time, so there is no single
        // right answer, and the first frame's theme is the one an author sees when the level opens.
        //
        // A level that authored no palette of its own carries an EMPTY themes[] and names one of
        // the game's own by index, so the shipped table is read whenever the theme track points at
        // an id the file does not define - see ABDefaultThemes. Those land in the level's own
        // Resources.Themes as ordinary custom themes, which is what makes the converted level
        // self-contained: nothing afterwards, here or in the editor, can tell them apart from a
        // theme the author wrote.
        private static void ImportThemes(VgdLevel source, Level level, ABImportContext context)
        {
            if (source.Themes != null)
                for (var i = 0; i < source.Themes.Count; i++)
                {
                    var sourceTheme = source.Themes[i];
                    if (sourceTheme == null) continue;

                    var theme = ABThemeMap.Import(sourceTheme, context.Report, $"themes[{i}]");
                    level.Resources.Themes[theme.ThemeId] = theme;
                }

            ImportReferencedDefaultThemes(source, level, context);
            context.ReferenceTheme = ResolveReferenceTheme(source, level);
        }

        private static void ImportReferencedDefaultThemes(VgdLevel source, Level level,
            ABImportContext context)
        {
            var missing = 0;

            foreach (var key in source.GetEvents(ABEventTrack.Theme))
            {
                var sourceId = key?.GetString(0);
                if (string.IsNullOrEmpty(sourceId)) continue;

                var themeId = ABIdMap.ToThemeId(sourceId);
                if (level.Resources.Themes.ContainsKey(themeId)) continue;

                var shipped = ABDefaultThemes.Get(sourceId);
                if (shipped == null)
                {
                    missing++;
                    continue;
                }

                level.Resources.Themes[themeId] =
                    ABThemeMap.Import(shipped, context.Report, $"default_themes[{sourceId}]");
            }

            if (missing > 0)
                context.Report.Dropped("theme_reference_unknown",
                    "The theme track names themes this level does not define and the source game does not ship; those keyframes select nothing.",
                    "events");
        }

        // The earliest keyframe of the theme track, not the earliest one written: the track is a
        // list in file order, which a hand-edited document is under no obligation to sort.
        private static ThemeData ResolveReferenceTheme(VgdLevel source, Level level)
        {
            string startId = null;
            var startTime = float.MaxValue;

            foreach (var key in source.GetEvents(ABEventTrack.Theme))
            {
                var sourceId = key?.GetString(0);
                if (string.IsNullOrEmpty(sourceId) || key.Time >= startTime) continue;

                startTime = key.Time;
                startId = sourceId;
            }

            if (startId != null
                && level.Resources.Themes.TryGetValue(ABIdMap.ToThemeId(startId), out var started))
                return started;

            foreach (var pair in level.Resources.Themes) return pair.Value;
            return null;
        }

        // The node Afterbeat hangs every camera-parented object off - see ABImportContext
        // .CameraScaleRootId for why it has to exist at all. It is built BEFORE the objects, since
        // resolving their "camera" parent is what it exists for.
        //
        // Built only when the level has something parented to the camera: an empty object costs a
        // timeline row and an id, and a level that never uses the feature should read exactly as it
        // did. A level whose zoom never moves still gets one - the factor is constant then, but it
        // is a constant of 1.5 on the ordinary authored zoom of 30, not 1.
        private static void ImportCameraScaleRoot(VgdLevel source, Level level, ABImportContext context)
        {
            if (source.Objects == null) return;

            var used = false;
            foreach (var obj in source.Objects)
                if (obj != null && obj.IsParentedToCamera)
                {
                    used = true;
                    break;
                }

            if (!used) return;

            var root = new RectObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                ParentObjectId = ObjectId.Camera,
                Name = CameraScaleRootName,
                Active = true,
                Layer = ValueRules.DefaultLayer,
                Span = new FrameSpan(FrameRules.MinFrame,
                    Math.Max(FrameRules.MinFrameDuration, level.Settings.FrameDuration)),
            };

            ReadCameraScale(source, context, root.Scales);

            if (root.Scales.Count == 0)
                root.Scales.Add(new ScaKey(Vector2Value.One, FrameRules.MinFrame));

            level.Game.Objects[root.ObjectId] = root;
            context.CameraScaleRootId = root.ObjectId;

            context.Report.Info("camera_scale_root",
                "Afterbeat scales everything parented to its camera by the camera's own zoom, so a node carrying that scale was rebuilt and those objects hang off it - without it they draw at the size they were authored at rather than the size they were seen at.",
                "objects");
        }

        /// <summary> Name the rebuilt camera-scale node carries. The export reads it back to flatten
        /// the node away again - the source format applies the same factor itself, so writing the
        /// node out would scale that content twice. </summary>
        public const string CameraScaleRootName = "Camera Scale";

        // One key per zoom keyframe, capped like any other track. The factor is the zoom over the
        // format's neutral 20; the eases cross with it, so a zoom ramp scales the way it ramped.
        //
        // Deduplicated through the SAME call the camera's own Zooms track goes through, and that
        // matters beyond validity: these scales and that track are two readings of one source
        // event, so a collision resolved differently here would leave the camera framed by one
        // zoom keyframe while everything parented to it is scaled by the other. Capping happens
        // after, since a key that is about to be dropped should not spend a slot.
        private static void ReadCameraScale(VgdLevel source, ABImportContext context, List<ScaKey> into)
        {
            var framerate = context.Options.Framerate;

            foreach (var key in source.GetEvents(ABEventTrack.CameraZoom))
            {
                var factor = key.GetFloat(0) / ABEventsImporter.DefaultSourceZoom;
                into.Add(new ScaKey(new Vector2Value(factor, factor),
                    ABTimeMap.ToFrame(key.Time, framerate),
                    ABEaseMap.Import(key.Ease, context.Report, "events")));
            }

            ABTimeMap.DeduplicateByFrame(into, k => k.Frame, context.Report, "events");

            // TWO ceilings meet on this one track and the lower of them wins, which is not a
            // belt-and-braces clamp but the only correct answer: what is built here is an ordinary
            // object's Scales track, so RectObject's own MaxObjectKeys rule validates it - yet it
            // MIRRORS the camera's Zooms track, so a slot past MaxCameraKeys could never be filled
            // by anything. Capping by the camera's number alone would fail validation the moment
            // the two constants diverged; capping by the object's alone is what used to happen, and
            // while that was 32 against the camera's 512 it froze every camera-parented object at
            // its 32nd keyframe - three quarters of the way through one real level's zoom work,
            // with the camera itself still zooming correctly around them.
            var cap = System.Math.Min(LevelRules.MaxObjectKeys, LevelRules.MaxCameraKeys);
            if (into.Count <= cap) return;

            into.RemoveRange(cap, into.Count - cap);
            context.Report.Dropped("camera_scale_keys_over_cap",
                $"This level animates its camera zoom with more than {cap} keyframes, which is this format's limit for the hidden node that keeps camera-parented objects the right size; past that point those objects hold the size they had.",
                "events");
        }

        // WHERE THE PLACEMENTS SIT IS AN INPUT TO THE TEMPLATES, which is why they are measured
        // here rather than read in the pass that imports them: a Song Time autokill inside a
        // template names an absolute moment of the song and cannot be resolved without knowing what
        // the template's own times are relative to. See ABPrefabImporter.ResolveAbsoluteBase.
        private static void ImportPrefabs(VgdLevel source, Level level, ABImportContext context)
        {
            if (source.Prefabs == null) return;

            var placements = ABPrefabImporter.MeasurePlacements(source.PrefabPlacements);

            for (var i = 0; i < source.Prefabs.Count; i++)
            {
                var sourcePrefab = source.Prefabs[i];
                if (sourcePrefab == null) continue;

                // Counted rather than summarised as "the rest": a level over the cap loses whole
                // sections of itself, and the two numbers are what tell an author whether what they
                // are looking at is the level or a fraction of it.
                if (level.Resources.Prefabs.Count >= LevelRules.MaxPrefabs)
                {
                    CountPrefabsOverCap(source, placements, i, out var lostPrefabs, out var lostPlacements);
                    context.Report.Dropped("prefabs_over_cap",
                        $"This format allows {LevelRules.MaxPrefabs} prefabs per level and this one has {source.Prefabs.Count}: {lostPrefabs} were dropped, taking {lostPlacements} of their placements with them.",
                        "prefabs");
                    break;
                }

                var prefab = ABPrefabImporter.ImportTemplate(sourcePrefab, context.Options,
                    context.Report, level.Resources.CompositeShapes, context.ReferenceTheme,
                    $"prefabs[{i}]", level.Resources.Effects, context.LayerPlan, placements,
                    context.PackedTemplateLayers?.GetValueOrDefault(sourcePrefab));
                if (prefab == null) continue;

                level.Resources.Prefabs[prefab.PrefabId] = prefab;
                ABPrefabImporter.RegisterLeadTime(sourcePrefab, prefab, context);
            }
        }

        /// <summary> What the prefab cap costs this level, counted from the first template it could
        /// not take. </summary>
        private static void CountPrefabsOverCap(VgdLevel source,
            IReadOnlyDictionary<string, ABPrefabImporter.PlacementWindow> placements,
            int firstDropped, out int lostPrefabs, out int lostPlacements)
        {
            lostPrefabs = 0;
            lostPlacements = 0;

            for (var i = firstDropped; i < source.Prefabs.Count; i++)
            {
                var sourcePrefab = source.Prefabs[i];
                if (sourcePrefab == null) continue;

                lostPrefabs++;
                if (placements != null && !string.IsNullOrEmpty(sourcePrefab.Id)
                                       && placements.TryGetValue(sourcePrefab.Id, out var window))
                    lostPlacements += window.Count;
            }
        }

        // Placements are minted into the LEVEL's scope, after the ordinary objects, so an id table
        // built for the objects cannot collide with them. Their children are minted too - into the
        // placement's own id table rather than as objects, which is what a level file carries now;
        // see ABPrefabImporter's header.
        private static void ImportPlacements(VgdLevel source, Level level, ABImportContext context)
        {
            if (source.PrefabPlacements == null) return;

            // Minted up front for the same reason the objects are: a placement can be PARENTED to
            // another placement, and the source list is in no particular order.
            foreach (var sourcePlacement in source.PrefabPlacements)
                if (sourcePlacement != null)
                    context.Mint(sourcePlacement.Id);

            for (var i = 0; i < source.PrefabPlacements.Count; i++)
            {
                var sourcePlacement = source.PrefabPlacements[i];
                if (sourcePlacement == null) continue;

                var placement = ABPrefabImporter.ImportPlacement(sourcePlacement, context,
                    level.Settings.FrameDuration, $"prefab_objects[{i}]", level.Resources.Prefabs, i);
                if (placement == null) continue;

                // A placement naming a template this level does not have is ordinary content over
                // there - Afterbeat drops those on load - and a dangling reference here, which the
                // graph analyzer reports one per placement. Real levels carry dozens of them, and
                // the prefab cap makes more of them. Dropping matches what the source game does.
                if (placement.PrefabId.IsEnabled()
                    && !level.Resources.Prefabs.ContainsKey(placement.PrefabId))
                {
                    context.Report.Dropped("placement_prefab_missing",
                        "Some prefab placements name a template this level does not contain; Afterbeat removes those on load, and they were not imported.",
                        $"prefab_objects[{i}]");
                    continue;
                }

                level.Game.Objects[placement.ObjectId] = placement;
            }

            if (source.PrefabPlacements.Count > 0)
                context.Report.Info("placements_imported",
                    "Prefab placements were imported as placements rather than as copies of their templates, with the id table that makes their content rebuildable.",
                    "prefab_objects");
        }

        #endregion

        #region Packed layout

        // The packer works on the SOURCE document, not on the models being built, because it has to
        // run before any of them exist: an object's own layer is written as it is imported, and the
        // row it gets depends on every other object alive at the same time. So the lifetimes are
        // resolved here the way the importer will resolve them - the same ABTimeMap call, the same
        // parent the object will end up with - and clipped to that parent's, which is what a child
        // actually plays as (a child outside its parent is not drawn).
        //
        // Templates are packed first and each on its own - a template's rows start at 1, above its
        // Root - because a placement's block in the level is as tall as its template drew.
        private static void PackLayers(VgdLevel source, ABOptions options, ABImportContext context)
        {
            var framerate = options.Framerate;
            var templates = new Dictionary<string, (ABLayoutPacker.Result Result, int Length, float Lead)>();

            if (options.ImportPrefabs && source.Prefabs != null)
            {
                context.PackedTemplateLayers = new Dictionary<VgpPrefab, Dictionary<string, int>>();
                var windows = ABPrefabImporter.MeasurePlacements(source.PrefabPlacements);

                for (var i = 0; i < source.Prefabs.Count; i++)
                {
                    var prefab = source.Prefabs[i];
                    if (prefab?.Objects == null) continue;

                    var absoluteBase = 0f;
                    if (windows != null && !string.IsNullOrEmpty(prefab.Id)
                                        && windows.TryGetValue(prefab.Id, out var window))
                        absoluteBase = window.Earliest - prefab.Offset;

                    var entries = BuildEntries(prefab.Objects, framerate, absoluteBase, out var end);
                    var result = ABLayoutPacker.Pack(entries, ABLayoutPacker.Scope.Template,
                        context.Report, $"prefabs[{i}]");

                    context.PackedTemplateLayers[prefab] = Unprefix(result.OwnLayers, ObjectPrefix);
                    if (!string.IsNullOrEmpty(prefab.Id))
                        templates[prefab.Id] = (result,
                            Math.Max(FrameRules.MinFrameDuration, end - FrameRules.MinFrame), prefab.Offset);
                }
            }

            var levelEntries = BuildEntries(source.Objects, framerate, 0f, out _);

            if (options.ImportPrefabs && source.PrefabPlacements != null)
                foreach (var placement in source.PrefabPlacements)
                {
                    if (placement == null || string.IsNullOrEmpty(placement.Id)) continue;
                    if (placement.PrefabId == null || !templates.TryGetValue(placement.PrefabId, out var template))
                        continue;

                    var start = Math.Max(FrameRules.MinFrame,
                        ABTimeMap.ToFrame(placement.StartTime - template.Lead, framerate));
                    levelEntries.Add(new ABLayoutPacker.Entry
                    {
                        Id = PlacementPrefix + placement.Id,
                        ParentId = string.IsNullOrEmpty(placement.ParentId)
                                   || placement.ParentId == VgdObject.CameraParentId
                            ? null
                            : ObjectPrefix + placement.ParentId,
                        IsPlacement = true,
                        Start = start,
                        End = start + template.Length,
                        Band = template.Result.MajorityBand,
                        Depth = template.Result.NearestDepth,
                        FarDepth = template.Result.FarthestDepth,
                        RenderHeight = template.Result.Highest,
                        Collides = template.Result.AnyCollides,
                        Order = levelEntries.Count,
                    });
                }

            var packed = ABLayoutPacker.Pack(levelEntries, ABLayoutPacker.Scope.Level, context.Report, "objects",
                options.CollidersAbovePlayer);

            context.PackedLayers = Unprefix(packed.OwnLayers, ObjectPrefix);
            context.PackedPlacementLayers = Unprefix(packed.OwnLayers, PlacementPrefix);
            context.RegisterContentLayers(packed.Lowest, packed.Highest);
        }

        // The source keeps objects and placements in two id spaces, and the packer sees one.
        private const string ObjectPrefix = "o:";
        private const string PlacementPrefix = "p:";

        private static Dictionary<string, int> Unprefix(Dictionary<string, int> layers, string prefix)
        {
            var result = new Dictionary<string, int>();
            foreach (var pair in layers)
                if (pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                    result[pair.Key.Substring(prefix.Length)] = pair.Value;
            return result;
        }

        /// <summary> One scope's objects as packer entries, parented the way the importer will
        /// parent them and spanned the way they will play. <paramref name="end"/> is the latest end
        /// boundary any of them reaches. </summary>
        private static List<ABLayoutPacker.Entry> BuildEntries(IReadOnlyList<VgdObject> sources,
            int framerate, float absoluteBase, out int end)
        {
            var reach = FrameRules.MinFrame;
            var entries = new List<ABLayoutPacker.Entry>();
            if (sources == null)
            {
                end = reach;
                return entries;
            }

            var byId = new Dictionary<string, VgdObject>();
            foreach (var obj in sources)
                if (obj != null && !string.IsNullOrEmpty(obj.Id))
                    byId[obj.Id] = obj;

            var spans = new Dictionary<VgdObject, (int Start, int End)>();

            for (var i = 0; i < sources.Count; i++)
            {
                var obj = sources[i];
                if (obj == null || string.IsNullOrEmpty(obj.Id)) continue;

                var parent = ResolvePackParent(obj, byId);
                var (start, stop) = Resolve(obj, new HashSet<VgdObject>());

                // What a template is as long as counts keyframes past an object's own end, exactly
                // as ABPrefabImporter.MeasureDuration does, and the AUTHORED span rather than the
                // clipped one, which is what that measures - a placement packed shorter than it
                // plays would share a row with whatever it actually overlaps.
                var authored = ABTimeMap.ResolveSpan(obj, framerate, null, null, absoluteBase);
                reach = Math.Max(reach, Math.Max(stop, authored.StartFrame + authored.FrameDuration));
                if (obj.Tracks != null)
                    foreach (var track in obj.Tracks)
                        if (track?.Keyframes != null)
                            foreach (var key in track.Keyframes)
                                if (key != null)
                                    reach = Math.Max(reach,
                                        ABTimeMap.ToFrame(obj.StartTime + key.Time, framerate) + 1);

                entries.Add(new ABLayoutPacker.Entry
                {
                    Id = ObjectPrefix + obj.Id,
                    ParentId = parent == null ? null : ObjectPrefix + parent.Id,
                    Start = start,
                    End = stop,
                    Band = ToPackBand(ABLayerMap.ToBand(obj)),
                    Depth = ABLayerMap.ToDepth(obj),
                    Rendered = obj.ObjectType is not ((int)ABObjectType.Empty or (int)ABObjectType.AlphaEmpty),
                    Collides = obj.ObjectType is (int)ABObjectType.Normal or (int)ABObjectType.Hit
                               && obj.Shape != (int)ABShape.Text,
                    Order = i,
                });
            }

            end = reach;
            return entries;

            (int Start, int End) Resolve(VgdObject obj, HashSet<VgdObject> seen)
            {
                if (spans.TryGetValue(obj, out var cached)) return cached;

                var span = ABTimeMap.ResolveSpan(obj, framerate, null, null, absoluteBase);
                var start = span.StartFrame;
                var stop = span.StartFrame + span.FrameDuration;

                var parent = ResolvePackParent(obj, byId);
                if (parent != null && seen.Add(obj))
                {
                    var (parentStart, parentEnd) = Resolve(parent, seen);
                    start = Math.Max(start, parentStart);
                    stop = Math.Min(stop, parentEnd);
                    if (stop <= start) stop = start + 1;
                }

                spans[obj] = (start, stop);
                return (start, stop);
            }
        }

        // The parent the importer will actually give the object: none for an object inheriting
        // nothing (it is imported as a root), none for the camera (the camera-scale node sits on
        // layer 0, and a template has no camera at all), none for a missing id.
        private static VgdObject ResolvePackParent(VgdObject obj, Dictionary<string, VgdObject> byId)
        {
            if (string.IsNullOrEmpty(obj.ParentId) || obj.IsParentedToCamera) return null;
            if (ABObjectImporter.InheritsNothing(obj)) return null;
            return byId.TryGetValue(obj.ParentId, out var parent) && parent != obj ? parent : null;
        }

        private static ABLayoutPacker.Band ToPackBand(ABRenderLayer band) => band switch
        {
            ABRenderLayer.AbovePlayer => ABLayoutPacker.Band.AbovePlayer,
            ABRenderLayer.Background => ABLayoutPacker.Band.Background,
            _ => ABLayoutPacker.Band.Default,
        };

        #endregion

        #region Length

        // A LEVEL IS AS LONG AS ITS SONG, and measuring it off its content is the fallback rather
        // than the rule. Afterbeat has no length field because it does not need one: its timeline
        // IS the audio clip, an object timed past the end of the song never plays, and its editor
        // cannot scroll past it. A converted level whose length came from its content therefore
        // ends wherever its last object happened to - not where the song does, and not where its
        // author was working - which is the mismatch this resolves.
        //
        // Three sources, in falling order of trust:
        //
        //   the HOST's own measurement (ABOptions.AudioLengthSeconds), which is the clip itself and
        //   is what the source game would have used;
        //
        //   the metadata's song.time, which IS the clip length when it was written with the song
        //   loaded - and is the literal 60 when it was not (DataManager.SaveMetadata's own fallback
        //   initialises it to 60f). A real clip is essentially never exactly 60.0000 seconds long,
        //   so that one value is read as "unset" rather than as a minute. A level really a minute
        //   long loses nothing by it: the measured length below still covers its content;
        //
        //   and failing both, the content, with a tail so the level does not end on the exact frame
        //   its final object dies.
        //
        // Content reaching past the song is legal and is NOT extended over: a root object's span is
        // not bounded by the level's length in this format either, so it survives in the file and
        // plays exactly as much as it did over there, which is none of it. It is reported, because
        // an author looking at a timeline that stops before their last object deserves to know why.
        private static int ResolveDuration(VgdLevel source, VgmMeta meta, ABOptions options,
            InteropReport report)
        {
            var measured = MeasureDuration(source, options);
            var songSeconds = ResolveSongLength(meta, options);
            if (songSeconds <= 0f) return measured;

            var songFrames = Math.Clamp(ABTimeMap.ToFrameCount(songSeconds, options.Framerate),
                FrameRules.MinFrameDuration, FrameRules.MaxFrameDuration);

            if (measured > songFrames)
                report.Info("content_past_the_song",
                    "This level holds content timed past the end of its song. The timeline is the song's length, exactly as it is in Afterbeat, so that content is kept but never plays.",
                    "level");

            return songFrames;
        }

        /// <summary> What <see cref="VgmMeta"/> writes into song.time when it is saved with no song
        /// loaded, and therefore the one value of it that means nothing. </summary>
        public const float UnsetSongTime = 60f;

        private static float ResolveSongLength(VgmMeta meta, ABOptions options)
        {
            if (options.AudioLengthSeconds > 0f) return options.AudioLengthSeconds;

            var declared = meta?.Song?.Time ?? 0f;
            if (declared <= 0f) return 0f;
            return Math.Abs(declared - UnsetSongTime) < 1e-4f ? 0f : declared;
        }

        // The fallback: measured off everything that carries a time - object lifetimes, checkpoints,
        // markers, prefab placements, and every level-global event keyframe. Missing any of those
        // would produce a timeline that ends before content the file still contains.
        private static int MeasureDuration(VgdLevel source, ABOptions options)
        {
            var seconds = 0f;

            // An object with no autokill rule is skipped, not measured: it never dies, so it
            // reaches 5000 seconds and would make a three-minute level five thousand seconds long
            // all by itself. What it says about the level's length is nothing.
            if (source.Objects != null)
                foreach (var obj in source.Objects)
                {
                    if (obj == null) continue;
                    if ((ABAutokillType)obj.AutokillType == ABAutokillType.OldStyleNoAutokill) continue;

                    var end = ABTimeMap.ResolveEndTime(obj);
                    if (end > seconds) seconds = end;
                }

            if (source.Checkpoints != null)
                foreach (var checkpoint in source.Checkpoints)
                    if (checkpoint != null && checkpoint.Time > seconds)
                        seconds = checkpoint.Time;

            if (source.Markers != null)
                foreach (var marker in source.Markers)
                    if (marker != null && marker.Time > seconds)
                        seconds = marker.Time;

            if (source.Events != null)
                foreach (var track in source.Events)
                {
                    if (track == null) continue;
                    foreach (var key in track)
                        if (key != null && key.Time > seconds)
                            seconds = key.Time;
                }

            // A placement plus the length of what it places. Measured off the TEMPLATE's own
            // objects rather than off the placement, which carries no length of its own - a level
            // whose last content is a prefab would otherwise end before the prefab does.
            if (source.PrefabPlacements != null)
            {
                var templates = BuildTemplateIndex(source);
                foreach (var placement in source.PrefabPlacements)
                {
                    if (placement == null) continue;

                    var end = placement.StartTime;
                    if (templates != null && placement.PrefabId != null
                                          && templates.TryGetValue(placement.PrefabId, out var template))
                        end += MeasureTemplateLength(template,
                            placement.StartTime - (template?.Offset ?? 0f));

                    if (end > seconds) seconds = end;
                }
            }

            var frames = ABTimeMap.ToFrameCount(seconds, options.Framerate) + TailFrames;
            return Math.Clamp(frames, FrameRules.MinFrameDuration, FrameRules.MaxFrameDuration);
        }

        /// <summary> The source templates by their own string id, for measuring what a placement
        /// reaches. Null when the document defines none. </summary>
        private static Dictionary<string, VgpPrefab> BuildTemplateIndex(VgdLevel source)
        {
            if (source.Prefabs == null || source.Prefabs.Count == 0) return null;

            var templates = new Dictionary<string, VgpPrefab>(source.Prefabs.Count);
            foreach (var prefab in source.Prefabs)
                if (prefab != null && !string.IsNullOrEmpty(prefab.Id))
                    templates[prefab.Id] = prefab;
            return templates;
        }

        /// <summary> How far past a placement's own start the template it places reaches, in
        /// seconds. <paramref name="absoluteBase"/> is where that placement puts it, which one
        /// autokill rule out of five needs - see <see cref="ABTimeMap.ResolveEndTime"/>. </summary>
        private static float MeasureTemplateLength(VgpPrefab template, float absoluteBase)
        {
            if (template?.Objects == null) return 0f;

            var end = 0f;
            foreach (var obj in template.Objects)
            {
                if (obj == null) continue;
                if ((ABAutokillType)obj.AutokillType == ABAutokillType.OldStyleNoAutokill) continue;

                var reach = ABTimeMap.ResolveEndTime(obj, null, null, absoluteBase);
                if (reach > end) end = reach;
            }

            return end;
        }

        #endregion
    }
}