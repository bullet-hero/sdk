using BH.SDK.Models;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.SettingGroups;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // OpenMenuOnLose defaults FALSE (a lost run rewinds to the last checkpoint); no bump, an old file has
    // no key and the constructor's false stays.


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

        // The three HUD switches default TRUE with zero FALSE, so an older file reads with the HUD SHOWN.
        // They ride an object initializer in Copy(), which CopyAndPull_CarryEveryField cannot catch.
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

        // MenuBackgroundKind's zero is None but its default is Bot; an absent key is never written, so
        // the constructor's Bot survives.
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

        // The three overlay blocks default to zero (additive); they are three SEPARATE answers.

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

            // The one block that used to be drawn whatever the file said; the loss is deliberate.
            Assert.IsFalse(settings.StatsMemory);
        }

        // The default is Unlock: screens lay out from `.portrait`. LevelOrientation still outranks it
        // while a level runs, so freeing it frees the menu, the browser and settings alone.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_LeaveTheScreenFreeToRotate()
        {
            Assert.AreEqual(ScreenOrientationLock.Unlock,
                new InterfaceSettings().ScreenOrientation);
        }

        // Unlock is the zero value now, so this proves the mechanism only via the present StatsActive key.
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