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
        public void SweepCounts_DoubleFromTheStart_CappedByPointsAndMax()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 4, 8, 12 }, BenchmarkMath.SweepCounts(1, 12));
            Assert.AreEqual(PerformanceRules.BenchmarkMaxPoints, BenchmarkMath.SweepCounts(10, 1_000_000).Count);
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
        public void DeviceProfile_RoundTrips_AndAnUntrustedOneIsNotUsable()
        {
            var service = new SerializationService(new SerializationSettings());
            var profile = new DeviceProfile { DeviceModel = "phone", GpuName = "gpu", CpuBase = 1.5f, Trusted = true };
            profile.GpuCoefficients[3] = 0.25f;

            var read = service.DeserializeData<DeviceProfile>(service.SerializeData(profile));

            Assert.AreEqual(profile, read);
            Assert.IsTrue(DeviceProfile.IsUsable(read));
            read.Trusted = false;
            Assert.IsFalse(DeviceProfile.IsUsable(read));
        }
    }
}
