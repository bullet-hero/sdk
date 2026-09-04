using System;
using System.Text;
using System.Text.RegularExpressions;
using BH.SDK.Models;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Keyframes;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THE FONT-SIZE TRACK IS THE ONLY PER-OBJECT TRACK WHOSE KEY CLASS IS POLYMORPHIC, so it is the
    // only one where a round trip can lose the KIND while preserving every number and still look
    // like it worked. Level.Equals does catch that - the two classes are not equal to each other -
    // but only if the fixture actually carries both, which is why MockData does.
    //
    // The wire shape is asserted separately from the round trip on purpose: a codec that writes an
    // untagged key and reads it back as the plain kind round-trips perfectly and has silently made
    // auto sizing unrepresentable.

    [TestFixture]
    public class FontSizeKeyTests
    {
        private static readonly SerializationService Service = new();

        private static IDataSerializer Serializer(SerializationType type) => Service.GetDataSerializer(type);

        private static Level RoundTrip(Level level, SerializationType type)
        {
            var serializer = Serializer(type);
            var bytes = serializer.SerializeEnvelope(DataDomains.Level, new EnvelopeData(new Version(1, 0), level));
            return serializer.DeserializeEnvelope(bytes, typeof(Level)).GetPayload<Level>();
        }

        private static TextObject Text(Level level) => (TextObject)level.Game.Objects[new ObjectId(2)];

        [TestCase(SerializationType.Json)]
        [TestCase(SerializationType.Blob)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void BothKindsOfFontSizeKey_SurviveARoundTrip(SerializationType type)
        {
            var level = MockData.CreateTestLevel();

            var read = RoundTrip(level, type);

            var sizes = Text(read).FontSizes;
            Assert.AreEqual(2, sizes.Count, "the track lost a key");

            Assert.AreEqual(FontSizeKeyType.Value, sizes[0].GetModelType());
            Assert.IsInstanceOf<FontSizeKey>(sizes[0]);

            Assert.AreEqual(FontSizeKeyType.Auto, sizes[1].GetModelType());
            var auto = (AutoFontSizeKey)sizes[1];
            Assert.AreEqual(0.25f, ((FloatValue)auto.MinValue).Value, 1e-4f);
            Assert.AreEqual(3f, ((FloatValue)auto.MaxValue).Value, 1e-4f);
            Assert.AreEqual(10, auto.Frame);
        }

        /// <summary> The track is written as a tagged union, so every key is [type, payload]. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TheJsonFontSizeTrack_IsTagged()
        {
            var level = MockData.CreateTestLevel();
            var bytes = Serializer(SerializationType.Json)
                .SerializeEnvelope(DataDomains.Level, new EnvelopeData(new Version(1, 0), level));

            var json = Encoding.UTF8.GetString(bytes);

            Assert.IsTrue(Regex.IsMatch(json, "\"" + Names.FontSize + "\"\\s*:\\s*\\[\\s*\\["),
                "the font-size track is not written as a tagged union");
        }

        /// <summary> Both defaults are what makes a key switched to auto sizing look unchanged
        /// until the text stops fitting - see AutoFontSizeKey's own header. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheDefaults_AreTheOnesTheEditorReliesOn()
        {
            Assert.AreEqual(TextRules.FontSize_Fallback,
                ((FloatValue)new FontSizeKey().Value).Value, 1e-4f);

            var auto = new AutoFontSizeKey();
            Assert.AreEqual(TextRules.AutoFontSize_Min_Default, ((FloatValue)auto.MinValue).Value, 1e-4f);
            Assert.AreEqual(TextRules.AutoFontSize_Max_Default, ((FloatValue)auto.MaxValue).Value, 1e-4f);
        }

        /// <summary> Two keys of different kinds are never equal, which is what makes the round-trip
        /// assertion above able to fail at all. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void APlainKeyAndAnAutoKey_AreNotEqual()
        {
            var plain = new FontSizeKey(new FloatValue(1f), 0, EaseType.Linear);
            var auto = new AutoFontSizeKey(new FloatValue(1f), new FloatValue(1f), 0, EaseType.Linear);

            Assert.IsFalse(((IFontSizeKey)plain).Equals(auto));
            Assert.IsFalse(((IFontSizeKey)auto).Equals(plain));
        }
    }
}
