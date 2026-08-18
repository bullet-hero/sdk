using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;

namespace BH.SDK.Validations
{
    // The walk is the one full-graph pass a level pays on every load (a consumer's level loader
    // validates before handing the level over), and a level is ~150k nodes, so what this class does
    // NOT do per node is as load-bearing as what it does. Two things were measured and removed:
    //
    // - The [RuleContainer] lookup used to run uncached on every node. Under Mono a custom-attribute
    //   query is the most expensive reflection call there is - it allocates a fresh attribute
    //   instance per call - and the caches for object rules and properties sat right next to it.
    // - A property was fetched (PropertyInfo.GetValue, boxing every value type) even when nothing
    //   could come of it. RuleContainerAttribute is AttributeTargets.Class, so no value type can
    //   ever be one, and a property with no rules whose value can hold no container below it is
    //   pure cost: fetch, box, recurse, return immediately. PropertyEntry decides that once per
    //   type instead of once per instance - see Walkable.

    public class RuleAnalyzer
    {
        // Boxing an int is what a RulePath costs per list/array element, and a level's keyframe
        // tracks are the bulk of the walk. Indices below this reuse one box each.
        private const int BoxedIndexCount = 1024;

        private static readonly object[] BoxedIndexes = CreateBoxedIndexes();

        private readonly List<RulePath> _trace = new(16);
        private readonly Dictionary<Type, PropertyEntry[]> _typesCache = new(32);
        private readonly Dictionary<Type, BaseObjectRuleAttribute[]> _objectRulesCache = new(32);
        private readonly Dictionary<Type, bool> _containerCache = new(64);
        private readonly Stack<List<(object, PropertyEntry)>> _nextObjectsPool;

        public RuleAnalyzer()
        {
            _nextObjectsPool = new Stack<List<(object, PropertyEntry)>>(16);
            for (var i = 0; i < 16; i++)
                _nextObjectsPool.Push(new List<(object, PropertyEntry)>(8));

            // Warm the caches for every [RuleContainer] type in the assembly - not just typeof(Level) -
            // so aggregate roots that aren't reachable from Level's own object graph (UserSettings,
            // LevelMeta, and any future one) get cached too. Analyze() itself never hardcoded Level;
            // this was only ever a warm-up gap.
            var visited = new HashSet<Type>();
            foreach (var contextType in GetType().Assembly.GetTypes())
            {
                if (IsRuleContainer(contextType))
                    CacheRecursively(contextType, visited);
            }
        }

        private static object[] CreateBoxedIndexes()
        {
            var indexes = new object[BoxedIndexCount];
            for (var i = 0; i < BoxedIndexCount; i++)
                indexes[i] = i;
            return indexes;
        }

        private void CacheRecursively(Type contextType, HashSet<Type> visited)
        {
            if (!visited.Add(contextType)) return;
            if (!IsRuleContainer(contextType)) return;

            GetObjectRules(contextType);
            var entries = GetObjProperties(contextType);

            foreach (var entry in entries)
            {
                var propertyType = entry.Property.PropertyType;

                if (propertyType.IsList())
                    CacheRecursively(propertyType.GetGenericArguments()[0], visited);
                else if (propertyType.IsArray)
                    CacheRecursively(propertyType.GetElementType(), visited);
                else if (propertyType.IsDictionary())
                    CacheRecursively(propertyType.GetDictionaryValueGenericParameterOrDefault(), visited);
                else CacheRecursively(propertyType, visited);
            }
        }

        // Nothing is logged from in here. An issue used to be written to the console the moment it
        // was found, on top of whoever consumed the returned list writing it again - so a level with
        // several hundred findings paid for every one of them twice, in stack traces the analyzer
        // has no use for. What to do about a report is the caller's policy (see ValidationFacade).

        public List<RuleIssue> Analyze(object obj, RuleAnalyzerSettings settings)
        {
            var result = new List<RuleIssue>(8);

            try
            {
                AnalyzeRecursive(obj, settings, result, RuleContext.ForRoot(obj));
            }
            finally
            {
                // A misapplied rule aborts the walk by design, and the analyzer is meant to be
                // reusable afterwards - so the trace is reset unconditionally, not only on success.
                // Leaving it behind poisons every later issue's path with a dead prefix.
                _trace.Clear();
            }

            return result;
        }

