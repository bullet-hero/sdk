using BH.SDK.Models.PostProcessing;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> ColorCurves' eight curves: what a null one stands for, and what the two neutral
    /// shapes are. </summary>
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

    }
}
