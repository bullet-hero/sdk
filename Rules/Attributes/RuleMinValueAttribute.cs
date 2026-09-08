using System;
using System.Globalization;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> A number with a lower bound and no upper one. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleMinValueAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_min_value"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_min_value";

        /// <summary> Lower bound, converted to the property's own type when the rule runs. </summary>
        public object Min { get; set; } // always include

        /// <summary> What a repair writes instead of the nearer bound, when a bound is not the right answer. </summary>
        public object DefaultValue { get; set; }

        /// <summary> The bound, as <c>byte</c>. </summary>
        public RuleMinValueAttribute(byte min) { Min = min; }
        /// <summary> The bound, as <c>sbyte</c>. </summary>
        public RuleMinValueAttribute(sbyte min) { Min = min; }
        /// <summary> The bound, as <c>short</c>. </summary>
        public RuleMinValueAttribute(short min) { Min = min; }
        /// <summary> The bound, as <c>ushort</c>. </summary>
        public RuleMinValueAttribute(ushort min) { Min = min; }
        /// <summary> The bound, as <c>int</c>. </summary>
        public RuleMinValueAttribute(int min) { Min = min; }
        /// <summary> The bound, as <c>uint</c>. </summary>
        public RuleMinValueAttribute(uint min) { Min = min; }
        /// <summary> The bound, as <c>long</c>. </summary>
        public RuleMinValueAttribute(long min) { Min = min; }
        /// <summary> The bound, as <c>ulong</c>. </summary>
        public RuleMinValueAttribute(ulong min) { Min = min; }
        /// <summary> The bound, as <c>float</c>. </summary>
        public RuleMinValueAttribute(float min) { Min = min; }
        /// <summary> The bound, as <c>double</c>. </summary>
        public RuleMinValueAttribute(double min) { Min = min; }
        /// <summary> The bound, as <c>decimal</c>. </summary>
        public RuleMinValueAttribute(decimal min) { Min = min; }
        /// <summary> The bound, as <c>object</c>. </summary>
        public RuleMinValueAttribute(object min) { Min = min; }
        
        /// <summary> The bound as <c>byte</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(byte min, byte defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>sbyte</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(sbyte min, sbyte defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>short</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(short min, short defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>ushort</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(ushort min, ushort defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>int</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(int min, int defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>uint</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(uint min, uint defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>long</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(long min, long defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>ulong</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(ulong min, ulong defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>float</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(float min, float defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>double</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(double min, double defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>decimal</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(decimal min, decimal defaultValue) { Min = min; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>object</c>, plus what a repair writes instead of it. </summary>
        public RuleMinValueAttribute(object min, object defaultValue) { Min = min; DefaultValue = defaultValue; }

        private static readonly Type[] SupportedTypes =
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        /// <summary> Applies to any of the numeric types listed above. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => Array.IndexOf(SupportedTypes, property.PropertyType) >= 0;

        /// <summary> Passes when the value is at or above the bound. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (Min == null) return false;

            var type = value.GetType();
            var min = ConvertBoundary(type, Min);
            var convertedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            if (convertedValue is not IComparable comparableValue) return false;
            
            return comparableValue.CompareTo(min) >= 0;
        }

        /// <summary> Clamps up to the bound, or writes DefaultValue when one was given. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (Min == null) return;

            var value = property.GetValue(target);
            if (value == null) return;

            var type = property.PropertyType;
            var min = ConvertBoundary(type, Min);
            var convertedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            if (convertedValue is not IComparable comparableValue) return;

            if (comparableValue.CompareTo(min) < 0)
            {
                var newValue = DefaultValue ?? min;
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
