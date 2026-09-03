using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    /// <summary>
    /// One switch per post-processing effect, mirroring PostProcessingEvents field for field - so a
    /// player can disable exactly the effects that cost them frames (or that they find unreadable)
    /// while keeping the rest of the level's look. Inherited Render kills the whole stack.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class PostProcessingGraphicsSettings : BaseGraphicsSettings,
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
    }
}