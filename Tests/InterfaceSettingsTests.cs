using BH.SDK.Models;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.SettingGroups;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // OpenMenuOnLose defaults to FALSE, and that default is the behaviour rather than a preference
    // about it: off, a lost run rewinds itself to the last checkpoint it reached, which is what a
    // player retrying a hard section wants and what the result window used to stand between them
    // and. On is for reading the outcome, changing a setting or leaving.
    //
    // It was added after UserSettings was already at 2.0 and deliberately does not bump it - a file
    // written before it simply has no key, and Newtonsoft leaves the constructor's false in place.
    // That claim is what the deserialization case below pins.


    /// <summary> The interface group, and chiefly that OpenMenuOnLose's false default survives a settings file
    /// written before the key existed - which is what let it ship without a version bump. </summary>
    public class InterfaceSettingsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_LeaveTheResultWindowClosed()
        {
            Assert.IsFalse(new InterfaceSettings().OpenMenuOnLose);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_RestoresTheDefault()
        {
            var settings = new InterfaceSettings(true, true, 0.25f, 0.75f, MenuBackgroundKind.Shapes,
                ScreenOrientationLock.Vertical);

            settings.Reset();

            Assert.AreEqual(new InterfaceSettings(), settings);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CopyAndPull_CarryEveryField()
        {
            var source = new InterfaceSettings(true, true, 0.25f, 0.75f, MenuBackgroundKind.Shapes,
                ScreenOrientationLock.Vertical);

            var copy = source.Copy();
            var pulled = new InterfaceSettings();
            pulled.Pull(source);

            Assert.AreEqual(source, copy);
            Assert.AreEqual(source, pulled);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Equality_SeesTheNewField()
        {
            Assert.AreNotEqual(
                new InterfaceSettings(false, false, 0f, 1f, MenuBackgroundKind.Bot,
                    ScreenOrientationLock.Horizontal),
                new InterfaceSettings(true, false, 0f, 1f, MenuBackgroundKind.Bot,
                    ScreenOrientationLock.Horizontal));

            // The orientation alone, so a Copy or an Equals that forgot it cannot pass on the back
            // of one of the five fields that came before.
            Assert.AreNotEqual(
                new InterfaceSettings(false, false, 0f, 1f, MenuBackgroundKind.Bot,
                    ScreenOrientationLock.Horizontal),
                new InterfaceSettings(false, false, 0f, 1f, MenuBackgroundKind.Bot,
                    ScreenOrientationLock.Vertical));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsEveryField()
        {
            var source = new InterfaceSettings(true, true, 0.25f, 0.75f, MenuBackgroundKind.Shapes,
                ScreenOrientationLock.Vertical);

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<InterfaceSettings>(json);

            Assert.AreEqual(source, restored);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SettingsWrittenBeforeTheField_ReadBackWithTheResultWindowClosed()
        {
            var settings = JsonConvert.DeserializeObject<InterfaceSettings>(
                "{\"stats_active\":true,\"stats_alignment_x\":0.5,\"stats_alignment_y\":0.5}");

            Assert.IsFalse(settings.OpenMenuOnLose);
            Assert.IsTrue(settings.StatsActive);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void UserSettings_WithoutTheGroup_ReadsBackAsTheDefault()
        {
            var settings = JsonConvert.DeserializeObject<UserSettings>("{}");

            Assert.IsNotNull(settings.Interface);
            Assert.IsFalse(settings.Interface.OpenMenuOnLose);
        }

        // The three HUD switches. Two things about them are worth a test each and neither is
        // caught by anything already here.
        //
        // TRUE IS THE DEFAULT AND FALSE IS THE ZERO VALUE, so an older settings.json - which
        // carries none of the three keys - has to read back with the HUD SHOWN. That is the same
        // mechanism MenuBackgroundKind.Bot relies on one block down, and it is what makes all
        // three additive with no generation bump.
        //
        // AND THEY RIDE AN OBJECT INITIALIZER IN Copy() rather than the constructor, because a
        // seventh parameter would break every caller. An initializer is the one shape a
        // copy-paste silently drops, and CopyAndPull_CarryEveryField above cannot catch it: it
        // builds its source through the constructor, which defaults all three to true, so a Copy
        // that lost them would still compare equal.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_ShowTheWholeHud()
        {
            var settings = new InterfaceSettings();

            Assert.IsTrue(settings.ShowGameProgress);
            Assert.IsTrue(settings.ShowGamePause);
            Assert.IsTrue(settings.ShowGameInterface);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CopyPullAndUpdate_CarryTheHudFlags()
        {
            var source = new InterfaceSettings
            {
                ShowGameProgress = false,
                ShowGamePause = false,
                ShowGameInterface = false,
            };

            var copy = source.Copy();
            var pulled = new InterfaceSettings();
            pulled.Pull(source);
            var updated = new InterfaceSettings();
            updated.Update(source);

            Assert.AreEqual(source, copy);
            Assert.AreEqual(source, pulled);
            Assert.AreEqual(source, updated);
            Assert.IsFalse(copy.ShowGameProgress);
            Assert.IsFalse(copy.ShowGamePause);
            Assert.IsFalse(copy.ShowGameInterface);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Equality_SeesEachHudFlagOnItsOwn()
        {
            Assert.AreNotEqual(new InterfaceSettings(),
                new InterfaceSettings { ShowGameProgress = false });
            Assert.AreNotEqual(new InterfaceSettings(),
                new InterfaceSettings { ShowGamePause = false });
            Assert.AreNotEqual(new InterfaceSettings(),
                new InterfaceSettings { ShowGameInterface = false });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SettingsWrittenBeforeTheFlags_ReadBackWithTheHudShown()
        {
            var settings = JsonConvert.DeserializeObject<InterfaceSettings>(
                "{\"stats_active\":true}");

            Assert.IsTrue(settings.ShowGameProgress);
            Assert.IsTrue(settings.ShowGamePause);
            Assert.IsTrue(settings.ShowGameInterface);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsTheHudFlags()
        {
            var source = new InterfaceSettings { ShowGamePause = false };

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<InterfaceSettings>(json);

            Assert.AreEqual(source, restored);
            Assert.IsFalse(restored.ShowGamePause);
            Assert.IsTrue(restored.ShowGameProgress);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_DrawTheArena()
        {
            Assert.AreEqual(MenuBackgroundKind.Bot, new InterfaceSettings().MenuBackground);
        }

        // MenuBackgroundKind's zero value is None while its default is Bot, which looks like it should
        // cost an older file its background - and does not. An absent key is never written at all, so
        // the constructor's Bot survives; nothing here ever sees a zero. This is the same mechanism
        // OpenMenuOnLose relies on, and it is what makes ordering the members the way the three modes
        // are OFFERED free rather than a trade.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SettingsWrittenBeforeTheField_ReadBackWithTheArena()
        {
            var settings = JsonConvert.DeserializeObject<InterfaceSettings>(
                "{\"open_menu_on_lose\":false,\"stats_active\":true," +
                "\"stats_alignment_x\":0.5,\"stats_alignment_y\":0.5}");

            Assert.AreEqual(MenuBackgroundKind.Bot, settings.MenuBackground);
            Assert.IsTrue(settings.StatsActive);
        }

        // THE THREE OVERLAY BLOCKS. Their default is the zero value, which is what makes them
        // additive with neither a generation bump nor a migrator. For two of them it is also the
        // behaviour the overlay already had; the memory block is the exception and is asserted as
        // one below, because it used to be drawn unconditionally. What is worth a test of its own
        // is that they are three SEPARATE answers: one flag standing in for another is exactly the
        // shape a copy-paste produces.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_DrawNoOverlayBlock()
        {
            var settings = new InterfaceSettings();

            Assert.IsFalse(settings.StatsFrameObjects);
            Assert.IsFalse(settings.StatsLevelObjects);
            Assert.IsFalse(settings.StatsMemory);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Equality_SeesEachOverlayBlockOnItsOwn()
        {
            Assert.AreNotEqual(new InterfaceSettings(),
                new InterfaceSettings { StatsFrameObjects = true });
            Assert.AreNotEqual(new InterfaceSettings(),
                new InterfaceSettings { StatsLevelObjects = true });
            Assert.AreNotEqual(new InterfaceSettings(),
                new InterfaceSettings { StatsMemory = true });
            Assert.AreNotEqual(new InterfaceSettings { StatsFrameObjects = true },
                new InterfaceSettings { StatsLevelObjects = true });
            Assert.AreNotEqual(new InterfaceSettings { StatsLevelObjects = true },
                new InterfaceSettings { StatsMemory = true });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CopyPullAndUpdate_CarryTheOverlayBlocks()
        {
            var source = new InterfaceSettings
            {
                StatsFrameObjects = true,
                StatsLevelObjects = true,
                StatsMemory = true,
            };

            var copy = source.Copy();
            var pulled = new InterfaceSettings();
            pulled.Pull(source);
            var updated = new InterfaceSettings();
            updated.Update(source);

            Assert.AreEqual(source, copy);
            Assert.AreEqual(source, pulled);
            Assert.AreEqual(source, updated);
            Assert.IsTrue(copy.StatsFrameObjects);
            Assert.IsTrue(copy.StatsLevelObjects);
            Assert.IsTrue(copy.StatsMemory);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsTheOverlayBlocksApart()
        {
            var source = new InterfaceSettings { StatsLevelObjects = true };

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<InterfaceSettings>(json);

            Assert.AreEqual(source, restored);
            Assert.IsTrue(restored.StatsLevelObjects);
            Assert.IsFalse(restored.StatsFrameObjects);
            Assert.IsFalse(restored.StatsMemory);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SettingsWrittenBeforeTheBlocks_ReadBackWithNoneDrawn()
        {
            var settings = JsonConvert.DeserializeObject<InterfaceSettings>(
                "{\"stats_active\":true,\"stats_alignment_x\":0.5,\"stats_alignment_y\":0.5}");

            Assert.IsTrue(settings.StatsActive);
            Assert.IsFalse(settings.StatsFrameObjects);
            Assert.IsFalse(settings.StatsLevelObjects);

            // THE ONE BLOCK THAT USED TO BE DRAWN WHATEVER THE FILE SAID. Rule 11 is what makes
            // that affordable - there is no settings.json on a player's disk yet - and asserting
            // it here is what keeps the loss deliberate rather than something noticed later on a
            // screen that stopped showing a number.
            Assert.IsFalse(settings.StatsMemory);
        }

        // THE DEFAULT REVERSED, and this pair is what pins it. It was Horizontal, because Unlock
        // meant free rotation on screens with no portrait layout; screens lay themselves out from
        // `.portrait` now, so a phone the player turns over is expected to follow. What did NOT
        // move is which side wins while a level runs - LevelOrientation still defaults to
        // Horizontal and still outranks this - so freeing it frees the menu, the browser and the
        // settings screens alone.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_LeaveTheScreenFreeToRotate()
        {
            Assert.AreEqual(ScreenOrientationLock.Unlock,
                new InterfaceSettings().ScreenOrientation);
        }

        // Unlock is also the zero value now, so this can no longer prove the absent-key mechanism on
        // its own - a missing key and a constructor default are the same answer here. It is kept
        // because the OTHER field in the same document still proves it: StatsActive is present and
        // true, so the object really was deserialized rather than left at its defaults wholesale.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SettingsWrittenBeforeTheField_ReadBackFreeToRotate()
        {
            var settings = JsonConvert.DeserializeObject<InterfaceSettings>(
                "{\"open_menu_on_lose\":false,\"stats_active\":true," +
                "\"stats_alignment_x\":0.5,\"stats_alignment_y\":0.5,\"menu_background\":2}");

            Assert.AreEqual(ScreenOrientationLock.Unlock, settings.ScreenOrientation);
            Assert.IsTrue(settings.StatsActive);
        }
    }
}