using System;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    // RuleEnumValid's counterpart for [Flags] enums: asks "does it carry an undeclared bit". Fix masks
    // the unknown bits off instead of resetting, since a newer file's combination is mostly meaningful.

    /// <summary>
    /// A [Flags] enum field must not carry bits outside the union of its declared members. Fix clears
    /// the unrecognized bits, keeping every recognized one.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleEnumFlagsValidAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_enum_flags_valid"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_enum_flags_valid";

        /// <summary> Applies to enum properties carrying the flags attribute. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => property.PropertyType.IsEnum
               && property.PropertyType.IsDefined(typeof(FlagsAttribute), false);

        /// <summary> Passes when every set bit is one the enum declares. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            var type = value.GetType();
            if (!type.IsEnum) return false;
            return (ToBits(value) & ~DeclaredBits(type)) == 0;
        }

        /// <summary> Masks the undeclared bits off, keeping what the file did mean. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var type = property.PropertyType;
            if (!type.IsEnum) return;

            var value = property.GetValue(target);
            if (value == null) return;

            var bits = ToBits(value);
            var declared = DeclaredBits(type);
            if ((bits & ~declared) == 0) return;

            // Enum.ToObject is handed the masked value in the underlying type's OWN domain: a signed
            // enum declaring its sign bit sign-extends into a ulong that no longer fits back into an
            // int, and the unsigned overload would throw instead of writing the fix.
            var masked = bits & declared;
            property.SetValue(target, IsSigned(Enum.GetUnderlyingType(type))
                ? Enum.ToObject(type, unchecked((long)masked))
                : Enum.ToObject(type, masked));
        }

        private static ulong DeclaredBits(Type type)
        {
            var bits = 0ul;
            foreach (var member in Enum.GetValues(type))
                bits |= ToBits(member);
            return bits;
        }

        // A boxed enum cannot be unboxed straight to its underlying primitive, and a signed
        // underlying type would overflow Convert.ToUInt64 on a negative value - so route signed types
        // through Int64 and reinterpret the bit pattern.
        private static ulong ToBits(object value)
            => IsSigned(Enum.GetUnderlyingType(value.GetType()))
                ? unchecked((ulong)Convert.ToInt64(value))
                : Convert.ToUInt64(value);

        private static bool IsSigned(Type underlying) => Type.GetTypeCode(underlying)
            is TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64;
    }
}
