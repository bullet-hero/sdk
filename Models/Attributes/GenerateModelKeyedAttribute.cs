using System;

namespace BH.SDK.Models.Attributes
{
    // Names the property a dictionary key is recovered from, so the collection writes as an array of
    // values with the key dropped. Without it: an array of {K,V} pairs (string keys: a JSON object).

    /// <summary>
    /// Names the property on the VALUE that holds the dictionary's key, so the collection can be
    /// written as a bare array and rebuilt from it on read.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class GenerateModelKeyedAttribute : Attribute
    {
        /// <summary> Built from its property. </summary>
        public GenerateModelKeyedAttribute(string keyProperty) => KeyProperty = keyProperty;

        /// <summary> Property name on the value type. </summary>
        public string KeyProperty { get; }
    }
}
