using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// Clamps the view between two aspect ratios - the practical middle ground between None and
    /// Fixed: a level plays natively on anything from phone to ultrawide, letterboxed only outside.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ScreenLimitBounds : IScreenLimit, IModel<ScreenLimitBounds>
    {
        /// <summary> Narrowest ratio still shown as-is; anything narrower is clamped to it. </summary>
        [RuleNotNull]
        [JsonProperty(Names.MinAspect)]
        public ScreenAspect MinAspect { get; set; }

        /// <summary> Widest ratio still shown as-is; anything wider is clamped to it. </summary>
        [RuleNotNull]
        [JsonProperty(Names.MaxAspect)]
        public ScreenAspect MaxAspect { get; set; }
        
        public ScreenLimitType GetModelType() => ScreenLimitType.Bounds;
        public bool IsValid(float currentAspect)
        {
            var minAspect = MinAspect.GetAspect();
            if (currentAspect < minAspect) return false;
            
            var maxAspect = MaxAspect.GetAspect();
            if (currentAspect > maxAspect) return false;
            
            return true;
        }
        public float GetValid(float currentAspect)
        {
            var minAspect = MinAspect.GetAspect();
            var maxAspect = MaxAspect.GetAspect();
            return BHSDKMath.Clamp(currentAspect, minAspect, maxAspect);
        }

        public ScreenLimitBounds()
        {
            MinAspect = new ScreenAspect();
            MaxAspect = new ScreenAspect();
        }
        public ScreenLimitBounds(ScreenAspect minAspect, ScreenAspect maxAspect)
        {
            MinAspect = minAspect;
            MaxAspect = maxAspect;
        }
    }
}