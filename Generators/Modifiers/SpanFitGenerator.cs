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

    // A child reaching outside its parent is legal authored data, not damage: a consumer resolves the
    // containment on READ (the Unity project's FrameSpanFitMath), so the overhanging part simply never
    // plays and comes back the moment the parent grows again. That is why this is a generator the
    // author asks for rather than a validation rule that follows them around - it used to be
    // GraphRule.ChildSpanOutsideParent, whose repair is now this generator's ClampChildren mode.
    //
    // The window is an INSTRUCTION here, exactly as in mod_content_remover, and through the same
    // WindowSelection.Selects: it names which children are being talked about, not where content may
    // be written. So "fit the whole level" (window = whole timeline) and "fit this section" are one
    // operation, and a host offering a whole-level switch on its window gets the first for free.
    //
    // Direction of the walk is not a detail. Clamping runs PARENT-FIRST, because cutting a parent
    // moves the boundary its own children must fit into; expanding runs CHILD-FIRST, because a
    // stretched parent is what its own parent then has to cover. Reversing either leaves the far end
    // of a chain outside after a run that reported success.

    /// <summary>
    /// Makes every child's lifetime fit its parent's: by cutting the children down, or by stretching
    /// the parents out.
    /// </summary>
    public class SpanFitGenerator : BaseModifier<SpanFitGenerator.Parameters>
    {
        /// <summary> <c>"mod_span_fit"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "mod_span_fit";

        // Whole-scope rather than the selection, same reasoning as mod_content_remover: "make the
        // lifetimes agree" is a statement about the scope, and quietly limiting it to what happens to
        // be selected would leave the rest overhanging while reporting success. Not LevelScope
        // either - a Prefab template is a hierarchy like any other and wants this just as much.

        /// <summary> Nothing beyond a scope to run against. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.None;

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.Mode), nameof(Parameters.Invert))
            .Section(GeneratorSections.Additional, nameof(Parameters.Outside))
            .VisibleWhen(nameof(Parameters.Outside),
                parameters => ((Parameters)parameters).Mode == SpanFitMode.ClampChildren)
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

        // The three configurations that reach past what the author is looking at: Invert works on
        // everything the window does NOT cover, a window spanning the whole timeline covers all of it
        // either way, and Delete removes content rather than resizing it. All three are legitimate -
        // "clean up everything that can no longer play" is the headline use case - which is why they
        // get a confirmation instead of a refusal.

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
            var limit = TimelineEnd(context);

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

        // Only children with a parent that actually resolves. A root object is bounded by nothing -
        // running past the end of the level is legal and simply never plays - and a ParentObjectId
        // pointing at nothing is GraphRule.MissingParent's business, not a lifetime to fit.
        //
        // Sorted by depth so the walk can go strictly parent-first or strictly child-first, with the
        // id as the tie-breaker: List.Sort is unstable, and a run that shuffles between invocations
        // breaks "same inputs, same level".
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

        // A materialized prefab child is pointed at by its placement's own ObjectIds table, so
        // deleting one while the placement survives leaves a broken remap table - a cleanup pass that
        // breaks the level it cleaned, which is the same case mod_content_remover rescues from the
        // other side. It is cut into its parent instead: still a fit, just not a removal. A doomed
        // placement takes its children along anyway (below), so this only ever catches the mixed case.
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

        // The active timeline, not always the level's: inside Prefab Mode the bound is the template's
        // own FrameDuration (Prefab implements IFrameDuration, Level.Game doesn't), which is the same
        // rule the editor's own window clamp and mod_content_remover use.
        private static int TimelineEnd(GeneratorContext context)
        {
            if (context.Scope is IFrameDuration scope) return scope.FrameDuration;
            return context.Settings?.FrameDuration ?? FrameRules.MinFrameDuration;
        }

        private static bool CoversWholeTimeline(GeneratorContext context)
        {
            if (context == null) return false;
            return context.Span.StartFrame <= FrameRules.MinFrame
                   && context.Span.EndFrame >= TimelineEnd(context);
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
