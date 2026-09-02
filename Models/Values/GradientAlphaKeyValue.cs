using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// One opacity stop of a GradientValue. Separate from the color stops so fade-in/out can be
    /// authored independently of hue.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class GradientAlphaKeyValue : IModel<GradientAlphaKeyValue>
    {
        /// <summary> Opacity at this stop, 0..1. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.AlphaShort)]
        public float Alpha { get; set; }

        /// <summary> Position along the ramp (0..1), unrelated to level frames. </summary>
        [RuleInRange(ValueRules.MinGradientTime, ValueRules.MaxGradientTime)]
        [JsonProperty(Names.TimeShort)]
        public float Time { get; set; }
        
        public GradientAlphaKeyValue()
        {
            Alpha = ValueRules.FloatOne;
            Time = ValueRules.FloatZero;
        }
        public GradientAlphaKeyValue(float alpha, float time)
        {
            Alpha = alpha;
            Time = time;
        }
    }
}