using System;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Utils.Performance
{
    // Window i covers [i * WindowHopSeconds, i * WindowHopSeconds + WindowSeconds) of level time; the last
    // one may run past the level's end. Frame f begins at (f - FrameRules.MinFrame) / fps.

    /// <summary> The grid of demand windows a level is measured on, and exact integration of a lifetime
    /// over it: a value added here is the MEAN of that quantity over each window. </summary>
    public static class DemandWindows
    {
        /// <summary> How many windows cover a level of this length; zero for an empty level. </summary>
        public static int Count(float durationSeconds)
        {
            if (!(durationSeconds > 0f)) return 0;
            if (durationSeconds <= PerformanceRules.WindowSeconds) return 1;

            var hops = (durationSeconds - PerformanceRules.WindowSeconds) / PerformanceRules.WindowHopSeconds;
            return (int)Math.Ceiling(hops - Epsilon) + 1;
        }

        /// <summary> Where window <paramref name="index"/> starts and ends, in seconds. </summary>
        public static (float Start, float End) Bounds(int index)
        {
            var start = index * PerformanceRules.WindowHopSeconds;
            return (start, start + PerformanceRules.WindowSeconds);
        }

        /// <summary> Adds <c>weight * |span ∩ window| / WindowSeconds</c> to every window the span touches.
        /// Windows past the end of <paramref name="perWindow"/> are ignored. </summary>
        public static void Accumulate(FrameSpan span, int fps, float weight, float[] perWindow)
        {
            if (perWindow == null || perWindow.Length == 0 || fps <= 0 || weight == 0f) return;

            var start = (double)(span.StartFrame - FrameRules.MinFrame) / fps;
            var end = (double)(span.EndFrame - FrameRules.MinFrame) / fps;
            AccumulateSeconds(start, end, weight, perWindow);
        }

        /// <summary> The same as <see cref="Accumulate"/>, for a lifetime given in seconds. </summary>
        public static void AccumulateSeconds(double start, double end, float weight, float[] perWindow)
        {
            if (perWindow == null || !(end > start)) return;

            const double window = PerformanceRules.WindowSeconds;
            const double hop = PerformanceRules.WindowHopSeconds;

            var first = Math.Max(0, (int)Math.Floor((start - window) / hop));
            var last = Math.Min(perWindow.Length - 1, (int)Math.Floor(end / hop));

            for (var i = first; i <= last; i++)
            {
                var windowStart = i * hop;
                var overlap = Math.Min(end, windowStart + window) - Math.Max(start, windowStart);
                if (overlap > 0d) perWindow[i] += (float)(weight * overlap / window);
            }
        }

        private const double Epsilon = 1e-6;
    }
}
