using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.PostProcessing;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Game
{
    /// <summary>
    /// The level's screen-effect stack: one independent keyframe track per URP effect. Two levels of
    /// switching - this Active gates the whole stack, and every key has its own Active on top of it.
    /// Fields are grouped below as general / color grading / glitches.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.PostProcessingEvents, 1, 0)]
    [GenerateModel]
    public sealed partial class PostProcessingEvents : IModel<PostProcessingEvents>
    {
        /// <summary> Master switch for all post-processing, on by default - the opposite default
        /// from LevelTrackEffects.Active. </summary>
        [JsonProperty(Names.ActiveShort)]
        public bool Active { get; set; }

        // General

        /// <summary> Glow around bright pixels. Expensive on phones. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(BloomKey.Frame))]
        [JsonProperty(Names.BloomShort)]
        public List<BloomKey> Blooms { get; set; }

        /// <summary> Color-channel separation toward the edges. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(ChromaticAberrationKey.Frame))]
        [JsonProperty(Names.ChromaticShort)]
        public List<ChromaticAberrationKey> Chromatics { get; set; }

        /// <summary> Darkened screen edges. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(VignetteKey.Frame))]
        [JsonProperty(Names.VignetteShort)]
        public List<VignetteKey> Vignettes { get; set; }

        /// <summary> Lens-style warping of the image. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(LensDistortionKey.Frame))]
        [JsonProperty(Names.LensShort)]
        public List<LensDistortionKey> Lenses { get; set; }

        /// <summary> Film grain overlay. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(FilmGrainKey.Frame))]
        [JsonProperty(Names.GrainShort)]
        public List<FilmGrainKey> Grains { get; set; }

        /// <summary> Motion smearing. Expensive on phones. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(MotionBlurKey.Frame))]
        [JsonProperty(Names.MotionBlurShort)]
        public List<MotionBlurKey> MotionBlurs { get; set; }

        // Colors

        /// <summary> Hue/saturation remapping. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(ColorCurvesKey.Frame))]
        [JsonProperty(Names.ColorCurvesShort)]
        public List<ColorCurvesKey> ColorCurveses { get; set; }

        /// <summary> Three-way grading by fixed tonal ranges. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(LiftGammaGainKey.Frame))]
        [JsonProperty(Names.LiftGammaGainShort)]
        public List<LiftGammaGainKey> LiftGammaGains { get; set; }

        /// <summary> Three-way grading with authorable band boundaries. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(ShadowsMidtonesHighlightsKey.Frame))]
        [JsonProperty(Names.ShadowsMidtonesHighlightsShort)]
        public List<ShadowsMidtonesHighlightsKey> ShadowsMidtonesHighlightses { get; set; }

        /// <summary> Global temperature/tint shift. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(WhiteBalanceKey.Frame))]
        [JsonProperty(Names.WhiteBalanceShort)]
        public List<WhiteBalanceKey> WhiteBalances { get; set; }
        
        // Glitches
        
        /// <summary> Broken-CRT artifacts. Expensive on phones. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(AnalogGlitchKey.Frame))]
        [JsonProperty(Names.AnalogGlitchShort)]
        public List<AnalogGlitchKey> AnalogGlitches { get; set; }

        /// <summary> Corrupted-datastream artifacts. Expensive on phones. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPostProcessingKeys)]
        [RuleCollectionUnique(nameof(DigitalGlitchKey.Frame))]
        [JsonProperty(Names.DigitalGlitchShort)]
        public List<DigitalGlitchKey> DigitalGlitches { get; set; }

        public PostProcessingEvents()
        {
            Active = PostProcessingRules.ActiveDefault;
            Blooms = new List<BloomKey>();
            Chromatics = new List<ChromaticAberrationKey>();
            Vignettes = new List<VignetteKey>();
            Lenses = new List<LensDistortionKey>();
            Grains = new List<FilmGrainKey>();
            MotionBlurs = new List<MotionBlurKey>();
            ColorCurveses = new List<ColorCurvesKey>();
            LiftGammaGains = new List<LiftGammaGainKey>();
            ShadowsMidtonesHighlightses = new List<ShadowsMidtonesHighlightsKey>();
            WhiteBalances = new List<WhiteBalanceKey>();
            AnalogGlitches = new List<AnalogGlitchKey>();
            DigitalGlitches = new List<DigitalGlitchKey>();
        }
        public PostProcessingEvents(bool active,
            List<BloomKey> blooms, 
            List<ChromaticAberrationKey> chromatics, 
            List<VignetteKey> vignettes, 
            List<LensDistortionKey> lenses, 
            List<FilmGrainKey> grains, 
            List<MotionBlurKey> motionBlurs, 
            List<ColorCurvesKey> colorCurveses, 
            List<LiftGammaGainKey> liftGammaGains, 
            List<ShadowsMidtonesHighlightsKey> shadowsMidtonesHighlightses, 
            List<WhiteBalanceKey> whiteBalances, 
            List<AnalogGlitchKey> analogGlitches, 
            List<DigitalGlitchKey> digitalGlitches)
        {
            Active = active;
            Blooms = blooms;
            Chromatics = chromatics;
            Vignettes = vignettes;
            Lenses = lenses;
            Grains = grains;
            MotionBlurs = motionBlurs;
            ColorCurveses = colorCurveses;
            LiftGammaGains = liftGammaGains;
            ShadowsMidtonesHighlightses = shadowsMidtonesHighlightses;
            WhiteBalances = whiteBalances;
            AnalogGlitches = analogGlitches;
            DigitalGlitches = digitalGlitches;
        }
    }
}