using BH.SDK.Models;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Serialization;
using BH.SDK.Versions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> That a settings.json 1.0.0 wrote still opens after the first post-release bump - its
    /// groups carried, cursor_return dropped, the tutorial flag unset - and that today's shape keeps
    /// the flag. </summary>
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

        // 1.0.0 wrote cursor_return into every settings file, and the default was the one that let
        // the avatar walk on after the button was up. Whichever way it was set, the controls group
        // has to arrive with everything else in it intact - the switch is simply gone.
        [TestCase(true)]
        [TestCase(false)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AReleaseFile_DropsCursorReturnAndKeepsTheRestOfTheControls(bool cursorReturn)
        {
            var service = new SerializationService();
            var json = ReleaseShaped(service, TouchscreenControlMode.Absolute, cursorReturn);

            var settings = service.DeserializeData<UserSettings>(json);

            Assert.AreEqual(TouchscreenControlMode.Absolute, settings.Controls.Touchscreen.Mode);
            Assert.AreEqual(1.5f, settings.Controls.Common.CursorScale);
            Assert.IsFalse(settings.Controls.Common.CursorRecenter);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TodaysFile_KeepsTheFlag()
        {
            var service = new SerializationService();
            var json = service.SerializeData(new UserSettings { TutorialCompleted = true });

            Assert.AreEqual(ModelGenerations.Tutorial, (int)JObject.Parse(json)["g"]);
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
            root["g"] = ModelGenerations.Release;
            var value = (JObject)root["v"];
            value.Remove(Names.TutorialCompleted);
            ((JObject)value[Names.Controls][Names.Common])["cursor_return"] = cursorReturn;
            return root.ToString();
        }
    }
}
