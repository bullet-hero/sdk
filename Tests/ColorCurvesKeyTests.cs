using System.Collections.Generic;
using BH.SDK.Models.Enums;
using BH.SDK.Models.PostProcessing;
using BH.SDK.Rules;
using BH.SDK.Versions.V1_0;
using BH.SDK.Versions.V1_0.Migrations;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary>
    /// ColorCurves' eight curves: what a null one stands for, and what the two 1.0 scalars became.
    /// </summary>
    public class ColorCurvesKeyTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NewKey_HasNoCurves()
        {
            var key = new ColorCurvesKey();

            Assert.That(key.Master, Is.Null);
            Assert.That(key.Red, Is.Null);
            Assert.That(key.Green, Is.Null);
            Assert.That(key.Blue, Is.Null);
            Assert.That(key.HueVsHue, Is.Null);
            Assert.That(key.HueVsSat, Is.Null);
            Assert.That(key.SatVsSat, Is.Null);
            Assert.That(key.LumVsSat, Is.Null);
        }

        // The four YRGB curves are an absolute mapping, so their neutral is the identity LINE -
        // tangents included, or two points at (0,0) and (1,1) evaluate as an S curve and the
        // "neutral changes nothing" contract is quietly false in the midtones.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CreateIdentity_IsAStraightLineFromZeroToOne()
        {
            var curve = ColorCurvesKey.CreateIdentity();

            Assert.That(curve.KeyFrames, Has.Count.EqualTo(2));
            Assert.That(curve.KeyFrames[0].Time, Is.EqualTo(ValueRules.MinCurveTime));
            Assert.That(curve.KeyFrames[0].Value, Is.EqualTo(PostProcessingRules.ColorCurves.CurveMin));
            Assert.That(curve.KeyFrames[1].Time, Is.EqualTo(ValueRules.MaxCurveTime));
            Assert.That(curve.KeyFrames[1].Value, Is.EqualTo(PostProcessingRules.ColorCurves.CurveMax));
            Assert.That(curve.KeyFrames[0].OutTangent, Is.EqualTo(1f));
            Assert.That(curve.KeyFrames[1].InTangent, Is.EqualTo(1f));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CreateNeutral_IsFlatAtTheNeutralValue()
        {
            var curve = ColorCurvesKey.CreateNeutral();

            Assert.That(curve.KeyFrames, Has.Count.EqualTo(2));
            foreach (var key in curve.KeyFrames)
                Assert.That(key.Value, Is.EqualTo(PostProcessingRules.ColorCurves.CurveNeutral));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Migration_TurnsAMovedScalarIntoAFlatCurve()
        {
            var migrated = Migrate(new ColorCurvesKeyV1_0
            {
                Active = true, Frame = 42, Ease = EaseType.Linear, HueVsHue = 0.75f, SatVsSat = 0.25f,
            });

            Assert.That(migrated.Active, Is.True);
            Assert.That(migrated.Frame, Is.EqualTo(42));
            Assert.That(migrated.Ease, Is.EqualTo(EaseType.Linear));

            Assert.That(migrated.HueVsHue, Is.Not.Null);
            foreach (var key in migrated.HueVsHue.KeyFrames)
                Assert.That(key.Value, Is.EqualTo(0.75f));

            Assert.That(migrated.SatVsSat, Is.Not.Null);
            foreach (var key in migrated.SatVsSat.KeyFrames)
                Assert.That(key.Value, Is.EqualTo(0.25f));
        }

        // A level that never touched colour grading must come out of the migration holding nothing,
        // or every one of them arrives looking edited.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Migration_TurnsAnUntouchedScalarIntoNull()
        {
            var migrated = Migrate(new ColorCurvesKeyV1_0
            {
                Frame = 7,
                HueVsHue = PostProcessingRules.ColorCurves.CurveNeutral,
                SatVsSat = PostProcessingRules.ColorCurves.CurveNeutral,
            });

            Assert.That(migrated.HueVsHue, Is.Null);
            Assert.That(migrated.SatVsSat, Is.Null);
        }

        // Six of the eight curves did not exist at 1.0 and have no scalar to come from.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Migration_LeavesTheSixNewCurvesEmpty()
        {
            var migrated = Migrate(new ColorCurvesKeyV1_0 { HueVsHue = 0.1f, SatVsSat = 0.9f });

            Assert.That(migrated.Master, Is.Null);
            Assert.That(migrated.Red, Is.Null);
            Assert.That(migrated.Green, Is.Null);
            Assert.That(migrated.Blue, Is.Null);
            Assert.That(migrated.HueVsSat, Is.Null);
            Assert.That(migrated.LumVsSat, Is.Null);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Migration_CarriesEveryOtherTrackOver()
        {
            var source = new PostProcessingEventsV1_0
            {
                Active = false,
                Blooms = new List<BloomKey> { new() { Frame = 3 } },
                WhiteBalances = new List<WhiteBalanceKey> { new() { Frame = 5 } },
            };

            var migrated = new PostProcessingEventsV1_0ToV1_1().Migrate(source);

            Assert.That(migrated.Active, Is.False);
            Assert.That(migrated.Blooms, Has.Count.EqualTo(1));
            Assert.That(migrated.Blooms[0].Frame, Is.EqualTo(3));
            Assert.That(migrated.WhiteBalances, Has.Count.EqualTo(1));
            Assert.That(migrated.WhiteBalances[0].Frame, Is.EqualTo(5));
            Assert.That(migrated.ColorCurveses, Is.Empty);

            // Every list a 1.0 file simply did not carry must come back as an empty list, never null -
            // the current model's own constructor is what every consumer is written against.
            Assert.That(migrated.Chromatics, Is.Not.Null);
            Assert.That(migrated.Vignettes, Is.Not.Null);
            Assert.That(migrated.Lenses, Is.Not.Null);
            Assert.That(migrated.Grains, Is.Not.Null);
            Assert.That(migrated.MotionBlurs, Is.Not.Null);
            Assert.That(migrated.LiftGammaGains, Is.Not.Null);
            Assert.That(migrated.ShadowsMidtonesHighlightses, Is.Not.Null);
            Assert.That(migrated.AnalogGlitches, Is.Not.Null);
            Assert.That(migrated.DigitalGlitches, Is.Not.Null);
        }

        private static ColorCurvesKey Migrate(ColorCurvesKeyV1_0 key)
        {
            var source = new PostProcessingEventsV1_0
            {
                ColorCurveses = new List<ColorCurvesKeyV1_0> { key },
            };
            var migrated = new PostProcessingEventsV1_0ToV1_1().Migrate(source);

            Assert.That(migrated.ColorCurveses, Has.Count.EqualTo(1));
            return migrated.ColorCurveses[0];
        }
    }
}
