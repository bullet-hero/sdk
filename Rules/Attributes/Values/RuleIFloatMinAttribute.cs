using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored float value with a lower bound, in every form of it. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIFloatMinAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_ifloat_min"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_ifloat_min";

        // always include

        /// <summary> Lower bound. </summary>
        public float Min { get; set; }
        
        /// <summary> The bound, as <c>float</c>. </summary>
        public RuleIFloatMinAttribute(float min)
        {
            Min = min;
        }

        /// <summary> Applies to authored float properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IFloat).IsAssignableFrom(property.PropertyType);
        
        /// <summary> Passes when every number the value can produce is at or above the bound. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IFloat flt) return false;

            switch (flt.GetModelType())
            {
                case FloatType.Value:
                {
                    var floatValue = (FloatValue)value;
                    if (floatValue.Value < Min) return false;
                    return true;
                }
                case FloatType.RandomMinMax:
                {
                    var floatRandomMinMax = (FloatMinMax)value;
                    if (floatRandomMinMax.Min < Min) return false;
                    return true;
                }
                case FloatType.RandomMinMaxStep:
                {
                    var floatRandomMinMaxStep = (FloatMinMaxStep)value;
                    if (floatRandomMinMaxStep.Min < Min) return false;
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Clamps each form of it up to the bound. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IFloat flt) return;

            switch (flt.GetModelType())
            {
                case FloatType.Value:
                {
                    var floatValue = (FloatValue)value;
                    if (floatValue.Value < Min)
                        floatValue.Value = BHSDKMath.Max(floatValue.Value, Min);
                    break;
                }
                case FloatType.RandomMinMax:
                {
                    var floatRandomMinMax = (FloatMinMax)value;
                    if (floatRandomMinMax.Min < Min) floatRandomMinMax.Min = Min;
                    break;
                }
                case FloatType.RandomMinMaxStep:
                {
                    var floatRandomMinMaxStep = (FloatMinMaxStep)value;
                    if (floatRandomMinMaxStep.Min < Min) floatRandomMinMaxStep.Min = Min;
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
