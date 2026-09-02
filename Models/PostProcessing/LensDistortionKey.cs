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
    /// Bends the image like a wide or fisheye lens. Note it warps only what is drawn - collision
    /// still uses the undistorted positions, so heavy values change the picture, not the fairness.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class LensDistortionKey : PostProcessingKeyframe, IModel<LensDistortionKey>
    {
        /// <summary> Amount of bend: negative pinches inward, positive bulges outward. </summary>
        [RuleInRange(PostProcessingRules.LensDistortion.IntensityMin,
            PostProcessingRules.LensDistortion.IntensityMax)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }

        /// <summary> Per-axis weight of the distortion - zero on one axis limits the bend to the
        /// other. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(PostProcessingRules.LensDistortion.MultiplierMin,
             PostProcessingRules.LensDistortion.MultiplierMax)]
        [JsonProperty(Names.Multiplier)]
        public IVector2 Multiplier { get; set; }

        /// <summary> Screen point the distortion radiates from. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(PostProcessingRules.LensDistortion.CenterMin,
             PostProcessingRules.LensDistortion.CenterMax)]
        [JsonProperty(Names.Center)]
        public IVector2 Center { get; set; }

        /// <summary> Zoom applied after bending, to crop away the empty edges distortion leaves. </summary>
        [RuleInRange(PostProcessingRules.LensDistortion.ScaleMin,
            PostProcessingRules.LensDistortion.ScaleMax)]
        [JsonProperty(Names.Scale)]
        public float Scale { get; set; }

        public LensDistortionKey()
        {
            Intensity = 0.5f;
            Multiplier = new Vector2Value(1f, 1f);
            Center = new Vector2Value(0.5f, 0.5f);
            Scale = 1f;
        }
        public LensDistortionKey(float intensity, IVector2 multiplier, IVector2 center, float scale,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Intensity = intensity;
            Multiplier = multiplier;
            Center = center;
            Scale = scale;
        }
    }
}