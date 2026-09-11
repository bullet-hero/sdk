using System;
using System.Collections.Generic;
using BH.SDK.Generators.External;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Modifiers
{
    // Changing Framerate alone reinterprets every frame number in the level: at 30 fps a key on frame
    // 60 is two seconds in, at 60 fps it is one. So this remaps the numbers instead - frame * to / from
    // - and the level plays the same as before at a different sampling rate. FrameDuration is remapped
    // unconditionally, with no switch of its own, because it is the same statement as Framerate: a
    // timeline keeping its old length while its framerate doubles is a level that got twice as short,
    // which nobody asks for by changing a framerate.
    //
    // The three switches are about REACH, not about correctness: an author lowering the framerate to
    // fit a device usually wants the objects moved and is not ready to lose keys off audio automation
    // or camera events in the same click. Leaving one off means that part keeps its old frame numbers
    // and therefore its old timing shifts - which is a deliberate, visible outcome, not a silent one.
    //
    // Shrinking is lossy and there is no way around it: two frames that were distinct at 60 fps can
    // land on the same frame at 30, and a track's frames must stay unique ([RuleCollectionUnique]).
    // MaxKeyShift is the whole policy - how far a key may be nudged off its sampled frame to find a
    // free slot before it is dropped instead. Zero means "never nudge, drop on the first collision";
    // the default 1 keeps the common 2:1 downsample lossless for keys that were at least two frames
    // apart. Nudging is tried forward first because keys are packed in ascending order, so the slots
    // below the ideal one are the ones already taken.

    /// <summary>
    /// Retimes the whole level to a different framerate: frame numbers are resampled so the content
    /// keeps its wall-clock timing, and keys colliding on the way down are nudged or dropped.
    /// </summary>
    public class FramerateRemapGenerator : BaseModifier<FramerateRemapGenerator.Parameters>
    {
        /// <summary> <c>"mod_framerate_remap"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "mod_framerate_remap";

        /// <summary> Whole-scope, and level-only: the framerate belongs to the level, so running this
        /// against a Prefab template would rewrite a timeline the template does not own. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.LevelScope;

        // One section, so a host draws no header over a form this short. CurrentFramerate is listed
        // like any other field and marked read-only - it is there to be read next to the value being
        // typed, never typed into.

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.CurrentFramerate), nameof(Parameters.Framerate),
                nameof(Parameters.RemapObjects), nameof(Parameters.RemapAudio), nameof(Parameters.RemapEvents),
                nameof(Parameters.MaxKeyShift))
            .ReadOnly(nameof(Parameters.CurrentFramerate))
            .Range(nameof(Parameters.CurrentFramerate), FrameRules.MinFramerate, FrameRules.MaxFramerate)
            .Range(nameof(Parameters.Framerate), FrameRules.MinFramerate, FrameRules.MaxFramerate)
            .Range(nameof(Parameters.MaxKeyShift), 0, FrameRules.MaxFrame)
            .Unit(nameof(Parameters.MaxKeyShift), "frames")
            .Build();

        /// <summary> Applies this run's edit. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var settings = context.Settings;
            var from = settings.Fps;
            var to = Math.Clamp(parameters.Framerate, FrameRules.MinFramerate, FrameRules.MaxFramerate);
            if (from < FrameRules.MinFramerate || to == from) return;

            var frameDuration = Math.Clamp(Remap(settings.FrameDuration, from, to),
                FrameRules.MinFrameDuration, FrameRules.MaxFrameDuration);
            var last = FrameRules.LastFrameOf(frameDuration);
            var shift = Math.Clamp(parameters.MaxKeyShift, 0, last);

            // Both through the journal: a framerate left behind by an undo is a level that plays at
            // one rate with frame numbers written for another.
            context.SetValue(() => settings.Fps, value => settings.Fps = value, to);
            context.SetValue(() => settings.FrameDuration, value => settings.FrameDuration = value, frameDuration);

            if (parameters.RemapObjects) RemapObjects(context, from, to, shift, last);
            if (parameters.RemapAudio) RemapAudio(context, from, to, shift, last);
            if (parameters.RemapEvents) RemapEvents(context, from, to, shift, last);
        }

        // Adds nothing - it moves and drops. GeneratorCost describes additions, and reporting one here
        // would read as "this will add N".

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => GeneratorCost.Zero;

        /// <summary> Any real framerate change is dangerous: it rewrites every frame number in the
        /// level at once and can drop keys, neither of which the author pointed at object by object. </summary>
        protected override bool IsDangerousTyped(GeneratorContext context, Parameters parameters)
            => context?.Settings != null && parameters.Framerate != context.Settings.Fps;

        private static void RemapObjects(GeneratorContext context, int from, int to, int shift, int last)
        {
            // Snapshotted: Edit does not touch the dictionary, but a plan that iterates it while
            // handing out live objects is one refactor away from doing so.
            var ids = new List<ObjectId>(context.Objects.Keys);

            foreach (var id in ids)
            {
                var obj = context.Edit(id);

                obj.Span = RemapSpan(obj.Span, from, to, last);

                foreach (var track in ObjectTracks.Of(obj, ObjectTrackMask.All))
                    RemapObjectTrack(track, from, to, shift, last);
            }
        }

        private static void RemapAudio(GeneratorContext context, int from, int to, int shift, int last)
        {
            if (context.Audio?.Tracks == null) return;

            foreach (var track in context.Audio.Tracks.Values)
            {
                // OffsetTime is seconds INTO the clip, not a level frame - it survives a framerate
                // change untouched, and remapping it would move the playhead inside the audio.
                var span = RemapSpan(track.Span, from, to, last);
                context.SetValue(() => track.Span, value => track.Span = value, span);

                if (track.Effects == null) continue;
                RemapKeyList(context, track.Effects.Volumes, from, to, shift, last);
                RemapKeyList(context, track.Effects.StereoPans, from, to, shift, last);
            }
        }

        /// <summary> Every level-global track, all 25 of them. </summary>
        private static void RemapEvents(GeneratorContext context, int from, int to, int shift, int last)
        {
            if (context.Game == null) return;

            var events = context.Game.Events;
            RemapKeyList(context, events.Markers, from, to, shift, last);
            RemapKeyList(context, events.Checkpoints, from, to, shift, last);
            RemapKeyList(context, events.ScreenLimits, from, to, shift, last);
            RemapKeyList(context, events.Backgrounds, from, to, shift, last);
            RemapKeyList(context, events.Themes, from, to, shift, last);

            var camera = context.Game.CameraEvents;
            RemapKeyList(context, camera.Positions, from, to, shift, last);
            RemapKeyList(context, camera.Rotations, from, to, shift, last);
            RemapKeyList(context, camera.Zooms, from, to, shift, last);
            RemapKeyList(context, camera.Pivots, from, to, shift, last);
            RemapKeyList(context, camera.Shakes, from, to, shift, last);

            var post = context.Game.PostProcessingEvents;
            RemapKeyList(context, post.Blooms, from, to, shift, last);
            RemapKeyList(context, post.Chromatics, from, to, shift, last);
            RemapKeyList(context, post.Vignettes, from, to, shift, last);
            RemapKeyList(context, post.Lenses, from, to, shift, last);
            RemapKeyList(context, post.Grains, from, to, shift, last);
            RemapKeyList(context, post.MotionBlurs, from, to, shift, last);
            RemapKeyList(context, post.ColorCurves, from, to, shift, last);
            RemapKeyList(context, post.LiftGammaGains, from, to, shift, last);
            RemapKeyList(context, post.ShadowsMidtonesHighlights, from, to, shift, last);
            RemapKeyList(context, post.WhiteBalances, from, to, shift, last);
            RemapKeyList(context, post.AnalogGlitches, from, to, shift, last);
            RemapKeyList(context, post.DigitalGlitches, from, to, shift, last);

            var player = context.Game.PlayerEvents;
            RemapKeyList(context, player.Visibilities, from, to, shift, last);
            RemapKeyList(context, player.Controls, from, to, shift, last);
            RemapKeyList(context, player.Collisions, from, to, shift, last);
        }

        /// <summary> An object's own track: the whole object is already snapshotted by Edit, so this
        /// rewrites the live keys in place and drops the losers by index, walking DOWNWARD because
        /// removal shifts everything after it. </summary>
        private static void RemapObjectTrack(ObjectTracks.Track track, int from, int to, int shift, int last)
        {
            var count = track.Count;
            if (count == 0) return;

            var frames = new int[count];
            for (var i = 0; i < count; i++) frames[i] = track.FrameAt(i);

            var plan = Plan(frames, from, to, shift, last);

            for (var i = 0; i < count; i++)
                if (FrameRules.IsValidFrame(plan[i]))
                    track.SetFrameAt(i, plan[i]);

            for (var i = count - 1; i >= 0; i--)
                if (!FrameRules.IsValidFrame(plan[i]))
                    track.RemoveAt(i);
        }

        // A level-global (or audio) track has no owning object to snapshot, so it goes through the
        // context's own remove/add pair. The survivors are re-added as COPIES on purpose: the removal
        // entries in the journal hold the original instances, and moving a key that undo is holding
        // onto would restore it at its new frame instead of its old one.

        /// <summary> An ownerless keyframe list: rebuilt through the journal rather than mutated. </summary>
        private static void RemapKeyList<TKey>(GeneratorContext context, List<TKey> track,
            int from, int to, int shift, int last) where TKey : IFrame, ICopyable<TKey>
        {
            if (track == null || track.Count == 0) return;

            var count = track.Count;
            var frames = new int[count];
            for (var i = 0; i < count; i++) frames[i] = track[i].Frame;

            var plan = Plan(frames, from, to, shift, last);

            var survivors = new List<TKey>(count);
            for (var i = 0; i < count; i++)
            {
                if (!FrameRules.IsValidFrame(plan[i])) continue;
                var copy = track[i].Copy();
                copy.Frame = plan[i];
                survivors.Add(copy);
            }

            context.RemoveLevelKeys(track, _ => true);
            foreach (var key in survivors) context.AddLevelKey(track, key);
        }

        // Packing, in one place for both track shapes. Ascending by original frame so the result is
        // independent of the order a track happens to store its keys in, and so "nudge forward" only
        // ever competes with keys already placed.

        /// <summary> Where each key ends up, indexed like the input. <see cref="FrameRules.NoFrame"/>
        /// means it could not be placed within MaxKeyShift of its sampled frame and is dropped. </summary>
        private static int[] Plan(int[] frames, int from, int to, int shift, int last)
        {
            var count = frames.Length;
            var order = new int[count];
            var keys = new int[count];
            for (var i = 0; i < count; i++)
            {
                order[i] = i;
                keys[i] = frames[i];
            }

            Array.Sort(keys, order);

            var taken = new HashSet<int>();
            var plan = new int[count];

            foreach (var index in order)
            {
                var ideal = ClampFrame(RemapFrame(frames[index], from, to), last);
                var slot = FindSlot(ideal, taken, shift, last);
                plan[index] = slot;
                if (FrameRules.IsValidFrame(slot)) taken.Add(slot);
            }

            return plan;
        }

        /// <summary> The nearest free frame within shift of ideal, forward before backward, or
        /// <see cref="FrameRules.NoFrame"/> when there is none. </summary>
        private static int FindSlot(int ideal, HashSet<int> taken, int shift, int last)
        {
            for (var distance = 0; distance <= shift; distance++)
            {
                var forward = ideal + distance;
                if (forward <= last && !taken.Contains(forward)) return forward;

                if (distance == 0) continue;

                var backward = ideal - distance;
                if (backward >= FrameRules.MinFrame && !taken.Contains(backward)) return backward;
            }

            return FrameRules.NoFrame;
        }

        /// <summary> A COUNT of frames, sampled at the new rate. Rounded rather than truncated: a
        /// truncating remap pulls every key a little earlier, which accumulates into audible drift
        /// against a track that was not remapped. </summary>
        private static int Remap(int frameCount, int from, int to)
        {
            if (from == to) return frameCount;
            var value = (double)frameCount * to / from;
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        // A FRAME IS NOT A COUNT HERE EITHER, and this is the one place where confusing them is not
        // an off-by-one but a stretch: scaling a frame number directly would multiply the origin too,
        // sending the level's first frame to to/from instead of leaving it where it is. What scales
        // is the DISTANCE from the first frame, which the origin then goes back onto. An exclusive
        // end boundary takes the same path on purpose - it is the instant after its last frame, and
        // (b - MinFrame) is exactly the count of frames before it.

        /// <summary> The same frame (or exclusive boundary), sampled at the new rate. </summary>
        private static int RemapFrame(int frame, int from, int to)
        {
            if (from == to) return frame;
            return Remap(frame - FrameRules.MinFrame, from, to) + FrameRules.MinFrame;
        }

        // Both edges are remapped and the result rebuilt, rather than remapping the start and
        // scaling the duration: rounding then happens once per edge, exactly as it did when the two
        // were separate fields, so no object drifts by a frame relative to its own keyframes. The
        // end is clamped one higher than the start because it is an exclusive boundary.
        private static FrameSpan RemapSpan(in FrameSpan span, int from, int to, int last)
        {
            var start = ClampFrame(RemapFrame(span.StartFrame, from, to), last);
            var end = ClampFrame(RemapFrame(span.EndFrame, from, to), last + 1);
            return FrameSpan.FromBounds(start, end, span.Anchors);
        }

        private static int ClampFrame(int frame, int last)
            => frame < FrameRules.MinFrame ? FrameRules.MinFrame : frame > last ? last : frame;

        /// <summary> The framerate being moved to, and what happens to keys that collide on the way down. Public
        /// mutable fields, like every parameters class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters : ICurrentFramerateInput
        {
            /// <summary> What the level runs at now. Display only - the run reads the real value off
            /// the level itself, so a host that never fills this in still retimes correctly. </summary>
            public int CurrentFramerate = FrameRules.MinFramerate;

            /// <summary> What it should run at. </summary>
            public int Framerate = 60;

            /// <summary> Retime objects: their bounds and all ten of their keyframe tracks. </summary>
            public bool RemapObjects = true;

            /// <summary> Retime audio tracks: their bounds and their volume/pan automation. </summary>
            public bool RemapAudio;

            /// <summary> Retime level-global event keyframes (camera, post-processing, player,
            /// markers, checkpoints, ...). </summary>
            public bool RemapEvents;

            /// <summary> How far a key may be nudged off its resampled frame when that frame is
            /// already taken. Beyond this - or with nowhere free at all - the key is dropped.
            /// Only ever bites when lowering the framerate. </summary>
            public int MaxKeyShift = 1;

            int ICurrentFramerateInput.CurrentFramerate
            {
                get => CurrentFramerate;
                set => CurrentFramerate = value;
            }
        }
    }
}