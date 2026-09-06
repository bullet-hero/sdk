using System.Collections.Generic;
using BH.SDK.Models.Game;
using BH.SDK.Models.PostProcessing;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Versions.V1_0.Migrations
{
    // ReSharper disable once InconsistentNaming

    // A SCALAR BECOMES THE FLAT CURVE IT ALWAYS DESCRIBED, and only when it was moved: a value
    // sitting on the neutral migrates to null rather than to a flat neutral curve, so a level that
    // never touched colour grading comes out of this holding nothing, exactly as one authored today
    // would. Faithful either way - the two are the same picture - but null is what the editor reads
    // as "untouched", and a level should not look edited because it was migrated.
    //
    // What the 1.0 build DID with these two numbers is not reproduced, and must not be: it scaled
    // them into degrees and percents before handing them to a curve URP reads as 0..1, so the
    // neutral 0.5 arrived as a constant 0 - a 180-degree hue rotation and a multiply-by-zero on
    // saturation. The authored value was always the honest one; only the consumer was wrong.
    public class PostProcessingEventsV1_0ToV1_1 : DataMigration<PostProcessingEventsV1_0, PostProcessingEvents>
    {
        public override PostProcessingEvents Migrate(PostProcessingEventsV1_0 from)
        {
            var events = new PostProcessingEvents
            {
                Active = from.Active,
                Blooms = from.Blooms ?? new List<BloomKey>(),
                Chromatics = from.Chromatics ?? new List<ChromaticAberrationKey>(),
                Vignettes = from.Vignettes ?? new List<VignetteKey>(),
                Lenses = from.Lenses ?? new List<LensDistortionKey>(),
                Grains = from.Grains ?? new List<FilmGrainKey>(),
                MotionBlurs = from.MotionBlurs ?? new List<MotionBlurKey>(),
                ColorCurveses = new List<ColorCurvesKey>(),
                LiftGammaGains = from.LiftGammaGains ?? new List<LiftGammaGainKey>(),
                ShadowsMidtonesHighlightses = from.ShadowsMidtonesHighlightses
                    ?? new List<ShadowsMidtonesHighlightsKey>(),
                WhiteBalances = from.WhiteBalances ?? new List<WhiteBalanceKey>(),
                AnalogGlitches = from.AnalogGlitches ?? new List<AnalogGlitchKey>(),
                DigitalGlitches = from.DigitalGlitches ?? new List<DigitalGlitchKey>(),
            };

            if (from.ColorCurveses == null) return events;

            foreach (var key in from.ColorCurveses)
            {
                if (key == null) continue;

                events.ColorCurveses.Add(new ColorCurvesKey(key.Active, key.Frame, key.Ease)
                {
                    HueVsHue = ToFlatCurve(key.HueVsHue),
                    SatVsSat = ToFlatCurve(key.SatVsSat),
                });
            }
            return events;
        }

        private static CurveValue ToFlatCurve(float value)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator - the 1.0 default was written
            // as this exact literal by the same constructor every time, so an epsilon would only
            // widen what counts as untouched.
            if (value == PostProcessingRules.ColorCurves.CurveNeutral) return null;

            return new CurveValue(new List<CurveKeyframeValue>
            {
                new(ValueRules.MinCurveTime, value),
                new(ValueRules.MaxCurveTime, value),
            }, Models.Enums.Values.CurveWrapMode.ClampForever, Models.Enums.Values.CurveWrapMode.ClampForever);
        }
    }
}
