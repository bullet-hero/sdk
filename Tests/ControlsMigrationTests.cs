using System;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Rules;
using BH.SDK.Serialization;
using BH.SDK.Versions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> That the controls part of a settings.json 1.0.0 wrote survives user-settings
    /// generation 2: the priority, the manual pick, the pad's dash buttons and every form of
    /// motion-sensor locality dropped, the sensor's Relative mode mapped onto Direction, and its tilt
    /// angle carried over as the sensitivity that reaches full deflection at the same tilt. </summary>
    [TestFixture]
    public class ControlsMigrationTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AReleaseFile_DropsTheRemovedControlsAndKeepsTheRest()
        {
            var settings = Migrate(ReleaseShaped(DeviceGyroControlModeV1Relative, 20f));

            Assert.AreEqual(1.5f, settings.Controls.Gamepad.ResponseCurve);
            Assert.AreEqual(0.3f, settings.Controls.DeviceGyro.DeadZone, 1e-6f);
            Assert.IsTrue(settings.Controls.DeviceGyro.InvertX);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AReleaseFile_RelativeGyro_BecomesDirection()
        {
            var settings = Migrate(ReleaseShaped(DeviceGyroControlModeV1Relative, 20f));

            Assert.AreEqual(DeviceGyroControlMode.Direction, settings.Controls.DeviceGyro.Mode);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AReleaseFile_AbsoluteGyro_StaysAbsolute()
        {
            var settings = Migrate(ReleaseShaped((int)DeviceGyroControlMode.Absolute, 20f));

            Assert.AreEqual(DeviceGyroControlMode.Absolute, settings.Controls.DeviceGyro.Mode);
        }

        // The default angle is what a sensitivity of 1 means, a smaller angle is a more sensitive
        // sensor, and both ends stay inside the range the rule allows.
        [TestCase(20f, 1f)]
        [TestCase(10f, 1.9696f)]
        [TestCase(40f, 0.5321f)]
        [TestCase(90f, 0.3420f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AReleaseFile_TiltAngle_BecomesTheMatchingSensitivity(float maxTiltAngle, float expected)
        {
            var settings = Migrate(ReleaseShaped((int)DeviceGyroControlMode.Direction, maxTiltAngle));

            Assert.AreEqual(expected, settings.Controls.DeviceGyro.Sensitivity, 1e-3f);
            Assert.That(settings.Controls.DeviceGyro.Sensitivity,
                Is.InRange(ControlsRules.MinSensitivity, ControlsRules.MaxSensitivity));
        }

        private const int DeviceGyroControlModeV1Relative = 1;

        private static UserSettings Migrate(string json)
        {
            var service = new SerializationService();
            var report = new SerializationReport();
            UserSettings settings;
            using (SerializationReport.Begin(report)) settings = service.DeserializeData<UserSettings>(json);

            Assert.That(report.Entries, Has.None.Matches<SerializationSubstitution>(e =>
                e.Domain == ModelDomains.UserSettings), "the migration chain did not complete");
            return settings;
        }

        /// <summary> A real write of today's settings, stamped Release and given back every controls
        /// key generation 2 removed - what 1.0.0 put on disk. </summary>
        private static string ReleaseShaped(int gyroMode, float maxTiltAngle)
        {
            var service = new SerializationService();
            var settings = new UserSettings();
            settings.Controls.Gamepad.ResponseCurve = 1.5f;
            settings.Controls.DeviceGyro.DeadZone = 0.3f;
            settings.Controls.DeviceGyro.InvertX = true;

            var root = JObject.Parse(service.SerializeData(settings));
            root["g"] = ModelGenerations.V1_AlphaRelease;
            var value = (JObject)root["v"];
            value.Remove(Names.TutorialCompleted);

            var controls = (JObject)value[Names.Controls];
            controls["priority"] = new JArray(2, 0, 3, 1);

            var common = (JObject)controls[Names.Common];
            common["selection"] = 1;
            common["manual_device"] = 2;
            common["cursor_return"] = false;

            ((JObject)controls[Names.Gamepad])["dash_buttons"] = 2 | 64;

            var gyro = (JObject)controls[Names.DeviceGyro];
            gyro[Names.Mode] = gyroMode;
            gyro["axis_mapping"] = 0;
            gyro["calibrate_on_start"] = false;
            gyro["tilt_cntr_x"] = 0.25f;
            gyro["tilt_cntr_y"] = -0.25f;
            gyro["max_tilt_ang"] = maxTiltAngle;
            return root.ToString();
        }
    }
}
