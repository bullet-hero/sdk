using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored float value that must lie between two bounds - every form of it, the random ones
    /// included, so a range cannot be authored partly outside what the field accepts. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIFloatInRangeAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_ifloat_in_range"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_ifloat_in_range";

        // always include

        /// <summary> Lower bound. </summary>
        public float Min { get; set; }

        /// <summary> Upper bound. </summary>
        public float Max { get; set; }
        
        /// <summary> Width of the range. </summary>
        public float Diff => Max - Min;
        /// <summary> Half the width of the range - the offset from its midpoint to either end. </summary>
        public float HalfDiff => (Max - Min) / 2f;

        /// <summary> Both bounds, as <c>float</c>. </summary>
        public RuleIFloatInRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        /// <summary> Applies to authored float properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IFloat).IsAssignableFrom(property.PropertyType);
        
        /// <summary> Passes when every number the value can produce lies between both bounds. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IFloat flt) return false;

            switch (flt.GetModelType())
            {
                case FloatType.Value:
                {
                    var floatValue = (FloatValue)value;
                    if (floatValue.Value < Min || floatValue.Value > Max) return false;
                    return true;
                }
                case FloatType.RandomMinMax:
                {
                    var floatRandomMinMax = (FloatMinMax)value;
                    if (floatRandomMinMax.Min < Min || floatRandomMinMax.Max > Max) return false;
                    return true;
                }
                case FloatType.RandomMinMaxStep:
                {
                    var floatRandomMinMaxStep = (FloatMinMaxStep)value;
                    if (floatRandomMinMaxStep.Min < Min || floatRandomMinMaxStep.Max > Max) return false;
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Clamps each form of it - the plain number, and both ends of the random ones. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IFloat flt) return;

            switch (flt.GetModelType())
            {
                case FloatType.Value:
                {
                    var floatValue = (FloatValue)value;
                    if (floatValue.Value < Min || floatValue.Value > Max)
                        floatValue.Value = BHSDKMath.Clamp(floatValue.Value, Min, Max);
                    break;
                }
                case FloatType.RandomMinMax:
                {
                    var floatRandomMinMax = (FloatMinMax)value;
                    if (floatRandomMinMax.Min < Min) floatRandomMinMax.Min = Min;
                    if (floatRandomMinMax.Max > Max) floatRandomMinMax.Max = Max;
                    break;
                }
                case FloatType.RandomMinMaxStep:
                {
                    var floatRandomMinMaxStep = (FloatMinMaxStep)value;
                    if (floatRandomMinMaxStep.Min < Min) floatRandomMinMaxStep.Min = Min;
                    if (floatRandomMinMaxStep.Max > Max) floatRandomMinMaxStep.Max = Max;
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
