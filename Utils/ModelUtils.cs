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
    public static class ModelUtils
    {
        private const float ByteMaxValue = byte.MaxValue;
        
        public static Color4Value ToColorValue(this Pixel pixel) => new(
            pixel.r / ByteMaxValue, 
            pixel.g / ByteMaxValue, 
            pixel.b / ByteMaxValue, 
            pixel.a / ByteMaxValue);
        
        public static Pixel ToPixel(this Color4Value color4Value) => new(
            (byte)(color4Value.R * ByteMaxValue),
            (byte)(color4Value.G * ByteMaxValue),
            (byte)(color4Value.B * ByteMaxValue),
            (byte)(color4Value.A * ByteMaxValue));

        // RESET KEEPS THE COLLECTION INSTANCE, and that is not a detail: a reset object is the same
        // object, so whatever holds its lists still holds them. Every hand-written Reset here
        // cleared in place for exactly that reason. The defaults come from a freshly constructed
        // instance, which is thrown away afterwards - so sharing its items costs nothing.

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

        // TWO ARRAY COPIES, FOR THE REASON THERE ARE THREE DICTIONARY COPIES: what an element is
        // decides whether copying the array is enough. This used to be one method constrained
        // `where T : ICopyable<T>` that then blitted with CopyTo, which honours the constraint for a
        // struct element and silently breaks it for a class one - ThemeData.Matrix is
        // Color4Value[64] and Color4Value is a class, so a copied theme shared all sixty-four
        // colours with the theme it came from, and editing either edited both. Undo snapshots,
        // autosave and prefab materialization all copy. `class` in the constraint is what makes the
        // wrong call a compile error instead of a shared instance.

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
        public static List<T> CopyList<T>(this List<T> list) where T : ICopyable<T>
        {
            var copyList = new List<T>(list.Count);
            foreach (var item in list)
                copyList.Add(item.Copy());
            return copyList;
        }
        public static Dictionary<TKey, TValue> CopyDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
            where TKey : unmanaged where TValue : ICopyable<TValue>
        {
            var copyDictionary = new Dictionary<TKey, TValue>(dictionary.Count);
            foreach (var (key, value) in dictionary)
                copyDictionary.Add(key, value.Copy());
            return copyDictionary;
        }
        public static Dictionary<TKey, TValue> CopyDictionaryUnmanaged<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
            where TKey : unmanaged where TValue : unmanaged
        {
            var copyDictionary = new Dictionary<TKey, TValue>(dictionary.Count);
            foreach (var (key, value) in dictionary)
                copyDictionary.Add(key, value);
            return copyDictionary;
        }
        public static Dictionary<TKey, TValue> CopyDictionaryManaged<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
            where TKey : ICopyable<TKey> where TValue : ICopyable<TValue>
        {
            var copyDictionary = new Dictionary<TKey, TValue>(dictionary.Count);
            foreach (var (key, value) in dictionary)
                copyDictionary.Add(key.Copy(), value.Copy());
            return copyDictionary;
        }

        // The one place a POLYMORPHIC model field is merged, and the reason it exists rather than
        // a bare target.Pull(source): a Vector2Value cannot become a RandomVector2, so the instance
        // is keepable only while the two concrete types agree. Pulling blindly would silently keep
        // the old value - every interface-level Pull here is a no-op on a sibling implementation -
        // which is the failure this returns a replacement for instead. A field whose declared type
        // is a concrete model needs none of it and calls Pull directly.

        /// <summary> Merges source into target in place while their concrete types allow it, and
        /// returns what the field must now hold - the same instance, or a fresh copy of source. </summary>
        public static T PullFrom<T>(this T target, T source) where T : class, IModel<T>
        {
            if (source is null) return null;
            if (target is null || target.GetType() != source.GetType()) return source.Copy();
            target.Pull(source);
            return target;
        }

        // The keyed counterpart of PullFrom, and it exists for the collections something HOLDS INTO.
        // Every other collection here is replaced wholesale by Pull on purpose - an element is
        // addressed by its index or key, so keeping the instance buys nothing - but in an object scope
        // (GameLevel, Prefab) the reference IS what everything else holds: the editor's selection, its
        // operation buffer and every materialized prefab child point at RectObjects. A ClipboardData's
        // five sections are the same case one level up, each held as its own timeline's buffer.
        //
        // pullValue is a delegate rather than a constraint because the value type may be a polymorphic
        // BASE, and target.Pull(source) through a base reference writes the base half and stops
        // (CLAUDE.md, "IModel<T> pattern"). Who dispatches to the concrete overload cannot live in a
        // generic method - the generated <Base>ModelPull.PullValue is what the object scopes pass; a concretely
        // typed value like LevelTrack needs no callback and takes the overload below.

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

        public static bool ArrayEquals<T>(this T[] array, T[] other)
        {
            if (array is null || other is null) return false;
            if (ReferenceEquals(array, other)) return true;
            if (array.Length != other.Length) return false;
            var result = array.SequenceEqual(other);
            return result;
        }
        public static bool ListEquals<T>(this List<T> list, List<T> other)
        {
            if (list is null || other is null) return false;
            if (ReferenceEquals(list, other)) return true;
            if (list.Count != other.Count) return false;
            var result = list.SequenceEqual(other);
            return result;
        }
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
        // Order-INDEPENDENT by construction, never by sorting the keys first. Sorting demanded an
        // IComparable key, which most id structs here are not (ObjectId, every TypedResourceId,
        // every Guid-based id) - so PrefabObject.GetHashCode threw the moment anything put a
        // placement in a HashSet. Entry hashes are summed instead: a Dictionary's enumeration order
        // is not part of its value, and the keys are unique, so no two entries can trade places.

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
        
        public static string GetPath(this List<RulePath> trace)
        {
            if (trace.Count == 0) return string.Empty;
            var builder = new StringBuilder();
            trace.BuildTracePath(builder);
            return builder.ToString();
        }
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