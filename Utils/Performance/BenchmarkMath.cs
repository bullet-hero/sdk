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

        /// <summary> Coefficient of determination; 1 when the points lie on the line or do not vary. </summary>
        public readonly double R2;

        /// <summary> How many points it was fitted to; below two there is no line. </summary>
        public readonly int Points;

        /// <summary> Every member at once, in declaration order. </summary>
        public LinearFit(double intercept, double slope, double r2, int points)
        {
            Intercept = intercept;
            Slope = slope;
            R2 = r2;
            Points = points;
        }

        /// <summary> Whether the fit passes <see cref="PerformanceRules.MinFitR2"/> on at least two points. </summary>
        public bool IsLinear => Points >= 2 && R2 >= PerformanceRules.MinFitR2;
    }

    /// <summary> The arithmetic of the device stress test, kept pure so the server and the tests share it. </summary>
    public static class BenchmarkMath
    {
        /// <summary> The least-squares line through the points. </summary>
        public static LinearFit Fit(IReadOnlyList<double> x, IReadOnlyList<double> y)
        {
            var count = Math.Min(x?.Count ?? 0, y?.Count ?? 0);
            if (count == 0) return new LinearFit(0d, 0d, 0d, 0);
            if (count == 1) return new LinearFit(y[0], 0d, 0d, 1);

            double meanX = 0d, meanY = 0d;
            for (var i = 0; i < count; i++)
            {
                meanX += x[i];
                meanY += y[i];
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
            return new LinearFit(intercept, slope, r2, count);
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

        /// <summary> The unit counts of one sweep: <paramref name="start"/> doubling, at most
        /// <see cref="PerformanceRules.BenchmarkMaxPoints"/> of them and none above <paramref name="max"/>. </summary>
        public static List<int> SweepCounts(int start, int max)
        {
            var counts = new List<int>(PerformanceRules.BenchmarkMaxPoints);
            var n = Math.Max(1, start);
            while (counts.Count < PerformanceRules.BenchmarkMaxPoints && n <= max)
            {
                counts.Add(n);
                if (n > int.MaxValue / 2) break;
                n *= 2;
            }

            if (counts.Count < PerformanceRules.BenchmarkMaxPoints && max > 0 && (counts.Count == 0 || counts[counts.Count - 1] < max))
                counts.Add(max);
            return counts;
        }
    }
}
