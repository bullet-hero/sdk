using System;
using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.PostProcessing;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Game
{
    /// <summary>
    /// The level's screen-effect stack: one independent keyframe track per URP effect. Two levels of
    /// switching - this Active gates the whole stack, and every key has its own Active on top of it.
    /// Fields are grouped below as general / color grading / glitches.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.PostProcessingEvents, 1, 0)]
    public class PostProcessingEvents : IModel<PostProcessingEvents>
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
        public void Reset()
        {
            Active = PostProcessingRules.ActiveDefault;
            Blooms.Clear();
            Chromatics.Clear();
            Vignettes.Clear();
            Lenses.Clear();
            Grains.Clear();
            MotionBlurs.Clear();
            ColorCurveses.Clear();
            LiftGammaGains.Clear();
            ShadowsMidtonesHighlightses.Clear();
            WhiteBalances.Clear();
            AnalogGlitches.Clear();
            DigitalGlitches.Clear();
        }

        public object Clone() => Copy();
        public PostProcessingEvents Copy() => new(Active, Blooms.CopyList(), Chromatics.CopyList(), Vignettes.CopyList(),
            Lenses.CopyList(), Grains.CopyList(), MotionBlurs.CopyList(), ColorCurveses.CopyList(),
            LiftGammaGains.CopyList(), ShadowsMidtonesHighlightses.CopyList(), WhiteBalances.CopyList(),
            AnalogGlitches.CopyList(), DigitalGlitches.CopyList());

        public void Update(PostProcessingEvents src)
        {
            Active = src.Active;
            Blooms = src.Blooms.CopyList();
            Chromatics = src.Chromatics.CopyList();
            Vignettes = src.Vignettes.CopyList();
            Lenses = src.Lenses.CopyList();
            Grains = src.Grains.CopyList();
            MotionBlurs = src.MotionBlurs.CopyList();
            ColorCurveses = src.ColorCurveses.CopyList();
            LiftGammaGains = src.LiftGammaGains.CopyList();
            ShadowsMidtonesHighlightses = src.ShadowsMidtonesHighlightses.CopyList();
            WhiteBalances = src.WhiteBalances.CopyList();
            AnalogGlitches = src.AnalogGlitches.CopyList();
            DigitalGlitches = src.DigitalGlitches.CopyList();
        }

        public void Pull(PostProcessingEvents src)
        {
            Active = src.Active;
            Blooms = src.Blooms.CopyList();
            Chromatics = src.Chromatics.CopyList();
            Vignettes = src.Vignettes.CopyList();
            Lenses = src.Lenses.CopyList();
            Grains = src.Grains.CopyList();
            MotionBlurs = src.MotionBlurs.CopyList();
            ColorCurveses = src.ColorCurveses.CopyList();
            LiftGammaGains = src.LiftGammaGains.CopyList();
            ShadowsMidtonesHighlightses = src.ShadowsMidtonesHighlightses.CopyList();
            WhiteBalances = src.WhiteBalances.CopyList();
            AnalogGlitches = src.AnalogGlitches.CopyList();
            DigitalGlitches = src.DigitalGlitches.CopyList();
        }

        public override bool Equals(object obj) => obj is PostProcessingEvents value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Active.GetHashCode());
            hashCode.Add(Blooms.GetListHashCode());
            hashCode.Add(Chromatics.GetListHashCode());
            hashCode.Add(Vignettes.GetListHashCode());
            hashCode.Add(Lenses.GetListHashCode());
            hashCode.Add(Grains.GetListHashCode());
            hashCode.Add(MotionBlurs.GetListHashCode());
            hashCode.Add(ColorCurveses.GetListHashCode());
            hashCode.Add(LiftGammaGains.GetListHashCode());
            hashCode.Add(ShadowsMidtonesHighlightses.GetListHashCode());
            hashCode.Add(WhiteBalances.GetListHashCode());
            hashCode.Add(AnalogGlitches.GetListHashCode());
            hashCode.Add(DigitalGlitches.GetListHashCode());
            return hashCode.ToHashCode();
        }

        public bool Equals(PostProcessingEvents other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Active == other.Active
                         && Blooms.ListEquals(other.Blooms)
                         && Chromatics.ListEquals(other.Chromatics)
                         && Vignettes.ListEquals(other.Vignettes)
                         && Lenses.ListEquals(other.Lenses)
                         && Grains.ListEquals(other.Grains)
                         && MotionBlurs.ListEquals(other.MotionBlurs)
                         && ColorCurveses.ListEquals(other.ColorCurveses)
                         && LiftGammaGains.ListEquals(other.LiftGammaGains)
                         && ShadowsMidtonesHighlightses.ListEquals(other.ShadowsMidtonesHighlightses)
                         && WhiteBalances.ListEquals(other.WhiteBalances)
                         && AnalogGlitches.ListEquals(other.AnalogGlitches)
                         && DigitalGlitches.ListEquals(other.DigitalGlitches);
            return result;
        }
    }
}