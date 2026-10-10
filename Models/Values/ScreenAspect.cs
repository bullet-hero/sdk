using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// An aspect ratio kept as its two whole numbers (16:9), not a single float. Storing the pair
    /// keeps the author's intent readable in the file and avoids rounding drift when comparing.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ScreenAspect : IModel<ScreenAspect>
    {
        /// <summary> Width part of the ratio - a proportion, not a pixel count. </summary>
        [RuleInRange(ValueRules.MinAspectWidth, ValueRules.MaxAspectWidth)]
        [JsonProperty(Names.WidthShort)]
        public int Width { get; set; }

        /// <summary> Height part of the ratio. Zero makes the aspect invalid rather than infinite -
        /// see IsValid/GetAspect. </summary>
        [RuleInRange(ValueRules.MinAspectHeight, ValueRules.MaxAspectHeight)]
        [JsonProperty(Names.HeightShort)]
        public int Height { get; set; }
        
        // No orientation metadata, deliberately: LevelSettings.Orientation already says it, and ScreenLimits
        // is a keyframe track, so it would be animatable. A vertical level authors a vertical ratio.

        /// <summary> Width over height, or zero where either is unset. </summary>
        public float GetAspect() => IsValid() ? Width / (float)Height : 0f;

        /// <summary> True when both halves were authored. </summary>
        public bool IsValid() => Width != 0f && Height != 0f;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ScreenAspect()
        {
            Width = ValueRules.DefaultAspectWidth;
            Height = ValueRules.DefaultAspectHeight;
        }
        /// <summary> Built from its width and height. </summary>
        public ScreenAspect(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}