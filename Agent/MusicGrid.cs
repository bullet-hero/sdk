using System;
using System.Collections.Generic;
using System.Globalization;
using BH.SDK.Models.Events;
using BH.SDK.Models.Primitives;
using BH.SDK.Utils;

namespace BH.SDK.Agent
{
    // The bar numbering convention across segments: Docs/MUSIC.md. Beat frames are BeatMath's
    // (start + round(offset + index * framesPerBeat), in float), so a bar lands on the line the editor draws.

    /// <summary> One bar and beat numbering over a level's beat segments, and the conversions between it
    /// and global frames. Immutable once built, so it is safe to share across threads. </summary>
    public sealed class MusicGrid
    {
        private readonly MusicGridSegment[] _segments;

        /// <summary> Framerate the frames are counted at. </summary>
        public int Framerate { get; }

        /// <summary> Segments that carry at least one beat, in start order, numbered. </summary>
        public IReadOnlyList<MusicGridSegment> Segments => _segments;

        /// <summary> Bars across every segment. </summary>
        public int BarCount { get; }

        /// <summary> Beats across every segment. </summary>
        public int BeatCount { get; }

        /// <summary> No segment carries a beat: there is no grid to address. </summary>
        public bool IsEmpty => _segments.Length == 0;

        /// <summary> Built from the level's segments (any order; null and unusable ones are skipped) at its
        /// framerate. The segments are copied, so later edits to them do not reach the grid. </summary>
        public MusicGrid(IReadOnlyList<BeatSegment> segments, int framerate)
        {
            Framerate = framerate;
            var built = new List<MusicGridSegment>();
            if (segments != null)
                for (var i = 0; i < segments.Count; i++)
                    if (BeatMath.IsUsable(segments[i], framerate))
                        built.Add(new MusicGridSegment(segments[i], framerate));

            built.Sort((a, b) => a.Span.StartFrame.CompareTo(b.Span.StartFrame));

            var bar = 1;
            var beat = 1;
            var numbered = new List<MusicGridSegment>(built.Count);
            foreach (var segment in built)
            {
                if (segment.BeatCount <= 0) continue;
                segment.Number(bar, beat);
                bar += segment.BarCount;
                beat += segment.BeatCount;
                numbered.Add(segment);
            }

            _segments = numbered.ToArray();
            BarCount = bar - 1;
            BeatCount = beat - 1;
        }

        /// <summary> The frame of beat <paramref name="beat"/> (1-based) of bar <paramref name="bar"/>.
        /// </summary>
        public bool TryFrameOfBar(int bar, int beat, out int frame, out string error)
        {
            frame = 0;
            if (!TryFindBar(bar, out var segment, out error)) return false;

            if (beat < 1 || beat > segment.BeatsPerBar)
            {
                error = Invariant($"bar {bar} has beats 1-{segment.BeatsPerBar}, not {beat}");
                return false;
            }

            var index = segment.FirstIndexOfBar(bar) + beat - 1;
            if (index < segment.FirstIndex || index > segment.LastIndex)
            {
                error = Invariant($"beat {beat} of bar {bar} falls outside {Describe(segment)}");
                return false;
            }

            frame = segment.FrameOfIndex(index);
            return true;
        }

        /// <summary> The frame of the first beat of bar <paramref name="bar"/> that lies inside its segment
        /// (beat 1, or the first beat of a pickup bar). </summary>
        public bool TryFrameOfBar(int bar, out int frame, out string error)
        {
            frame = 0;
            if (!TryFindBar(bar, out var segment, out error)) return false;

            frame = segment.FrameOfIndex(Math.Max(segment.FirstIndex, segment.FirstIndexOfBar(bar)));
            return true;
        }

