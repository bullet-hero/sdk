using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Models.SettingGroups.Graphics;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // This group is the device's half of how a level's images are loaded, and the property that
    // carries the whole design is that every field defaults to Auto: a settings.json written before
    // the group existed has no "textures" key at all, so Newtonsoft leaves the constructor's values
    // in place, and Auto is what resolves per platform. A default of Off or On instead would make
    // an older file assert a device policy nobody chose - which is why the default is pinned here
    // rather than left to whoever edits the class next.

    public class TexturesGraphicsSettingsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreAuto()
        {
            var settings = new TexturesGraphicsSettings();

            Assert.AreEqual(TextureCompressionMode.Auto, settings.Compression);
            Assert.AreEqual(TextureSizeLimit.Auto, settings.SizeLimit);
            Assert.AreEqual(TextureMipmapMode.Auto, settings.Mipmaps);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_RestoresAuto()
        {
            var settings = new TexturesGraphicsSettings(TextureCompressionMode.On,
                TextureSizeLimit.Side1024, TextureMipmapMode.Off);

            settings.Reset();

            Assert.AreEqual(new TexturesGraphicsSettings(), settings);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CopyAndPull_CarryEveryField()
        {
            var source = new TexturesGraphicsSettings(TextureCompressionMode.Off,
                TextureSizeLimit.Side4096, TextureMipmapMode.On);

            var copy = source.Copy();
            var pulled = new TexturesGraphicsSettings();
            pulled.Pull(source);

            Assert.AreEqual(source, copy);
            Assert.AreEqual(source, pulled);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsEveryField()
        {
            var source = new TexturesGraphicsSettings(TextureCompressionMode.On,
                TextureSizeLimit.Side2048, TextureMipmapMode.Off);

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<TexturesGraphicsSettings>(json);

            Assert.AreEqual(source, restored);
        }

        // The additive shape the whole group relies on: an older file simply has no key here.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GraphicsSettings_WithoutTheGroup_ReadsBackAsAuto()
        {
            var settings = JsonConvert.DeserializeObject<GraphicsSettings>("{}");

            Assert.IsNotNull(settings.Textures);
            Assert.AreEqual(new TexturesGraphicsSettings(), settings.Textures);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Equality_SeesEveryField()
        {
            var settings = new TexturesGraphicsSettings();

            Assert.AreNotEqual(settings, new TexturesGraphicsSettings(TextureCompressionMode.On,
                TextureSizeLimit.Auto, TextureMipmapMode.Auto));
            Assert.AreNotEqual(settings, new TexturesGraphicsSettings(TextureCompressionMode.Auto,
                TextureSizeLimit.Side1024, TextureMipmapMode.Auto));
            Assert.AreNotEqual(settings, new TexturesGraphicsSettings(TextureCompressionMode.Auto,
                TextureSizeLimit.Auto, TextureMipmapMode.On));
        }
    }
}
