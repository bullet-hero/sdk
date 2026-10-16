using System;
using System.Collections.Generic;
using BH.SDK.Rules;

namespace BH.SDK.Utils.Performance
{
    /// <summary> A least-squares line <c>y = Intercept + Slope * x</c> and how well it fits. </summary>
    public readonly struct LinearFit
    {
        /// <summary> Value at x = 0. </summary>
        public readonly double Intercept;

        /// <summary> Change of y per unit of x. </summary>
        public readonly double Slope;

        /// <summary> Coefficient of determination; 1 when the points lie on the line or do not vary.
        /// </summary>
        public readonly double R2;

        /// <summary> How many points it was fitted to; below two there is no line. </summary>
        public readonly int Points;

        /// <summary> How far the line rises across the fitted x range, in y units; never negative. </summary>
        public readonly double Rise;

        /// <summary> Every member at once, in declaration order. </summary>
        public LinearFit(double intercept, double slope, double r2, int points, double rise = 0d)
        {
            Intercept = intercept;
            Slope = slope;
            R2 = r2;
            Points = points;
            Rise = rise;
        }

        /// <summary> Whether the fit passes <see cref="PerformanceRules.MinFitR2"/> on at least two points.
        /// </summary>
        public bool IsLinear => Points >= 2 && R2 >= PerformanceRules.MinFitR2;

        /// <summary> Whether the line rises less than <see cref="PerformanceRules.NoiseFloorMs"/>: nothing to
        /// fit. </summary>
        public bool IsFlat => Rise < PerformanceRules.NoiseFloorMs;

        /// <summary> The linearity gate of a sweep: linear, or flat. </summary>
        public bool PassesLinearity => IsLinear || Points >= 2 && IsFlat;
    }

    /// <summary> The arithmetic of the device stress test, kept pure so the server and the tests share it.
    /// </summary>
    public static class BenchmarkMath
    {
        /// <summary> The least-squares line through the points. </summary>
        public static LinearFit Fit(IReadOnlyList<double> x, IReadOnlyList<double> y)
        {
            var count = Math.Min(x?.Count ?? 0, y?.Count ?? 0);
            if (count == 0) return new LinearFit(0d, 0d, 0d, 0);
            if (count == 1) return new LinearFit(y[0], 0d, 0d, 1);

            double meanX = 0d, meanY = 0d, minX = x[0], maxX = x[0];
            for (var i = 0; i < count; i++)
            {
                meanX += x[i];
                meanY += y[i];
                minX = Math.Min(minX, x[i]);
                maxX = Math.Max(maxX, x[i]);
            }

            meanX /= count;
            meanY /= count;

            double sxx = 0d, sxy = 0d, syy = 0d;
            for (var i = 0; i < count; i++)
            {
                var dx = x[i] - meanX;
                var dy = y[i] - meanY;
                sxx += dx * dx;
                sxy += dx * dy;
                syy += dy * dy;
            }

            if (sxx <= 0d) return new LinearFit(meanY, 0d, 0d, count);

            var slope = sxy / sxx;
            var intercept = meanY - slope * meanX;
            var r2 = syy <= 0d ? 1d : sxy * sxy / (sxx * syy);
            return new LinearFit(intercept, slope, r2, count, Math.Abs(slope) * (maxX - minX));
        }

        /// <summary> The median of the values; zero for none. Sorts a copy. </summary>
        public static double Median(IReadOnlyList<double> values)
        {
            if (values == null || values.Count == 0) return 0d;

            var sorted = new double[values.Count];
            for (var i = 0; i < sorted.Length; i++) sorted[i] = values[i];
            Array.Sort(sorted);

            var middle = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) * 0.5d;
        }

        /// <summary> Relative difference of the up and the down slope of the thermal check; zero when both
        /// are zero. </summary>
        public static double Drift(double upSlope, double downSlope)
        {
            var scale = Math.Max(Math.Abs(upSlope), Math.Abs(downSlope));
            return scale <= 0d ? 0d : Math.Abs(upSlope - downSlope) / scale;
        }

        /// <summary> The thermal check: the slopes within <see cref="PerformanceRules.MaxDriftError"/> of
        /// each other, or both passes flat, where the slopes are noise. </summary>
        public static bool PassesDrift(LinearFit up, LinearFit down)
            => Drift(up.Slope, down.Slope) <= PerformanceRules.MaxDriftError || up.IsFlat && down.IsFlat;

        /// <summary> The unit counts of one sweep: <see cref="PerformanceRules.BenchmarkMaxPoints"/> of them
        /// spread geometrically from <paramref name="start"/> to <paramref name="max"/>, rounded, without
        /// repeats. </summary>
        public static List<int> SweepCounts(int start, int max)
        {
            var counts = new List<int>(PerformanceRules.BenchmarkMaxPoints);
            start = Math.Max(1, start);
            if (max < start) return counts;

            var steps = PerformanceRules.BenchmarkMaxPoints - 1;
            var ratio = steps > 0 ? Math.Pow((double)max / start, 1d / steps) : 1d;
            for (var i = 0; i <= steps; i++)
            {
                var n = i == steps ? max : (int)Math.Round(start * Math.Pow(ratio, i));
                if (counts.Count == 0 || n > counts[counts.Count - 1]) counts.Add(n);
            }

            return counts;
        }
    }
}
