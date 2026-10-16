using BH.SDK.Rules;
using BH.SDK.Utils.Performance;
using NUnit.Framework;

namespace BH.SDK.Tests.Performance
{
    /// <summary> The arithmetic of spec checks 7 to 9. </summary>
    public class PerformanceValidationTests
    {
        private const double Tolerance = 1e-9;

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Spearman_AMonotoneSeries_IsOne()
        {
            Assert.AreEqual(1d, PerformanceValidation.Spearman(new[] { 1d, 2d, 5d, 9d }, new[] { 3d, 4d, 40d, 41d }),
                Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Spearman_AReversedSeries_IsMinusOne()
        {
            Assert.AreEqual(-1d, PerformanceValidation.Spearman(new[] { 1d, 2d, 3d }, new[] { 9d, 5d, 1d }), Tolerance);
        }

        // Ties take the mean of their ranks: a = ranks 1, 2.5, 2.5, 4 against b = 1, 2, 3, 4.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Spearman_Ties_TakeTheMeanRank()
        {
            var expected = 4.5d / System.Math.Sqrt(4.5d * 5d);

            Assert.AreEqual(expected,
                PerformanceValidation.Spearman(new[] { 1d, 7d, 7d, 9d }, new[] { 1d, 2d, 3d, 4d }), Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Spearman_ASeriesThatDoesNotVary_IsNaN()
        {
            Assert.IsNaN(PerformanceValidation.Spearman(new[] { 2d, 2d, 2d }, new[] { 1d, 2d, 3d }));
            Assert.IsNaN(PerformanceValidation.Spearman(new[] { 2d }, new[] { 1d }));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void RelativeError_IsAgainstTheMeasuredValue()
        {
            Assert.AreEqual(0.25d, PerformanceValidation.RelativeError(5d, 4d), Tolerance);
            Assert.AreEqual(0.5d, PerformanceValidation.RelativeError(2d, 4d), Tolerance);
            Assert.AreEqual(0d, PerformanceValidation.RelativeError(0d, 0d), Tolerance);
            Assert.IsTrue(double.IsPositiveInfinity(PerformanceValidation.RelativeError(1d, 0d)));
        }

        // The PC's mixes of 2026-10-17: 26% off, but 0.36 ms on a 1.4 ms frame.
        [TestCase(1.72d, 1.365d, true)]
        [TestCase(21.1d, 24.4d, true)]
        [TestCase(10d, 12d, false)]
        [TestCase(5d, 4d, false)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PassesAdditivity_WithinTheShareOrUnderTheNoiseFloor(double predicted, double measured,
            bool passes)
            => Assert.AreEqual(passes, PerformanceValidation.PassesAdditivity(predicted, measured));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void JensenGap_OneSideAlwaysBound_IsZero()
        {
            Assert.AreEqual(0d, PerformanceValidation.JensenGap(new[] { 1d, 2d, 3d }, new[] { 4d, 5d, 9d }), Tolerance);
        }

        // Frames alternate (cpu 4, gpu 2) and (cpu 2, gpu 4): every frame takes 4, both means are 3.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void JensenGap_AFlippingBoundSide_IsWhatTheMaxOfMeansMisses()
        {
            Assert.AreEqual(1d, PerformanceValidation.JensenGap(new[] { 4d, 2d, 4d, 2d }, new[] { 2d, 4d, 2d, 4d }),
                Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Compare_FindsBothPeaksAndTheirError()
        {
            var result = PerformanceValidation.Compare(new[] { 2d, 8d, 4d }, new[] { 3d, 6d, 10d }, 100);

            Assert.AreEqual(3, result.Windows);
            Assert.AreEqual(1, result.PredictedPeakIndex);
            Assert.AreEqual(2, result.MeasuredPeakIndex);
            Assert.AreEqual(0.2d, result.PeakError, Tolerance);
            Assert.AreEqual(0.5d, result.Spearman, Tolerance);
        }

        // Budget 10 ms: a predicted 9 ms holds (tight), a measured 12 ms does not (drops).
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Compare_PredictedToHoldButMeasuredNot_IsAFalseHold()
        {
            var result = PerformanceValidation.Compare(new[] { 1d, 9d }, new[] { 1d, 12d }, 100);

            Assert.AreEqual(PerformanceBand.Tight, result.PredictedBand);
            Assert.AreEqual(PerformanceBand.Drops, result.MeasuredBand);
            Assert.IsTrue(result.FalseHold);
            Assert.IsFalse(result.Passed);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Compare_PredictedHeavyButMeasuredLight_IsNoFalseHold()
        {
            var result = PerformanceValidation.Compare(new[] { 1d, 12d }, new[] { 1d, 9d }, 100);

            Assert.IsFalse(result.FalseHold);
        }

        // The PC's demo of 2026-10-17: the peak lands, the order does not, and the frame moved under 1 ms.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Compare_AFlatLevel_SkipsTheRankGate()
        {
            var predicted = new[] { 1.6d, 3.0d, 2.2d, 2.6d, 1.9d, 2.4d, 2.0d, 2.8d, 1.7d, 2.5d, 3.04d };
            var measured = new[] { 2.3d, 2.1d, 2.5d, 2.2d, 2.4d, 2.3d, 2.6d, 2.2d, 2.5d, 2.4d, 2.94d };

            var result = PerformanceValidation.Compare(predicted, measured, 165);

            Assert.Less(result.Spearman, PerformanceRules.MinRankCorrelation);
            Assert.IsTrue(result.IsFlat);
            Assert.IsTrue(result.Passed);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Compare_ALevelThatMovesTheFrame_KeepsTheRankGate()
        {
            var predicted = new[] { 2d, 6d, 3d, 5d, 4d };
            var measured = new[] { 5d, 2d, 4d, 3d, 6d };

            var result = PerformanceValidation.Compare(predicted, measured, 60);

            Assert.IsFalse(result.IsFlat);
            Assert.Less(result.Spearman, PerformanceRules.MinRankCorrelation);
            Assert.IsFalse(result.Passed);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Compare_NoWindows_ComparesNothing()
        {
            var result = PerformanceValidation.Compare(new double[0], new double[0], 60);

            Assert.AreEqual(0, result.Windows);
            Assert.AreEqual(-1, result.PredictedPeakIndex);
        }
    }
}
