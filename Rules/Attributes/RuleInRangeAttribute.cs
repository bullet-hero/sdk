using System;
using System.Globalization;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> A number that must lie between two bounds. One overload per numeric type, so the bounds are
    /// written as literals at the call site rather than boxed by hand. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleInRangeAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_in_range"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_in_range";

        /// <summary> Lower bound, converted to the property's own type when the rule runs. </summary>
        public object Min { get; set; } // always include

        /// <summary> Upper bound, converted to the property's own type when the rule runs. </summary>
        public object Max { get; set; } // always include

        /// <summary> What a repair writes instead of the nearer bound, when a bound is not the right answer. </summary>
        public object DefaultValue { get; set; }

        /// <summary> Both bounds, as <c>byte</c>. </summary>
        public RuleInRangeAttribute(byte min, byte max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>sbyte</c>. </summary>
        public RuleInRangeAttribute(sbyte min, sbyte max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>short</c>. </summary>
        public RuleInRangeAttribute(short min, short max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>ushort</c>. </summary>
        public RuleInRangeAttribute(ushort min, ushort max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>int</c>. </summary>
        public RuleInRangeAttribute(int min, int max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>uint</c>. </summary>
        public RuleInRangeAttribute(uint min, uint max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>long</c>. </summary>
        public RuleInRangeAttribute(long min, long max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>ulong</c>. </summary>
        public RuleInRangeAttribute(ulong min, ulong max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>float</c>. </summary>
        public RuleInRangeAttribute(float min, float max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>double</c>. </summary>
        public RuleInRangeAttribute(double min, double max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>decimal</c>. </summary>
        public RuleInRangeAttribute(decimal min, decimal max) { Min = min; Max = max; }
        /// <summary> Both bounds, as <c>object</c>. </summary>
        public RuleInRangeAttribute(object min, object max) { Min = min; Max = max; }

        /// <summary> Both bounds as <c>byte</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(byte min, byte max, byte defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>sbyte</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(sbyte min, sbyte max, sbyte defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>short</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(short min, short max, short defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>ushort</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(ushort min, ushort max, ushort defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>int</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(int min, int max, int defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>uint</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(uint min, uint max, uint defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>long</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(long min, long max, long defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>ulong</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(ulong min, ulong max, ulong defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>float</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(float min, float max, float defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>double</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(double min, double max, double defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>decimal</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(decimal min, decimal max, decimal defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }
        /// <summary> Both bounds as <c>object</c>, plus what a repair writes instead of one of them. </summary>
        public RuleInRangeAttribute(object min, object max, object defaultValue)
            { Min = min; Max = max; DefaultValue = defaultValue; }

        private static readonly Type[] SupportedTypes =
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        /// <summary> Applies to any of the numeric types listed above. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => Array.IndexOf(SupportedTypes, property.PropertyType) >= 0;

        /// <summary> Passes when the value lies between both bounds inclusively. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (Min == null || Max == null) return false;

            var type = value.GetType();
            var min = ConvertBoundary(type, Min);
            var max = ConvertBoundary(type, Max);
            var convertedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            if (convertedValue is not IComparable comparableValue) return false;
            
            return comparableValue.CompareTo(min) >= 0 && comparableValue.CompareTo(max) <= 0;
        }

        /// <summary> Clamps to whichever bound was crossed, or writes DefaultValue when one was given. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (Min == null || Max == null) return;

            var value = property.GetValue(target);
            if (value == null) return;

            var type = property.PropertyType;
            var min = ConvertBoundary(type, Min);
            var max = ConvertBoundary(type, Max);
            var convertedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            if (convertedValue is not IComparable comparableValue) return;
            
            if (comparableValue.CompareTo(min) < 0)
            {
                var newValue = DefaultValue ?? min;
                property.SetValue(target, newValue);
            }
            else if (comparableValue.CompareTo(max) > 0)
            {
                var newValue = DefaultValue ?? max;
                property.SetValue(target, newValue);
            }
        }
        
        private static object ConvertBoundary(Type targetType, object boundary)
        {
            return targetType.IsInstanceOfType(boundary)
                ? boundary : Convert.ChangeType(boundary, targetType, CultureInfo.InvariantCulture);
        }
    }
}
