using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// One control point of a CurveValue, with Bezier tangents on both sides. Unlike a level Keyframe
    /// it carries no EaseType - shape comes from the tangents here, not from a named easing.
    /// </summary>
    [RuleContainer]
    public class CurveKeyframeValue : IModel<CurveKeyframeValue>
    {
        // TODO maybe replace FloatValue to IFloat (in editor step)

        /// <summary> Normalized position along the curve (0..1), not a level frame. </summary>
        [RuleInRange(ValueRules.MinCurveTime, ValueRules.MaxCurveTime)]
        [JsonProperty(Names.TimeShort)]
        public float Time { get; set; }

        /// <summary> Curve height at this point - what the evaluation actually returns. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.ValueShort)]
        public float Value { get; set; }

        /// <summary> Which sides honour InWeight/OutWeight; without it weights are ignored. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.WeightedMode)]
        public CurveWeightedMode WeightedMode { get; set; }

        /// <summary> How tangents are derived (free, auto, broken ...) - editor intent kept in the
        /// file so re-editing the curve behaves the same way it did when authored. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.TangentMode)]
        public CurveTangentMode TangentMode { get; set; }


        /// <summary> Slope arriving at this point, shaping the segment before it. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.InTangent)]
        public float InTangent { get; set; }

        /// <summary> Slope leaving this point, shaping the segment after it. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.OutTangent)]
        public float OutTangent { get; set; }

        /// <summary> How far the incoming tangent reaches; honoured only per WeightedMode. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.InWeight)]
        public float InWeight { get; set; }

        /// <summary> How far the outgoing tangent reaches; honoured only per WeightedMode. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.OutWeight)]
        public float OutWeight { get; set; }
        
        public CurveKeyframeValue()
        {
            Time = ValueRules.FloatZero;
            Value = ValueRules.FloatZero;
            WeightedMode = CurveWeightedMode.None;
            TangentMode = CurveTangentMode.Free;
            InTangent = ValueRules.FloatZero;
            OutTangent = ValueRules.FloatZero;
            InWeight = ValueRules.FloatZero;
            OutWeight = ValueRules.FloatZero;
        }
        public CurveKeyframeValue(float time, float value)
        {
            Time = time;
            Value = value;
            WeightedMode = CurveWeightedMode.None;
            TangentMode = CurveTangentMode.Free;
            InTangent = ValueRules.FloatZero;
            OutTangent = ValueRules.FloatZero;
            InWeight = ValueRules.FloatZero;
            OutWeight = ValueRules.FloatZero;
        }
        public CurveKeyframeValue(float time, float value, 
            float inTangent, float outTangent, float inWeight, float outWeight)
        {
            Time = time;
            Value = value;
            WeightedMode = CurveWeightedMode.Both;
            TangentMode = CurveTangentMode.Free;
            InTangent = inTangent;
            OutTangent = outTangent;
            InWeight = inWeight;
            OutWeight = outWeight;
        }
        public CurveKeyframeValue(float time, float value,
            CurveWeightedMode weightedMode, CurveTangentMode tangentMode,
            float inTangent, float outTangent, float inWeight, float outWeight)
        {
            Time = time;
            Value = value;
            WeightedMode = weightedMode;
            TangentMode = tangentMode;
            InTangent = inTangent;
            OutTangent = outTangent;
            InWeight = inWeight;
            OutWeight = outWeight;
        }
        public void Reset()
        {
            Time = ValueRules.FloatZero;
            Value = ValueRules.FloatZero;
            WeightedMode = CurveWeightedMode.None;
            TangentMode = CurveTangentMode.Free;
            InTangent = ValueRules.FloatZero;
            OutTangent = ValueRules.FloatZero;
            InWeight = ValueRules.FloatZero;
            OutWeight = ValueRules.FloatZero;
        }

        public object Clone() => Copy();
        public CurveKeyframeValue Copy() => new(Time, Value,
            WeightedMode, TangentMode, InTangent, OutTangent, InWeight, OutWeight);

        public void Update(CurveKeyframeValue src)
        {
            Time = src.Time;
            Value = src.Value;
            WeightedMode = src.WeightedMode;
            TangentMode = src.TangentMode;
            InTangent = src.InTangent;
            OutTangent = src.OutTangent;
            InWeight = src.InWeight;
            OutWeight = src.OutWeight;
        }

        public void Pull(CurveKeyframeValue src)
        {
            Time = src.Time;
            Value = src.Value;
            WeightedMode = src.WeightedMode;
            TangentMode = src.TangentMode;
            InTangent = src.InTangent;
            OutTangent = src.OutTangent;
            InWeight = src.InWeight;
            OutWeight = src.OutWeight;
        }

        public override bool Equals(object obj) => obj is CurveKeyframeValue value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Time, Value,
            (int)WeightedMode, (int)TangentMode, InTangent, OutTangent, InWeight, OutWeight);

        public bool Equals(CurveKeyframeValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Time.Equals(other.Time)
                         && Value.Equals(other.Value)
                         && WeightedMode == other.WeightedMode
                         && TangentMode == other.TangentMode
                         && InTangent.Equals(other.InTangent)
                         && OutTangent.Equals(other.OutTangent)
                         && InWeight.Equals(other.InWeight)
                         && OutWeight.Equals(other.OutWeight);
            return result;
        }
    }
}