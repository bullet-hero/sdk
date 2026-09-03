using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;

namespace BH.SDK.Validations
{
    // The walk is the one full-graph pass a level pays when an author opens it (a consumer's level
    // loader validates on the editor path; playback skips it), and a level is ~150k nodes, so what
    // this class does NOT do per node is as load-bearing as what it does. Two things were measured
    // and removed:
    //
    // - The [RuleContainer] lookup used to run uncached on every node. Under Mono a custom-attribute
    //   query is the most expensive reflection call there is - it allocates a fresh attribute
    //   instance per call - and the caches for object rules and properties sat right next to it.
    // - A property was fetched (PropertyInfo.GetValue, boxing every value type) even when nothing
    //   could come of it. RuleContainerAttribute is AttributeTargets.Class, so no value type can
    //   ever be one, and a property with no rules whose value can hold no container below it is
    //   pure cost: fetch, box, recurse, return immediately. PropertyEntry decides that once per
    //   type instead of once per instance - see Walkable.
    //
    // THE SPLIT WITH RuleWalk. Everything decided once per TYPE lives here - which properties are
    // walked, which rules sit on them, whether a type is a container, and the buffer pool that
    // outlives a single analysis. Everything belonging to one CALL lives on the walk - the trace,
    // the findings, the settings. That is what makes a walk cheap to allocate per Analyze, and it is
    // the seam a generated Validate arrives through: RuleWalk.Node is the only place a child node is
    // reached from, and WalkNode below is what it falls back to when a value has no generated walk
    // of its own. The two are mutually recursive through that one point on purpose.

    public class RuleAnalyzer
    {
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
                if (!IsRuleContainer(contextType)) continue;

                CacheRecursively(contextType, visited);
                WarmGeneratedTable(contextType);
            }
        }

        // A GENERATED WALK ADDRESSES ITS PROPERTIES BY ORDINAL, and RuleTable's static initializer is
        // what checks those ordinals against reflection. Left lazy, a disagreement would surface on
        // whichever node first reaches that type - halfway through a level, wrapped in a
        // TypeInitializationException, on a path that has nothing to do with the cause. Forcing the
        // initializers here moves it to "the analyzer was constructed", which is where a build
        // problem belongs; a stale BH.SDK.Roslyn.dll is the likeliest cause and the message says so.
        //
        // It costs nothing extra: the loop above already walks every [RuleContainer] type and warms
        // the same reflection this makes the tables out of.

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

        // Nothing is logged from in here. An issue used to be written to the console the moment it
        // was found, on top of whoever consumed the returned list writing it again - so a level with
        // several hundred findings paid for every one of them twice, in stack traces the analyzer
        // has no use for. What to do about a report is the caller's policy (see ValidationFacade).
        //
        // THE TRACE NEEDS NO RESET ANY MORE, and that is the one behavioural note about this method:
        // it used to live on the analyzer and had to be cleared in a finally, because a misapplied
        // rule aborts the walk by design and a leftover trace poisoned every later issue's path with
        // a dead prefix. It lives on the walk now, and the walk dies with the call - so an aborted
        // analysis leaves nothing behind to clear. RuleAnalyzerPoolTests still pins the outcome.

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
            if (target is IFrameScope frameScope) context = context.WithScope(frameScope);

            // Object rules run before the property walk, so an issue spanning two properties is
            // reported against the object owning both - and lands in the trace before either of
            // them, which is what makes the reverse-order fix pass repair the pair first.
            walk.ObjectRules(GetObjectRules(targetType), target, targetType, context);


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

            /// <summary> Whether every rule here already answered IsValidType, once, for this type.
            /// </summary>
            public readonly bool TypeChecked;

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