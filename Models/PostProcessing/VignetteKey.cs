using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Darkens (or tints) the edges of the screen, pulling attention to the center - the cheapest
    /// way to make a section feel tense without changing the level itself.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class VignetteKey : PostProcessingKeyframe, IModel<VignetteKey>
    {
        // No alpha and no HDR part, by URP's own declaration (hdr: false, showAlpha: false).

        /// <summary> Color the edges fade toward; black is the usual choice, but any tint works. </summary>
        [RuleOptional]
        [JsonProperty(Names.Color)]
        public IColor3 Color3 { get; set; }

        /// <summary> Screen point the vignette opens around, in 0..1 - offset it to frame something
        /// off-center. </summary>
        [RuleIVector2InRange(PostProcessingRules.Vignette.CenterMin,
             PostProcessingRules.Vignette.CenterMax)]
        [RuleOptional]
        [JsonProperty(Names.Center)]
        public IVector2 Center { get; set; }

        /// <summary> How far in the darkening reaches. </summary>
        [RuleInRange(PostProcessingRules.Vignette.IntensityMin,
             PostProcessingRules.Vignette.IntensityMax)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }

        /// <summary> How gradual the edge of the vignette is. </summary>
        [RuleInRange(PostProcessingRules.Vignette.SmoothnessMin,
            PostProcessingRules.Vignette.SmoothnessMax)]
        [JsonProperty(Names.Smoothness)]
        public float Smoothness { get; set; }

        /// <summary> Circular instead of aspect-stretched - keeps its shape on ultrawide screens. </summary>
        [JsonProperty(Names.Rounded)]
        public bool Rounded { get; set; }

        // Colour and centre are born null - "the author never touched this" - and read back as the
        // neutrals PostProcessingRules now names. See docs/NAMING.md; this is compression rather
        // than a third state, since the neutral is also a legal authored value.

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public VignetteKey()
        {
            Intensity = 0.3f;
            Smoothness = 0.5f;
            Rounded = false;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public VignetteKey(IColor3 color3, IVector2 center, float intensity, float smoothness, bool rounded,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Color3 = color3;
            Center = center;
            Intensity = intensity;
            Smoothness = smoothness;
            Rounded = rounded;
        }
    }
}