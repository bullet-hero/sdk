using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Graphics;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The group is desktop-only, so what these pin is not behaviour but the two claims that let it
    // ship without a generation bump: the defaults are what the game already does (Windowed is
    // ProjectSettings.asset's own fullscreenMode, native resolution, no render scaling, vsync off -
    // which is what SettingsApplier used to write unconditionally), and a settings file written
    // before the group, or before VSync joined it, reads back as exactly those.
    //
    // THE WINDOW DEFAULT MOVED, and this is the second half of that change rather than a test being
    // repaired around it: it was FullScreenWindow, matching a fullscreenMode of 1, and both went to
    // Windowed together because the game may never put ITSELF full screen. A fresh install used to
    // be handed a mode nobody picked. The assert stays worded as "what the project already ships",
    // because that is still exactly what it checks - the project ships something else now.
    //
    // NativeResolution is a zero SENTINEL, not a resolution: HasResolution is the only thing allowed
    // to ask whether one was authored, the same never-a-literal discipline LevelRules.IsValidSeed
    // keeps for the seed.


    /// <summary> The desktop display group: that its defaults are what the game already did, that an older
    /// settings file reads back as exactly those, and that a zero resolution is a sentinel rather than a size. </summary>
    public class DisplayGraphicsSettingsTests
    {
        // EVERY MEMBER HERE DIFFERS FROM ITS DEFAULT, which is the only reason a round trip over this
        // value proves anything. The mode is FullScreenWindow rather than Windowed for exactly that:
        // Windowed became the default, and an "authored" value equal to the default cannot tell a
        // copy that carried it from one that never read it.
        private static DisplayGraphicsSettings Authored() =>
            new(WindowMode.FullScreenWindow, 1080, 1920, 0.75f, VSyncMode.Half);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_MatchWhatTheProjectAlreadyShips()
        {
            var settings = new DisplayGraphicsSettings();

            Assert.AreEqual(WindowMode.Windowed, settings.WindowMode);
            Assert.AreEqual(DisplayGraphicsSettings.NativeResolution, settings.ResolutionWidth);
            Assert.AreEqual(DisplayGraphicsSettings.NativeResolution, settings.ResolutionHeight);
            Assert.AreEqual(1f, settings.RenderScale);
            Assert.AreEqual(VSyncMode.Off, settings.VSync);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void HasResolution_IsFalseUntilBothSidesAreAuthored()
        {
            Assert.IsFalse(new DisplayGraphicsSettings().HasResolution());
            Assert.IsFalse(new DisplayGraphicsSettings(WindowMode.Windowed, 1920, 0, 1f, VSyncMode.Off).HasResolution());
            Assert.IsFalse(new DisplayGraphicsSettings(WindowMode.Windowed, 0, 1080, 1f, VSyncMode.Off).HasResolution());
            Assert.IsTrue(new DisplayGraphicsSettings(WindowMode.Windowed, 1920, 1080, 1f, VSyncMode.Off).HasResolution());
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
            Assert.AreEqual(WindowMode.Windowed, settings.Display.WindowMode);
            Assert.IsFalse(settings.Display.HasResolution());
            Assert.AreEqual(1f, settings.Display.RenderScale);
            Assert.AreEqual(VSyncMode.Off, settings.Display.VSync);
        }

        // The same claim one level down, for the field that joined the group after it shipped: a
        // display object with every other key present and no "vsync" is what an existing install
        // holds, and Off is what the game did before the setting existed.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void DisplayWrittenBeforeVSync_ReadsBackAsOff()
        {
            var settings = JsonConvert.DeserializeObject<DisplayGraphicsSettings>(
                "{\"window_mode\":1,\"resolution_width\":1920,\"resolution_height\":1080,\"render_sca\":1.0}");

            Assert.AreEqual(VSyncMode.Off, settings.VSync);

            // 1 is FullScreenWindow, deliberately not the constructor's own default: a mode equal to
            // the default would pass this assert even if the key were never read at all.
            Assert.AreEqual(WindowMode.FullScreenWindow, settings.WindowMode);
            Assert.AreEqual(1f, settings.RenderScale);
        }
    }
}