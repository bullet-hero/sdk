using System;
using System.Collections.Generic;
using System.Reflection;
using BH.SDK.Models.Objects;
using Newtonsoft.Json;

namespace BH.SDK.Utils
{
    /// <summary> Resolves a modification's field path against a live object: which property each segment names,
    /// and what to do when one indexes into a list. Cached per type, since a resync walks every placement. </summary>
    public static class ModificationUtils
    {
        private static readonly Dictionary<(Type type, string name),
            (PropertyCategory category, PropertyInfo info)> Data = new(32);
        private static readonly Dictionary<Type, PropertyInfo> ListIndexers = new(16);
        
        private static readonly List<PathSegment> SegmentBuffer = new(4);
        private static readonly object[] ParamsCache = { 0 };
        private static readonly HashSet<Type> ProcessedTypes = new();
        
        // THE ROOTS, AND NOTHING BUT THE ROOTS. This used to list every polymorphic implementation
        // as well - forty-odd type arguments across eleven families - because the walk below cannot
        // get from an interface to what implements it. That list had gone stale (three Color3
        // variants and both keyframe families were missing, so an override addressed at one of them
        // was silently dropped), and it is gone: ModificationImplementations is generated from the
        // models themselves, so a variant registers itself by existing. See its generator's header.
        static ModificationUtils()
        {
            AddPropertiesRecursive<RectObject, ShapeObject, EffectObject, TextObject, PrefabObject>();
        }

