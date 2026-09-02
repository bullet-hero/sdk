using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// One control point of a CurveValue, with Bezier tangents on both sides. Unlike a level Keyframe
    /// it carries no EaseType - shape comes from the tangents here, not from a named easing.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class CurveKeyframeValue : IModel<CurveKeyframeValue>
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
    }
}