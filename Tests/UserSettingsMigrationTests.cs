using BH.SDK.Models;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Serialization;
using BH.SDK.Versions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> That a settings.json 1.0.0 wrote still opens after the first post-release bump - its
    /// groups carried, cursor_return moved into the pointer groups, the tutorial flag unset - and that
    /// today's shape keeps both. </summary>
    [TestFixture]
    public class UserSettingsMigrationTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AReleaseFile_MigratesWithItsGroupsAndTheFlagUnset()
        {
            var service = new SerializationService();
            var json = ReleaseShaped(service, TouchscreenControlMode.Absolute);

            var report = new SerializationReport();
            UserSettings settings;
            using (SerializationReport.Begin(report)) settings = service.DeserializeData<UserSettings>(json);

            // The root reader reports only a chain that did NOT complete; a mode other than the default
            // arriving intact is what proves the groups went through the migrator rather than around it.
            Assert.IsFalse(settings.TutorialCompleted);
            Assert.AreNotEqual(TouchscreenControlMode.Absolute, new UserSettings().Controls.Touchscreen.Mode);
            Assert.AreEqual(TouchscreenControlMode.Absolute, settings.Controls.Touchscreen.Mode);
            Assert.That(report.Entries, Has.None.Matches<SerializationSubstitution>(e =>
                e.Domain == ModelDomains.UserSettings));
        }

        // 1.0.0 wrote ONE cursor_return into every settings file, default off - the default that let
        // the avatar walk on after the button was up, which playtests read as a bug on a mouse. Off
        // therefore lands on each device's own default (mouse on, touch off); on stays on for both.
        [TestCase(true, true, true)]
        [TestCase(false, true, false)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AReleaseFile_MovesCursorReturnIntoThePointerGroups(bool cursorReturn,
            bool expectedMouse, bool expectedTouch)
        {
            var service = new SerializationService();
            var json = ReleaseShaped(service, TouchscreenControlMode.Absolute, cursorReturn);

            var settings = service.DeserializeData<UserSettings>(json);

            Assert.AreEqual(TouchscreenControlMode.Absolute, settings.Controls.Touchscreen.Mode);
            Assert.AreEqual(1.5f, settings.Controls.Common.CursorScale);
            Assert.IsFalse(settings.Controls.Common.CursorRecenter);
            Assert.AreEqual(expectedMouse, settings.Controls.KeyboardMouse.CursorReturn);
            Assert.AreEqual(expectedTouch, settings.Controls.Touchscreen.CursorReturn);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TodaysFile_KeepsCursorReturnPerDevice()
        {
            var service = new SerializationService();
            var settings = new UserSettings();
            settings.Controls.KeyboardMouse.CursorReturn = false;
            settings.Controls.Touchscreen.CursorReturn = true;

            var read = service.DeserializeData<UserSettings>(service.SerializeData(settings));

            Assert.IsFalse(read.Controls.KeyboardMouse.CursorReturn);
            Assert.IsTrue(read.Controls.Touchscreen.CursorReturn);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TodaysFile_KeepsTheFlag()
        {
            var service = new SerializationService();
            var json = service.SerializeData(new UserSettings { TutorialCompleted = true });

            Assert.AreEqual(ModelGenerations.V2_SimplifyEntrance, (int)JObject.Parse(json)["g"]);
            Assert.IsTrue(service.DeserializeData<UserSettings>(json).TutorialCompleted);
        }

        /// <summary> A real write of today's settings with the flag's key removed and the envelope
        /// stamped Release - exactly what 1.0.0 put on disk. </summary>
        private static string ReleaseShaped(SerializationService service, TouchscreenControlMode mode,
            bool cursorReturn = false)
        {
            var settings = new UserSettings();
            settings.Controls.Touchscreen.Mode = mode;
            settings.Controls.Common.CursorScale = 1.5f;
            settings.Controls.Common.CursorRecenter = false;

            var root = JObject.Parse(service.SerializeData(settings));
            root["g"] = ModelGenerations.V1_AlphaRelease;
            var value = (JObject)root["v"];
            value.Remove(Names.TutorialCompleted);
            var controls = (JObject)value[Names.Controls];
            ((JObject)controls[Names.Common])["cursor_return"] = cursorReturn;
            ((JObject)controls[Names.KeyboardMouse]).Remove(Names.CursorReturn);
            ((JObject)controls[Names.Touchscreen]).Remove(Names.CursorReturn);
            return root.ToString();
        }
    }
}