        /// <summary> Writes one override onto a materialized object, resolving its path as it goes. </summary>
        public static void Apply(this RectObject obj, Modification mod)
        {
            ParsePath(mod.Key.Path, SegmentBuffer);
            if (SegmentBuffer.Count == 0) return;

            object currentObject = obj;
            var preLength = SegmentBuffer.Count - 1;
            for (var i = 0; i < preLength; i++)
            {
                if (currentObject == null)
                {
                    // Debug.LogWarning($"CurrentObject is null, index={i}");
                    return;
                }
                
                var segment = SegmentBuffer[i];
                (Type type, string name) key = (currentObject.GetType(), segment.Name);
                if (!Data.TryGetValue(key, out var value))
                {
                    // Debug.LogWarning($"Can't find data for key {key.type}/{key.name}");
                    return;
                }

                switch (value.category)
                {
                    case PropertyCategory.Value:
                    {
                        currentObject = value.info.GetValue(currentObject);
                        break;
                    }
                    case PropertyCategory.List:
                    {
                        currentObject = GetListValue(value.info, segment, currentObject);
                        break;
                    }
                    case PropertyCategory.Array:
                    {
                        currentObject = GetArrayValue(value.info, currentObject, segment);
                        break;
                    }
                    default: throw new ArgumentOutOfRangeException();
                }
            }
            
            if (currentObject == null)
            {
                // Debug.LogWarning($"CurrentObject is null, index={preLength}");
                return;
            }
            
            var lastSegment = SegmentBuffer[preLength];
            (Type type, string name) lastKey = (currentObject.GetType(), lastSegment.Name);
            if (!Data.TryGetValue(lastKey, out var lastValue))
            {
                // Debug.LogWarning($"Can't find data for key {lastKey.type}/{lastKey.name}");
                return;
            }
            
            switch (lastValue.category)
            {
                case PropertyCategory.Value:
                {
                    if (!CheckTypeMatch(mod.Value.GetType(), lastValue.info.PropertyType)) return;
                    
                    lastValue.info.SetValue(currentObject, mod.Value);
                    break;
                }
                case PropertyCategory.List:
                {
                    // No [idx] in the path - replace the WHOLE list (see Modification's own doc
                    // comment on track-level overrides), not one element of it.
                    if (lastSegment.Index < 0)
                    {
                        if (!CheckTypeMatch(mod.Value.GetType(), lastValue.info.PropertyType)) return;
                        lastValue.info.SetValue(currentObject, mod.Value);
                        break;
                    }

                    var toType = lastValue.info.PropertyType.GetGenericArguments()[0];
                    if (!CheckTypeMatch(mod.Value.GetType(), toType)) return;

                    SetListValue(lastValue.info, lastSegment, currentObject, mod.Value);
                    break;
                }
                case PropertyCategory.Array:
                {
                    var toType = lastValue.info.PropertyType.GetElementType();
                    if (!CheckTypeMatch(mod.Value.GetType(), toType)) return;
                    
                    SetArrayValue(lastValue.info, lastSegment, currentObject, mod.Value);
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        // IsAssignableFrom (not strict equality) so a concrete polymorphic value (StringValue) can
        // target its declared interface property (IString), and a whole List<TKeyframe> can target
        // a List<TKeyframe>-typed property (see the PropertyCategory.List whole-list branch above) -
        // an exact type match still satisfies IsAssignableFrom, so this is strictly more permissive
        // than the equality check it replaces.
        private static bool CheckTypeMatch(Type from, Type to)
        {
            if (to.IsAssignableFrom(from)) return true;

            // Debug.LogWarning($"Type mismatch, from={from}, to={to}");
            return false;
        }
        private static object GetListValue(PropertyInfo info, PathSegment segment, object currentObject)
        {
            ParamsCache[0] = segment.Index;
            var list = info.GetValue(currentObject);
            var indexer = ListIndexers[info.PropertyType];
            return indexer.GetValue(list, ParamsCache);
        }
        private static void SetListValue(PropertyInfo info, PathSegment segment, object currentObject, object newValue)
        {
            ParamsCache[0] = segment.Index;
            var list = info.GetValue(currentObject);
            var listIndexer = ListIndexers[info.PropertyType];
            listIndexer.SetValue(list, newValue, ParamsCache);
        }
        
        private static object GetArrayValue(PropertyInfo info, object currentObject, PathSegment segment)
        {
            var array = info.GetValue(currentObject);
            return ((Array)array).GetValue(segment.Index);
        }
        private static void SetArrayValue(PropertyInfo info, PathSegment segment, object currentObject, object newValue)
        {
            var array = info.GetValue(currentObject);
            ((Array)array).SetValue(newValue, segment.Index);
        }

        /// <summary> Registers several types at once. </summary>
        public static void AddProperties<T>()
            => AddProperties(typeof(T));
        /// <summary> Registers several types at once. </summary>
        public static void AddProperties<T1, T2>()
            => AddProperties(typeof(T1), typeof(T2));
        /// <summary> Registers several types at once. </summary>
        public static void AddProperties<T1, T2, T3>()
            => AddProperties(typeof(T1), typeof(T2), typeof(T3));
        /// <summary> Registers several types at once. </summary>
        public static void AddProperties<T1, T2, T3, T4>()
            => AddProperties(typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        /// <summary> Registers several types at once. </summary>
        public static void AddProperties<T1, T2, T3, T4, T5>()
            => AddProperties(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
        /// <summary> Registers several types at once. </summary>
        public static void AddProperties<T1, T2, T3, T4, T5, T6>()
            => AddProperties(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));
        /// <summary> Registers several types at once. </summary>
        public static void AddProperties<T1, T2, T3, T4, T5, T6, T7>()
            => AddProperties(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));
        /// <summary> Registers several types at once. </summary>
        public static void AddProperties<T1, T2, T3, T4, T5, T6, T7, T8>()
            => AddProperties(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));
        
        /// <summary> Registers several types at once, descending into what their properties hold. </summary>
        public static void AddPropertiesRecursive<T>()
            => AddPropertiesRecursive(typeof(T));
        /// <summary> Registers several types at once, descending into what their properties hold. </summary>
        public static void AddPropertiesRecursive<T1, T2>()
            => AddPropertiesRecursive(typeof(T1), typeof(T2));
        /// <summary> Registers several types at once, descending into what their properties hold. </summary>
        public static void AddPropertiesRecursive<T1, T2, T3>()
            => AddPropertiesRecursive(typeof(T1), typeof(T2), typeof(T3));
        /// <summary> Registers several types at once, descending into what their properties hold. </summary>
        public static void AddPropertiesRecursive<T1, T2, T3, T4>()
            => AddPropertiesRecursive(typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        /// <summary> Registers several types at once, descending into what their properties hold. </summary>
        public static void AddPropertiesRecursive<T1, T2, T3, T4, T5>()
            => AddPropertiesRecursive(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
        /// <summary> Registers several types at once, descending into what their properties hold. </summary>
        public static void AddPropertiesRecursive<T1, T2, T3, T4, T5, T6>()
            => AddPropertiesRecursive(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));
        /// <summary> Registers several types at once, descending into what their properties hold. </summary>
        public static void AddPropertiesRecursive<T1, T2, T3, T4, T5, T6, T7>()
            => AddPropertiesRecursive(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));
        /// <summary> Registers several types at once, descending into what their properties hold. </summary>
        public static void AddPropertiesRecursive<T1, T2, T3, T4, T5, T6, T7, T8>()
            => AddPropertiesRecursive(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));
        
        /// <summary> Registers the properties of each type, so a path can address them. </summary>
        public static void AddProperties(params Type[] types)
        {
            foreach (var type in types)
                AddProperties(type);
        }

        /// <summary> The same, descending into whatever those properties hold. </summary>
        public static void AddPropertiesRecursive(params Type[] types)
        {
            foreach (var type in types)
                AddPropertiesRecursive(type);
        }
        
        /// <summary> Registers one type's properties. </summary>
        public static void AddProperties(Type type)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                RegisterProperty(type, property);
            }
        }
        /// <summary> The same, descending into whatever those properties hold. </summary>
        public static void AddPropertiesRecursive(Type type)
        {
            if (!ProcessedTypes.Add(type)) return; // recursive protection

            // AN INTERFACE HAS NO SERIALIZED PROPERTIES OF ITS OWN, so walking one registers nothing
            // and the descent used to end here. What a path actually meets at run time is one of its
            // implementations - Apply keys every lookup on currentObject.GetType() - so this is where
            // the walk fans out into them.
            if (type.IsInterface)
            {
                foreach (var implementation in ModificationImplementations.Of(type))
                    AddPropertiesRecursive(implementation);

                ProcessedTypes.Remove(type);
                return;
            }
            
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var (propertyType, propertyCategory) = RegisterProperty(type, property);
                if (propertyType == null) continue;
                
                switch (propertyCategory)
                {
                    case PropertyCategory.Value:
                    {
                        AddPropertiesRecursive(propertyType);
                        break;
                    }
                    case PropertyCategory.List:
                    {
                        var elementType = propertyType.GetGenericArguments()[0];
                        AddPropertiesRecursive(elementType);
                        break;
                    }
                    case PropertyCategory.Array:
                    {
                        var elementType = propertyType.GetElementType();
                        AddPropertiesRecursive(elementType);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            ProcessedTypes.Remove(type);
        }

        private static (Type, PropertyCategory) RegisterProperty(Type type, PropertyInfo property)
        {
            var attr = property.GetCustomAttribute<JsonPropertyAttribute>();
            if (attr == null) return (null, PropertyCategory.Value);
            
            var propertyType = property.PropertyType;
            var propertyName = attr.PropertyName;
            var propertyCategory = GetCategory(propertyType);
            Data[(type, propertyName)] = (propertyCategory, property);

            if (propertyCategory == PropertyCategory.List)
            {
                if (!ListIndexers.ContainsKey(propertyType))
                {
                    var indexer = propertyType.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public)
                                  ?? throw new InvalidOperationException($"No indexer on {propertyType}");
                    ListIndexers.Add(propertyType, indexer);
                }
            }
            
            // Debug.Log($"t: {type}, pt: {propertyType}, pn: {propertyName}");
            return (propertyType, propertyCategory);
        }

        private static PropertyCategory GetCategory(Type type)
        {
            if (type.IsArray)  return PropertyCategory.Array;
            if (type.IsList()) return PropertyCategory.List;
            return PropertyCategory.Value;
        }
        
        /// <summary> Whether a property is a plain value, a list or an array - which decides how a path segment
        /// indexes into it. </summary>
        private enum PropertyCategory : byte
        {
            Value = 0,
            List = 1,
            Array = 2,
        }

        /// <summary> One step of a parsed path: a property name, and an index when that step indexes into it. </summary>
        public struct PathSegment
        {
            /// <summary> The property this step names. </summary>
            public string Name;
            /// <summary> The collection index it carries, where it has one. </summary>
            public int Index;
        }

        /// <summary> Splits a dotted, indexed path into its steps, reusing the caller's buffer. </summary>
        public static void ParsePath(string path, List<PathSegment> buffer)
        {
            buffer.Clear();
            var index = 0;
            while (index < path.Length)
            {
                var startIndex = index;
                while (index < path.Length && path[index] != '.' && path[index] != '[') index++;
                var name = path[startIndex..index];
                if (name.Length == 0)
                {
                    // Debug.LogWarning($"FormatException: Empty name at {index} in '{path}'");
                    buffer.Clear();
                    return;
                }

                var segmentIndex = -1;
                if (index < path.Length && path[index] == '[')
                {
                    index++;
                    var segmentIndexStart = index;
                    while (index < path.Length && path[index] != ']') index++;
                    if (index == path.Length)
                    {
                        // Debug.LogWarning($"FormatException: Missing ']'");
                        buffer.Clear();
                        return;
                    }
                    
                    segmentIndex = int.Parse(path[segmentIndexStart..index]);
                    index++;
                }

                buffer.Add(new PathSegment { Name = name, Index = segmentIndex });

                if (index < path.Length && path[index] == '.') index++;
            }
        }
    }
}