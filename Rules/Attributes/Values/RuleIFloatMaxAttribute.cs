using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored float value with an upper bound, in every form of it. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIFloatMaxAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_ifloat_max"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_ifloat_max";

        // always include

        /// <summary> Upper bound. </summary>
        public float Max { get; set; }
        
        /// <summary> The bound, as <c>float</c>. </summary>
        public RuleIFloatMaxAttribute(float max)
        {
            Max = max;
        }

        /// <summary> Applies to authored float properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IFloat).IsAssignableFrom(property.PropertyType);
        
        /// <summary> Passes when every number the value can produce is at or below the bound. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IFloat flt) return false;

            switch (flt.GetModelType())
            {
                case FloatType.Value:
                {
                    var floatValue = (FloatValue)value;
                    if (floatValue.Value > Max) return false;
                    return true;
                }
                case FloatType.RandomMinMax:
                {
                    var floatRandomMinMax = (FloatMinMax)value;
                    if (floatRandomMinMax.Max > Max) return false;
                    return true;
                }
                case FloatType.RandomMinMaxStep:
                {
                    var floatRandomMinMaxStep = (FloatMinMaxStep)value;
                    if (floatRandomMinMaxStep.Max > Max) return false;
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Clamps each form of it down to the bound. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IFloat flt) return;

            switch (flt.GetModelType())
            {
                case FloatType.Value:
                {
                    var floatValue = (FloatValue)value;
                    if (floatValue.Value > Max)
                        floatValue.Value = BHSDKMath.Min(floatValue.Value, Max);
                    break;
                }
                case FloatType.RandomMinMax:
                {
                    var floatRandomMinMax = (FloatMinMax)value;
                    if (floatRandomMinMax.Max > Max) floatRandomMinMax.Max = Max;
                    break;
                }
                case FloatType.RandomMinMaxStep:
                {
                    var floatRandomMinMaxStep = (FloatMinMaxStep)value;
                    if (floatRandomMinMaxStep.Max > Max) floatRandomMinMaxStep.Max = Max;
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
