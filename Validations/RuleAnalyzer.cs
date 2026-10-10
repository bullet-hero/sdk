using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BH.SDK.Models.Objects;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;

namespace BH.SDK.Validations
{
    // Per-type decisions live here, per-call state on RuleWalk (mutually recursive through Node). Two things
    // were measured and removed from the per-node path: an uncached [RuleContainer] lookup and fetching
    // properties that lead nowhere (IsWalkable). Reasons and numbers: BH.SDK/Docs/VALIDATIONS.md.

    /// <summary> Walks a model graph and reports every rule it violates. Everything decided once per TYPE lives
    /// here; everything belonging to one call lives on the walk it hands out. </summary>
    public class RuleAnalyzer
    {
        private readonly Dictionary<Type, PropertyEntry[]> _typesCache = new(32);
        private readonly Dictionary<Type, BaseObjectRuleAttribute[]> _objectRulesCache = new(32);
        private readonly Dictionary<Type, bool> _containerCache = new(64);
        private readonly Stack<List<(object, PropertyEntry)>> _nextObjectsPool;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public RuleAnalyzer()
        {
            _nextObjectsPool = new Stack<List<(object, PropertyEntry)>>(16);
            for (var i = 0; i < 16; i++)
                _nextObjectsPool.Push(new List<(object, PropertyEntry)>(8));

            // Warm every [RuleContainer] type in the assembly, not just Level, so UserSettings and LevelMeta
            // are cached too.
            var visited = new HashSet<Type>();
            foreach (var contextType in GetType().Assembly.GetTypes())
            {
                if (!IsRuleContainer(contextType)) continue;

                CacheRecursively(contextType, visited);
                WarmGeneratedTable(contextType);
            }
        }

        // A generated walk addresses properties by ordinal and RuleTable's static init checks them. Forced
        // here so a stale BH.SDK.Roslyn.dll fails at construction, not halfway through a level.

        private static void WarmGeneratedTable(Type type)
        {
            if (type.IsAbstract || !typeof(IValidatable).IsAssignableFrom(type)) return;
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
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

        // Nothing is logged in here; what to do with a report is the caller's policy (ValidationFacade).
        // The trace lives on the walk, which dies with the call, so an aborted analysis needs no cleanup.

        /// <summary> Every rule violated anywhere in that object's graph. </summary>
        public List<RuleIssue> Analyze(object obj, RuleAnalyzerSettings settings)
        {
            var walk = new RuleWalk(this, settings);
            walk.Node(obj, RuleContext.ForRoot(obj));
            return walk.Issues;
        }

        /// <summary> Walk one node by reflection. Reached only through <see cref="RuleWalk.Node"/>,
        /// and its own recursion goes back through it - so a value carrying a generated walk is
        /// still walked by that walk even when its owner has none. </summary>
        internal void WalkNode(object target, RuleContext context, RuleWalk walk)
        {
            if (target == null) return;
            var targetType = target.GetType();

            if (!IsRuleContainer(targetType)) return;

            // Entering a scope of its own (a prefab template) rebases everything scope-relative -
            // frames against its own timeline, parent ids against its own reserved targets - for
            // this subtree and everything below it.
            if (target is Prefab prefab) context = context.WithScope(prefab);

            // Object rules run before the property walk, so an issue spanning two properties is
            // reported against the object owning both - and lands in the trace before either of
            // them, which is what makes the reverse-order fix pass repair the pair first.
            walk.ObjectRules(GetObjectRules(targetType), target, targetType, context);


            var objProperties = GetObjProperties(targetType);

            // A reuse optimization, never a capacity limit: an exhausted pool allocates. Return through
            // finally, or the by-design misapplied-rule throw drains a shared analyzer.
            var nextObjects = _nextObjectsPool.Count > 0
                ? _nextObjectsPool.Pop()
                : new List<(object, PropertyEntry)>(8);

            try
            {
                // PHASE A - every property's own rules, and no descent at all.
                foreach (var entry in objProperties)
                {
                    // Nothing to check and nowhere to descend - reading the property at all would
                    // only box its value for a walk that returns on its first line.
                    var rules = entry.Rules;
                    if (rules.Length == 0 && !entry.Walkable) continue;

                    var nextObj = entry.Property.GetValue(target);
                    var canDescend = walk.Check(entry.Property, rules, nextObj, context,
                        entry.TypeChecked);

                    if (entry.Walkable && canDescend) nextObjects.Add((nextObj, entry));
                }

                // PHASE B - the descent, as a SECOND pass in the same property order. Never fused
                // with the loop above: an object with two walkable properties reports in a different
                // order the moment the two interleave.
                foreach (var (nextObj, nextEntry) in nextObjects)
                    walk.Descend(nextEntry.Property, nextObj, context);
            }
            finally
            {
                nextObjects.Clear();
                _nextObjectsPool.Push(nextObjects);
            }
        }

        private PropertyEntry[] GetObjProperties(Type objType)
        {
            if (!_typesCache.TryGetValue(objType, out var entries))
            {
                entries = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.CanRead && property.CanWrite)
                    .Select(CreateEntry)
                    .ToArray();

                _typesCache.Add(objType, entries);
            }

            return entries;
        }

        private static PropertyEntry CreateEntry(PropertyInfo property)
        {
            var rules = property.GetCustomAttributes<BasePropertyRuleAttribute>(true).ToArray();

            // Whether a rule may sit on a property is a fact about the TYPE, so it is settled here
            // rather than per node. Where every rule answered yes, the walk skips the check
            // entirely; where one did not, it keeps the original per-rule check, throw and all.
            var typeChecked = true;
            foreach (var rule in rules) typeChecked &= rule.IsValidType(property);

            return new PropertyEntry(property, rules, IsWalkable(property.PropertyType), typeChecked);
        }

        // Can a [RuleContainer] be reached through a value of this type? A value type, or a collection of
        // them, is a dead end (the walk descends into items/values only); anything else may hold a subclass.
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
            /// <summary> The property this entry describes. </summary>
            public readonly PropertyInfo Property;

            /// <summary> The rules written on it, resolved once per type rather than once per instance. </summary>
            public readonly BasePropertyRuleAttribute[] Rules;

            /// <summary> Can a [RuleContainer] be reached through this property's value? </summary>
            public readonly bool Walkable;

            /// <summary> Whether every rule here already answered IsValidType, once, for this type.
            /// </summary>
            public readonly bool TypeChecked;

            /// <summary> Every member at once, in declaration order. </summary>
            public PropertyEntry(PropertyInfo property, BasePropertyRuleAttribute[] rules,
                bool walkable, bool typeChecked)
            {
                Property = property;
                Rules = rules;
                Walkable = walkable;
                TypeChecked = typeChecked;
            }
        }
    }
}