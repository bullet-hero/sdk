using System.Collections.Generic;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;

namespace BH.SDK.Interop.AfterBeat.Export
{
    // A PLACEMENT CROSSES AS A PLACEMENT WHEREVER AFTERBEAT CAN SAY THE SAME THING, and is flattened
    // into its copies only where it cannot. Flattening everything was the old answer and it cost the
    // exported level its whole structure: a prefab-heavy level arrived as thousands of loose objects
    // hanging off empties, with prefabs[] a library nothing referenced.
    //
    // What an Afterbeat placement CAN say is narrow, and the rules below are exactly its shape
    // (VgdPrefabPlacement): one static position, scale and rotation; a start time and nothing that
    // trims the template; a parent that is an object, the camera or nothing; and no per-placement
    // divergence at all, since the source game copies the template verbatim. Anything else - an
    // animated placement, an override on some inner object, a trimmed or cut one - is content only
    // its copies carry, so it takes the flatten path, and each rule reports under its own code so
    // the author knows which one to change.
    //
    // THE LEAD TIME IS NOT RECONSTRUCTED. The import folds a template's lead time into the
    // placement's start (the source spawns at `t - prefab.Offset + st`, so only the difference ever
    // mattered), and writing Offset 0 with the placement's own start plays at exactly the same
    // moment. Nothing in the level remembers the original split, so nothing is invented here.
    //
    // Decided BEFORE the objects are written: an expressed placement's copies must not be exported
    // as objects too, or Afterbeat draws the template twice - once from the placement it expands on
    // load and once from the copies.

    /// <summary> A level's prefab placements as Afterbeat placements, or flattened where they
    /// cannot be one. </summary>
    public static class ABPlacementExporter
    {
        /// <summary> Why a placement could not be written as one. </summary>
        public enum Reason
        {
            /// <summary> Expressible. </summary>
            None = 0,
            /// <summary> More than one position, scale or rotation keyframe. </summary>
            Animated,
            /// <summary> A random value on its transform. </summary>
            Random,
            /// <summary> Sizes, anchors or a pivot of its own - a placement over there has none. </summary>
            Rect,
            /// <summary> Overrides beyond the root transform the importer itself writes. </summary>
            Modified,
            /// <summary> Parented to something that is not exported. </summary>
            Parent,
            /// <summary> Shorter than its template, or started from inside it. </summary>
            Trimmed,
            /// <summary> Ordinary objects hang off the placement itself. </summary>
            Children,
            /// <summary> Its template is not in the level. </summary>
            Template,
            /// <summary> Switched off - it is not exported at all. </summary>
            Inactive,
        }

        /// <summary> What the export decided for one level: which placements cross as placements,
        /// and which objects therefore stay out of objects[]. </summary>
        public sealed class Plan
        {
            /// <summary> The placements written as prefab_objects. </summary>
            public List<PrefabObject> Expressed { get; } = new();

            /// <summary> The placement objects and every copy they own - none of them is written as
            /// an object. </summary>
            public HashSet<ObjectId> Skipped { get; } = new();

            /// <summary> Flattened placements, by reason. </summary>
            public Dictionary<Reason, int> Flattened { get; } = new();
        }

        /// <summary> Decides every placement of the level's own scope. </summary>
        public static Plan Decide(Level level, ABExportContext context)
        {
            var plan = new Plan();
            var objects = level.Game.Objects;

            var parentsOf = new HashSet<ObjectId>();
            var copies = new HashSet<ObjectId>();
            foreach (var pair in objects)
            {
                if (pair.Value == null) continue;
                parentsOf.Add(pair.Value.ParentObjectId);
                if (pair.Value is PrefabObject owner && owner.ObjectIds != null)
                    foreach (var copy in owner.ObjectIds.Values)
                        copies.Add(copy);
            }

            foreach (var pair in objects)
            {
                if (pair.Value is not PrefabObject placement) continue;

                // A copy of a NESTED placement is the outer placement's content, not a placement of
                // the level - it is expressed or flattened with its owner.
                if (copies.Contains(placement.ObjectId)) continue;

                var reason = Classify(placement, level, context, objects, copies, parentsOf);
                if (reason == Reason.None)
                {
                    plan.Expressed.Add(placement);
                    plan.Skipped.Add(placement.ObjectId);
                    foreach (var copy in placement.ObjectIds.Values) plan.Skipped.Add(copy);
                    continue;
                }

                plan.Flattened[reason] = plan.Flattened.GetValueOrDefault(reason) + 1;
            }

            return plan;
        }