        private void AnalyzeRecursive(object target, RuleAnalyzerSettings settings,
            List<RuleIssue> result, RuleContext context)
        {
            if (target == null) return;
            var targetType = target.GetType();

            if (!IsRuleContainer(targetType)) return;

            // Entering a scope of its own (a prefab template) rebases everything scope-relative -
            // frames against its own timeline, parent ids against its own reserved targets - for
            // this subtree and everything below it.
            if (target is IFrameScope frameScope) context = context.WithScope(frameScope);

            // Object rules run before the property walk, so an issue spanning two properties is
            // reported against the object owning both - and lands in the trace before either of
            // them, which is what makes the reverse-order fix pass repair the pair first.
            foreach (var rule in GetObjectRules(targetType))
            {
                if (!rule.IsValidType(targetType))
                {
                    throw new ArgumentException($"Can't apply rule {rule.GetType().Name} " +
                                                $"to type {targetType.Name}, path: {_trace.GetPath()}");
                }

                if (!settings.Reports(rule.Group)) continue;
                if (rule.IsValid(target, context)) continue;

                result.Add(new RuleIssue(rule, context, new List<RulePath>(_trace)));
            }

            var objProperties = GetObjProperties(targetType);

            // The pool is a reuse optimization, never a capacity limit: a graph deeper than the
            // prewarmed count is legal, so an exhausted pool allocates instead of throwing. Returning
            // the buffer through finally is what keeps it a pool at all - the misapplied-rule throw
            // below is by design, and leaking one buffer per such throw drained a shared analyzer
            // dry, after which every later Analyze died on an empty stack instead of reporting rules.
            var nextObjects = _nextObjectsPool.Count > 0
                ? _nextObjectsPool.Pop()
                : new List<(object, PropertyEntry)>(8);

            try
            {
                foreach (var entry in objProperties)
                {
                    // Nothing to check and nowhere to descend - reading the property at all would
                    // only box its value for an AnalyzeRecursive that returns on its first line.
                    var rules = entry.Rules;
                    if (rules.Length == 0 && !entry.Walkable) continue;

                    var property = entry.Property;
                    var nextObj = property.GetValue(target);
                    _trace.Add(new RulePath(property));

                    var hasInvalidRule = false;
                    foreach (var rule in rules)
                    {
                        // TODO move to Roslyn Analyzer, this must not be in runtime
                        if (!rule.IsValidType(property))
                        {
                            throw new ArgumentException($"Can't apply rule {rule.GetType().Name} " +
                                                     $"to type {nextObj?.GetType().Name}, path: {_trace.GetPath()}");
                        }

                        if (!settings.Reports(rule.Group)) continue;

                        if (!rule.IsValid(nextObj, context))
                        {
                            hasInvalidRule = true;
                            result.Add(new RuleIssue(rule, context, new List<RulePath>(_trace)));

                            if (!settings.analyzeAllPropertyRules) break;
                        }
                    }

                    if (entry.Walkable && nextObj != null
                        && (!hasInvalidRule || settings.analyzeAllRecursiveRules))
                    {
                        nextObjects.Add((nextObj, entry));
                    }

                    _trace.RemoveAt(_trace.Count - 1);
                }

                foreach (var (nextObj, nextEntry) in nextObjects)
                {
                    var nextProp = nextEntry.Property;
                    if (nextObj is IList list)
                    {
                        for (var i = 0; i < list.Count; i++)
                        {
                            _trace.Add(new RulePath(nextProp, BoxIndex(i)));
                            AnalyzeRecursive(list[i], settings, result, context);
                            _trace.RemoveAt(_trace.Count - 1);
                        }
                    }
                    else if (nextObj is IDictionary dictionary)
                    {
                        // Values only, deliberately. A RulePath addresses "this property, at this key",
                        // which is how RuleFixer finds its way back to a value - there is no way to
                        // express "the key itself", so an issue raised on a key would be repaired into
                        // the value instead. Keys stay safe without the walk: every key in the format is
                        // an id struct, and a struct can never be a [RuleContainer] anyway (it is boxed
                        // on the way in, so any Fix would write into a copy and vanish). Anything a key
                        // needs checking for - above all whether it agrees with its value's own id - is
                        // relational, and belongs to the graph pass rather than here.
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            _trace.Add(new RulePath(nextProp, entry.Key));
                            AnalyzeRecursive(entry.Value, settings, result, context);
                            _trace.RemoveAt(_trace.Count - 1);
                        }
                    }
                    else if (nextObj.GetType().IsArray)
                    {
                        var array = (Array)nextObj;
                        for (var i = 0; i < array.Length; i++)
                        {
                            _trace.Add(new RulePath(nextProp, BoxIndex(i)));
                            AnalyzeRecursive(array.GetValue(i), settings, result, context);
                            _trace.RemoveAt(_trace.Count - 1);
                        }
                    }
                    else
                    {
                        _trace.Add(new RulePath(nextProp));
                        AnalyzeRecursive(nextObj, settings, result, context);
                        _trace.RemoveAt(_trace.Count - 1);
                    }
                }
            }
            finally
            {
                nextObjects.Clear();
                _nextObjectsPool.Push(nextObjects);
            }
        }