        /// <summary> The frame of the <paramref name="beat"/>-th beat (1-based, fractional) counting every
        /// segment's beats in order. </summary>
        public bool TryFrameOfBeat(double beat, out int frame, out string error)
        {
            frame = 0;
            error = null;
            if (double.IsNaN(beat) || double.IsInfinity(beat) || beat < 1 || beat >= BeatCount + 1)
            {
                error = IsEmpty
                    ? "the level has no beat grid"
                    : Invariant($"beat {Show(beat)} does not exist: the grid has beats 1-{BeatCount}; ")
                      + "the nearest segment is " + Describe(beat < 1 ? _segments[0] : _segments[_segments.Length - 1]);
                return false;
            }

            var whole = (int)Math.Floor(beat);
            var segment = _segments[0];
            foreach (var candidate in _segments)
                if (candidate.FirstBeat <= whole)
                    segment = candidate;

            var index = segment.FirstIndex + (whole - segment.FirstBeat);
            return TryFrameAt(segment, index, beat - whole, Invariant($"beat {Show(beat)}"), out frame, out error);
        }

        /// <summary> Where a global frame sits: bar, beat and the fraction of a beat past it. A frame outside
        /// every segment has no position, and the error names the nearest segment. </summary>
        public bool TryPositionOf(int frame, out MusicPosition position, out string error)
        {
            position = default;
            error = null;

            MusicGridSegment segment = null;
            foreach (var candidate in _segments)
                if (candidate.Span.Contains(frame))
                    segment = candidate;

            if (segment == null)
            {
                error = IsEmpty
                    ? "the level has no beat grid"
                    : Invariant($"frame {frame} is outside every beat segment; the nearest is ")
                      + Describe(Nearest(frame));
                return false;
            }

            var index = (int)Math.Floor((frame - segment.Span.StartFrame - segment.Offset) / segment.FramesPerBeat);
            if (segment.FrameOfIndex(index + 1) <= frame) index++;
            if (segment.FrameOfIndex(index) > frame) index--;
            index = Math.Max(segment.FirstIndex, Math.Min(segment.LastIndex, index));

            var fraction = (frame - segment.FrameOfIndex(index)) / (double)segment.FramesPerBeat;
            var bar = segment.FirstBar + FloorDiv(index, segment.BeatsPerBar) - FloorDiv(segment.FirstIndex,
                segment.BeatsPerBar);
            var beat = index - FloorDiv(index, segment.BeatsPerBar) * segment.BeatsPerBar + 1;
            var global = segment.FirstBeat + (index - segment.FirstIndex) + fraction;

            position = new MusicPosition(bar, beat, fraction, global, Array.IndexOf(_segments, segment));
            return true;
        }

        /// <summary> The frame <paramref name="bars"/> bars away from <paramref name="frame"/>, at the same
        /// beat and fraction of the target bar. </summary>
        public bool TryShiftBars(int frame, int bars, out int result, out string error)
        {
            result = 0;
            if (!TryPositionOf(frame, out var position, out error)) return false;

            var bar = position.Bar + bars;
            if (!TryFindBar(bar, out var segment, out error)) return false;

            if (position.Beat > segment.BeatsPerBar)
            {
                error = Invariant($"bar {bar} has beats 1-{segment.BeatsPerBar}, not {position.Beat}");
                return false;
            }

            var index = segment.FirstIndexOfBar(bar) + position.Beat - 1;
            return TryFrameAt(segment, index, position.Fraction, Invariant($"bar {bar}.{position.Beat}"), out result,
                out error);
        }

        /// <summary> The frame <paramref name="beats"/> beats (fractional) away from
        /// <paramref name="frame"/>, counting every segment's beats in order. </summary>
        public bool TryShiftBeats(int frame, double beats, out int result, out string error)
        {
            result = 0;
            if (!TryPositionOf(frame, out var position, out error)) return false;
            return TryFrameOfBeat(position.GlobalBeat + beats, out result, out error);
        }

        /// <summary> Every segment shares one tempo and one bar length: a beat or bar is then the same number
        /// of frames anywhere. </summary>
        public bool TryUniform(out float framesPerBeat, out int beatsPerBar)
        {
            framesPerBeat = 0f;
            beatsPerBar = 0;
            if (IsEmpty) return false;

            framesPerBeat = _segments[0].FramesPerBeat;
            beatsPerBar = _segments[0].BeatsPerBar;
            foreach (var segment in _segments)
                if (Math.Abs(segment.FramesPerBeat - framesPerBeat) > 1e-4f || segment.BeatsPerBar != beatsPerBar)
                    return false;

            return true;
        }

