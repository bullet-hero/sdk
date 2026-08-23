using System;
using System.Globalization;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    [AttributeUsage(PropertyTarget)]
    public class RuleMaxValueAttribute : BasePropertyRuleAttribute
    {
        public override string RuleNameKey => "rule_max_value";

        public object Max { get; set; } // always include
        public object DefaultValue { get; set; }

        public RuleMaxValueAttribute(byte max) { Max = max; }
        public RuleMaxValueAttribute(sbyte max) { Max = max; }
        public RuleMaxValueAttribute(short max) { Max = max; }
        public RuleMaxValueAttribute(ushort max) { Max = max; }
        public RuleMaxValueAttribute(int max) { Max = max; }
        public RuleMaxValueAttribute(uint max) { Max = max; }
        public RuleMaxValueAttribute(long max) { Max = max; }
        public RuleMaxValueAttribute(ulong max) { Max = max; }
        public RuleMaxValueAttribute(float max) { Max = max; }
        public RuleMaxValueAttribute(double max) { Max = max; }
        public RuleMaxValueAttribute(decimal max) { Max = max; }
        public RuleMaxValueAttribute(object max) { Max = max; }

        public RuleMaxValueAttribute(byte max, byte defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(sbyte max, sbyte defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(short max, short defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(ushort max, ushort defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(int max, int defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(uint max, uint defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(long max, long defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(ulong max, ulong defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(float max, float defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(double max, double defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(decimal max, decimal defaultValue) { Max = max; DefaultValue = defaultValue; }
        public RuleMaxValueAttribute(object max, object defaultValue) { Max = max; DefaultValue = defaultValue; }

        private static readonly Type[] SupportedTypes =
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        protected override bool IsValidTypeInternal(PropertyInfo property)
            => Array.IndexOf(SupportedTypes, property.PropertyType) >= 0;

        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (Max == null) return false;

            var type = value.GetType();
            var max = ConvertBoundary(type, Max);
            var convertedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            if (convertedValue is not IComparable comparableValue) return false;
            
            return comparableValue.CompareTo(max) <= 0;
        }

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
