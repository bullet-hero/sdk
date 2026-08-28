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
            var settings = new InterfaceSettings(true, true, 0.25f, 0.75f, MenuBackgroundKind.Shapes);

            settings.Reset();

            Assert.AreEqual(new InterfaceSettings(), settings);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CopyAndPull_CarryEveryField()
        {
            var source = new InterfaceSettings(true, true, 0.25f, 0.75f, MenuBackgroundKind.Shapes);

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
            Assert.AreNotEqual(new InterfaceSettings(false, false, 0f, 1f, MenuBackgroundKind.Bot),
                new InterfaceSettings(true, false, 0f, 1f, MenuBackgroundKind.Bot));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsEveryField()
        {
            var source = new InterfaceSettings(true, true, 0.25f, 0.75f, MenuBackgroundKind.Shapes);

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
    }
}