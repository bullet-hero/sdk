using System;
using System.Collections.Generic;
using BH.SDK.Generators.External;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Audio
{
    // The first LevelScope generator, and the first destructive one - which makes it the proof that
    // both halves of the contract work. It touches only GameLevel.CameraEvents (null inside a Prefab
    // template, hence LevelScope), and its ClearRange wipes existing keys THROUGH the context, so
    // undo restores what it removed alongside what it added. Wiping a List directly would compile,
    // run, look correct, and quietly make the run un-undoable.

    /// <summary>
    /// Punches the camera on every beat: a quick zoom in and back out, optionally with a shake.
    /// Camera only - it never creates objects.
    /// </summary>
    public class BeatFlashGenerator : BaseContentGenerator<BeatFlashGenerator.Parameters>
    {
        /// <summary> <c>"gen_beat_flash"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_beat_flash";

        /// <summary> What must be true before a host offers this run. </summary>
        public override GeneratorRequirements Requirements =>
            GeneratorRequirements.LevelScope | GeneratorRequirements.ExternalAnalysis;

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.BaseZoom), nameof(Parameters.ZoomPunch),
                nameof(Parameters.DecayFrames))
            .Section(GeneratorSections.Additional, nameof(Parameters.Shake),
                nameof(Parameters.ShakeIntensity), nameof(Parameters.ShakeSpeed),
                nameof(Parameters.ClearRange), nameof(Parameters.BeatFrames))
            .Range(nameof(Parameters.BaseZoom), ValueRules.MinZoom, ValueRules.MaxZoom)
            .Range(nameof(Parameters.ZoomPunch), 0f, ValueRules.MaxZoom)
            .Range(nameof(Parameters.DecayFrames), 1, 240)
            .Range(nameof(Parameters.ShakeIntensity), 0f, 100f)
            .Range(nameof(Parameters.ShakeSpeed), 0f, 100f)
            .Unit(nameof(Parameters.DecayFrames), "frames")
            .VisibleWhen(nameof(Parameters.ShakeIntensity), p => ((Parameters)p).Shake)
            .VisibleWhen(nameof(Parameters.ShakeSpeed), p => ((Parameters)p).Shake)
            .Hidden(nameof(Parameters.BeatFrames))
            .Build();

        /// <summary> Writes this run's objects into the scope. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var camera = context.Game.CameraEvents;

            if (parameters.ClearRange)
            {
                var window = context.Span;
                context.RemoveLevelKeys(camera.Zooms, key => window.Contains(key.Frame));
                if (parameters.Shake)
                    context.RemoveLevelKeys(camera.Shakes, key => window.Contains(key.Frame));
            }

            var zoomFrames = OccupiedFrames(camera.Zooms.Count, camera.Zooms, key => key.Frame);
            var shakeFrames = OccupiedFrames(camera.Shakes.Count, camera.Shakes, key => key.Frame);
            var decay = Decay(parameters.DecayFrames);

            foreach (var beat in PlannedBeats(context, parameters))
            {
                var release = beat + decay;
                if (release > context.Span.LastFrame) release = context.Span.LastFrame;

                // Beats closer together than the decay would want two keys on one frame; the first
                // one there wins, since a punch that already started matters more than its tail.
                TryAdd(context, camera.Zooms, zoomFrames, beat,
                    () => new ZoomKey(new FloatValue(parameters.BaseZoom - parameters.ZoomPunch), beat));
                TryAdd(context, camera.Zooms, zoomFrames, release,
                    () => new ZoomKey(new FloatValue(parameters.BaseZoom), release));

                if (!parameters.Shake) continue;
                TryAdd(context, camera.Shakes, shakeFrames, beat,
                    () => new ShakeKey(parameters.ShakeIntensity, parameters.ShakeSpeed, 1f, 1f, beat));
                TryAdd(context, camera.Shakes, shakeFrames, release,
                    () => new ShakeKey(0f, parameters.ShakeSpeed, 1f, 1f, release));
            }
        }

        // Replays exactly what Generate would write, counting instead of adding: the key count
        // depends on how beats collide with each other and with keys already on the track, so
        // multiplying beats by four would be wrong the moment two beats sit a decay apart.
        //
        // GeneratorCost counts what a run ADDS. Keys that ClearRange removes are not subtracted -
        // an estimate is what the author is about to gain, and "8 keys, and by the way 5 disappear"
        // is two different questions.

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            if (context?.Game == null) return GeneratorCost.Zero;

            var camera = context.Game.CameraEvents;
            var decay = Decay(parameters.DecayFrames);
            var keys = 0;

            var zoomFrames = new HashSet<int>();
            var shakeFrames = new HashSet<int>();
            foreach (var key in camera.Zooms)
                if (!parameters.ClearRange || !context.Span.Contains(key.Frame))
                    zoomFrames.Add(key.Frame);
            foreach (var key in camera.Shakes)
                if (!parameters.ClearRange || !context.Span.Contains(key.Frame))
                    shakeFrames.Add(key.Frame);

            foreach (var beat in PlannedBeats(context, parameters))
            {
                var release = beat + decay;
                if (release > context.Span.LastFrame) release = context.Span.LastFrame;

                if (zoomFrames.Add(beat)) keys++;
                if (zoomFrames.Add(release)) keys++;
                if (!parameters.Shake) continue;
                if (shakeFrames.Add(beat)) keys++;
                if (shakeFrames.Add(release)) keys++;
            }
            return new GeneratorCost(0, keys);
        }

        /// <summary> Beats inside the run's window, capped so one run cannot blow past the camera
        /// track's own key limit. </summary>
        private static IEnumerable<int> PlannedBeats(GeneratorContext context, Parameters parameters)
        {
            var beats = parameters.BeatFrames;
            if (beats == null) yield break;

            var emitted = 0;
            foreach (var beat in beats)
            {
                if (!context.Span.Contains(beat)) continue;
                if (emitted >= MaxBeats) yield break;
                emitted++;
                yield return beat;
            }
        }

        private static void TryAdd<TKey>(GeneratorContext context, List<TKey> track, HashSet<int> occupied,
            int frame, Func<TKey> create)
        {
            if (!occupied.Add(frame)) return;
            context.AddLevelKey(track, create());
        }

        private static HashSet<int> OccupiedFrames<TKey>(int capacity, List<TKey> track, Func<TKey, int> frameOf)
        {
            var frames = new HashSet<int>(capacity);
            foreach (var key in track) frames.Add(frameOf(key));
            return frames;
        }

        // Two keys per beat per track; LevelRules.MaxCameraKeys bounds the track itself, so a run is
        // capped at a quarter of it to leave room for both tracks and for what was already there.
        private const int MaxBeats = LevelRules.MaxCameraKeys / 4;

        private static int Decay(int value) => value < 1 ? 1 : value;

        /// <summary> Which beats are punched and how hard. Public mutable fields, like every parameters class
        /// here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters : IBeatFramesInput
        {
            /// <summary> The zoom the camera rests at between beats. </summary>
            public float BaseZoom = ValueRules.DefaultZoom;
            /// <summary> How far it punches in on one. </summary>
            public float ZoomPunch = 1.5f;
            /// <summary> How long it takes to settle back. </summary>
            public int DecayFrames = 8;
            /// <summary> Whether the punch is accompanied by a shake. </summary>
            public bool Shake = true;
            /// <summary> How hard. </summary>
            public float ShakeIntensity = 0.5f;
            /// <summary> How fast. </summary>
            public float ShakeSpeed = 20f;

            /// <summary> Wipes every camera zoom/shake key inside the run's window first. Undoable
            /// like everything else - the removed keys live on in the change log. </summary>
            public bool ClearRange;

            /// <summary> Which frames are beats; filled by the host from the level's beat map, or from its markers. </summary>
            public int[] BeatFrames = Array.Empty<int>();

            int[] IBeatFramesInput.BeatFrames
            {
                get => BeatFrames;
                set => BeatFrames = value;
            }
        }
    }
}
