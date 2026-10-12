using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Modifiers
{
    // The one generator that only DELETES, and the window is an INSTRUCTION here, not a boundary.
    // Partial overlap survives BOTH modes (WindowSelection.Selects is asymmetric on purpose). Deletion is
    // never per-object: survivors keep their ancestor chain and a placement's children. Docs/GENERATORS.md.

    /// <summary>
    /// Deletes level content by frame range: everything outside the run's window, or everything
    /// inside it. Objects always, audio tracks and level-global event keyframes on request.
    /// </summary>
    public class ContentRemoverGenerator : BaseModifier<ContentRemoverGenerator.Parameters>
    {
        /// <summary> <c>"mod_content_remover"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "mod_content_remover";

        // Whole-scope, not the selection (a selection-limited cleanup leaves the rest while reporting
        // success). Not LevelScope: the window comes from the context, so it works in a Prefab too.

        /// <summary> Nothing beyond a scope to run against. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.None;

        // One section for four switches: a host renders no header while there is a single section
        // (HintsSections_CoverEveryParameterField checks every field is listed through Section).

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.Invert), nameof(Parameters.Objects),
                nameof(Parameters.Audio), nameof(Parameters.EventFrames))
            .Describe(nameof(Parameters.Invert),
                "Off (default): delete what lies wholly inside the run's frame window. On: delete what " +
                "shares no frame with it. Partly overlapping content survives.")
            .Describe(nameof(Parameters.Objects),
                "Delete objects. An object that stays keeps its parents, even ones the window would " +
                "remove.")
            .Describe(nameof(Parameters.Audio),
                "Also delete audio tracks. Level scope only.")
            .Describe(nameof(Parameters.EventFrames),
                "Also delete level-wide event keyframes: camera, post-processing, player, markers, " +
                "checkpoints, themes, backgrounds. Level scope only.")
            .Build();

        /// <summary> Applies this run's edit. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var window = context.Span;

            if (parameters.Objects) RemoveObjects(context, window, parameters.Invert);
            if (parameters.Audio) RemoveAudioTracks(context, window, parameters.Invert);
            if (parameters.EventFrames) RemoveEventKeys(context, window, parameters.Invert);
        }

        // Deletes only. GeneratorCost describes what a run ADDS, and reporting anything here would
        // read as "this will add N", which is the opposite of what happens.

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => GeneratorCost.Zero;

        // The configurations whose reach exceeds the window the author sees (Invert; a whole-timeline
        // window) are legitimate headline uses, so they get a confirmation, not a refusal.

        /// <summary> True where these parameters would destroy or rewrite content the author did not point at. </summary>
        protected override bool IsDangerousTyped(GeneratorContext context, Parameters parameters)
            => parameters.Invert || CoversWholeTimeline(context);

        // The active timeline: in Prefab Mode the window is bounded by Prefab.Root's span, as the
        // editor's window clamp does. Matched on the concrete Prefab: a scope holds a root, not a timeline.
        private static bool CoversWholeTimeline(GeneratorContext context)
        {
            if (context == null) return false;

            var frameDuration = context.Scope is Prefab prefab
                ? prefab.Root?.Span.FrameDuration ?? 0
                : context.Settings?.FrameDuration ?? 0;

            return context.Span.StartFrame <= FrameRules.MinFrame
                   && context.Span.EndFrame >= FrameRules.EndBoundaryOf(frameDuration);
        }

        private static void RemoveObjects(GeneratorContext context, in FrameSpan window, bool invert)
        {
            var keep = new HashSet<ObjectId>();
            foreach (var pair in context.Objects)
                if (!Doomed(pair.Value.Span, window, invert))
                    KeepWithAncestors(context, keep, pair.Key);

            KeepMaterializedChildren(context, keep);

            // Collected first: Delete writes through the very dictionary being enumerated.
            var doomed = new List<ObjectId>();
            foreach (var id in context.Objects.Keys)
                if (!keep.Contains(id))
                    doomed.Add(id);

            foreach (var id in doomed) context.Delete(id);
        }

        private static void RemoveAudioTracks(GeneratorContext context, in FrameSpan window, bool invert)
        {
            // Null in Prefab Mode - a template has objects but no scheduled audio, so there is
            // nothing to remove rather than something to refuse.
            if (context.Audio?.Tracks == null) return;

            var doomed = new List<AudioId>();
            foreach (var pair in context.Audio.Tracks)
                if (Doomed(pair.Value.Span, window, invert))
                    doomed.Add(pair.Key);

            foreach (var id in doomed) context.RemoveResource(context.Audio.Tracks, id);
        }

        /// <summary> Every level-global track, all 25 of them. They carry no owning object, so they
        /// go through RemoveLevelKeys rather than riding along on an Edit. Null in Prefab Mode, same
        /// as audio. </summary>
        private static void RemoveEventKeys(GeneratorContext context, in FrameSpan window, bool invert)
        {
            if (context.Game == null) return;

            var events = context.Game.Events;
            Trim(context, events.Markers, window, invert);
            Trim(context, events.Checkpoints, window, invert);
            Trim(context, events.ScreenLimits, window, invert);
            Trim(context, events.Backgrounds, window, invert);
            Trim(context, events.Themes, window, invert);

            var camera = context.Game.CameraEvents;
            Trim(context, camera.Positions, window, invert);
            Trim(context, camera.Rotations, window, invert);
            Trim(context, camera.Zooms, window, invert);
            Trim(context, camera.Pivots, window, invert);
            Trim(context, camera.Shakes, window, invert);

            var post = context.Game.PostProcessingEvents;
            Trim(context, post.Blooms, window, invert);
            Trim(context, post.Chromatics, window, invert);
            Trim(context, post.Vignettes, window, invert);
            Trim(context, post.Lenses, window, invert);
            Trim(context, post.Grains, window, invert);
            Trim(context, post.MotionBlurs, window, invert);
            Trim(context, post.ColorCurves, window, invert);
            Trim(context, post.LiftGammaGains, window, invert);
            Trim(context, post.ShadowsMidtonesHighlights, window, invert);
            Trim(context, post.WhiteBalances, window, invert);
            Trim(context, post.AnalogGlitches, window, invert);
            Trim(context, post.DigitalGlitches, window, invert);

            var player = context.Game.PlayerEvents;
            Trim(context, player.Visibilities, window, invert);
            Trim(context, player.Controls, window, invert);
            Trim(context, player.Collisions, window, invert);
            Trim(context, player.Sizes, window, invert);
            Trim(context, player.Speeds, window, invert);
        }

        /// <summary> A keyframe is a point, so "overlaps the window" and "lies inside it" are the same
        /// question - the two modes are exact opposites here, with no partial case in between. </summary>
        private static void Trim<TKey>(GeneratorContext context, List<TKey> track, FrameSpan window, bool invert)
            where TKey : IFrame
        {
            if (track == null) return;
            context.RemoveLevelKeys(track, key => invert
                ? !window.Contains(key.Frame)
                : window.Contains(key.Frame));
        }

        /// <summary> Marks a surviving object and everything it hangs off, so a parent is never
        /// deleted out from under a child that stays. Depth-guarded: this walks author data, and a
        /// cyclic parent chain must not hang the run. </summary>
        private static void KeepWithAncestors(GeneratorContext context, HashSet<ObjectId> keep, ObjectId id)
        {
            for (var guard = 0; guard < LevelRules.MaxObjectDepth; guard++)
            {
                if (!context.Objects.TryGetValue(id, out var obj)) return;

                // Already marked means its own chain was walked on an earlier object's behalf.
                if (!keep.Add(id)) return;

                id = obj.ParentObjectId;
                if (!id.IsNotNull()) return;
            }
        }

        // A placement's ObjectIds table points at its materialized children: deleting one while the
        // placement survives is a broken remap table. A doomed placement is not kept, nor its children.

        /// <summary> Extends the keep set through every surviving PrefabObject's own materialized
        /// children, nested placements included. </summary>
        private static void KeepMaterializedChildren(GeneratorContext context, HashSet<ObjectId> keep)
        {
            var pending = new Stack<ObjectId>(keep);
            while (pending.Count > 0)
            {
                var id = pending.Pop();
                if (!context.Objects.TryGetValue(id, out var obj)) continue;
                if (obj is not PrefabObject placement || placement.ObjectIds == null) continue;

                foreach (var child in placement.ObjectIds.Values)
                    if (context.Objects.ContainsKey(child) && keep.Add(child))
                        pending.Push(child);
            }
        }

        /// <summary> Whether a lifetime is what this run removes: with Invert, one sharing no frame
        /// at all with the window; without it, one lying wholly inside. </summary>
        private static bool Doomed(in FrameSpan span, in FrameSpan window, bool invert)
            => WindowSelection.Selects(span, window, invert);

        /// <summary> Which window is kept or cleared, and which kinds of content the run touches. Public mutable
        /// fields, like every parameters class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters
        {
            /// <summary> On: remove what falls outside the frame range. Off (the default, and the
            /// narrower of the two): remove what falls inside it. Content only partly overlapping the
            /// range survives either way. </summary>
            public bool Invert;

            /// <summary> Remove objects. </summary>
            public bool Objects = true;

            /// <summary> Remove audio tracks. Level scope only - a Prefab template has no audio. </summary>
            public bool Audio;

            /// <summary> Remove level-global event keyframes (camera, post-processing, player,
            /// markers, checkpoints, ...). Level scope only, same as Audio. </summary>
            public bool EventFrames;
        }
    }
}