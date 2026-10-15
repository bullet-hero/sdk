using System;
using System.Collections.Generic;

namespace BH.SDK.Utils.Performance
{
    // Why the front loses nothing and the merge only over-reads: Docs/PERFORMANCE_RATING.md.

    /// <summary> Reduces a set of non-negative demand vectors to the ones no other vector dominates, then
    /// caps the count by merging the closest pair into its component-wise maximum. Deterministic. </summary>
    public static class ParetoFront
    {
        /// <summary> The front of <paramref name="windows"/>, at most <paramref name="cap"/> long.
        /// <paramref name="exact"/> is false when a merge happened. Input arrays are never modified.
        /// </summary>
        public static List<float[]> Reduce(IReadOnlyList<float[]> windows, int cap, out bool exact)
            => Reduce(windows, cap, out exact, out _);

        /// <summary> The same, also naming for every output vector the earliest input index it stands
        /// for. </summary>
        public static List<float[]> Reduce(IReadOnlyList<float[]> windows, int cap, out bool exact,
            out List<int> origins)
        {
            exact = true;
            var front = new List<float[]>();
            origins = new List<int>();
            if (windows == null || windows.Count == 0) return front;
            if (cap < 1) cap = 1;

            BuildFront(windows, front, origins);
            if (front.Count <= cap) return front;

            exact = false;
            Merge(front, origins, cap);
            return front;
        }

        /// <summary> Whether <paramref name="a"/> is at least <paramref name="b"/> in every component.
        /// </summary>
        public static bool Covers(float[] a, float[] b)
        {
            for (var k = 0; k < b.Length; k++)
                if (a[k] < b[k]) return false;
            return true;
        }

        // A dominating vector has a sum at least as large, so in descending-sum order every candidate meets
        // its dominators first; equal vectors collapse into the first one met.
        private static void BuildFront(IReadOnlyList<float[]> windows, List<float[]> front, List<int> origins)
        {
            var order = new int[windows.Count];
            var sums = new double[windows.Count];
            for (var i = 0; i < order.Length; i++)
            {
                order[i] = i;
                sums[i] = Sum(windows[i]);
            }

            Array.Sort(order, (x, y) =>
            {
                var bySum = sums[y].CompareTo(sums[x]);
                return bySum != 0 ? bySum : x.CompareTo(y);
            });

            var kept = new List<int>();
            foreach (var index in order)
            {
                var candidate = windows[index];
                var covered = false;
                foreach (var keptIndex in kept)
                {
                    if (!Covers(windows[keptIndex], candidate)) continue;
                    covered = true;
                    break;
                }

                if (!covered) kept.Add(index);
            }

            // Output in input order, so the result reads along the level's timeline.
            kept.Sort();
            foreach (var index in kept)
            {
                front.Add((float[])windows[index].Clone());
                origins.Add(index);
            }
        }

        // Each live entry caches its nearest neighbour; a merge recomputes only the entries that pointed at
        // the merged pair, so the whole merge stays near O(n^2) instead of O(n^3).
        private static void Merge(List<float[]> front, List<int> origins, int cap)
        {
            var count = front.Count;
            var alive = new bool[count];
            var nearest = new int[count];
            var distance = new double[count];
            for (var i = 0; i < count; i++) alive[i] = true;
            for (var i = 0; i < count; i++) FindNearest(front, alive, i, nearest, distance);

            var live = count;
            while (live > cap)
            {
                var a = -1;
                for (var i = 0; i < count; i++)
                {
                    if (!alive[i] || nearest[i] < 0) continue;
                    if (a < 0 || distance[i] < distance[a]) a = i;
                }

                if (a < 0) break;
                var b = nearest[a];
                var keep = Math.Min(a, b);
                var drop = Math.Max(a, b);

                var merged = front[keep];
                var other = front[drop];
                for (var k = 0; k < merged.Length; k++) merged[k] = Math.Max(merged[k], other[k]);
                origins[keep] = Math.Min(origins[keep], origins[drop]);
                alive[drop] = false;
                live--;

                // The max may now cover other entries; dropping them keeps the max over the set unchanged.
                for (var i = 0; i < count; i++)
                {
                    if (!alive[i] || i == keep || !Covers(merged, front[i])) continue;
                    alive[i] = false;
                    origins[keep] = Math.Min(origins[keep], origins[i]);
                    live--;
                }

                for (var i = 0; i < count; i++)
                {
                    if (!alive[i]) continue;
                    if (i == keep || !alive[nearest[i]] || nearest[i] == keep)
                    {
                        FindNearest(front, alive, i, nearest, distance);
                        continue;
                    }

                    var toMerged = L1(front[i], merged);
                    if (toMerged < distance[i] || (toMerged == distance[i] && keep < nearest[i]))
                    {
                        nearest[i] = keep;
                        distance[i] = toMerged;
                    }
                }
            }

            var write = 0;
            for (var i = 0; i < count; i++)
            {
                if (!alive[i]) continue;
                front[write] = front[i];
                origins[write] = origins[i];
                write++;
            }

            front.RemoveRange(write, count - write);
            origins.RemoveRange(write, count - write);
        }

        private static void FindNearest(List<float[]> front, bool[] alive, int i, int[] nearest, double[] distance)
        {
            nearest[i] = -1;
            distance[i] = double.MaxValue;
            for (var j = 0; j < front.Count; j++)
            {
                if (j == i || !alive[j]) continue;
                var d = L1(front[i], front[j]);
                if (d >= distance[i]) continue;
                nearest[i] = j;
                distance[i] = d;
            }
        }

        private static double L1(float[] a, float[] b)
        {
            var sum = 0d;
            for (var k = 0; k < a.Length; k++) sum += Math.Abs((double)a[k] - b[k]);
            return sum;
        }

        private static double Sum(float[] values)
        {
            var sum = 0d;
            foreach (var value in values) sum += value;
            return sum;
        }
    }
}
