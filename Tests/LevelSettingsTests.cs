using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Rules;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THIS FILE EXISTS FOR Copy(), and one line of it. LevelSettings' four-argument constructor
    // deliberately takes neither Seed nor Orientation - every existing caller authors a level
    // without either - so both ride an object initializer instead, and an initializer is the one
    // shape a copy-paste drops silently. ModelContractTests cannot catch it: that sweep asks about
    // REFERENCES on default-constructed pairs, so a Copy that loses a value field passes it.
    //
    // Orientation's default is Horizontal while its zero value is NotSpecified, and here that is
    // load-bearing rather than tidy: every level authored before the field existed must read back as
    // Horizontal, which is how the game already played it. A NotSpecified default would have opted
    // every level that exists into a portrait screen its content was never composed for.

    public class LevelSettingsTests
    {
        private static LevelSettings Authored() =>
            new(45, 4500, 20, 3)
            {
                Seed = 12345,
                Orientation = LevelOrientation.Vertical,
            };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_ComposeForAHorizontalFrame()
        {
            Assert.AreEqual(LevelOrientation.Horizontal, new LevelSettings().Orientation);
            Assert.AreEqual(LevelOrientation.Horizontal, new LevelSettings(45, 4500, 0, 0).Orientation);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_LeaveTheSeedUnset()
        {
            Assert.AreEqual(LevelRules.NullSeed, new LevelSettings().Seed);
        }

        // The reason this file was written. Both initializer-carried fields, checked one at a time
        // so a Copy that drops exactly one of them cannot hide behind the other.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Copy_CarriesTheInitializerFields()
        {
            var copy = Authored().Copy();

            Assert.AreEqual(LevelOrientation.Vertical, copy.Orientation);
            Assert.AreEqual(12345, copy.Seed);
            Assert.AreEqual(Authored(), copy);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void UpdateAndPull_CarryTheInitializerFields()
        {
            var source = Authored();

            var updated = new LevelSettings();
            updated.Update(source);
            var pulled = new LevelSettings();
            pulled.Pull(source);

            Assert.AreEqual(LevelOrientation.Vertical, updated.Orientation);
            Assert.AreEqual(LevelOrientation.Vertical, pulled.Orientation);
            Assert.AreEqual(source, updated);
            Assert.AreEqual(source, pulled);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_RestoresTheDefaults()
        {
            var settings = Authored();

            settings.Reset();

            Assert.AreEqual(new LevelSettings(), settings);
        }

        // Orientation alone, so an Equals or a GetHashCode that forgot it cannot pass on the back of
        // the five fields that came before.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Equality_SeesTheOrientation()
        {
            var horizontal = new LevelSettings(45, 4500, 0, 0);
            var vertical = new LevelSettings(45, 4500, 0, 0) { Orientation = LevelOrientation.Vertical };

            Assert.AreNotEqual(horizontal, vertical);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsEveryField()
        {
            var source = Authored();

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<LevelSettings>(json);

            Assert.AreEqual(source, restored);
        }

        // The claim that made this field additive: no DataVersion bump, no migrator, because an
        // absent key is never written and the constructor's Horizontal survives deserialization.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void LevelsWrittenBeforeTheField_ReadBackHorizontal()
        {
            var settings = JsonConvert.DeserializeObject<LevelSettings>(
                "{\"fps\":45,\"f_dur\":4500,\"id_counter\":20,\"aid_counter\":3,\"seed\":0}");

            Assert.AreEqual(LevelOrientation.Horizontal, settings.Orientation);
            Assert.AreEqual(45, settings.Framerate);
        }
    }
}
