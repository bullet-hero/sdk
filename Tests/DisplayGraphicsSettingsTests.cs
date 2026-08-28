using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Graphics;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The group is desktop-only, so what these pin is not behaviour but the two claims that let it
    // ship without a DataVersion bump: the defaults are what the game already does (FullScreenWindow
    // is ProjectSettings.asset's own fullscreenMode, native resolution, no render scaling), and a
    // settings file written before the group existed reads back as exactly those.
    //
    // NativeResolution is a zero SENTINEL, not a resolution: HasResolution is the only thing allowed
    // to ask whether one was authored, the same never-a-literal discipline LevelRules.IsValidSeed
    // keeps for the seed.

    public class DisplayGraphicsSettingsTests
    {
        private static DisplayGraphicsSettings Authored() =>
            new(WindowMode.Windowed, 1080, 1920, 0.75f);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_MatchWhatTheProjectAlreadyShips()
        {
            var settings = new DisplayGraphicsSettings();

            Assert.AreEqual(WindowMode.FullScreenWindow, settings.WindowMode);
            Assert.AreEqual(DisplayGraphicsSettings.NativeResolution, settings.ResolutionWidth);
            Assert.AreEqual(DisplayGraphicsSettings.NativeResolution, settings.ResolutionHeight);
            Assert.AreEqual(1f, settings.RenderScale);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void HasResolution_IsFalseUntilBothSidesAreAuthored()
        {
            Assert.IsFalse(new DisplayGraphicsSettings().HasResolution());
            Assert.IsFalse(new DisplayGraphicsSettings(WindowMode.Windowed, 1920, 0, 1f).HasResolution());
            Assert.IsFalse(new DisplayGraphicsSettings(WindowMode.Windowed, 0, 1080, 1f).HasResolution());
            Assert.IsTrue(new DisplayGraphicsSettings(WindowMode.Windowed, 1920, 1080, 1f).HasResolution());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CopyPullAndUpdate_CarryEveryField()
        {
            var source = Authored();

            var copy = source.Copy();
            var pulled = new DisplayGraphicsSettings();
            pulled.Pull(source);
            var updated = new DisplayGraphicsSettings();
            updated.Update(source);

            Assert.AreEqual(source, copy);
            Assert.AreEqual(source, pulled);
            Assert.AreEqual(source, updated);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_RestoresTheDefaults()
        {
            var settings = Authored();

            settings.Reset();

            Assert.AreEqual(new DisplayGraphicsSettings(), settings);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsEveryField()
        {
            var source = Authored();

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<DisplayGraphicsSettings>(json);

            Assert.AreEqual(source, restored);
        }

        // The claim that made the group additive: GraphicsSettings' constructor builds one, so an
        // absent "display" key leaves that instance and all four of its defaults in place.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GraphicsWrittenBeforeTheGroup_ReadsBackAsTheDefault()
        {
            var settings = JsonConvert.DeserializeObject<GraphicsSettings>(
                "{\"framerate_target\":0,\"fixed_framerate\":60}");

            Assert.IsNotNull(settings.Display);
            Assert.AreEqual(WindowMode.FullScreenWindow, settings.Display.WindowMode);
            Assert.IsFalse(settings.Display.HasResolution());
            Assert.AreEqual(1f, settings.Display.RenderScale);
        }
    }
}
