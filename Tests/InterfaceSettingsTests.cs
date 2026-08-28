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
        // three additive with no DataVersion bump.
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

        // Horizontal is the default while Unlock is the zero value, and here that ordering is
        // load-bearing rather than tidy: a file written before the field must not read back as
        // Unlock, because Unlock is free rotation on screens that have no portrait layout yet. The
        // same absent-key mechanism MenuBackground relies on is what guarantees it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_LockTheScreenHorizontally()
        {
            Assert.AreEqual(ScreenOrientationLock.Horizontal,
                new InterfaceSettings().ScreenOrientation);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SettingsWrittenBeforeTheField_ReadBackLockedHorizontally()
        {
            var settings = JsonConvert.DeserializeObject<InterfaceSettings>(
                "{\"open_menu_on_lose\":false,\"stats_active\":true," +
                "\"stats_alignment_x\":0.5,\"stats_alignment_y\":0.5,\"menu_background\":2}");

            Assert.AreEqual(ScreenOrientationLock.Horizontal, settings.ScreenOrientation);
            Assert.IsTrue(settings.StatsActive);
        }
    }
}