using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Darkens (or tints) the edges of the screen, pulling attention to the center - the cheapest
    /// way to make a section feel tense without changing the level itself.
    /// </summary>
    [RuleContainer]
    public class VignetteKey : PostProcessingKeyframe, IModel<VignetteKey>
    {
        /// <summary> Color the edges fade toward; black is the usual choice, but any tint works. </summary>
        [RuleNotNull(typeof(Color4Value))] // TODO add extra part for checking HDR part
        [JsonProperty(Names.Color)]
        public IColor4 Color4 { get; set; }

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
            Color4 = Color4Value.black;
            Center = new Vector2Value(0.5f, 0.5f);
            Intensity = 0.3f;
            Smoothness = 0.5f;
            Rounded = false;
        }
        public VignetteKey(IColor4 color4, IVector2 center, float intensity, float smoothness, bool rounded,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Color4 = color4;
            Center = center;
            Intensity = intensity;
            Smoothness = smoothness;
            Rounded = rounded;
        }
        public override void Reset()
        {
            base.Reset();
            Color4 = Color4Value.black;
            Center = new Vector2Value(0.5f, 0.5f);
            Intensity = 0.3f;
            Smoothness = 0.5f;
            Rounded = false;
        }
        
        public override object Clone() => CopyImpl();
        public override PostProcessingKeyframe Copy() => CopyImpl();
        VignetteKey ICopyable<VignetteKey>.Copy() => CopyImpl();
        
        private VignetteKey CopyImpl() => new(Color4.Copy(), Center.Copy(), Intensity, Smoothness, Rounded, Active, Frame, Ease);
        
        public void Update(VignetteKey src)
        {
            base.Update(src);

            Color4 = src.Color4.Copy();
            Center = src.Center.Copy();
            Intensity = src.Intensity;
            Smoothness = src.Smoothness;
            Rounded = src.Rounded;
        }

        public void Pull(VignetteKey src)
        {
            base.Pull(src);

            Color4 = Color4.PullFrom(src.Color4);
            Center = Center.PullFrom(src.Center);
            Intensity = src.Intensity;
            Smoothness = src.Smoothness;
            Rounded = src.Rounded;
        }

        public override bool Equals(object obj) => obj is VignetteKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(),
            Color4, Center, Intensity, Smoothness, Rounded);

        public bool Equals(VignetteKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Color4.Equals(other.Color4)
                         && Center.Equals(other.Center)
                         && Intensity.Equals(other.Intensity)
                         && Smoothness.Equals(other.Smoothness)
                         && Rounded == other.Rounded;
            return result;
        }
    }
}