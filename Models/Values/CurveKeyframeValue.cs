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
        /// <summary> Normalized position along the curve (0..1), not a level frame. </summary>
        [RuleInRange(ValueRules.MinCurveTime, ValueRules.MaxCurveTime)]
        [JsonProperty(Names.TimeShort)]
        public float Time { get; set; }

        /// <summary> Curve height at this point - what the evaluation actually returns. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.ValueShort)]
        public float Value { get; set; }

        /// <summary> Which sides honour InWeight/OutWeight; without it weights are ignored. </summary>
        [RuleEnumFlagsValid]
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

        // A SEPARATE BIT RATHER THAN A TangentMode MEMBER, which is what CurveTangentMode's own
        // header prescribes: in Unity brokenness is independent of how each side's tangent is
        // derived, so "Auto and broken" is a real state no fifth enum member could express.
        //
        // It is authored intent, not data - InTangent and OutTangent have always been two separate
        // numbers, so a broken key was already REPRESENTABLE and a Unity preset imported through
        // LevelValuesExtensions could already carry one. What was missing is whether the author MEANT
        // the two to differ, and without that an editor dragging one handle has to guess: mirror the
        // other side and silently straighten an imported corner, or never mirror and make an ordinary
        // smooth key impossible to keep smooth.
        //
        // Additive with a false default, so every curve authored before it reads back unbroken -
        // which is what every one of them already was as far as any editor was concerned.

        /// <summary> Whether the two tangents are meant to differ - a corner rather than a smooth
        /// pass-through. False mirrors one side onto the other as it is dragged. </summary>
        [JsonProperty(Names.BrokenTangents)]
        public bool BrokenTangents { get; set; }
        
        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
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
            BrokenTangents = false;
        }
        /// <summary> Built from its time and value. </summary>
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
            BrokenTangents = false;
        }
        /// <summary> Every member at once, in declaration order. </summary>
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
            BrokenTangents = false;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public CurveKeyframeValue(float time, float value,
            CurveWeightedMode weightedMode, CurveTangentMode tangentMode,
            float inTangent, float outTangent, float inWeight, float outWeight,
            bool brokenTangents = false)
        {
            Time = time;
            Value = value;
            WeightedMode = weightedMode;
            TangentMode = tangentMode;
            InTangent = inTangent;
            OutTangent = outTangent;
            InWeight = inWeight;
            OutWeight = outWeight;
            BrokenTangents = brokenTangents;
        }
    }
}