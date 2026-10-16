using System;
using System.Collections.Generic;
using BH.SDK.Rules;

namespace BH.SDK.Utils.Performance
{
    /// <summary> Predicted against measured frame time over the windows of one played level (spec check 9).
    /// </summary>
    public readonly struct WindowComparison
    {
        /// <summary> How many windows were compared. </summary>
        public readonly int Windows;

        /// <summary> Spearman rank correlation of the two series; NaN when either does not vary. </summary>
        public readonly double Spearman;

        /// <summary> The largest predicted time, ms, and its index in the compared series. </summary>
        public readonly double PredictedPeak;

        /// <summary> Index of <see cref="PredictedPeak"/>. </summary>
        public readonly int PredictedPeakIndex;

        /// <summary> The largest measured time, ms. </summary>
        public readonly double MeasuredPeak;

        /// <summary> Index of <see cref="MeasuredPeak"/>. </summary>
        public readonly int MeasuredPeakIndex;

        /// <summary> The frame budget, ms, the bands were read against. </summary>
        public readonly double Budget;

        /// <summary> How far the measured windows spread, ms: their 90th percentile less their 10th.
        /// </summary>
        public readonly double MeasuredSpread;

        /// <summary> Every member at once, in declaration order. </summary>
        public WindowComparison(int windows, double spearman, double predictedPeak, int predictedPeakIndex,
            double measuredPeak, int measuredPeakIndex, double budget, double measuredSpread)
        {
            Windows = windows;
            Spearman = spearman;
            PredictedPeak = predictedPeak;
            PredictedPeakIndex = predictedPeakIndex;
            MeasuredPeak = measuredPeak;
            MeasuredPeakIndex = measuredPeakIndex;
            Budget = budget;
            MeasuredSpread = measuredSpread;
        }

        /// <summary> The peaks' relative error against the measured one. </summary>
        public double PeakError => PerformanceValidation.RelativeError(PredictedPeak, MeasuredPeak);

        /// <summary> The band the predicted peak reads in. </summary>
        public PerformanceBand PredictedBand => PerformanceValidation.BandOf(PredictedPeak, Budget);

        /// <summary> The band the measured peak reads in. </summary>
        public PerformanceBand MeasuredBand => PerformanceValidation.BandOf(MeasuredPeak, Budget);

        /// <summary> Predicted to hold (up to <see cref="PerformanceRules.ScoreLimit"/>) and measured not to:
        /// the one error check 9 allows none of. </summary>
        public bool FalseHold => PerformanceBands.Holds(PredictedBand) && PerformanceBands.Fails(MeasuredBand);

        /// <summary> Whether the level barely moved the frame on this device: its measured windows spread
        /// less than <see cref="PerformanceRules.NoiseFloorMs"/>, so their order is the order of noise.
        /// </summary>
        public bool IsFlat => MeasuredSpread < PerformanceRules.NoiseFloorMs;

        /// <summary> Whether every gate of check 9 passed; a flat level skips the rank gate. </summary>
        public bool Passed => !FalseHold && PeakError <= PerformanceRules.MaxPeakError
                                         && (IsFlat || !(Spearman < PerformanceRules.MinRankCorrelation));
    }

    // The checks and their thresholds: Docs/PERFORMANCE_RATING.md, "Checking the model".

    /// <summary> The arithmetic of the spec's experiments: rank correlation, relative error, the Jensen gap
    /// and the per-window comparison. Pure, so a device, a server and the tests compute them alike. </summary>
    public static class PerformanceValidation
    {
        /// <summary> Compares the two series window by window; both are frame times in ms, index for index.
        /// </summary>
        public static WindowComparison Compare(IReadOnlyList<double> predicted, IReadOnlyList<double> measured,
            int targetFps)
        {
            var count = Math.Min(predicted?.Count ?? 0, measured?.Count ?? 0);
            var budget = 1000d / Math.Max(1, targetFps);
            if (count == 0) return new WindowComparison(0, double.NaN, 0d, -1, 0d, -1, budget, 0d);

            var predictedPeak = 0;
            var measuredPeak = 0;
            for (var i = 1; i < count; i++)
            {
                if (predicted[i] > predicted[predictedPeak]) predictedPeak = i;
                if (measured[i] > measured[measuredPeak]) measuredPeak = i;
            }

            return new WindowComparison(count, Spearman(predicted, measured), predicted[predictedPeak], predictedPeak,
                measured[measuredPeak], measuredPeak, budget, Spread(measured, count));
        }

