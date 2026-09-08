using System.Reflection;
using System.Text;

namespace BH.SDK.Validations
{
    /// <summary> One step of the route to a finding: a property, plus the list index or dictionary key when the
    /// step went through a collection. </summary>
    public readonly struct RulePath
    {
        /// <summary> The property this step went through. </summary>
        public readonly PropertyInfo Property;

        /// <summary> The index or key it went through, or null when it went straight through the property. </summary>
        public readonly object Key;

        /// <summary> True when this step indexed into a collection. </summary>
        public bool HasKey => Key != null;

        /// <summary> Built from its property. </summary>
        public RulePath(PropertyInfo property)
        {
            Property = property;
            Key = null;
        }
        // key is either a List/array index (int) or a dictionary key (ObjectId, ThemeId, ...)

        /// <summary> Built from its property and key. </summary>
        public RulePath(PropertyInfo property, object key)
        {
            Property = property;
            Key = key;
        }

        /// <summary> One line, for a log. </summary>
        public override string ToString()
        {
            return HasKey ? $"{Property.Name}[{Key}]" : Property.Name;
        }

        /// <summary> Writes this step into a path being built. </summary>
        public void Append(StringBuilder builder)
        {
            builder.Append(Property.Name);
            if (!HasKey) return;
            builder.Append("[");
            builder.Append(Key);
            builder.Append("]");
        }
    }
}
