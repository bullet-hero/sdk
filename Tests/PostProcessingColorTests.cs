using System.IO;
using BH.SDK.Models.PostProcessing;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using Newtonsoft.Json;
using BH.SDK.Models;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Grading colours carry THREE channels: URP reads the fourth as a signed offset, not opacity. An older
    // four-channel payload drops the extra property. BH.SDK/Docs/TESTING.md, "Other fixtures".

    /// <summary> Why the grading colours carry three channels: URP reads a fourth as a signed offset, so an
    /// alpha defaulting to 1 pushed a whole tonal band four stops the moment its range was switched on. </summary>
    public class PostProcessingColorTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GradingColours_DefaultToWhiteWithNoFourthComponent()
        {
            var lift = new LiftGammaGainKey();

            Assert.IsInstanceOf<Color3Value>(lift.LiftColor3);
            Assert.IsInstanceOf<Color3Value>(lift.GammaColor3);
            Assert.IsInstanceOf<Color3Value>(lift.GainColor3);

            var white = (Color3Value)lift.LiftColor3;
            Assert.AreEqual(1f, white.R);
            Assert.AreEqual(1f, white.G);
            Assert.AreEqual(1f, white.B);
        }

        // SMH colours and limits are born ABSENT and read back as the neutrals PostProcessingRules names.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GradingColours_AreAbsentUntilSet()
        {
            var shadows = new ShadowsMidtonesHighlightsKey();

            Assert.IsNull(shadows.ShadowsColor3);
            Assert.IsNull(shadows.MidtonesColor3);
            Assert.IsNull(shadows.HighlightsColor3);
            Assert.IsNull(shadows.ShadowLimits);
            Assert.IsNull(shadows.HighlightLimits);

            Assert.AreEqual(1f, PostProcessingRules.ShadowsMidtonesHighlights.ColorR_Default,
                "the neutral an absent grading colour reads back as stopped being white");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void VignetteColour_IsAbsentUntilSetAndItsNeutralIsBlack()
        {
            var vignette = new VignetteKey();

            Assert.IsNull(vignette.Color3, "the colour is born absent, not black");
            Assert.IsNull(vignette.Center);

            // What absent reads back as - the neutral moved out of the constructor and into the
            // rules when it did, so this is where it can be checked at all.
            Assert.AreEqual(0f, PostProcessingRules.Vignette.ColorR_Default);
            Assert.AreEqual(0f, PostProcessingRules.Vignette.ColorG_Default);
            Assert.AreEqual(0f, PostProcessingRules.Vignette.ColorB_Default);

            // And a colour that IS set is still three components.
            vignette.Color3 = Color3Value.white;
            Assert.IsInstanceOf<Color3Value>(vignette.Color3);
        }

        // A level written before the fourth component was removed. Its colour is a four-property
        // payload behind the same tag, and what has to survive is the colour - the component that
        // was doing the damage is what may not.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AFourComponentColour_WrittenBeforeThis_StillReadsBack()
        {
            // The payload carrying one component too many; key names come from the constants.
            var written = $"{{\"{Names.Lift}\":true,\"{Names.LiftColor}\":[0,{{\"r\":0.25,\"g\":0.5,\"b\":0.75,\"a\":0.9}}],"
                          + $"\"{Names.Gamma}\":false,\"{Names.Gain}\":false,\"{Names.ActiveShort}\":true,\"{Names.FrameShort}\":7}}";

            var key = Read<LiftGammaGainKey>(written);

            Assert.IsNotNull(key, "An older payload stopped deserializing entirely.");
            Assert.IsTrue(key.Lift);
            Assert.AreEqual(7, key.Frame);

            var colour = key.LiftColor3 as Color3Value;
            Assert.IsNotNull(colour, "The colour did not come back as a literal.");
            Assert.AreEqual(0.25f, colour.R, 1e-4f);
            Assert.AreEqual(0.5f, colour.G, 1e-4f);
            Assert.AreEqual(0.75f, colour.B, 1e-4f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AThemeReferenceInAGradingColour_RoundTrips()
        {
            var key = new LiftGammaGainKey { LiftColor3 = new Color3ThemeRef(12) };

            var restored = Read<LiftGammaGainKey>(Write(key));

            var themeRef = restored.LiftColor3 as Color3ThemeRef;
            Assert.IsNotNull(themeRef, "A theme reference stopped surviving the round trip.");
            Assert.AreEqual(12, themeRef.ThemeColorIndex);
        }

        private static string Write<T>(T value)
        {
            var serializer = new SerializationService().Serializer;

            using var writer = new StringWriter();
            using var json = new JsonTextWriter(writer);

            serializer.Serialize(json, value);
            return writer.ToString();
        }

        private static T Read<T>(string text)
        {
            var serializer = new SerializationService().Serializer;

            using var reader = new StringReader(text);
            using var json = new JsonTextReader(reader);

            return serializer.Deserialize<T>(json);
        }
    }
}
