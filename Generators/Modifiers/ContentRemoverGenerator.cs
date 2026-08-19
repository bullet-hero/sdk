using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Modifiers
{
    // The one generator that only ever DELETES, and the only one for which the context window is an
    // instruction rather than a boundary. Everywhere else the context span says where new
    // content may be written; here it says which content is being talked about - which is what makes
    // "clean up everything the level can no longer play" (window = whole level, Invert on) and "wipe
    // this section" (window = the section, Invert off) the same operation with one flag between them.
    // A host that offers a whole-level switch on its window - the in-game editor does - gets the
    // first for free without this generator knowing anything about a level's own FrameDuration.
    //
    // Partial overlap survives BOTH modes on purpose, and the asymmetry in WindowSelection.Selects
    // is what implements it: Invert deletes what shares NO frame with the window, and its opposite
    // deletes only what lies WHOLLY inside it. Making both tests "overlaps" would mean an object
    // hanging over the edge is deleted whichever way the flag is set, which is not a mode - it is a
    // trap. mod_span_fit selects the objects it fits through the very same helper.
    //
    // Deleting is never per-object: an object being removed may still PARENT one that is staying,
    // and dropping it alone leaves a dangling ParentObjectId that LevelGraphAnalyzer then reports -
    // a cleanup pass that breaks the level it cleaned. Hence keep-marking (a surviving object marks
    // its whole ancestor chain) instead of a per-object test, plus the same for a surviving
    // PrefabObject's materialized children, which its own ObjectIds table points at.

    /// <summary>
    /// Deletes level content by frame range: everything outside the run's window, or everything
    /// inside it. Objects always, audio tracks and level-global event keyframes on request.
    /// </summary>
    public class ContentRemoverGenerator : BaseModifier<ContentRemoverGenerator.Parameters>
    {
        public override string NameKey => "mod_content_remover";

        // Whole-scope rather than the selection: "everything in/outside this range" is a statement
        // about the scope, and quietly limiting it to what happens to be selected would leave the
        // rest behind while reporting success. Not LevelScope either, even though Audio/EventFrames
        // need Game/Audio - the window comes from the context, so removing objects is just as
        // meaningful inside a Prefab template, and declaring LevelScope would make a host disable the
        // whole generator there for the sake of two switches that default to off.

        /// <summary> Nothing beyond a scope to run against. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.None;

        // One section for four switches: splitting them would put a header above each half of a form
        // short enough to read at a glance, and a host renders no header at all while there is only
        // one section (Section is still what every field is listed through - see
        // HintsSections_CoverEveryParameterField).
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.Invert), nameof(Parameters.Objects),
                nameof(Parameters.Audio), nameof(Parameters.EventFrames))
            .Build();

        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var window = context.Span;

            if (parameters.Objects) RemoveObjects(context, window, parameters.Invert);
            if (parameters.Audio) RemoveAudioTracks(context, window, parameters.Invert);
            if (parameters.EventFrames) RemoveEventKeys(context, window, parameters.Invert);
        }

        // Deletes only. GeneratorCost describes what a run ADDS, and reporting anything here would
        // read as "this will add N", which is the opposite of what happens.
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => GeneratorCost.Zero;

        // The two configurations whose reach is larger than the window the author is looking at:
        // Invert deletes everything the window does NOT cover (so the smaller the window, the more
        // it takes), and a window spanning the whole timeline covers all of it either way. Both are
        // legitimate - they are the generator's two headline use cases - which is exactly why they
        // get a confirmation instead of a refusal.
        protected override bool IsDangerousTyped(GeneratorContext context, Parameters parameters)
            => parameters.Invert || CoversWholeTimeline(context);

        // The active timeline, not always the level's: inside Prefab Mode the window is bounded by
        // the template's own FrameDuration (Prefab implements IFrameDuration, Level.Game doesn't), which
        // is the same rule the editor's own window clamp uses.
        private static bool CoversWholeTimeline(GeneratorContext context)
        {
            if (context == null) return false;

            var frameDuration = context.Scope is IFrameDuration scope
                ? scope.FrameDuration
                : context.Settings?.FrameDuration ?? 0;

            return context.Span.StartFrame <= FrameRules.MinFrame && context.Span.EndFrame >= frameDuration;
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
            Trim(context, post.ColorCurveses, window, invert);
            Trim(context, post.LiftGammaGains, window, invert);
            Trim(context, post.ShadowsMidtonesHighlightses, window, invert);
            Trim(context, post.WhiteBalances, window, invert);
            Trim(context, post.AnalogGlitches, window, invert);
            Trim(context, post.DigitalGlitches, window, invert);

            var player = context.Game.PlayerEvents;
            Trim(context, player.Visibles, window, invert);
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

        // A materialized child is an ordinary object in every other respect, but its placement's
        // ObjectIds table points straight at it - deleting one while the placement survives is a
        // broken remap table, which is a validation error rather than a tidier level. A placement
        // that is itself doomed is not kept in the first place, and then neither are its children,
        // so this only ever rescues the mixed case.

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
