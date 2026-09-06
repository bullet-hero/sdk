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
        
        // NO ORIENTATION METADATA HERE, DELIBERATELY. LevelSettings.Orientation already answers
        // "which way is this level held", once, for the whole level; a second answer sitting beside
        // two numbers that already imply it is a second source of truth. ScreenLimits is also a
        // KEYFRAME TRACK, so metadata on this type would be animatable - a level that rotates the
        // phone mid-run. That may one day be a feature; shipping it by accident is worse than not
        // shipping it. A level that wants vertical authors a vertical ratio, which has always been
        // legal data (MinAspect* is 1, so ScreenAspect(9, 16) validates).

        public float GetAspect() => IsValid() ? Width / (float)Height : 0f;

        public bool IsValid() => Width != 0f && Height != 0f;

        public ScreenAspect()
        {
            Width = ValueRules.DefaultAspectWidth;
            Height = ValueRules.DefaultAspectHeight;
        }
        public ScreenAspect(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}