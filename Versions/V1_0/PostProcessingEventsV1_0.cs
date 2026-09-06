using System.Collections.Generic;
using BH.SDK.Models.Enums;
using BH.SDK.Models.PostProcessing;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1_0
{
    // ReSharper disable once InconsistentNaming

    // THE FIRST SNAPSHOT IN THIS REPO THAT CARRIES REAL DATA, and it exists for ONE of the twelve
    // tracks: ColorCurves stopped being two floats and became eight curves. Every other list is
    // typed with its CURRENT key class on purpose - those shapes did not move, and restating them
    // would only give the format a second place to drift from.
    //
    // Frozen literals, never Names.Xxx: those constants track the CURRENT wire format, and this
    // class's whole job is to describe a wire format that is no longer current.

    [DataVersion(DataDomains.PostProcessingEvents, 1, 0)]
    public class PostProcessingEventsV1_0
    {
        [JsonProperty("a")]
        public bool Active { get; set; }

        [JsonProperty("blm")]
        public List<BloomKey> Blooms { get; set; }

        [JsonProperty("chr")]
        public List<ChromaticAberrationKey> Chromatics { get; set; }

        [JsonProperty("vgn")]
        public List<VignetteKey> Vignettes { get; set; }

        [JsonProperty("lns")]
        public List<LensDistortionKey> Lenses { get; set; }

        [JsonProperty("grn")]
        public List<FilmGrainKey> Grains { get; set; }

        [JsonProperty("mbr")]
        public List<MotionBlurKey> MotionBlurs { get; set; }

        [JsonProperty("ccv")]
        public List<ColorCurvesKeyV1_0> ColorCurveses { get; set; }

        [JsonProperty("lgg")]
        public List<LiftGammaGainKey> LiftGammaGains { get; set; }

        [JsonProperty("smh")]
        public List<ShadowsMidtonesHighlightsKey> ShadowsMidtonesHighlightses { get; set; }

        [JsonProperty("wbl")]
        public List<WhiteBalanceKey> WhiteBalances { get; set; }

        [JsonProperty("agl")]
        public List<AnalogGlitchKey> AnalogGlitches { get; set; }

        [JsonProperty("dgl")]
        public List<DigitalGlitchKey> DigitalGlitches { get; set; }
    }

    // ReSharper disable once InconsistentNaming

    /// <summary> One ColorCurves key as it was written at 1.0: a hue shift and a saturation scale,
    /// each a bare 0..1 scalar standing in for a flat curve. Not independently versioned - it is
    /// only ever reached through the snapshot above, so it carries no [DataVersion] of its own. </summary>
    public class ColorCurvesKeyV1_0
    {
        [JsonProperty("a")]
        public bool Active { get; set; }

        [JsonProperty("f")]
        public int Frame { get; set; }

        [JsonProperty("ease")]
        public EaseType Ease { get; set; }

        [JsonProperty("hue_vs_hue")]
        public float HueVsHue { get; set; }

        [JsonProperty("sat_vs_sat")]
        public float SatVsSat { get; set; }
    }
}
