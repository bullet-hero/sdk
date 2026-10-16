using BH.SDK.Models;
using BH.SDK.Models.Device;
using BH.SDK.Models.Profile;
using BH.SDK.Rules;
using BH.SDK.Serialization;
using BH.SDK.Services.Profile;
using BH.SDK.Utils.Performance;
using NUnit.Framework;

namespace BH.SDK.Tests.Performance
{
    /// <summary> The stress test's arithmetic, and the device profile's place on disk. </summary>
    public class BenchmarkMathTests
    {
        private const double Tolerance = 1e-9;

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Fit_PointsOnALine_RecoverItExactly()
        {
            var fit = BenchmarkMath.Fit(new[] { 1d, 2d, 4d, 8d }, new[] { 5d, 7d, 11d, 19d });

            Assert.AreEqual(3d, fit.Intercept, Tolerance);
            Assert.AreEqual(2d, fit.Slope, Tolerance);
            Assert.AreEqual(1d, fit.R2, Tolerance);
            Assert.IsTrue(fit.IsLinear);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Fit_ACurve_FallsBelowTheLinearityGate()
        {
            var fit = BenchmarkMath.Fit(new[] { 1d, 2d, 3d, 4d, 5d, 6d }, new[] { 1d, 4d, 9d, 16d, 25d, 100d });

            Assert.Less(fit.R2, PerformanceRules.MinFitR2);
            Assert.IsFalse(fit.IsLinear);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Fit_OnePoint_IsNoLine()
        {
            Assert.IsFalse(BenchmarkMath.Fit(new[] { 3d }, new[] { 4d }).IsLinear);
            Assert.IsFalse(BenchmarkMath.Fit(new[] { 3d }, new[] { 4d }).PassesLinearity);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Fit_Rise_IsTheLineAcrossTheXRange()
        {
            var fit = BenchmarkMath.Fit(new[] { 8d, 2d, 4d }, new[] { 19d, 7d, 11d });

            Assert.AreEqual(12d, fit.Rise, Tolerance);
        }

        // Run 2 in the Editor: keyframes cost nothing measurable, the medians wander and R² is 0.03.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Fit_NoiseBelowTheFloor_IsFlatAndPasses()
        {
            var fit = BenchmarkMath.Fit(new[] { 1d, 2d, 4d, 8d }, new[] { 5.8d, 5.5d, 6.1d, 5.7d });

            Assert.IsFalse(fit.IsLinear);
            Assert.IsTrue(fit.IsFlat);
            Assert.IsTrue(fit.PassesLinearity);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Fit_ACurveThatRises_StillFails()
        {
            var fit = BenchmarkMath.Fit(new[] { 1d, 2d, 3d, 4d, 5d, 6d }, new[] { 1d, 4d, 9d, 16d, 25d, 100d });

            Assert.IsFalse(fit.IsFlat);
            Assert.IsFalse(fit.PassesLinearity);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PassesDrift_FlatBothWays_PassesWhateverTheSlopes()
        {
            var up = BenchmarkMath.Fit(new[] { 0d, 8d }, new[] { 1d, 1.6d });
            var down = BenchmarkMath.Fit(new[] { 0d, 8d }, new[] { 1d, 1.2d });

            Assert.Greater(BenchmarkMath.Drift(up.Slope, down.Slope), PerformanceRules.MaxDriftError);
            Assert.IsTrue(BenchmarkMath.PassesDrift(up, down));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PassesDrift_ARealRiseThatDrifts_Fails()
        {
            var up = BenchmarkMath.Fit(new[] { 0d, 8d }, new[] { 1d, 9d });
            var down = BenchmarkMath.Fit(new[] { 0d, 8d }, new[] { 1d, 6d });

            Assert.IsFalse(BenchmarkMath.PassesDrift(up, down));
            Assert.IsTrue(BenchmarkMath.PassesDrift(up, BenchmarkMath.Fit(new[] { 0d, 8d }, new[] { 1d, 9.5d })));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Median_OddAndEven()
        {
            Assert.AreEqual(3d, BenchmarkMath.Median(new[] { 9d, 1d, 3d }), Tolerance);
            Assert.AreEqual(2.5d, BenchmarkMath.Median(new[] { 4d, 1d, 2d, 3d }), Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Drift_IsTheRelativeGapOfTheTwoSlopes()
        {
            Assert.AreEqual(0.1d, BenchmarkMath.Drift(1d, 0.9d), Tolerance);
            Assert.AreEqual(0d, BenchmarkMath.Drift(0d, 0d), Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void SweepCounts_SpreadGeometricallyFromStartToMax()
        {
            var wide = BenchmarkMath.SweepCounts(10, 1_000_000);
            Assert.AreEqual(PerformanceRules.BenchmarkMaxPoints, wide.Count);
            Assert.AreEqual(10, wide[0]);
            Assert.AreEqual(1_000_000, wide[wide.Count - 1]);
            for (var i = 1; i < wide.Count; i++) Assert.Greater(wide[i], wide[i - 1]);

            CollectionAssert.AreEqual(new[] { 1, 2 }, BenchmarkMath.SweepCounts(1, 2));
            CollectionAssert.AreEqual(new[] { 5 }, BenchmarkMath.SweepCounts(5, 5));
            CollectionAssert.IsEmpty(BenchmarkMath.SweepCounts(5, 4));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void DeviceProfile_IsNoPartOfAProfileTransfer()
        {
            Assert.AreEqual(ProfileCategory.None, ProfileLayout.CategoryOf(FileNames.DeviceProfileFileName));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void DeviceProfile_RoundTrips_AndAnUntrustedOneIsStillUsable()
        {
            var service = new SerializationService(new SerializationSettings());
            var profile = new DeviceProfile { DeviceModel = "phone", GpuName = "gpu", CpuBase = 1.5f, Trusted = true };
            profile.GpuCoefficients[3] = 0.25f;

            var read = service.DeserializeData<DeviceProfile>(service.SerializeData(profile));

            Assert.AreEqual(profile, read);
            Assert.IsTrue(DeviceProfile.IsUsable(read));
            read.Trusted = false;
            Assert.IsTrue(DeviceProfile.IsUsable(read));
            read.BenchmarkVersion = PerformanceRules.BenchmarkVersion - 1;
            Assert.IsFalse(DeviceProfile.IsUsable(read));
        }
    }
}