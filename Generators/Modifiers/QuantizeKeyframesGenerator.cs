using System.Collections.Generic;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Modifiers
{
    /// <summary> How a keyframe that lands between two grid lines is resolved. </summary>
    public enum QuantizeMode : byte
    {
        /// <summary> Snap to whichever grid line is closer. </summary>
        Nearest = 0,

        /// <summary> Always snap backwards - keeps a key from ever moving later than authored. </summary>
        Floor = 1,

        /// <summary> Always snap forwards. </summary>
        Ceil = 2,
    }

    // The first Modifier, and the first generator that only ever EDITS. Two things fall out of that
    // and are worth reading before writing another one:
    //
    // 1. Everything goes through context.Edit(id), which snapshots the whole object once. That is
    //    what makes "quantize forty objects" a single undo step covering all forty.
    // 2. Frames within a track must stay unique ([RuleCollectionUnique]). Snapping is exactly the
    //    operation that collides two keys onto one frame, so the collision policy is not a detail -
    //    it is the feature. Two keys landing on the same grid line means the author's two keys were
    //    closer together than the grid they asked for, and the honest answer is to keep the first
    //    and leave the second where it was rather than silently deleting it.

    /// <summary>
    /// Snaps the selection's keyframes onto a beat grid - from BPM, or from a plain frame step.
    /// </summary>
    public class QuantizeKeyframesGenerator : BaseModifier<QuantizeKeyframesGenerator.Parameters>
    {
        public override string NameKey => "mod_quantize_keyframes";

        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.UseBpm), nameof(Parameters.Bpm),
                nameof(Parameters.Division), nameof(Parameters.StepFrames))
            .Section(GeneratorSections.Additional, nameof(Parameters.OffsetFrames),
                nameof(Parameters.Mode), nameof(Parameters.Tracks))
            .Range(nameof(Parameters.Bpm), 1f, 1000f)
            .Range(nameof(Parameters.Division), 1, 64)
            .Range(nameof(Parameters.StepFrames), 1, FrameRules.MaxFrameDuration)
            .Range(nameof(Parameters.OffsetFrames), -FrameRules.MaxFrameDuration, FrameRules.MaxFrameDuration)
            .Unit(nameof(Parameters.StepFrames), "frames")
            .Unit(nameof(Parameters.OffsetFrames), "frames")
            .VisibleWhen(nameof(Parameters.Bpm), p => ((Parameters)p).UseBpm)
            .VisibleWhen(nameof(Parameters.Division), p => ((Parameters)p).UseBpm)
            .VisibleWhen(nameof(Parameters.StepFrames), p => !((Parameters)p).UseBpm)
            .Build();

        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var step = StepOf(context, parameters);
            if (step <= 0) return;

            foreach (var id in Targets(context))
            {
                if (!context.Objects.ContainsKey(id)) continue;

                var obj = context.Edit(id);

                // A keyframe's Frame is LOCAL to its object, but a beat grid is a property of the
                // LEVEL's timeline - snapping the local number would put every object on a grid
                // offset by its own span start, so two objects starting a beat and a half apart
                // would quantize to two different grids. Everything below therefore works in global
                // frames and converts back on write.
                foreach (var track in ObjectTracks.Of(obj, parameters.Tracks))
                {
                    var taken = new HashSet<int>();
                    for (var i = 0; i < track.Count; i++) taken.Add(obj.Span.StartFrame + track.FrameAt(i));

                    for (var i = 0; i < track.Count; i++)
                    {
                        var frame = obj.Span.StartFrame + track.FrameAt(i);
                        var snapped = Snap(frame, step, parameters.OffsetFrames, parameters.Mode);
                        if (snapped < obj.Span.StartFrame) snapped = obj.Span.StartFrame; // a key cannot precede its object
                        if (snapped == frame) continue;

                        // The grid line is already occupied by a key this pass is not moving (or has
                        // already moved there) - leave this one alone rather than overwrite it.
                        if (taken.Contains(snapped)) continue;

                        taken.Remove(frame);
                        taken.Add(snapped);
                        track.SetFrameAt(i, snapped - obj.Span.StartFrame);
                    }
                }
            }
        }

        // A modifier creates nothing and adds no keys - it moves the ones already there. Reporting
        // anything else here would make the estimate read as "this will add N", which is exactly
        // what it will not do.
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => GeneratorCost.Zero;

        /// <summary> Grid spacing in frames. BPM is converted through the level's own framerate, so
        /// the same 120 BPM means different frame counts in a 30 fps and a 60 fps level - which is
        /// correct, since a frame is a different length of time in each. </summary>
        private static int StepOf(GeneratorContext context, Parameters parameters)
        {
            if (!parameters.UseBpm) return parameters.StepFrames < 1 ? 1 : parameters.StepFrames;

            var bpm = parameters.Bpm < 1f ? 1f : parameters.Bpm;
            var division = parameters.Division < 1 ? 1 : parameters.Division;
            var framerate = context?.Settings?.Fps ?? 60;

            var framesPerBeat = framerate * 60f / bpm / division;
            var step = (int)(framesPerBeat + 0.5f);
            return step < 1 ? 1 : step;
        }

        private static int Snap(int frame, int step, int offset, QuantizeMode mode)
        {
            var relative = frame - offset;
            var floor = FloorDiv(relative, step) * step;

            var snapped = mode switch
            {
                QuantizeMode.Floor => floor,
                QuantizeMode.Ceil => floor + (relative == floor ? 0 : step),
                _ => relative - floor < step - (relative - floor) ? floor : floor + step,
            };

            var result = snapped + offset;
            return result < FrameRules.MinFrame ? FrameRules.MinFrame : result;
        }

        // C# integer division truncates toward zero, which puts a negative frame on the WRONG side
        // of its grid line - a key at -1 with a step of 4 belongs to the line at -4, not 0.
        private static int FloorDiv(int value, int divisor)
        {
            var quotient = value / divisor;
            return value % divisor != 0 && (value < 0) != (divisor < 0) ? quotient - 1 : quotient;
        }

        /// <summary> The selection, or - when nothing is selected and the host ran it anyway - every
        /// object in scope, so the modifier is still useful from a script or a test. </summary>
        private static IEnumerable<ObjectId> Targets(GeneratorContext context)
        {
            if (context.Selection.Count > 0) return context.Selection;
            return new List<ObjectId>(context.Objects.Keys);
        }

        public class Parameters
        {
            public bool UseBpm = true;
            public float Bpm = 120f;

            /// <summary> Grid lines per beat: 1 = quarter notes, 2 = eighths, 4 = sixteenths. </summary>
            public int Division = 1;

            /// <summary> Grid spacing when UseBpm is off. </summary>
            public int StepFrames = 15;

            /// <summary> Shifts the whole grid, for a song whose first beat is not on frame zero. </summary>
            public int OffsetFrames;

            public QuantizeMode Mode = QuantizeMode.Nearest;
            public ObjectTrackMask Tracks = ObjectTrackMask.All;
        }
    }
}
