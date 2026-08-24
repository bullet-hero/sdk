using System;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    /// <summary>
    /// One switch per post-processing effect, mirroring PostProcessingEvents field for field - so a
    /// player can disable exactly the effects that cost them frames (or that they find unreadable)
    /// while keeping the rest of the level's look. Inherited Render kills the whole stack.
    /// </summary>
    public class PostProcessingGraphicsSettings : BaseGraphicsSettings,
        IModel<PostProcessingGraphicsSettings>, IMoveable<PostProcessingGraphicsSettings>
    {
        /// <summary> Allow bloom. One of the four effects flagged as heavy on phones. </summary>
        [JsonProperty(Names.RenderBloom)]
        public bool RenderBloom { get; set; }

        /// <summary> Allow chromatic aberration. </summary>
        [JsonProperty(Names.RenderChroma)]
        public bool RenderChroma { get; set; }

        /// <summary> Allow vignette. </summary>
        [JsonProperty(Names.RenderVignette)]
        public bool RenderVignette { get; set; }

        /// <summary> Allow lens distortion. </summary>
        [JsonProperty(Names.RenderLens)]
        public bool RenderLens { get; set; }

        /// <summary> Allow film grain. </summary>
        [JsonProperty(Names.RenderGrain)]
        public bool RenderGrain { get; set; }

        /// <summary> Allow motion blur. Heavy on phones. </summary>
        [JsonProperty(Names.RenderMotionBlur)]
        public bool RenderMotionBlur { get; set; }

        /// <summary> Allow hue/saturation remapping. </summary>
        [JsonProperty(Names.RenderColorCurves)]
        public bool RenderColorCurves { get; set; }

        /// <summary> Allow lift/gamma/gain grading. </summary>
        [JsonProperty(Names.RenderLiftGammaGain)]
        public bool RenderLiftGammaGain { get; set; }

        /// <summary> Allow shadows/midtones/highlights grading. </summary>
        [JsonProperty(Names.RenderShadowsMidtonesHighlights)]
        public bool RenderShadowsMidtonesHighlights { get; set; }

        /// <summary> Allow white balance shifts. </summary>
        [JsonProperty(Names.RenderWhiteBalance)]
        public bool RenderWhiteBalance { get; set; }

        /// <summary> Allow analog glitch. Heavy on phones, and the most likely to be disabled for
        /// comfort rather than performance. </summary>
        [JsonProperty(Names.RenderAnalogGlitch)]
        public bool RenderAnalogGlitch { get; set; }

        /// <summary> Allow digital glitch. Heavy on phones. </summary>
        [JsonProperty(Names.RenderDigitalGlitch)]
        public bool RenderDigitalGlitch { get; set; }

        public PostProcessingGraphicsSettings()
        {
            Render = true;
            RenderBloom = true;
            RenderChroma = true;
            RenderVignette = true;
            RenderLens = true;
            RenderGrain = true;
            RenderMotionBlur = true;
            RenderColorCurves = true;
            RenderLiftGammaGain = true;
            RenderShadowsMidtonesHighlights = true;
            RenderWhiteBalance = true;
            RenderAnalogGlitch = true;
            RenderDigitalGlitch = true;
        }
        public PostProcessingGraphicsSettings(bool render, bool renderBloom, bool renderChroma, bool renderVignette,
            bool renderLens, bool renderGrain, bool renderMotionBlur, bool renderColorCurves, bool renderLiftGammaGain,
            bool renderShadowsMidtonesHighlights, bool renderWhiteBalance, bool renderAnalogGlitch,
            bool renderDigitalGlitch) : base(render)
        {
            RenderBloom = renderBloom;
            RenderChroma = renderChroma;
            RenderVignette = renderVignette;
            RenderLens = renderLens;
            RenderGrain = renderGrain;
            RenderMotionBlur = renderMotionBlur;
            RenderColorCurves = renderColorCurves;
            RenderLiftGammaGain = renderLiftGammaGain;
            RenderShadowsMidtonesHighlights = renderShadowsMidtonesHighlights;
            RenderWhiteBalance = renderWhiteBalance;
            RenderAnalogGlitch = renderAnalogGlitch;
            RenderDigitalGlitch = renderDigitalGlitch;
        }
        public override void Reset() // PresetAll
        {
            base.Reset();
            Render = true;
            RenderBloom = true;
            RenderChroma = true;
            RenderVignette = true;
            RenderLens = true;
            RenderGrain = true;
            RenderMotionBlur = true;
            RenderColorCurves = true;
            RenderLiftGammaGain = true;
            RenderShadowsMidtonesHighlights = true;
            RenderWhiteBalance = true;
            RenderAnalogGlitch = true;
            RenderDigitalGlitch = true;
        }
        
        public override object Clone() => CopyImpl();
        public override BaseGraphicsSettings Copy() => CopyImpl();
        PostProcessingGraphicsSettings ICopyable<PostProcessingGraphicsSettings>.Copy() => CopyImpl();
        
        private PostProcessingGraphicsSettings CopyImpl() => new(Render, RenderBloom, RenderChroma, RenderVignette,
            RenderLens, RenderGrain, RenderMotionBlur, RenderColorCurves, RenderLiftGammaGain,
            RenderShadowsMidtonesHighlights, RenderWhiteBalance, RenderAnalogGlitch, RenderDigitalGlitch);

        public PostProcessingGraphicsSettings GetPresetNone() => new()
        {
            Render = false,
            RenderBloom = false,
            RenderChroma = false,
            RenderVignette = false,
            RenderLens = false,
            RenderGrain = false,
            RenderMotionBlur = false,
            RenderColorCurves = false,
            RenderLiftGammaGain = false,
            RenderShadowsMidtonesHighlights = false,
            RenderWhiteBalance = false,
            RenderAnalogGlitch = false,
            RenderDigitalGlitch = false,
        };
        public PostProcessingGraphicsSettings GetPresetMobile() => new()
        {
            Render = true,
            RenderBloom = false, // HEAVY
            RenderChroma = true,
            RenderVignette = true,
            RenderLens = true,
            RenderGrain = true,
            RenderMotionBlur = false, // HEAVY
            RenderColorCurves = true,
            RenderLiftGammaGain = true,
            RenderShadowsMidtonesHighlights = true,
            RenderWhiteBalance = true,
            RenderAnalogGlitch = false, // HEAVY
            RenderDigitalGlitch = false, // HEAVY
        };
        public PostProcessingGraphicsSettings GetPresetAll() => new()
        {
            Render = true,
            RenderBloom = true,
            RenderChroma = true,
            RenderVignette = true,
            RenderLens = true,
            RenderGrain = true,
            RenderMotionBlur = true,
            RenderColorCurves = true,
            RenderLiftGammaGain = true,
            RenderShadowsMidtonesHighlights = true,
            RenderWhiteBalance = true,
            RenderAnalogGlitch = true,
            RenderDigitalGlitch = true,
        };

        public void Pull(PostProcessingGraphicsSettings source)
        {
            Render = source.Render;
            RenderBloom = source.RenderBloom;
            RenderChroma = source.RenderChroma;
            RenderVignette = source.RenderVignette;
            RenderLens = source.RenderLens;
            RenderGrain = source.RenderGrain;
            RenderMotionBlur = source.RenderMotionBlur;
            RenderColorCurves = source.RenderColorCurves;
            RenderLiftGammaGain = source.RenderLiftGammaGain;
            RenderShadowsMidtonesHighlights = source.RenderShadowsMidtonesHighlights;
            RenderWhiteBalance = source.RenderWhiteBalance;
            RenderAnalogGlitch = source.RenderAnalogGlitch;
            RenderDigitalGlitch = source.RenderDigitalGlitch;
        }

        public void Update(PostProcessingGraphicsSettings src)
        {
            base.Update(src);

            RenderBloom = src.RenderBloom;
            RenderChroma = src.RenderChroma;
            RenderVignette = src.RenderVignette;
            RenderLens = src.RenderLens;
            RenderGrain = src.RenderGrain;
            RenderMotionBlur = src.RenderMotionBlur;
            RenderColorCurves = src.RenderColorCurves;
            RenderLiftGammaGain = src.RenderLiftGammaGain;
            RenderShadowsMidtonesHighlights = src.RenderShadowsMidtonesHighlights;
            RenderWhiteBalance = src.RenderWhiteBalance;
            RenderAnalogGlitch = src.RenderAnalogGlitch;
            RenderDigitalGlitch = src.RenderDigitalGlitch;
        }

        public override bool Equals(object obj) => obj is PostProcessingGraphicsSettings value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(base.GetHashCode());
            hashCode.Add(RenderBloom);
            hashCode.Add(RenderChroma);
            hashCode.Add(RenderVignette);
            hashCode.Add(RenderLens);
            hashCode.Add(RenderGrain);
            hashCode.Add(RenderMotionBlur);
            hashCode.Add(RenderColorCurves);
            hashCode.Add(RenderLiftGammaGain);
            hashCode.Add(RenderShadowsMidtonesHighlights);
            hashCode.Add(RenderWhiteBalance);
            hashCode.Add(RenderAnalogGlitch);
            hashCode.Add(RenderDigitalGlitch);
            return hashCode.ToHashCode();
        }
        
        public bool Equals(PostProcessingGraphicsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other)
                   && RenderBloom == other.RenderBloom
                   && RenderChroma == other.RenderChroma
                   && RenderVignette == other.RenderVignette
                   && RenderLens == other.RenderLens
                   && RenderGrain == other.RenderGrain
                   && RenderMotionBlur == other.RenderMotionBlur
                   && RenderColorCurves == other.RenderColorCurves
                   && RenderLiftGammaGain == other.RenderLiftGammaGain
                   && RenderShadowsMidtonesHighlights == other.RenderShadowsMidtonesHighlights
                   && RenderWhiteBalance == other.RenderWhiteBalance
                   && RenderAnalogGlitch == other.RenderAnalogGlitch
                   && RenderDigitalGlitch == other.RenderDigitalGlitch;
        }
    }
}