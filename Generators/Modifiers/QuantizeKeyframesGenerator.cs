using System;
using System.Collections.Generic;
using BH.SDK.Agent;
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

    // Everything goes through context.Edit(id): one undo step for any number of objects. Snapping collides
    // keys by nature, so the collision policy is the feature: keep the first key, leave the second where
    // it was. Frames stay unique ([RuleCollectionUnique]). Docs/GENERATORS.md.

    /// <summary>
    /// Snaps the selection's keyframes onto a beat grid - the level's own, from BPM, or from a plain frame step.
    /// </summary>
    public class QuantizeKeyframesGenerator : BaseModifier<QuantizeKeyframesGenerator.Parameters>
    {
        /// <summary> <c>"mod_quantize_keyframes"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "mod_quantize_keyframes";

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.UseBeatGrid), nameof(Parameters.UseBpm),
                nameof(Parameters.Bpm), nameof(Parameters.Division), nameof(Parameters.StepFrames))
            .Section(GeneratorSections.Additional, nameof(Parameters.OffsetFrames),
                nameof(Parameters.Mode), nameof(Parameters.Tracks))
            .Range(nameof(Parameters.Bpm), 1f, 1000f)
            .Range(nameof(Parameters.Division), 1, 64)
            .Range(nameof(Parameters.StepFrames), 1, FrameRules.MaxFrameDuration)
            .Range(nameof(Parameters.OffsetFrames), -FrameRules.MaxFrameDuration, FrameRules.MaxFrameDuration)
            .Unit(nameof(Parameters.StepFrames), "frames")
            .Unit(nameof(Parameters.OffsetFrames), "frames")
            .VisibleWhen(nameof(Parameters.Bpm), p => ((Parameters)p).UseBpm)
            .VisibleWhen(nameof(Parameters.Division),
                p => ((Parameters)p).UseBpm || ((Parameters)p).UseBeatGrid)
            .VisibleWhen(nameof(Parameters.StepFrames), p => !((Parameters)p).UseBpm)
            .Describe(nameof(Parameters.UseBeatGrid),
                "Snap to the level's own beat grid when it has one; Division splits each beat. Off, or " +
                "with no grid: Bpm or StepFrames decide.")
            .Describe(nameof(Parameters.UseBpm),
                "Without a beat grid: on, grid lines come from Bpm and Division at the level's " +
                "framerate; off, one line every StepFrames frames.")
            .Describe(nameof(Parameters.Bpm),
                "Tempo in beats per minute for the grid when UseBpm is on and no beat grid applies.")
            .Describe(nameof(Parameters.Division),
                "Grid lines per beat: 1 = quarter notes, 2 = eighths, 4 = sixteenths.")
            .Describe(nameof(Parameters.StepFrames),
                "Grid spacing in frames when UseBpm is off and no beat grid applies; lines start at " +
                "frame 1.")
            .Describe(nameof(Parameters.OffsetFrames),
                "Shifts every grid line by this many frames, a delta that may be negative.")
            .Describe(nameof(Parameters.Mode),
                "Where a key between two grid lines goes: Nearest, Floor (the earlier line) or Ceil (the " +
                "later one).")
            .Describe(nameof(Parameters.Tracks),
                "Which keyframe tracks are snapped, as flag names (Positions, Rotations, Colors, ...). " +
                "All by default.")
            .Build();

        /// <summary> Applies this run's edit. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var grid = GridOf(context, parameters);

            foreach (var id in Targets(context))
            {
                if (!context.Objects.ContainsKey(id)) continue;

                var obj = context.Edit(id);

                // A keyframe's Frame is LOCAL but the beat grid belongs to the LEVEL's timeline: work in
                // global frames and convert back on write, or objects land on grids offset by their start.
                foreach (var track in ObjectTracks.Of(obj, parameters.Tracks))
                {
                    var taken = new HashSet<int>();
                    for (var i = 0; i < track.Count; i++) taken.Add(obj.Span.ToGlobalFrame(track.FrameAt(i)));

                    for (var i = 0; i < track.Count; i++)
                    {
                        var frame = obj.Span.ToGlobalFrame(track.FrameAt(i));
                        var snapped = grid.Snap(frame, parameters.Mode);
                        if (snapped < obj.Span.StartFrame)
                            snapped = obj.Span.StartFrame; // a key cannot precede its object
                        if (snapped == frame) continue;

                        // The grid line is already occupied by a key this pass is not moving (or has
                        // already moved there) - leave this one alone rather than overwrite it.
                        if (taken.Contains(snapped)) continue;

                        taken.Remove(frame);
                        taken.Add(snapped);
                        track.SetFrameAt(i, obj.Span.ToLocalFrame(snapped));
                    }
                }
            }
        }

        // A modifier creates nothing and adds no keys - it moves the ones already there. Reporting
        // anything else here would make the estimate read as "this will add N", which is exactly
        // what it will not do.

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => GeneratorCost.Zero;

        /// <summary> The lines keys snap to. The level's beat grid wins when asked for and present (a
        /// template has none); else BPM through the level's own framerate, so 120 BPM is a different frame
        /// count at 30 and 60 fps; else a plain step. </summary>
        private static SnapGrid GridOf(GeneratorContext context, Parameters parameters)
        {
            var framerate = context?.Settings?.Fps ?? 60;
            var division = parameters.Division < 1 ? 1 : parameters.Division;
            var beats = context?.Game?.Events?.Beats;

            if (parameters.UseBeatGrid && beats != null && beats.Count > 0)
            {
                var lines = BeatLines(new MusicGrid(beats, framerate), division, parameters.OffsetFrames);
                if (lines.Count > 0) return SnapGrid.Of(lines);
            }

            if (!parameters.UseBpm)
                return SnapGrid.Regular(parameters.StepFrames < 1 ? 1 : parameters.StepFrames,
                    parameters.OffsetFrames);

            var bpm = parameters.Bpm < 1f ? 1f : parameters.Bpm;
            return SnapGrid.Regular(framerate * 60.0 / bpm / division, parameters.OffsetFrames);
        }

        // Every beat and its subdivisions, placed by the grid itself (a fractional beat), so a line is where
        // the editor draws it and where "beat:12.5" resolves.
        private static List<int> BeatLines(MusicGrid grid, int division, int offset)
        {
            var lines = new List<int>(grid.BeatCount * division);
            for (var beat = 1; beat <= grid.BeatCount; beat++)
            for (var part = 0; part < division; part++)
                if (grid.TryFrameOfBeat(beat + part / (double)division, out var frame, out _))
                    lines.Add(frame + offset);

            lines.Sort();
            return lines;
        }

        // Each line is rounded from its exact position, never stepped by a rounded spacing: 167 BPM is 21.56
        // frames a beat, and a 22-frame step is a beat off after forty.
        private sealed class SnapGrid
        {
            private List<int> _lines;
            private double _step;
            private int _origin;

            public static SnapGrid Of(List<int> lines) => new() { _lines = lines };

            // Anchored on the first FRAME (MinFrame), not on zero: line zero is the frame the level starts on.
            public static SnapGrid Regular(double step, int offset)
                => new() { _step = step, _origin = offset + FrameRules.MinFrame };

            public int Snap(int frame, QuantizeMode mode)
            {
                Bracket(frame, out var floor, out var ceil);
                var snapped = mode switch
                {
                    QuantizeMode.Floor => floor ?? ceil ?? frame,
                    QuantizeMode.Ceil => ceil ?? floor ?? frame,
                    _ => floor == null ? ceil ?? frame
                        : ceil == null || frame - floor.Value < ceil.Value - frame ? floor.Value
                        : ceil.Value,
                };

                return snapped < FrameRules.MinFrame ? FrameRules.MinFrame : snapped;
            }

            // The last line at or before the frame and the first at or after it; both the frame on a line.
            private void Bracket(int frame, out int? floor, out int? ceil)
            {
                if (_lines != null)
                {
                    var index = _lines.BinarySearch(frame);
                    if (index >= 0)
                    {
                        floor = ceil = frame;
                        return;
                    }

                    index = ~index;
                    floor = index > 0 ? _lines[index - 1] : null;
                    ceil = index < _lines.Count ? _lines[index] : null;
                    return;
                }

                var k = (long)Math.Floor((frame - _origin) / _step);
                while (Line(k) > frame) k--;
                while (Line(k + 1) <= frame) k++;

                floor = Line(k);
                ceil = floor == frame ? floor : Line(k + 1);
            }

            private int Line(long k) => _origin + (int)Math.Round(k * _step, MidpointRounding.AwayFromZero);
        }

        /// <summary> The selection, or - when nothing is selected and the host ran it anyway - every
        /// object in scope, so the modifier is still useful from a script or a test. </summary>
        private static IEnumerable<ObjectId> Targets(GeneratorContext context)
        {
            if (context.Selection.Count > 0) return context.Selection;
            return new List<ObjectId>(context.Objects.Keys);
        }

        /// <summary> Which grid keyframes are snapped to, and which way one between two lines goes. Public mutable
        /// fields, like every parameters class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters
        {
            /// <summary> Whether keys snap to the level's own beat grid (the Beat window's segments) when it
            /// has one; Division still splits each beat. Off, or with no grid, the fields below decide.
            /// </summary>
            public bool UseBeatGrid = true;

            /// <summary> Whether the grid is derived from a tempo rather than given in frames. </summary>
            public bool UseBpm = true;

            /// <summary> That tempo. </summary>
            public float Bpm = 120f;

            /// <summary> Grid lines per beat: 1 = quarter notes, 2 = eighths, 4 = sixteenths. </summary>
            public int Division = 1;

            /// <summary> Grid spacing when UseBpm is off. </summary>
            public int StepFrames = 15;

            /// <summary> Shifts the whole grid, for a song whose first beat is not on the level's first
            /// frame. A DELTA, so it counts from zero like any other. </summary>
            public int OffsetFrames;

            /// <summary> Which way a keyframe between two grid lines goes. </summary>
            public QuantizeMode Mode = QuantizeMode.Nearest;

            /// <summary> Which of an object's tracks are snapped. </summary>
            public ObjectTrackMask Tracks = ObjectTrackMask.All;
        }
    }
}