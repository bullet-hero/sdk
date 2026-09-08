using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored int value that must lie between two bounds, in every form of it. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIIntInRangeAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_iint_in_range"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_iint_in_range";

        // always include

        /// <summary> Lower bound. </summary>
        public int Min { get; set; }

        /// <summary> Upper bound. </summary>
        public int Max { get; set; }
        
        /// <summary> Width of the range. </summary>
        public int Diff => Max - Min;
        /// <summary> Half the width of the range - the offset from its midpoint to either end. </summary>
        public int HalfDiff => (Max - Min) / 2;

        /// <summary> Both bounds, as <c>int</c>. </summary>
        public RuleIIntInRangeAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }

        /// <summary> Applies to authored int properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IInt).IsAssignableFrom(property.PropertyType);
        
        /// <summary> Passes when every number the value can produce lies between both bounds. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IInt integer) return false;

            switch (integer.GetModelType())
            {
                case IntType.Value:
                {
                    var intValue = (IntValue)value;
                    if (intValue.Value < Min || intValue.Value > Max) return false;
                    return true;
                }
                case IntType.RandomMinMax:
                {
                    var intRandomMinMax = (IntMinMax)value;
                    if (intRandomMinMax.Min < Min || intRandomMinMax.Max > Max) return false;
                    return true;
                }
                case IntType.RandomMinMaxStep:
                {
                    var intRandomMinMaxStep = (IntMinMaxStep)value;
                    if (intRandomMinMaxStep.Min < Min || intRandomMinMaxStep.Max > Max) return false;
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary> Clamps each form of it - the plain number, and both ends of the random ones. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IInt integer) return;

            switch (integer.GetModelType())
            {
                case IntType.Value:
                {
                    var intValue = (IntValue)value;
                    if (intValue.Value < Min || intValue.Value > Max)
                        intValue.Value = BHSDKMath.Clamp(intValue.Value, Min, Max);
                    break;
                }
                case IntType.RandomMinMax:
                {
                    var intRandomMinMax = (IntMinMax)value;
                    if (intRandomMinMax.Min < Min) intRandomMinMax.Min = Min;
                    if (intRandomMinMax.Max > Max) intRandomMinMax.Max = Max;
                    break;
                }
                case IntType.RandomMinMaxStep:
                {
                    var intRandomMinMaxStep = (IntMinMaxStep)value;
                    if (intRandomMinMaxStep.Min < Min) intRandomMinMaxStep.Min = Min;
                    if (intRandomMinMaxStep.Max > Max) intRandomMinMaxStep.Max = Max;
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
