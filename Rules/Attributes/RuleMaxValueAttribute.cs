using System;
using System.Globalization;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> A number with an upper bound and no lower one. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleMaxValueAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_max_value"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_max_value";

        /// <summary> Upper bound, converted to the property's own type when the rule runs. </summary>
        public object Max { get; set; } // always include

        /// <summary> What a repair writes instead of the nearer bound, when a bound is not the right answer. </summary>
        public object DefaultValue { get; set; }

        /// <summary> The bound, as <c>byte</c>. </summary>
        public RuleMaxValueAttribute(byte max) { Max = max; }
        /// <summary> The bound, as <c>sbyte</c>. </summary>
        public RuleMaxValueAttribute(sbyte max) { Max = max; }
        /// <summary> The bound, as <c>short</c>. </summary>
        public RuleMaxValueAttribute(short max) { Max = max; }
        /// <summary> The bound, as <c>ushort</c>. </summary>
        public RuleMaxValueAttribute(ushort max) { Max = max; }
        /// <summary> The bound, as <c>int</c>. </summary>
        public RuleMaxValueAttribute(int max) { Max = max; }
        /// <summary> The bound, as <c>uint</c>. </summary>
        public RuleMaxValueAttribute(uint max) { Max = max; }
        /// <summary> The bound, as <c>long</c>. </summary>
        public RuleMaxValueAttribute(long max) { Max = max; }
        /// <summary> The bound, as <c>ulong</c>. </summary>
        public RuleMaxValueAttribute(ulong max) { Max = max; }
        /// <summary> The bound, as <c>float</c>. </summary>
        public RuleMaxValueAttribute(float max) { Max = max; }
        /// <summary> The bound, as <c>double</c>. </summary>
        public RuleMaxValueAttribute(double max) { Max = max; }
        /// <summary> The bound, as <c>decimal</c>. </summary>
        public RuleMaxValueAttribute(decimal max) { Max = max; }
        /// <summary> The bound, as <c>object</c>. </summary>
        public RuleMaxValueAttribute(object max) { Max = max; }

        /// <summary> The bound as <c>byte</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(byte max, byte defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>sbyte</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(sbyte max, sbyte defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>short</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(short max, short defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>ushort</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(ushort max, ushort defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>int</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(int max, int defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>uint</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(uint max, uint defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>long</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(long max, long defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>ulong</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(ulong max, ulong defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>float</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(float max, float defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>double</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(double max, double defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>decimal</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(decimal max, decimal defaultValue) { Max = max; DefaultValue = defaultValue; }
        /// <summary> The bound as <c>object</c>, plus what a repair writes instead of it. </summary>
        public RuleMaxValueAttribute(object max, object defaultValue) { Max = max; DefaultValue = defaultValue; }

        private static readonly Type[] SupportedTypes =
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        /// <summary> Applies to any of the numeric types listed above. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => Array.IndexOf(SupportedTypes, property.PropertyType) >= 0;

        /// <summary> Passes when the value is at or below the bound. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (Max == null) return false;

            var type = value.GetType();
            var max = ConvertBoundary(type, Max);
            var convertedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            if (convertedValue is not IComparable comparableValue) return false;
            
            return comparableValue.CompareTo(max) <= 0;
        }

        /// <summary> Clamps down to the bound, or writes DefaultValue when one was given. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (Max == null) return;

            var value = property.GetValue(target);
            if (value == null) return;

            var type = property.PropertyType;
            var max = ConvertBoundary(type, Max);
            var convertedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            if (convertedValue is not IComparable comparableValue) return;
            
            if (comparableValue.CompareTo(max) > 0)
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