        /// <summary> The segment whose span is closest to <paramref name="frame"/>; null when the grid is
        /// empty. </summary>
        public MusicGridSegment Nearest(int frame)
        {
            MusicGridSegment best = null;
            var distance = long.MaxValue;
            foreach (var segment in _segments)
            {
                long d = frame < segment.Span.StartFrame ? segment.Span.StartFrame - frame
                    : frame >= segment.Span.EndFrame ? frame - segment.Span.LastFrame
                    : 0;
                if (d >= distance) continue;
                distance = d;
                best = segment;
            }

            return best;
        }

        /// <summary> A segment in one phrase an error can carry: name, span and bars. </summary>
        public static string Describe(MusicGridSegment segment)
        {
            if (segment == null) return "no segment";
            var name = string.IsNullOrEmpty(segment.Name) ? "the segment" : "\"" + segment.Name + "\"";
            return Invariant($"{name} {segment.Span} (bars {segment.FirstBar}-{segment.LastBar}, ")
                   + Invariant($"beats {segment.FirstBeat}-{segment.LastBeat})");
        }

        private bool TryFindBar(int bar, out MusicGridSegment segment, out string error)
        {
            segment = null;
            error = null;
            if (IsEmpty)
            {
                error = "the level has no beat grid";
                return false;
            }

            foreach (var candidate in _segments)
                if (bar >= candidate.FirstBar && bar <= candidate.LastBar)
                {
                    segment = candidate;
                    return true;
                }

            var nearest = bar < 1 ? _segments[0] : _segments[_segments.Length - 1];
            error = Invariant($"bar {bar} does not exist: the grid has bars 1-{BarCount}; the nearest segment is ")
                    + Describe(nearest);
            return false;
        }

        private static bool TryFrameAt(MusicGridSegment segment, int index, double fraction, string what,
            out int frame, out string error)
        {
            error = null;
            frame = fraction == 0 && index >= segment.FirstIndex && index <= segment.LastIndex
                ? segment.FrameOfIndex(index)
                : segment.FrameAt(index + fraction);

            if (segment.Span.Contains(frame)) return true;

            error = Invariant($"{what} lands on frame {frame}, outside ") + Describe(segment);
            return false;
        }

        internal static int FloorDiv(int value, int divisor)
        {
            var quotient = value / divisor;
            return value % divisor != 0 && (value < 0) != (divisor < 0) ? quotient - 1 : quotient;
        }

