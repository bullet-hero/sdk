using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Validations;

namespace BH.SDK.Utils
{
    /// <summary> Conversions between the model types that keep meeting each other - pixels and colours, values
    /// and their primitives - plus the formatting a validation finding is printed with. </summary>
    public static class ModelUtils
    {
        private const float ByteMaxValue = byte.MaxValue;
        
        /// <summary> A byte pixel as the authored 0-1 colour a level stores. </summary>
        public static Color4Value ToColorValue(this Pixel pixel) => new(
            pixel.r / ByteMaxValue, 
            pixel.g / ByteMaxValue, 
            pixel.b / ByteMaxValue, 
            pixel.a / ByteMaxValue);
        
        /// <summary> The other way round. </summary>
        public static Pixel ToPixel(this Color4Value color4Value) => new(
            (byte)(color4Value.R * ByteMaxValue),
            (byte)(color4Value.G * ByteMaxValue),
            (byte)(color4Value.B * ByteMaxValue),
            (byte)(color4Value.A * ByteMaxValue));

        // Reset keeps the collection INSTANCE: whatever holds the lists must still hold them.

        /// <summary> Empties target and refills it from defaults, keeping target's own instance. </summary>
        public static List<T> ResetTo<T>(this List<T> target, List<T> defaults)
        {
            if (target is null) return defaults;
            target.Clear();
            if (defaults != null) target.AddRange(defaults);
            return target;
        }

        /// <summary> The dictionary form. </summary>
        public static Dictionary<TKey, TValue> ResetTo<TKey, TValue>(
            this Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> defaults)
        {
            if (target is null) return defaults;
            target.Clear();
            if (defaults != null)
                foreach (var pair in defaults)
                    target[pair.Key] = pair.Value;
            return target;
        }

        /// <summary> The array form. An array cannot resize, so a length that does not match is
        /// the one case where the instance has to go. </summary>
        public static T[] ResetTo<T>(this T[] target, T[] defaults)
        {
            if (target is null || defaults is null || target.Length != defaults.Length) return defaults;
            defaults.CopyTo(target, 0);
            return target;
        }

        // Two array copies, like three dictionary copies: a class element needs a deep copy, and `class` in
        // the constraint makes the wrong call a compile error (Docs/UTILS_DESIGN.md).

        /// <summary> Deep copy: every element is copied too. </summary>
        public static T[] CopyArray<T>(this T[] array) where T : class, ICopyable<T>
        {
            var copyArray = new T[array.Length];
            for (var i = 0; i < array.Length; i++)
                copyArray[i] = array[i]?.Copy();
            return copyArray;
        }

        /// <summary> Blit copy, for an element that IS its value - a pixel, an id. Millions of
        /// elements at a time here (a 4096x4096 texture), so it must not call anything per item. </summary>
        public static T[] CopyArrayUnmanaged<T>(this T[] array) where T : unmanaged
        {
            var copyArray = new T[array.Length];
            array.CopyTo(copyArray, 0);
            return copyArray;
        }

        /// <summary> A new list of the same items, or null when there was no list. Null is a legal
        /// state for a list the format writes as a whole-or-nothing member, and copying one has to
        /// preserve it rather than throw inside List's own copy constructor. </summary>
        public static List<T> CopyValueList<T>(this List<T> list) =>
            list is null ? null : new List<T>(list);

        /// <summary> A new list holding copies, not the same instances. </summary>
        public static List<T> CopyList<T>(this List<T> list) where T : ICopyable<T>
        {
            var copyList = new List<T>(list.Count);
            foreach (var item in list)
                copyList.Add(item.Copy());
            return copyList;
        }

        /// <summary> A new dictionary, copying values that know how and taking the rest as they are. </summary>
        public static Dictionary<TKey, TValue> CopyDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
            where TKey : unmanaged where TValue : ICopyable<TValue>
        {
            var copyDictionary = new Dictionary<TKey, TValue>(dictionary.Count);
            foreach (var (key, value) in dictionary)
                copyDictionary.Add(key, value.Copy());
            return copyDictionary;
        }

        /// <summary> A new dictionary of values that need no copying - a shallow copy IS a deep one there. </summary>
        public static Dictionary<TKey, TValue> CopyDictionaryUnmanaged<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
            where TKey : unmanaged where TValue : unmanaged
        {
            var copyDictionary = new Dictionary<TKey, TValue>(dictionary.Count);
            foreach (var (key, value) in dictionary)
                copyDictionary.Add(key, value);
            return copyDictionary;
        }

        /// <summary> A new dictionary holding copies of every value. </summary>
        public static Dictionary<TKey, TValue> CopyDictionaryManaged<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
            where TKey : ICopyable<TKey> where TValue : ICopyable<TValue>
        {
            var copyDictionary = new Dictionary<TKey, TValue>(dictionary.Count);
            foreach (var (key, value) in dictionary)
                copyDictionary.Add(key.Copy(), value.Copy());
            return copyDictionary;
        }

        // The one place a POLYMORPHIC field is merged: an interface-level Pull is a no-op on a sibling type,
        // so this returns a replacement instead. A concrete model field calls Pull directly.

