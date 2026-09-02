using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// One color stop of a GradientValue.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class GradientColorKeyValue : IModel<GradientColorKeyValue>
    {
        // TODO maybe replace FloatValue to IFloat (color too) (in editor step)

        /// <summary> Color at this stop. Typed as the concrete Color4Value, not IColor4 - a gradient
        /// stop can't be themed or randomized, only the gradient's usage can. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Color)]
        public Color4Value Color4 { get; set; }

        /// <summary> Position along the ramp (0..1). </summary>
        [RuleInRange(ValueRules.MinGradientTime, ValueRules.MaxGradientTime)]
        [JsonProperty(Names.TimeShort)]
        public float Time { get; set; }
        
        public GradientColorKeyValue()
        {
            Color4 = Color4Value.white;
            Time = ValueRules.FloatZero;
        }
        public GradientColorKeyValue(Color4Value color4, float time)
        {
            Color4 = color4;
            Time = time;
        }
    }
}