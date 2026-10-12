using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Modifiers
{
    /// <summary> Which side of a parent/child pair gives way when the child reaches outside. </summary>
    public enum SpanFitMode : byte
    {
        /// <summary> Cut every child down to what its parent covers. </summary>
        ClampChildren = 0,

        /// <summary> Stretch every parent until it covers its children, up the whole chain. </summary>
        ExpandParents = 1,
    }

    /// <summary> What happens to a child that shares no frame at all with its parent. </summary>
    public enum SpanFitOutside : byte
    {
        /// <summary> Remove it and everything hanging off it - it plays nowhere as it stands. </summary>
        Delete = 0,

        /// <summary> Cut it into the nearest edge of its parent, which leaves one frame of it. </summary>
        Clamp = 1,

        /// <summary> Leave it exactly as authored. </summary>
        Skip = 2,
    }

    // A child outside its parent is legal authored data resolved on READ, so fitting is a generator the
    // author asks for. The window is an INSTRUCTION, as in mod_content_remover (WindowSelection). The
    // walk direction matters: clamp PARENT-FIRST, expand CHILD-FIRST. Docs/GENERATORS.md.

    /// <summary>
    /// Makes every child's lifetime fit its parent's: by cutting the children down, or by stretching
    /// the parents out.
    /// </summary>
    public class SpanFitGenerator : BaseModifier<SpanFitGenerator.Parameters>
    {
        /// <summary> <c>"mod_span_fit"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "mod_span_fit";

        // Whole-scope, not the selection (as mod_content_remover), and not LevelScope: a Prefab template
        // is a hierarchy like any other.

        /// <summary> Nothing beyond a scope to run against. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.None;

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.Mode), nameof(Parameters.Invert))
            .Section(GeneratorSections.Additional, nameof(Parameters.Outside))
            .VisibleWhen(nameof(Parameters.Outside),
                parameters => ((Parameters)parameters).Mode == SpanFitMode.ClampChildren)
            .Describe(nameof(Parameters.Mode),
                "ClampChildren cuts each child's lifetime down to its parent's; ExpandParents stretches " +
                "each parent to cover its children, up the chain.")
            .Describe(nameof(Parameters.Invert),
                "Off (default): fit objects whose lifetime lies wholly inside the run's window. On: " +
                "those sharing no frame with it. Partial overlaps are left alone.")
            .Describe(nameof(Parameters.Outside),
                "ClampChildren only: a child sharing no frame with its parent is deleted with its " +
                "subtree (Delete), cut to one edge frame (Clamp) or left (Skip).")
            .Build();

        /// <summary> Applies this run's edit. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            if (parameters.Mode == SpanFitMode.ExpandParents) ExpandParents(context, parameters);
            else ClampChildren(context, parameters);
        }

        // Edits and deletes only. GeneratorCost describes what a run ADDS, and reporting anything
        // here would read as "this will add N", which is the opposite of what happens.

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => GeneratorCost.Zero;

        // The configurations reaching past what the author sees (Invert, whole timeline, Delete) are
        // legitimate headline uses, so they get a confirmation, not a refusal.

        /// <summary> True where these parameters would destroy or rewrite content the author did not point at. </summary>
        protected override bool IsDangerousTyped(GeneratorContext context, Parameters parameters)
            => parameters.Invert
               || CoversWholeTimeline(context)
               || (parameters.Mode == SpanFitMode.ClampChildren && parameters.Outside == SpanFitOutside.Delete);

        #region Modes

        private static void ClampChildren(GeneratorContext context, Parameters parameters)
        {
            foreach (var id in OrderedByDepth(context, parameters, false))
            {
                // Gone already: an earlier, shallower object took this whole subtree with it.
                if (!context.Objects.TryGetValue(id, out var child)) continue;
                if (!context.Objects.TryGetValue(child.ParentObjectId, out var parent)) continue;

                // Read off the live parent, which a shallower pass may already have cut down - that
                // is what makes the fit hold all the way down a chain rather than one level deep.
                var bounds = parent.Span;
                if (bounds.Contains(child.Span)) continue;

                if (bounds.Overlaps(child.Span))
                {
                    Clamp(context, id, bounds);
                    continue;
                }

                switch (parameters.Outside)
                {
                    case SpanFitOutside.Delete:
                        DeleteOrClamp(context, id, bounds);
                        break;
                    case SpanFitOutside.Clamp:
                        Clamp(context, id, bounds);
                        break;
                }
            }
        }

        private static void ExpandParents(GeneratorContext context, Parameters parameters)
        {
            // The ceiling is an EXCLUSIVE end, so it is the boundary past the timeline's last frame
            // rather than its length: a parent may legally reach the level's final frame.
            var limit = FrameRules.EndBoundaryOf(TimelineEnd(context));

            foreach (var id in OrderedByDepth(context, parameters, true))
            {
                if (!context.Objects.TryGetValue(id, out var child)) continue;

                var parentId = child.ParentObjectId;
                if (!context.Objects.TryGetValue(parentId, out var parent)) continue;
                if (parent.Span.Contains(child.Span)) continue;

                var start = Min(parent.Span.StartFrame, child.Span.StartFrame);
                var end = Max(parent.Span.EndFrame, child.Span.EndFrame);

                // A parent is content like anything else and stays inside the timeline it lives on,
                // even though a child may have been authored past it. What does not fit stays
                // overhanging - unplayed, and still there if the timeline is lengthened later.
                if (start < FrameRules.MinFrame) start = FrameRules.MinFrame;
                if (end > limit) end = limit;
                if (end - start > FrameRules.MaxFrameDuration) end = start + FrameRules.MaxFrameDuration;
                if (end - start < FrameRules.MinFrameDuration) continue;

                // Anchors are the author's: an anchor says "follow the parent's edge", and inventing
                // one here would turn a one-off fit into a permanent binding nobody asked for.
                var expanded = FrameSpan.FromBounds(start, end, parent.Span.Anchors);
                if (expanded.Equals(parent.Span)) continue;

                context.Edit(parentId).Span = expanded;
            }
        }

        #endregion

        #region Targets

        // Only children whose parent resolves (a root is bounded by nothing; a dangling ParentObjectId is
        // GraphRule.MissingParent's). Sorted by depth, id as tie-break: List.Sort is unstable.
        private static List<ObjectId> OrderedByDepth(GeneratorContext context, Parameters parameters,
            bool deepestFirst)
        {
            var targets = new List<ObjectId>();
            foreach (var pair in context.Objects)
            {
                var obj = pair.Value;
                if (obj == null) continue;
                if (!obj.ParentObjectId.IsValid()) continue;
                if (!context.Objects.ContainsKey(obj.ParentObjectId)) continue;
                if (!WindowSelection.Selects(obj.Span, context.Span, parameters.Invert)) continue;

                targets.Add(pair.Key);
            }

            var depths = new Dictionary<ObjectId, int>(targets.Count);
            foreach (var id in targets) depths[id] = Depth(context, id);

            targets.Sort((a, b) =>
            {
                var compared = deepestFirst
                    ? depths[b].CompareTo(depths[a])
                    : depths[a].CompareTo(depths[b]);
                return compared != 0 ? compared : a.value.CompareTo(b.value);
            });
            return targets;
        }

        /// <summary> How many resolvable ancestors an object has. Bounded rather than trusted: this
        /// walks author data, and a cyclic chain must not hang the run. </summary>
        private static int Depth(GeneratorContext context, ObjectId id)
        {
            var depth = 0;
            for (var guard = 0; guard < LevelRules.MaxObjectDepth; guard++)
            {
                if (!context.Objects.TryGetValue(id, out var obj)) break;

                var parentId = obj.ParentObjectId;
                if (!parentId.IsValid() || !context.Objects.ContainsKey(parentId)) break;

                depth++;
                id = parentId;
            }

            return depth;
        }

        #endregion

        #region Edits

        private static void Clamp(GeneratorContext context, ObjectId id, in FrameSpan bounds)
        {
            var obj = context.Edit(id);
            obj.Span = obj.Span.ClampedInto(bounds);
        }

        // A materialized prefab child is pointed at by its placement's ObjectIds table, so deleting it
        // while the placement survives breaks the remap table: it is clamped into its parent instead.
        private static void DeleteOrClamp(GeneratorContext context, ObjectId id, in FrameSpan bounds)
        {
            if (IsMaterializedChild(context, id))
            {
                Clamp(context, id, bounds);
                return;
            }

            DeleteSubtree(context, id);
        }

        /// <summary> Removes an object together with everything that hangs off it - ordinary children
        /// and, for a placement, its own materialized ones. A child of something that plays nowhere
        /// plays nowhere either, and leaving it behind would only stand it back up against the scope
        /// root. </summary>
        private static void DeleteSubtree(GeneratorContext context, ObjectId root)
        {
            var doomed = new HashSet<ObjectId> { root };
            var pending = new Stack<ObjectId>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var id = pending.Pop();

                foreach (var pair in context.Objects)
                {
                    if (pair.Value == null || pair.Value.ParentObjectId != id) continue;
                    if (doomed.Add(pair.Key)) pending.Push(pair.Key);
                }

                if (!context.Objects.TryGetValue(id, out var obj)) continue;
                if (obj is not PrefabObject placement || placement.ObjectIds == null) continue;

                foreach (var child in placement.ObjectIds.Values)
                    if (context.Objects.ContainsKey(child) && doomed.Add(child))
                        pending.Push(child);
            }

            // Collected first: Delete writes through the very dictionary being enumerated.
            var ordered = new List<ObjectId>(doomed);
            ordered.Sort(static (a, b) => a.value.CompareTo(b.value));
            foreach (var id in ordered) context.Delete(id);
        }

        private static bool IsMaterializedChild(GeneratorContext context, ObjectId id)
        {
            foreach (var pair in context.Objects)
            {
                if (pair.Value is not PrefabObject placement || placement.ObjectIds == null) continue;

                foreach (var outer in placement.ObjectIds.Values)
                    if (outer == id)
                        return true;
            }

            return false;
        }

        #endregion

        #region Timeline

        // The active timeline, not always the level's: inside Prefab Mode the bound is the
        // template's own length, which is its root's span (Prefab.Root) - the same rule the editor's
        // own window clamp and mod_content_remover use.
        private static int TimelineEnd(GeneratorContext context)
        {
            if (context.Scope is Prefab prefab)
                return prefab.Root?.Span.FrameDuration ?? FrameRules.MinFrameDuration;
            return context.Settings?.FrameDuration ?? FrameRules.MinFrameDuration;
        }

        private static bool CoversWholeTimeline(GeneratorContext context)
        {
            if (context == null) return false;
            return context.Span.StartFrame <= FrameRules.MinFrame
                   && context.Span.EndFrame >= FrameRules.EndBoundaryOf(TimelineEnd(context));
        }

        #endregion

        private static int Min(int a, int b) => a < b ? a : b;
        private static int Max(int a, int b) => a > b ? a : b;

        /// <summary> Which side of a parent/child pair gives way, and over which part of the level. Public mutable
        /// fields, like every parameters class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters
        {
            /// <summary> Cut the children down (the default, and the one that never grows anything),
            /// or stretch the parents out. </summary>
            public SpanFitMode Mode = SpanFitMode.ClampChildren;

            /// <summary> On: fit what falls outside the frame range. Off (the default): fit what falls
            /// inside it. Content only partly overlapping the range is left alone either way. </summary>
            public bool Invert;

            /// <summary> What to do with a child that shares no frame at all with its parent, and so
            /// plays nowhere as it stands. Clamping mode only. </summary>
            public SpanFitOutside Outside = SpanFitOutside.Delete;
        }
    }
}