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
        // NO ALPHA AND NO HDR PART, and both are URP's own call rather than ours: the component
        // declares its colour as `new ColorParameter(Color.black, hdr: false, showAlpha: false, ...)`,
        // so a value above one changes nothing and a fourth component is not read at all. The note
        // that used to ask for an HDR check here was asking for something the effect does not have.

        /// <summary> Color the edges fade toward; black is the usual choice, but any tint works. </summary>
        [RuleNotNull(typeof(Color3Value))]
        [JsonProperty(Names.Color)]
        public IColor3 Color3 { get; set; }

        /// <summary> Screen point the vignette opens around, in 0..1 - offset it to frame something
        /// off-center. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(PostProcessingRules.Vignette.CenterMin,
             PostProcessingRules.Vignette.CenterMax)]
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

        public VignetteKey()
        {
            Color3 = Color3Value.black;
            Center = new Vector2Value(0.5f, 0.5f);
            Intensity = 0.3f;
            Smoothness = 0.5f;
            Rounded = false;
        }
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