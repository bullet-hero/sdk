using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// One opacity stop of a GradientValue. Separate from the color stops so fade-in/out can be
    /// authored independently of hue.
    /// </summary>
    [RuleContainer]
    public class GradientAlphaKeyValue : IModel<GradientAlphaKeyValue>
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
        public void Reset()
        {
            Alpha = ValueRules.FloatOne;
            Time = ValueRules.FloatZero;
        }

        public object Clone() => Copy();
        public GradientAlphaKeyValue Copy() => new(Alpha, Time);

        public void Update(GradientAlphaKeyValue src)
        {
            Alpha = src.Alpha;
            Time = src.Time;
        }

        public void Pull(GradientAlphaKeyValue src)
        {
            Alpha = src.Alpha;
            Time = src.Time;
        }

        public override bool Equals(object obj) => obj is GradientAlphaKeyValue value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Alpha, Time);

        public bool Equals(GradientAlphaKeyValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Alpha.Equals(other.Alpha)
                         && Time.Equals(other.Time);
            return result;
        }
    }
}