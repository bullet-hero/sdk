using BH.SDK.Models.Device;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Rules;
using BH.SDK.Utils.Performance;
using NUnit.Framework;

namespace BH.SDK.Tests.Performance
{
    /// <summary> The shipped reference profiles: one per device, usable by this build, never shared. </summary>
    public class ReferenceDeviceProfilesTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void EveryReferenceDevice_HasAUsableProfileOfTheFullShape()
        {
            foreach (var device in ReferenceDeviceProfiles.All)
            {
                var profile = ReferenceDeviceProfiles.For(device);

                Assert.IsTrue(DeviceProfile.IsUsable(profile), $"{device}");
                Assert.AreEqual(DemandDimensions.Count, profile.CpuCoefficients.Length, $"{device}");
                Assert.AreEqual(DemandDimensions.Count, profile.GpuCoefficients.Length, $"{device}");
                Assert.AreEqual(PerformanceRules.NeutralAreaFactors().Length, profile.AreaFactors.Length, $"{device}");
                Assert.Greater(profile.CpuBase, 0f, $"{device}");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void For_HandsOutAFreshCopyEachTime()
        {
            var first = ReferenceDeviceProfiles.For(ReferenceDevice.Pc);
            first.CpuCoefficients[0] = 99f;

            Assert.AreNotEqual(99f, ReferenceDeviceProfiles.For(ReferenceDevice.Pc).CpuCoefficients[0]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheEstimatedDevices_AreTheTwoNobodyMeasured()
        {
            Assert.IsTrue(ReferenceDeviceProfiles.IsEstimate(ReferenceDevice.WeakAndroid));
            Assert.IsTrue(ReferenceDeviceProfiles.IsEstimate(ReferenceDevice.LaptopIGpu));
            Assert.IsFalse(ReferenceDeviceProfiles.IsEstimate(ReferenceDevice.MidAndroid));
            Assert.IsFalse(ReferenceDeviceProfiles.IsEstimate(ReferenceDevice.Pc));
        }
    }
}
