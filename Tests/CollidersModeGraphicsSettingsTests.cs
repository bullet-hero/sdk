using BH.SDK.Models;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Graphics;
using BH.SDK.Models.Values;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> The Colliders Only mode group: its defaults (off, the red fill, alpha used, a dark grey
    /// background), that a round trip keeps every member, and that a settings file written before the
    /// group reads back as those defaults. </summary>
    public class CollidersModeGraphicsSettingsTests
    {
        // EVERY MEMBER HERE DIFFERS FROM ITS DEFAULT, which is the only reason a round trip over this
        // value proves anything.
        private static CollidersModeGraphicsSettings Authored() =>
            new(true, new Color4Value(0.1f, 0.2f, 0.3f, 0.4f), false, new Color3Value(0.5f, 0.6f, 0.7f));

        private static void AssertDefaults(CollidersModeGraphicsSettings settings)
        {
            Assert.IsFalse(settings.Active);
            Assert.IsTrue(settings.UseAlpha);
            Assert.AreEqual(CollidersModeGraphicsSettings.DefaultColor(), settings.Color);
            Assert.AreEqual(CollidersModeGraphicsSettings.DefaultBackground(), settings.Background);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreOffDefaultColoursAndAlphaOn()
        {
            AssertDefaults(new CollidersModeGraphicsSettings());
            AssertDefaults(new GraphicsSettings().CollidersMode);
        }

        // Black is what the letterbox around the camera is, so a black default would hide the frame.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DefaultBackground_IsNotBlack()
        {
            var background = CollidersModeGraphicsSettings.DefaultBackground();

            Assert.Greater(background.R + background.G + background.B, 0f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void JsonRoundTrip_KeepsEveryMember()
        {
            var source = Authored();

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<CollidersModeGraphicsSettings>(json);

            Assert.AreEqual(source, restored);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CopyPullAndReset_CarryEveryMember()
        {
            var source = Authored();

            var copy = source.Copy();
            var pulled = new CollidersModeGraphicsSettings();
            pulled.Pull(source);
            var reset = Authored();
            reset.Reset();

            Assert.AreEqual(source, copy);
            Assert.AreEqual(source, pulled);
            AssertDefaults(reset);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GraphicsWrittenBeforeTheGroup_ReadsBackTheDefaults()
        {
            var settings = JsonConvert.DeserializeObject<UserSettings>("{\"graphics\":{}}");

            AssertDefaults(settings.Graphics.CollidersMode);
        }
    }
}
