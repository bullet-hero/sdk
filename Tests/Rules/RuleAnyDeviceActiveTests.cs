using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Controls;
using BH.SDK.Rules.Attributes;
using BH.SDK.Services.Controls;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // The one rule here whose fixture is a real model rather than a throwaway one: the invariant is
    // about ControlsSettings' own four device groups, and a stand-in class would prove nothing about the
    // type that actually ships. AssertHasIssue rather than AssertInvalid for the same reason - a real
    // aggregate may raise other issues, and this test is about this rule firing at all.

    /// <summary>
    /// RuleAnyDeviceActive: at least one control device stays active, and Fix reactivates the first one
    /// in priority order.
    /// </summary>
    public class RuleAnyDeviceActiveTests : BaseRuleTests
    {
        private static ControlsSettings AllInactive()
        {
            var settings = new ControlsSettings();
            foreach (var device in ControlDeviceCatalog.Devices)
                settings.GetDevice(device).Active = false;
            return settings;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Valid_Defaults_Pass() => AssertValid(new ControlsSettings());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Valid_OneActive_Passes()
        {
            var settings = AllInactive();
            settings.DeviceGyro.Active = true;

            AssertValid(settings);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Invalid_NoneActive_Reported()
            => AssertHasIssue<RuleAnyDeviceActiveAttribute>(AllInactive());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Fix_ActivatesFirstByPriority()
        {
            var settings = AllInactive();
            settings.Priority = new[]
            {
                ControlDevice.DeviceGyro,
                ControlDevice.KeyboardMouse,
                ControlDevice.Touchscreen,
                ControlDevice.Gamepad,
            };

            AssertFixed(settings);

            Assert.IsTrue(settings.DeviceGyro.Active);
            Assert.IsFalse(settings.KeyboardMouse.Active);
        }
    }
}
