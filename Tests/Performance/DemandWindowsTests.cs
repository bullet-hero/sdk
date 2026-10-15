using System.Linq;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Utils.Performance;
using NUnit.Framework;

namespace BH.SDK.Tests.Performance
{
    /// <summary> The demand window grid and exact integration of a lifetime over it. </summary>
    public class DemandWindowsTests
    {
        private const int Fps = 60;
        private const float Tolerance = 1e-4f;

        // Frame f begins at (f - 1) / fps, so a time t starts on frame t * fps + 1.
        private static int FrameAt(float seconds) => (int)System.Math.Round(seconds * Fps) + FrameRules.MinFrame;

        private static FrameSpan Seconds(float start, float end) => FrameSpan.FromBounds(FrameAt(start), FrameAt(end));

        [TestCase(0f, 0)]
        [TestCase(0.25f, 1)]
        [TestCase(0.5f, 1)]
        [TestCase(1f, 3)]
        [TestCase(1.1f, 4)]
        [TestCase(10f, 39)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Count_CoversTheLevelWithTheFewestWindows(float duration, int expected)
            => Assert.AreEqual(expected, DemandWindows.Count(duration));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Bounds_StartOneHopApart_AndLastOneWindow()
        {
            var (start, end) = DemandWindows.Bounds(2);

            Assert.AreEqual(2 * PerformanceRules.WindowHopSeconds, start, Tolerance);
            Assert.AreEqual(start + PerformanceRules.WindowSeconds, end, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Accumulate_ASpanCoveringOneWindowExactly_AddsOneToIt()
        {
            var windows = new float[DemandWindows.Count(1f)];
            var (start, end) = DemandWindows.Bounds(1);

            DemandWindows.Accumulate(Seconds(start, end), Fps, 1f, windows);

            Assert.AreEqual(1f, windows[1], Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Accumulate_ATwoFrameSpan_IsCountedExactly()
        {
            var windows = new float[DemandWindows.Count(1f)];

            DemandWindows.Accumulate(new FrameSpan(FrameAt(0.6f), 2), Fps, 1f, windows);

            var expected = 2f / Fps / PerformanceRules.WindowSeconds;
            Assert.AreEqual(expected, windows[2], Tolerance, "the window [0.5, 1) holds both frames");
            Assert.AreEqual(expected, windows[1], Tolerance, "so does [0.25, 0.75)");
            Assert.AreEqual(0f, windows[0], Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Accumulate_ASpanOverAHopBoundary_SplitsByOverlap()
        {
            var windows = new float[DemandWindows.Count(1f)];

            DemandWindows.Accumulate(Seconds(0.4f, 0.6f), Fps, 1f, windows);

            Assert.AreEqual(0.1f / PerformanceRules.WindowSeconds, windows[0], Tolerance);
            Assert.AreEqual(0.2f / PerformanceRules.WindowSeconds, windows[1], Tolerance);
            Assert.AreEqual(0.1f / PerformanceRules.WindowSeconds, windows[2], Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Accumulate_ALongSpanInsideTheLevel_SumsToItsLengthOverTheHop()
        {
            var windows = new float[DemandWindows.Count(10f)];

            DemandWindows.Accumulate(Seconds(2f, 6f), Fps, 1f, windows);

            Assert.AreEqual(4f / PerformanceRules.WindowHopSeconds, windows.Sum(), 1e-3f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Accumulate_ScalesByWeight_AndIgnoresWindowsPastTheArray()
        {
            var windows = new float[2];

            DemandWindows.Accumulate(Seconds(0f, 5f), Fps, 3f, windows);

            Assert.AreEqual(3f, windows[0], Tolerance);
            Assert.AreEqual(3f, windows[1], Tolerance);
        }
    }
}
