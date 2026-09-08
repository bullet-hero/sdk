using System;
using System.Linq;
using System.Reflection;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> A reference that must be set. Repaired by constructing an empty one, never left null. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleNotNullAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_not_null"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_not_null";

        /// <summary> Which type a repair constructs, for a property typed as an interface or an abstract class. </summary>
        public Type DefaultConstructType { get; set; }

        /// <summary> Arguments that constructor takes, for a type with no parameterless one. </summary>
        public object[] DefaultConstructArgs { get; set; }

        /// <summary> Their types, so the right overload is picked when an argument is null. </summary>
        public Type[] DefaultConstructArgTypes { get; set; }

        /// <summary> Takes nothing; the defaults apply. </summary>
        public RuleNotNullAttribute()
        {
            
        }
        /// <summary> Takes <c>defaultConstructType</c>. </summary>
        public RuleNotNullAttribute(Type defaultConstructType)
        {
            DefaultConstructType = defaultConstructType;
        }
        /// <summary> Takes <c>defaultConstructArgs</c>. </summary>
        public RuleNotNullAttribute(params object[] defaultConstructArgs)
        {
            DefaultConstructArgs = defaultConstructArgs;
            DefaultConstructArgTypes = defaultConstructArgs
                .Select(a => a?.GetType() ?? typeof(object)).ToArray();
        }
        /// <summary> Takes <c>defaultConstructType</c> and <c>defaultConstructArgs</c>. </summary>
        public RuleNotNullAttribute(Type defaultConstructType, params object[] defaultConstructArgs)
        {
            DefaultConstructType = defaultConstructType;
            DefaultConstructArgs = defaultConstructArgs;
            DefaultConstructArgTypes = defaultConstructArgs
                .Select(a => a?.GetType() ?? typeof(object)).ToArray();
        }

        /// <summary> Applies to any property that can hold null - a reference, or a nullable value type. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null;

        /// <summary> Passes on anything that is not null. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            return value != null;
        }
        /// <summary> Constructs one, from DefaultConstructType and its arguments where the property's own type cannot be built directly. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var valueType = DefaultConstructType ?? property.PropertyType;
            var value = CreateDefaultValue(valueType);
            if (value != null) property.SetValue(target, value);
        }
        
        private object CreateDefaultValue(Type type)
        {
            // struct
            if (type.IsValueType) return Activator.CreateInstance(type);
            
            // "Special" types
            if (type == typeof(string)) return string.Empty;
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return Array.CreateInstance(elementType, 0);
            }
            if (type.IsList())
            {
                return Activator.CreateInstance(type);
            }
            
            // Optional ctor with parameters
            if (DefaultConstructArgs != null && DefaultConstructArgs.Length > 0)
            {
                var ctor = type.GetConstructor(DefaultConstructArgTypes);
                if (ctor != null) return ctor.Invoke(DefaultConstructArgs);
            }
            // Parameterless ctor
            return type.GetConstructor(Type.EmptyTypes)?.Invoke(null);
        }
    }
}