        private static object BoxIndex(int index)
            => index < BoxedIndexCount ? BoxedIndexes[index] : index;

        private PropertyEntry[] GetObjProperties(Type objType)
        {
            if (!_typesCache.TryGetValue(objType, out var entries))
            {
                entries = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.CanRead && property.CanWrite)
                    .Select(property => new PropertyEntry(property,
                        property.GetCustomAttributes<BasePropertyRuleAttribute>(true).ToArray(),
                        IsWalkable(property.PropertyType)))
                    .ToArray();

                _typesCache.Add(objType, entries);
            }
            return entries;
        }

        // "Can a [RuleContainer] be reached through a value of this type at all?" The attribute is
        // AttributeTargets.Class, so a value type is always a dead end - and so is a collection OF
        // value types, since the walk only ever descends into a list's items or a dictionary's
        // values, never into the collection object itself. Everything else stays walkable: a
        // reference type's runtime type can be a subclass carrying the attribute even when the
        // declared one does not, and a declared IList/IEnumerable can be any implementation at all.
        private static bool IsWalkable(Type type)
        {
            if (type.IsList()) return !type.GetGenericArguments()[0].IsValueType;
            if (type.IsDictionary()) return !type.GetDictionaryValueGenericParameterOrDefault().IsValueType;
            if (type.IsArray) return !type.GetElementType()!.IsValueType;
            return !type.IsValueType && type != typeof(string);
        }

        private bool IsRuleContainer(Type type)
        {
            if (_containerCache.TryGetValue(type, out var isContainer)) return isContainer;

            isContainer = type.GetCustomAttribute<RuleContainerAttribute>() != null;
            _containerCache.Add(type, isContainer);
            return isContainer;
        }

        private BaseObjectRuleAttribute[] GetObjectRules(Type type)
        {
            if (!_objectRulesCache.TryGetValue(type, out var rules))
            {
                rules = type.GetCustomAttributes<BaseObjectRuleAttribute>(true).ToArray();
                _objectRulesCache.Add(type, rules);
            }
            return rules;
        }

        /// <summary> One property of one [RuleContainer] type, with everything about it that can be
        /// decided once per type rather than once per instance. </summary>
        private readonly struct PropertyEntry
        {
            public readonly PropertyInfo Property;
            public readonly BasePropertyRuleAttribute[] Rules;

            /// <summary> Can a [RuleContainer] be reached through this property's value? </summary>
            public readonly bool Walkable;

            public PropertyEntry(PropertyInfo property, BasePropertyRuleAttribute[] rules, bool walkable)
            {
                Property = property;
                Rules = rules;
                Walkable = walkable;
            }
        }
    }
}
