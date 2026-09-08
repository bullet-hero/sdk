using System;
using System.Collections.Generic;

namespace BH.SDK.Utils
{
    /// <summary> The reflection questions the rule walk and the modification walk both ask: is this a list or a
    /// dictionary, and what does it hold. </summary>
    public static class TypeExtensions
    {
        /// <summary> True for a closed List. </summary>
        public static bool IsList(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }
        /// <summary> What that list holds, or null when it is not one. </summary>
        public static Type GetListGenericParameterOrDefault(this Type type)
        {
            return type.IsList() ? type.GetGenericArguments()[0] : null;
        }
        /// <summary> True for a closed Dictionary. </summary>
        public static bool IsDictionary(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }
        /// <summary> What that dictionary's values are, or null when it is not one. </summary>
        public static Type GetDictionaryValueGenericParameterOrDefault(this Type type)
        {
            return type.IsDictionary() ? type.GetGenericArguments()[1] : null;
        }
    }
}