        private static string Show(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary> One beat segment as the grid numbers it: its beats by index (index % BeatsPerBar == 0 is a
    /// downbeat, as in <see cref="BeatMath.IsDownbeat"/>) and its first bar and beat in the whole grid.
    /// </summary>
    public sealed class MusicGridSegment
    {
        /// <summary> The frames the segment covers. </summary>
        public FrameSpan Span { get; }

        /// <summary> Tempo. </summary>
        public float Bpm { get; }

        /// <summary> Phase, frames from the span start, as authored. </summary>
        public float Offset { get; }

        /// <summary> Beats in a bar, at least 1. </summary>
        public int BeatsPerBar { get; }

        /// <summary> The segment's label, maybe empty. </summary>
        public string Name { get; }

        /// <summary> <see cref="BeatMath.FramesPerBeat"/> at the grid's framerate. </summary>
        public float FramesPerBeat { get; }

        /// <summary> Index of the first beat inside the span (0 unless the offset is negative). </summary>
        public int FirstIndex { get; }

        /// <summary> Index of the last beat inside the span. </summary>
        public int LastIndex { get; }

        /// <summary> Beats inside the span. </summary>
        public int BeatCount => LastIndex - FirstIndex + 1;

        /// <summary> Bars the beats fall in; a partial first or last bar counts as one. </summary>
        public int BarCount => MusicGrid.FloorDiv(LastIndex, BeatsPerBar) - MusicGrid.FloorDiv(FirstIndex, BeatsPerBar)
                               + 1;

        /// <summary> Global number of the segment's first bar. </summary>
        public int FirstBar { get; private set; }

        /// <summary> Global number of the segment's last bar. </summary>
        public int LastBar => FirstBar + BarCount - 1;

        /// <summary> Global number of the segment's first beat. </summary>
        public int FirstBeat { get; private set; }

        /// <summary> Global number of the segment's last beat. </summary>
        public int LastBeat => FirstBeat + BeatCount - 1;

        internal MusicGridSegment(BeatSegment segment, int framerate)
        {
            Span = segment.Span;
            Bpm = segment.BPM;
            Offset = segment.Offset;
            BeatsPerBar = Math.Max(1, segment.BeatsPerBar);
            Name = segment.Name ?? string.Empty;
            FramesPerBeat = BeatMath.FramesPerBeat(segment.BPM, framerate);

            var start = Span.StartFrame;
            var first = Math.Max(0, (int)Math.Floor(-Offset / FramesPerBeat));
            while (first > 0 && FrameOfIndex(first - 1) >= start) first--;
            while (FrameOfIndex(first) < start) first++;

            var last = (int)Math.Floor((Span.EndFrame - start - Offset) / FramesPerBeat) + 1;
            while (last >= first && FrameOfIndex(last) >= Span.EndFrame) last--;

            FirstIndex = first;
            LastIndex = last;
        }

        /// <summary> The frame of beat <paramref name="index"/>, exactly as <see cref="BeatMath"/> places it.
        /// </summary>
        public int FrameOfIndex(int index) => Span.StartFrame + RoundToInt(Offset + index * FramesPerBeat);

        /// <summary> The frame at a fractional beat index. </summary>
        public int FrameAt(double index) => Span.StartFrame + RoundToInt(Offset + index * (double)FramesPerBeat);

        internal int FirstIndexOfBar(int bar) =>
            (MusicGrid.FloorDiv(FirstIndex, BeatsPerBar) + bar - FirstBar) * BeatsPerBar;

        internal void Number(int firstBar, int firstBeat)
        {
            FirstBar = firstBar;
            FirstBeat = firstBeat;
        }

        // Lockstep: BeatMath.RoundToInt - half away from zero, never banker's rounding.
        private static int RoundToInt(float value) => (int)(value >= 0f ? value + 0.5f : value - 0.5f);

        private static int RoundToInt(double value) => (int)(value >= 0 ? value + 0.5 : value - 0.5);
    }

    /// <summary> A frame read as music: bar, beat (both 1-based), the fraction of a beat past that beat's
    /// frame, and the global beat number (1-based, fractional). </summary>
    public readonly struct MusicPosition
    {
        /// <summary> Global bar number. </summary>
        public readonly int Bar;

        /// <summary> Beat within the bar, 1-based. </summary>
        public readonly int Beat;

        /// <summary> Beats past <see cref="Beat"/>'s frame; negative only before a segment's first beat.
        /// </summary>
        public readonly double Fraction;

        /// <summary> The beat as <c>beat:X</c> spells it. </summary>
        public readonly double GlobalBeat;

        /// <summary> Index into <see cref="MusicGrid.Segments"/>. </summary>
        public readonly int Segment;

        /// <summary> Built from every value. </summary>
        public MusicPosition(int bar, int beat, double fraction, double globalBeat, int segment)
        {
            Bar = bar;
            Beat = beat;
            Fraction = fraction;
            GlobalBeat = globalBeat;
            Segment = segment;
        }

        /// <summary> <c>17.3</c>, or <c>17.3+0.25</c> between beats. </summary>
        public override string ToString()
        {
            var text = Bar.ToString(CultureInfo.InvariantCulture) + "." + Beat.ToString(CultureInfo.InvariantCulture);
            if (Math.Abs(Fraction) < 0.005) return text;
            return text + (Fraction > 0 ? "+" : "") + Fraction.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}