        // Percentiles, not the extremes: one hitch or one seek would otherwise make any level "move".
        private static double Spread(IReadOnlyList<double> values, int count)
        {
            var sorted = new double[count];
            for (var i = 0; i < count; i++) sorted[i] = values[i];
            Array.Sort(sorted);
            return sorted[(count - 1) * 9 / 10] - sorted[(count - 1) / 10];
        }

        /// <summary> Spearman's rank correlation, ties ranked by their mean; NaN for fewer than two pairs or
        /// a series that does not vary. </summary>
        public static double Spearman(IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            var count = Math.Min(a?.Count ?? 0, b?.Count ?? 0);
            if (count < 2) return double.NaN;

            var rankA = Ranks(a, count);
            var rankB = Ranks(b, count);
            var fit = BenchmarkMath.Fit(rankA, rankB);
            if (Varies(rankA) && Varies(rankB))
                return Math.Sign(fit.Slope) * Math.Sqrt(fit.R2);
            return double.NaN;
        }

        /// <summary> <c>|predicted - measured| / measured</c>; zero when both are zero, infinite when only
        /// the measured one is. </summary>
        public static double RelativeError(double predicted, double measured)
        {
            if (measured > 0d) return Math.Abs(predicted - measured) / measured;
            return predicted == 0d ? 0d : double.PositiveInfinity;
        }

        /// <summary> Check 7: a mixed level's prediction is within
        /// <see cref="PerformanceRules.MaxAdditivityError"/> of the measurement, or misses it by less than
        /// <see cref="PerformanceRules.NoiseFloorMs"/>. </summary>
        public static bool PassesAdditivity(double predicted, double measured)
            => RelativeError(predicted, measured) <= PerformanceRules.MaxAdditivityError
               || Math.Abs(predicted - measured) < PerformanceRules.NoiseFloorMs;

        /// <summary> <c>mean(max(cpu, gpu)) - max(mean cpu, mean gpu)</c> over frames, ms: what the model's
        /// max of means under-reads when the bound side flips. Never negative. </summary>
        public static double JensenGap(IReadOnlyList<double> cpu, IReadOnlyList<double> gpu)
        {
            var count = Math.Min(cpu?.Count ?? 0, gpu?.Count ?? 0);
            if (count == 0) return 0d;

            double sumMax = 0d, sumCpu = 0d, sumGpu = 0d;
            for (var i = 0; i < count; i++)
            {
                sumMax += Math.Max(cpu[i], gpu[i]);
                sumCpu += cpu[i];
                sumGpu += gpu[i];
            }

            return Math.Max(0d, (sumMax - Math.Max(sumCpu, sumGpu)) / count);
        }

        /// <summary> The band a frame time reads in against a budget. </summary>
        public static PerformanceBand BandOf(double time, double budget)
            => new ScoreResult(true, budget > 0d ? (float)(100d * time / budget) : float.PositiveInfinity, false, -1)
                .Band;

        private static double[] Ranks(IReadOnlyList<double> values, int count)
        {
            var order = new int[count];
            for (var i = 0; i < count; i++) order[i] = i;
            Array.Sort(order, (x, y) => values[x].CompareTo(values[y]));

            var ranks = new double[count];
            for (var start = 0; start < count;)
            {
                var end = start + 1;
                while (end < count && values[order[end]] == values[order[start]]) end++;

                var rank = (start + end + 1) * 0.5d;
                for (var i = start; i < end; i++) ranks[order[i]] = rank;
                start = end;
            }

            return ranks;
        }

        private static bool Varies(double[] values)
        {
            for (var i = 1; i < values.Length; i++)
                if (values[i] != values[0]) return true;
            return false;
        }
    }
}
