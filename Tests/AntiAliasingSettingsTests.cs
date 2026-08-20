using BH.SDK.Models;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.SettingGroups.Graphics;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Two things here are worth a test rather than a comment. First, the group shipped WITHOUT a
    // UserSettings version bump - a settings.json written before it existed has no "aa" key at all
    // and must come back holding the defaults, which is only true because the constructor supplies
    // them (the same additive shape LevelSettings.Seed and GameEvents.Beats were added in). Second,
    // MsaaType's value IS its sample count except for None, whose 0 has to read back as 1 - a cast
    // instead of ToSampleCount would silently ask a graphics API for zero samples per pixel.

    /// <summary> AntiAliasingGraphicsSettings: defaults, boilerplate, sample-count conversion, and
    /// what an older settings file deserializes to. </summary>
    public class AntiAliasingSettingsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreMsaaX2WithoutHdr()
        {
            var settings = new AntiAliasingGraphicsSettings();

            Assert.AreEqual(AntiAliasingType.Msaa, settings.Type);
            Assert.AreEqual(MsaaType.X2, settings.Msaa);
            Assert.IsFalse(settings.Hdr);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_RestoresDefaults()
        {
            var settings = new AntiAliasingGraphicsSettings(AntiAliasingType.Fxaa, MsaaType.X8, true);

            settings.Reset();

            Assert.AreEqual(AntiAliasingType.Msaa, settings.Type);
            Assert.AreEqual(MsaaType.X2, settings.Msaa);
            Assert.IsFalse(settings.Hdr);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CopyAndPull_CarryEveryField()
        {
            var source = new AntiAliasingGraphicsSettings(AntiAliasingType.Fxaa, MsaaType.X4, true);

            var copy = source.Copy();
            Assert.IsTrue(source.Equals(copy));

            var pulled = new AntiAliasingGraphicsSettings();
            pulled.Pull(source);
            Assert.IsTrue(source.Equals(pulled));
            Assert.AreEqual(source.GetHashCode(), pulled.GetHashCode());
        }

        // Three fields, so a Copy that folded two of them together would still pass the test above.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Equals_SeesEachFieldIndependently()
        {
            var a = new AntiAliasingGraphicsSettings();

            var b = a.Copy();
            b.Type = AntiAliasingType.Fxaa;
            Assert.IsFalse(a.Equals(b));

            var c = a.Copy();
            c.Msaa = MsaaType.X8;
            Assert.IsFalse(a.Equals(c));

            var d = a.Copy();
            d.Hdr = true;
            Assert.IsFalse(a.Equals(d));
        }

        // The sample count a graphics API is handed: one per pixel is what "no multisampling" is,
        // and it is never zero.
        [TestCase(MsaaType.None, 1)]
        [TestCase(MsaaType.X2, 2)]
        [TestCase(MsaaType.X4, 4)]
        [TestCase(MsaaType.X8, 8)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ToSampleCount_MapsEveryRung(MsaaType type, int expected)
        {
            Assert.AreEqual(expected, type.ToSampleCount());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void UserSettings_RoundTrip_KeepsAntiAliasing()
        {
            var service = new SerializationService(new SerializationSettings());

            var settings = new UserSettings();
            settings.Graphics.AntiAliasing.Type = AntiAliasingType.Fxaa;
            settings.Graphics.AntiAliasing.Msaa = MsaaType.X8;
            settings.Graphics.AntiAliasing.Hdr = true;

            var json = service.SerializeData(settings);
            var restored = service.DeserializeData<UserSettings>(json);

            Assert.AreEqual(AntiAliasingType.Fxaa, restored.Graphics.AntiAliasing.Type);
            Assert.AreEqual(MsaaType.X8, restored.Graphics.AntiAliasing.Msaa);
            Assert.IsTrue(restored.Graphics.AntiAliasing.Hdr);
        }

        // What a settings.json written before this group existed deserializes to. The key is simply
        // absent there, so the constructor's own values have to survive - not null, and not a zeroed
        // Type/Msaa pair, which would read as "no anti-aliasing" for every existing player.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void UserSettings_WithoutAntiAliasingKey_DeserializesToDefaults()
        {
            var service = new SerializationService(new SerializationSettings());

            var json = service.SerializeData(new UserSettings());
            var stripped = json.Replace($"\"{Names.AntiAliasing}\":", "\"unused_aa\":");

            var restored = service.DeserializeData<UserSettings>(stripped);

            Assert.IsNotNull(restored.Graphics.AntiAliasing);
            Assert.AreEqual(AntiAliasingType.Msaa, restored.Graphics.AntiAliasing.Type);
            Assert.AreEqual(MsaaType.X2, restored.Graphics.AntiAliasing.Msaa);
            Assert.IsFalse(restored.Graphics.AntiAliasing.Hdr);
        }
    }
}
