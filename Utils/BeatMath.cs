using System.Collections.Generic;
using BH.SDK.Models.Events;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // Lives in the SDK rather than in the consumer because both halves need the SAME grid: a
    // generator resolves beats without a Unity project around it, and the editor draws and snaps to
    // what that generator will act on. Two implementations of "where the beats are" would drift.
    //
    // Every beat frame is rounded from the segment's own start (Start + round(Offset + i * fpb)),
    // never accumulated step by step. Accumulation drifts by up to half a frame per beat and a
    // three-minute segment has thousands of them; rounding from the base keeps the error at half a
    // frame, once, everywhere.

    /// <summary> Where a <see cref="BeatSegment"/>'s beats actually fall, in level frames. </summary>
    public static class BeatMath
    {
        /// <summary> Length of one beat in frames. Fractional - beat frames round on the way out. </summary>
        public static float FramesPerBeat(float bpm, int framerate)
        {
            if (bpm <= 0f || framerate <= 0) return 0f;
            return 60f * framerate / bpm;
        }

        /// <summary> Can this segment produce a grid at all (real tempo, real framerate). </summary>
        public static bool IsUsable(BeatSegment segment, int framerate) =>
            segment != null && framerate > 0 && segment.BPM > 0f &&
            FramesPerBeat(segment.BPM, framerate) >= MinFramesPerBeat;

        /// <summary> The phase brought back into the first beat of the segment, [0, framesPerBeat). </summary>
        public static float NormalizeOffset(float offset, float framesPerBeat)
        {
            if (framesPerBeat <= 0f) return 0f;

            var normalized = offset % framesPerBeat;
            if (normalized < 0f) normalized += framesPerBeat;
            return normalized;
        }

        /// <summary> The segment covering this frame, if any. Segments never overlap, so at most one
        /// can answer. </summary>
        public static bool TryGetSegment(IReadOnlyList<BeatSegment> segments, int frame, out BeatSegment segment)
        {
            segment = null;
            if (segments == null) return false;

            for (var i = 0; i < segments.Count; i++)
            {
                var candidate = segments[i];
                if (candidate == null || !candidate.Span.Contains(frame)) continue;
                segment = candidate;
                return true;
            }
            return false;
        }

        /// <summary> Is this the first beat of a bar - what the grid draws thicker. </summary>
        public static bool IsDownbeat(BeatSegment segment, int beatIndex)
        {
            if (segment == null) return false;

            var perBar = segment.BeatsPerBar;
            if (perBar <= 1) return true;

            var index = beatIndex % perBar;
            if (index < 0) index += perBar;
            return index == 0;
        }

        /// <summary>
        /// Beat frames of one segment inside [fromFrame, toFrame), appended in ascending order.
        /// <paramref name="division"/> subdivides each beat (1 = beats, 2 = eighths, 4 = sixteenths) -
        /// it is a tool setting, never stored in the format. Returns how many were appended.
        /// </summary>
        public static int CollectSegment(BeatSegment segment, int framerate, int division,
            int fromFrame, int toFrame, List<int> destination, int limit = LevelRules.MaxBeatGridPoints)
        {
            if (destination == null || !IsUsable(segment, framerate)) return 0;
            if (division < 1) division = 1;

            var span = segment.Span;
            var start = span.StartFrame;
            var step = FramesPerBeat(segment.BPM, framerate) / division;
            if (step < MinFramesPerBeat) return 0;

            var offset = segment.Offset;
            var lower = fromFrame > start ? fromFrame : start;
            var upper = toFrame < span.EndFrame ? toFrame : span.EndFrame;
            if (upper <= lower) return 0;

            // Jump straight to the first index that can land at or past `lower` instead of walking
            // the segment from its own start - a viewport is a handful of beats and the segment can
            // be a hundred thousand.
            var index = FloorToInt((lower - start - offset) / step);
            if (index < 0) index = 0;

            var appended = 0;
            while (appended < limit)
            {
                var frame = start + RoundToInt(offset + index * step);
                index++;

                if (frame < lower) continue;
                if (frame >= upper) break;

                destination.Add(frame);
                appended++;
            }
            return appended;
        }

        /// <summary> Every segment's beats inside [fromFrame, toFrame), ascending across the whole
        /// list. Segments never overlap, so sorting them by start is all the ordering there is. </summary>
        public static int CollectBeats(IReadOnlyList<BeatSegment> segments, int framerate, int division,
            int fromFrame, int toFrame, List<int> destination, int limit = LevelRules.MaxBeatGridPoints)
        {
            if (segments == null || destination == null) return 0;

            var total = 0;
            for (var i = 0; i < segments.Count; i++)
            {
                if (total >= limit) break;
                total += CollectSegment(segments[i], framerate, division, fromFrame, toFrame,
                    destination, limit - total);
            }
            return total;
        }

        /// <summary> The whole grid of the level, for a consumer with no viewport of its own (a
        /// generator's beat input). Capped like every other collection here. </summary>
        public static int CollectBeats(IReadOnlyList<BeatSegment> segments, int framerate, List<int> destination,
            int limit = LevelRules.MaxBeatGridPoints) =>
            CollectBeats(segments, framerate, 1, 0, FrameRules.MaxFrameDuration, destination, limit);

        // A beat shorter than a frame is not a grid, it is a solid bar - and it would also make the
        // collectors below spin for as long as the limit allows. 1000 BPM at 30 fps is 1.8 frames,
        // so nothing authorable is anywhere near this.
        private const float MinFramesPerBeat = 0.5f;

        // The core assembly can't reference UnityEngine.Mathf, and BHSDKMath has no rounding of its
        // own (it is clamp/lerp-shaped) - System.Math returns double and MidpointRounding.ToEven
        // would put a beat at x.5 on the wrong side of half the frames it lands on.
        private static int RoundToInt(float value) => (int)(value >= 0f ? value + 0.5f : value - 0.5f);
        private static int FloorToInt(float value)
        {
            var truncated = (int)value;
            return value < truncated ? truncated - 1 : truncated;
        }
    }
}
