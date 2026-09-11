using System;

namespace BH.SDK.Roslyn.Modification
{
    /// <summary> One overridable member: the number that addresses it and everything the table needs to write it. </summary>
    internal readonly struct FieldSpec : IEquatable<FieldSpec>
    {
        /// <summary> A property that carries no usable attribute, and is therefore not in the table. </summary>
        public static readonly FieldSpec Empty = default;

        /// <summary> Everything the emitter needs about one member. </summary>
        public FieldSpec(int field, string declaringType, string declaringName, string property,
            string propertyType, string elementType)
        {
            Field = field;
            DeclaringType = declaringType;
            DeclaringName = declaringName;
            Property = property;
            PropertyType = propertyType;
            ElementType = elementType;
        }

        /// <summary> The ModificationFields constant's value. </summary>
        public int Field { get; }
        /// <summary> Fully qualified name of the type DECLARING the member - never a subclass inheriting it. </summary>
        public string DeclaringType { get; }
        /// <summary> Short name of the same, for a diagnostic message. </summary>
        public string DeclaringName { get; }
        /// <summary> The C# member name. </summary>
        public string Property { get; }
        /// <summary> Fully qualified type of the member. </summary>
        public string PropertyType { get; }
        /// <summary> Fully qualified element type when the member is a List, null otherwise - which is what makes an index legal. </summary>
        public string ElementType { get; }

        /// <summary> Whether an index may address inside this member. </summary>
        public bool IsCollection => ElementType != null;

        /// <summary> Whether this spec describes a member at all. </summary>
        public bool IsEmpty => DeclaringType is null;

        /// <summary> Compared by VALUE: an incremental generator that compares its specs by reference re-emits the table on every keystroke. </summary>
        public bool Equals(FieldSpec other)
        {
            return Field == other.Field
                   && string.Equals(DeclaringType, other.DeclaringType, StringComparison.Ordinal)
                   && string.Equals(Property, other.Property, StringComparison.Ordinal)
                   && string.Equals(PropertyType, other.PropertyType, StringComparison.Ordinal)
                   && string.Equals(ElementType, other.ElementType, StringComparison.Ordinal);
        }

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is FieldSpec other && Equals(other);

        /// <summary> Compared by VALUE, see Equals. </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Field;
                hash = (hash * 397) ^ (DeclaringType?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ (Property?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
