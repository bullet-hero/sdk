using System;
using System.Globalization;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    [AttributeUsage(PropertyTarget)]
    public class RuleMinValueAttribute : BasePropertyRuleAttribute
    {
        public override string RuleNameKey => "rule_min_value";

        public object Min { get; set; } // always include
        public object DefaultValue { get; set; }

        public RuleMinValueAttribute(byte min) { Min = min; }
        public RuleMinValueAttribute(sbyte min) { Min = min; }
        public RuleMinValueAttribute(short min) { Min = min; }
        public RuleMinValueAttribute(ushort min) { Min = min; }
        public RuleMinValueAttribute(int min) { Min = min; }
        public RuleMinValueAttribute(uint min) { Min = min; }
        public RuleMinValueAttribute(long min) { Min = min; }
        public RuleMinValueAttribute(ulong min) { Min = min; }
        public RuleMinValueAttribute(float min) { Min = min; }
        public RuleMinValueAttribute(double min) { Min = min; }
        public RuleMinValueAttribute(decimal min) { Min = min; }
        public RuleMinValueAttribute(object min) { Min = min; }
        
        public RuleMinValueAttribute(byte min, byte defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(sbyte min, sbyte defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(short min, short defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(ushort min, ushort defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(int min, int defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(uint min, uint defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(long min, long defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(ulong min, ulong defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(float min, float defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(double min, double defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(decimal min, decimal defaultValue) { Min = min; DefaultValue = defaultValue; }
        public RuleMinValueAttribute(object min, object defaultValue) { Min = min; DefaultValue = defaultValue; }

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
            if (Min == null) return false;

            var type = value.GetType();
            var min = ConvertBoundary(type, Min);
            var convertedValue = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);

            if (convertedValue is not IComparable comparableValue) return false;
            
            return comparableValue.CompareTo(min) >= 0;
        }

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