        /// <summary> Merges source into target in place while their concrete types allow it, and
        /// returns what the field must now hold - the same instance, or a fresh copy of source. </summary>
        public static T PullFrom<T>(this T target, T source) where T : class, IModel<T>
        {
            if (source is null) return null;
            if (target is null || target.GetType() != source.GetType()) return source.Copy();
            target.Pull(source);
            return target;
        }

        // Keyed counterpart of PullFrom, for collections something HOLDS INTO (selection, op buffer, prefab
        // children). pullValue is a delegate: a base-typed Pull writes the base half only
        // (Docs/UTILS_DESIGN.md).

        /// <summary> Merges source into target key by key: keys source no longer has are dropped,
        /// and every remaining value is whatever pullValue returns for the pair (a null target means
        /// the key is new). The dictionary instance itself is never replaced. </summary>
        public static void PullDictionary<TKey, TValue>(this Dictionary<TKey, TValue> target,
            Dictionary<TKey, TValue> source, Func<TValue, TValue, TValue> pullValue)
        {
            if (ReferenceEquals(target, source)) return;

            List<TKey> stale = null;
            foreach (var key in target.Keys)
            {
                if (source.ContainsKey(key)) continue;
                stale ??= new List<TKey>();
                stale.Add(key);
            }
            if (stale != null)
                foreach (var key in stale)
                    target.Remove(key);

            foreach (var (key, value) in source)
            {
                target.TryGetValue(key, out var mine);
                target[key] = pullValue(mine, value);
            }
        }

        /// <summary> The same merge where the value type is concrete, so PullFrom already knows how
        /// to dispatch and no callback is needed. </summary>
        public static void PullDictionary<TKey, TValue>(this Dictionary<TKey, TValue> target,
            Dictionary<TKey, TValue> source) where TValue : class, IModel<TValue>
            => target.PullDictionary(source, PullFrom);

        /// <summary> Element-by-element equality, which an array does not have on its own. </summary>
        public static bool ArrayEquals<T>(this T[] array, T[] other)
        {
            if (array is null || other is null) return false;
            if (ReferenceEquals(array, other)) return true;
            if (array.Length != other.Length) return false;
            var result = array.SequenceEqual(other);
            return result;
        }

        /// <summary> Element-by-element equality, which a list does not have on its own. </summary>
        public static bool ListEquals<T>(this List<T> list, List<T> other)
        {
            // Two absent lists ARE equal, unlike two absent arrays or dictionaries below - a list is
            // the only shape the format lets a member be null in, and there "neither was ever set"
            // has to compare equal or a model can never equal its own round trip.
            if (list is null && other is null) return true;
            if (list is null || other is null) return false;
            if (ReferenceEquals(list, other)) return true;
            if (list.Count != other.Count) return false;
            var result = list.SequenceEqual(other);
            return result;
        }

        /// <summary> Entry-by-entry equality, order-independent. </summary>
        public static bool DictionaryEquals<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Dictionary<TKey, TValue> other)
        {
            if (dictionary is null || other is null) return false;
            if (ReferenceEquals(dictionary, other)) return true;
            if (dictionary.Count != other.Count) return false;
            
            foreach (var (key, value) in dictionary)
            {
                if (!other.TryGetValue(key, out var otherValue))
                    return false;
                if (!value.Equals(otherValue))
                    return false;
            }
            return true;
        }
        
        /// <summary> A hash over the contents, matching <see cref="ArrayEquals"/>. </summary>
        public static int GetArrayHashCode<T>(this T[] array)
        {
            if (array is null) return 0;
            unchecked
            {
                int hash = 17;
                foreach (var item in array)
                    hash = hash * 31 + (item?.GetHashCode() ?? 0);
                return hash;
            }
        }

        /// <summary> A hash over the contents, matching <see cref="ListEquals"/>. </summary>
        public static int GetListHashCode<T>(this List<T> list)
        {
            if (list is null) return 0;
            unchecked
            {
                int hash = 17;
                foreach (var item in list)
                    hash = hash * 31 + (item?.GetHashCode() ?? 0);
                return hash;
            }
        }
        // Order-INDEPENDENT by summing entry hashes, never by sorting keys: most id structs are not
        // IComparable.

        /// <summary> A hash over the entries, order-independent to match <see cref="DictionaryEquals"/>. </summary>
        public static int GetDictionaryHashCode<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            if (dictionary is null) return 0;
            unchecked
            {
                int hash = 17;
                foreach (var kvp in dictionary)
                {
                    var entry = (kvp.Key?.GetHashCode() ?? 0) * 31 + (kvp.Value?.GetHashCode() ?? 0);
                    hash += entry;
                }
                return hash;
            }
        }
        
        /// <summary> A validation trace as the dotted, indexed path a person reads. </summary>
        public static string GetPath(this List<RulePath> trace)
        {
            if (trace.Count == 0) return string.Empty;
            var builder = new StringBuilder();
            trace.BuildTracePath(builder);
            return builder.ToString();
        }

        /// <summary> The same, written into a builder the caller already has. </summary>
        public static void BuildTracePath(this List<RulePath> trace, StringBuilder builder)
        {
            if (trace.Count == 0) return;
            for (var i = 0; i < trace.Count - 1; i++)
            {
                var path = trace[i];
                path.Append(builder);
                builder.Append('.');
            }
            trace[^1].Append(builder);
        }
    }
}