        private static Reason Classify(PrefabObject placement, Level level, ABExportContext context,
            Dictionary<ObjectId, RectObject> objects, HashSet<ObjectId> copies, HashSet<ObjectId> parentsOf)
        {
            if (!placement.Active) return Reason.Inactive;

            if (!placement.PrefabId.IsEnabled()
                || !level.Resources.Prefabs.TryGetValue(placement.PrefabId, out var template)
                || template?.Root == null)
                return Reason.Template;

            if (placement.Positions.Count > 1 || placement.Scales.Count > 1 || placement.Rotations.Count > 1)
                return Reason.Animated;

            foreach (var key in placement.Positions)
                if (key.Pos is not Vector2Value) return Reason.Random;
            foreach (var key in placement.Scales)
                if (key.Scale is not Vector2Value) return Reason.Random;
            foreach (var key in placement.Rotations)
                if (key.Angle is not FloatValue) return Reason.Random;

            if (placement.Sizes.Count > 0 || placement.AnchorsMin.Count > 0
                                          || placement.AnchorsMax.Count > 0 || placement.Pivots.Count > 0)
                return Reason.Rect;

            foreach (var modification in placement.Modifications.Keys)
                if (modification.ObjectId != ObjectId.PrefabRoot || !IsImportedRootField(modification.Field))
                    return Reason.Modified;

            if (placement.PlacementOffset != 0
                || placement.Span.FrameDuration != template.Root.Span.FrameDuration)
                return Reason.Trimmed;

            // Ordinary objects hanging off the placement itself: the placement would not be in
            // objects[] to be their parent.
            if (parentsOf.Contains(placement.ObjectId))
                foreach (var pair in objects)
                    if (pair.Value != null && pair.Value.ParentObjectId == placement.ObjectId
                                           && !placement.ObjectIds.ContainsValue(pair.Key))
                        return Reason.Children;

            var parent = placement.ParentObjectId;
            if (parent != ObjectId.Null && parent != ObjectId.Camera && parent != context.CameraScaleRootId)
            {
                if (!objects.TryGetValue(parent, out var parentObject) || parentObject == null
                                                                       || !parentObject.Active
                                                                       || parentObject is PrefabObject
                                                                       || copies.Contains(parent))
                    return Reason.Parent;
            }

            return Reason.None;
        }

        // Exactly what ABPrefabImporter.RecordRootOverrides writes: the placement's own name and
        // three transform tracks, restated as overrides of the template's root.
        private static bool IsImportedRootField(int field)
            => field is ModificationFields.Name or ModificationFields.Positions
                or ModificationFields.Rotations or ModificationFields.Scales;

        /// <summary> One expressed placement as the source's own. </summary>
        public static VgdPrefabPlacement Export(PrefabObject placement, ABExportContext context, string path)
        {
            var report = context.Report;
            var target = new VgdPrefabPlacement
            {
                Id = ABExportContext.ToSourceId(placement.ObjectId),
                PrefabId = ToPrefabSourceId(placement.PrefabId),
                StartTime = ABTimeMap.ToSeconds(placement.Span.StartFrame, context.Options.Framerate),
                ParentId = context.ToParentId(placement.ParentObjectId),
            };

            var (x, y) = placement.Positions.Count > 0
                ? ABValueMap.ExportVector(placement.Positions[0].Pos, report, path)
                : (0f, 0f);
            var (width, height) = placement.Scales.Count > 0
                ? ABValueMap.ExportVector(placement.Scales[0].Scale, report, path)
                : (1f, 1f);
            var degrees = placement.Rotations.Count > 0
                ? ABValueMap.ExportFloat(placement.Rotations[0].Angle, report, path) * ABValueMap.RadiansToDegrees
                : 0f;

            target.Tracks[VgdPrefabPlacement.TrackIndex.Position].Values = new List<float> { x, y };
            target.Tracks[VgdPrefabPlacement.TrackIndex.Scale].Values = new List<float> { width, height };
            target.Tracks[VgdPrefabPlacement.TrackIndex.Rotation].Values = new List<float> { degrees };
            return target;
        }

        /// <summary> The string a template is named by in prefabs[] and prefab_objects[] alike. </summary>
        public static string ToPrefabSourceId(PrefabId id) => id.value.ToString("N");

        /// <summary> The report code one reason is filed under. </summary>
        public static string ToCode(Reason reason) => reason switch
        {
            Reason.Animated => "placement_flattened:animated",
            Reason.Random => "placement_flattened:random",
            Reason.Rect => "placement_flattened:rect",
            Reason.Modified => "placement_flattened:modified",
            Reason.Parent => "placement_flattened:parent",
            Reason.Trimmed => "placement_flattened:trimmed",
            Reason.Children => "placement_flattened:children",
            Reason.Template => "placement_flattened:template",
            Reason.Inactive => "placement_flattened:inactive",
            _ => "placement_flattened",
        };

        /// <summary> What to tell the author about one reason. </summary>
        public static string ToMessage(Reason reason, int count) => reason switch
        {
            Reason.Animated => $"{count} prefab placements move, scale or rotate over time; an Afterbeat placement is static, so they were exported as their objects.",
            Reason.Random => $"{count} prefab placements have a random position, scale or rotation; an Afterbeat placement holds plain numbers, so they were exported as their objects.",
            Reason.Rect => $"{count} prefab placements carry a size, anchors or a pivot of their own; an Afterbeat placement has none, so they were exported as their objects.",
            Reason.Modified => $"{count} prefab placements override something inside their template; Afterbeat copies a template verbatim, so they were exported as their objects.",
            Reason.Parent => $"{count} prefab placements hang off something that is not exported as an object; they were exported as their objects.",
            Reason.Trimmed => $"{count} prefab placements are trimmed or cut; an Afterbeat placement always plays its whole template, so they were exported as their objects.",
            Reason.Children => $"{count} prefab placements have objects of their own hanging off them; an Afterbeat placement cannot be a parent, so they were exported as their objects.",
            Reason.Template => $"{count} prefab placements name a template this level does not hold; they were exported as whatever they own.",
            Reason.Inactive => $"{count} prefab placements are switched off and were not exported.",
            _ => $"{count} prefab placements were exported as their objects.",
        };
    }
}
