using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Broken-CRT artifacts: tearing scanlines, rolling picture, color bleed. The analog half of the
    /// glitch pair - DigitalGlitchKey corrupts blocks of data instead of the signal.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AnalogGlitchKey : PostProcessingKeyframe, IModel<AnalogGlitchKey> // HEAVY IN ANY CASE, PHONES DON'T LIKE IT
    {
        /// <summary> How much individual scanlines shift sideways. </summary>
        [RuleInRange(PostProcessingRules.AnalogGlitch.ScanLineJitterMin,
            PostProcessingRules.AnalogGlitch.ScanLineJitterMax)]
        [JsonProperty(Names.ScanLineJitter)]
        public float ScanLineJitter { get; set; }

        /// <summary> How much the whole picture rolls vertically. </summary>
        [RuleInRange(PostProcessingRules.AnalogGlitch.VerticalJumpMin,
            PostProcessingRules.AnalogGlitch.VerticalJumpMax)]
        [JsonProperty(Names.VerticalJump)]
        public float VerticalJump { get; set; }

        /// <summary> How much the whole picture jitters horizontally - the frame moves as one,
        /// unlike ScanLineJitter which tears it apart. </summary>
        [RuleInRange(PostProcessingRules.AnalogGlitch.HorizontalShakeMin,
            PostProcessingRules.AnalogGlitch.HorizontalShakeMax)]
        [JsonProperty(Names.HorizontalShake)]
        public float HorizontalShake { get; set; }

        /// <summary> How far the color channels separate. </summary>
        [RuleInRange(PostProcessingRules.AnalogGlitch.ColorDriftMin,
            PostProcessingRules.AnalogGlitch.ColorDriftMax)]
        [JsonProperty(Names.ColorDrift)]
        public float ColorDrift { get; set; }

        public AnalogGlitchKey()
        {
            ScanLineJitter = 0.5f;
            VerticalJump = 0f;
            HorizontalShake = 0f;
            ColorDrift = 0f;
        }
        public AnalogGlitchKey(float scanLineJitter, float verticalJump, float horizontalShake, float colorDrift,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            ScanLineJitter = scanLineJitter;
            VerticalJump = verticalJump;
            HorizontalShake = horizontalShake;
            ColorDrift = colorDrift;
        }
    }
}