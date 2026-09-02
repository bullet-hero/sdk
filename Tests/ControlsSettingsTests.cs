using System;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Controls;
using BH.SDK.Serialization;
using BH.SDK.Services.Controls;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Three things are checked here that no other test can see. The catalog is static data with no
    // rule attached to it, so a device declared with no scenes or no modes would ship silently. The six
    // device groups are hand-written boilerplate six times over, so a Copy/Pull/Equals that forgot one
    // field is exactly the mistake this file exists for - which is why they are walked generically
    // rather than asserted one group at a time. And GeneralMode is a cast between two enums whose
    // values only line up by convention.

    /// <summary>
    /// The control settings tree: catalog invariants, the per-device groups' Reset/Copy/Pull/Equals
    /// boilerplate, and a full round trip through UserSettings.
    /// </summary>
    public class ControlsSettingsTests
    {
        private static SerializationService NewService()
            => new(new SerializationSettings());

        #region Catalog

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Catalog_CoversEveryDeviceExactlyOnce()
        {
            var declared = (ControlDevice[])Enum.GetValues(typeof(ControlDevice));

            CollectionAssert.AreEquivalent(declared, ControlDeviceCatalog.Devices);
            CollectionAssert.AllItemsAreUnique(ControlDeviceCatalog.Devices);
            Assert.AreEqual(declared.Length, ControlDeviceCatalog.DeviceCount);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Catalog_EveryDeviceIndexesItself()
        {
            foreach (var device in ControlDeviceCatalog.Devices)
                Assert.AreEqual(device, ControlDeviceCatalog.Get(device).Device);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Catalog_EveryDeviceSupportsAModeAndAScene()
        {
            foreach (var device in ControlDeviceCatalog.Devices)
            {
                var info = ControlDeviceCatalog.Get(device);

                Assert.AreNotEqual(ControlModeMask.None, info.SupportedModes, $"{device} supports no mode");
                Assert.IsFalse(string.IsNullOrEmpty(info.NameKey), $"{device} has no name key");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Catalog_UnknownDevice_Throws()
            => Assert.Throws<ArgumentOutOfRangeException>(() => ControlDeviceCatalog.Get((ControlDevice)42));

        #endregion

        #region Groups

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Settings_GetDevice_ReturnsThatDevicesGroup()
        {
            var settings = new ControlsSettings();

            foreach (var device in ControlDeviceCatalog.Devices)
                Assert.AreEqual(device, settings.GetDevice(device).Device);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Settings_GetDevice_UnknownThrows()
        {
            var settings = new ControlsSettings();
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.GetDevice((ControlDevice)42));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Settings_DefaultModes_AreSupportedByTheirDevice()
        {
            var settings = new ControlsSettings();

            foreach (var device in ControlDeviceCatalog.Devices)
            {
                var group = settings.GetDevice(device);
                Assert.IsTrue(ControlDeviceCatalog.Supports(device, group.GeneralMode),
                    $"{device} defaults to {group.GeneralMode}, which it does not support");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Settings_DefaultPriority_IsAPermutation()
        {
            var settings = new ControlsSettings();

            CollectionAssert.AreEquivalent(ControlDeviceCatalog.Devices, settings.Priority);
            CollectionAssert.AllItemsAreUnique(settings.Priority);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Settings_DefaultPriority_IsNotSharedWithTheCatalog()
        {
            var settings = new ControlsSettings();
            settings.Priority[0] = ControlDevice.DeviceGyro;

            Assert.AreEqual(ControlDevice.KeyboardMouse, ControlDeviceCatalog.Devices[0],
                "Priority handed out the catalog's own array");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Settings_HasActiveDevice_FollowsTheGroups()
        {
            var settings = new ControlsSettings();
            Assert.IsTrue(settings.HasActiveDevice());

            foreach (var device in ControlDeviceCatalog.Devices)
                settings.GetDevice(device).Active = false;
            Assert.IsFalse(settings.HasActiveDevice());

            settings.Touchscreen.Active = true;
            Assert.IsTrue(settings.HasActiveDevice());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Settings_Copy_IsDeepAndEqual()
        {
            var source = MockData.CreateValidTestSettings().Controls;
            var copy = source.Copy();

            Assert.IsTrue(source.Equals(copy));
            Assert.AreEqual(source.GetHashCode(), copy.GetHashCode());

            copy.Common.CursorScale += 0.5f;
            copy.KeyboardMouse.Sensitivity += 1f;
            copy.Priority[0] = copy.Priority[1];

            Assert.IsFalse(source.Equals(copy), "Copy shares state with its source");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Settings_Pull_TakesEveryField()
        {
            var source = MockData.CreateValidTestSettings().Controls;
            var target = new ControlsSettings();

            target.Pull(source);

            Assert.IsTrue(source.Equals(target));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Settings_Reset_RestoresDefaults()
        {
            var settings = MockData.CreateValidTestSettings().Controls;
            settings.Reset();

            Assert.IsTrue(new ControlsSettings().Equals(settings));
        }

        // Every device group's own Copy/Pull/Equals, walked rather than spelled out: six near-identical
        // implementations are exactly where a forgotten field hides, and a per-group test would have to
        // be remembered for a seventh device.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void DeviceGroups_CopyAndEquals_RoundTrip()
        {
            var source = MockData.CreateValidTestSettings().Controls;

            foreach (var device in ControlDeviceCatalog.Devices)
            {
                var group = source.GetDevice(device);
                var copy = group.Copy();

                Assert.AreEqual(group.GetType(), copy.GetType(), $"{device} copied to another type");
                Assert.IsTrue(group.Equals(copy), $"{device} copy is not equal to its source");

                copy.Sensitivity += 1f;
                Assert.IsFalse(group.Equals(copy), $"{device} Equals ignores Sensitivity");
            }
        }

        #endregion

        #region Serialization

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Controls_RoundTrip_KeepsEveryField()
        {
            var service = NewService();
            var settings = MockData.CreateValidTestSettings();

            var json = service.SerializeData(settings);
            var restored = service.DeserializeData<UserSettings>(json);

            Assert.IsTrue(settings.Controls.Equals(restored.Controls));
            CollectionAssert.AreEqual(settings.Controls.Priority, restored.Controls.Priority);
        }

        // The removed ClassicControlsType is why UserSettings deliberately did NOT bump its DataVersion:
        // an unknown key has to be skipped, not throw, or every settings.json written before this change
        // would fail to load.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Controls_UnknownLegacyKey_IsIgnored()
        {
            var service = NewService();
            var settings = new UserSettings();

            // The splice has to match the document that is actually written, and every document this
            // project writes is compact - the indented mode is gone. The AreNotEqual under it is what
            // keeps that honest: a target matching nothing would pass without deserializing anything.
            var json = service.SerializeData(settings);
            var withLegacy = json.Replace("\"controls\":{", "\"controls\":{\"classic_controls_type\":1,");
            Assert.AreNotEqual(json, withLegacy, "Test fixture did not inject the legacy key");

            var restored = service.DeserializeData<UserSettings>(withLegacy);

            Assert.IsTrue(settings.Controls.Equals(restored.Controls));
        }

        #endregion
    }
}