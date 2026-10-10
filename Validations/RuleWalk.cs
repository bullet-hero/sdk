using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;

namespace BH.SDK.Validations
{
    // One Analyze call's mutable half and the only place a child is reached from. A class, not a ref struct.
    // The context is a parameter, never a field. TWO PHASES, NOT FUSED: Check every property, then Descend -
    // fusing reorders the report and RuleFixer's repairs do not commute. BH.SDK/Docs/VALIDATIONS.md.

    /// <summary> The state of one <see cref="RuleAnalyzer.Analyze"/> call, and the single point every
    /// child node is reached through. </summary>
    public sealed class RuleWalk
    {
        // Boxing an int is what a RulePath costs per list/array element, and a level's keyframe
        // tracks are the bulk of the walk. Indices below this reuse one box each.
        private const int BoxedIndexCount = 1024;

        private static readonly object[] BoxedIndexes = CreateBoxedIndexes();

        private readonly List<RulePath> _trace = new(16);
        private readonly List<RuleIssue> _result = new(8);
        private readonly RuleAnalyzerSettings _settings;
        private readonly RuleAnalyzer _reflective;
        private readonly bool _useGenerated;

        /// <summary> How thorough this walk is. </summary>
        public RuleAnalyzerSettings Settings => _settings;

        /// <summary> The findings so far, in the order they were reported. </summary>
        public List<RuleIssue> Issues => _result;

        /// <summary> How deep the walk currently is; one entry per property, index or key. </summary>
        public int Depth => _trace.Count;

        /// <summary> Built from its reflective and settings. </summary>
        public RuleWalk(RuleAnalyzer reflective, RuleAnalyzerSettings settings)
        {
            _reflective = reflective;
            _settings = settings;
            _useGenerated = settings.useGeneratedWalk;
        }

        private static object[] CreateBoxedIndexes()
        {
            var indexes = new object[BoxedIndexCount];
            for (var i = 0; i < BoxedIndexCount; i++)
                indexes[i] = i;
            return indexes;
        }

        // THE DISPATCH POINT. Every path into a child node comes through here - the four Descend
        // shapes, and Analyze's own first step - so there is exactly one place that decides whether
        // a value walks itself or is walked by reflection.

        /// <summary> Walk one value, by its own <see cref="IValidatable"/> if it has one and
        /// reflectively otherwise. </summary>
        public void Node(object value, RuleContext context)
        {
            if (value == null) return;

            if (_useGenerated && value is IValidatable validatable)
            {
                validatable.Validate(this, context);
                return;
            }

            _reflective.WalkNode(value, context, this);
        }

        // A rule that spans two properties is reported against the object owning both, and lands in
        // the trace BEFORE either of them - which is what makes the reverse-order fix pass repair
        // the pair first. So this runs before any Check, always.

        /// <summary> Run a type's class-level rules against the object itself. </summary>
        public void ObjectRules(BaseObjectRuleAttribute[] rules, object target, Type targetType,
            RuleContext context, bool typeChecked = false)
        {
            foreach (var rule in rules)
            {
                if (!typeChecked && !rule.IsValidType(targetType))
                {
                    throw new ArgumentException($"Can't apply rule {rule.GetType().Name} " +
                                                $"to type {targetType.Name}, path: {_trace.GetPath()}");
                }

                if (!_settings.Reports(rule.Group)) continue;
                if (rule.IsValid(target, context)) continue;

                _result.Add(new RuleIssue(rule, context, new List<RulePath>(_trace)));
            }
        }

        /// <summary>
        /// Run one property's own rules under the trace segment they are reported at. Returns whether the
        /// descent phase may walk into this value; a failing rule suppresses it.
        /// </summary>
        public bool Check(PropertyInfo property, BasePropertyRuleAttribute[] rules, object value,
            RuleContext context, bool typeChecked = false)
        {
            // An absent value breaks no bound, but only for a property marked [RuleOptional]; asked only when
            // the value IS null, so the ordinary path pays one reference comparison.
            if (value is null && IsOptional(property)) return true;

            if (rules.Length == 0) return value != null;

            // The trace segment is pushed LAZILY: nearly every rule passes, and pushing per property per node
            // cost about five million push/pop pairs for one level. The throw path pushes and never pops.
            var pushed = false;
            var hasInvalidRule = false;

            foreach (var rule in rules)
            {
                // Asked once per type, NOT per node - and NOT hoisted out of the loop: an earlier failing
                // rule breaks it, so a later misapplied rule is never reached and must not throw.
                if (!typeChecked && !rule.IsValidType(property))
                {
                    if (!pushed) _trace.Add(new RulePath(property));

                    throw new ArgumentException($"Can't apply rule {rule.GetType().Name} " +
                                                $"to type {value?.GetType().Name}, path: {_trace.GetPath()}");
                }

                if (!_settings.Reports(rule.Group)) continue;
                if (rule.IsValid(value, context)) continue;

                if (!pushed)
                {
                    _trace.Add(new RulePath(property));
                    pushed = true;
                }

                hasInvalidRule = true;
                _result.Add(new RuleIssue(rule, context, new List<RulePath>(_trace)));

                if (!_settings.analyzeAllPropertyRules) break;
            }

            if (pushed) _trace.RemoveAt(_trace.Count - 1);

            return value != null && (!hasInvalidRule || _settings.analyzeAllRecursiveRules);
        }

        // THE TWO A GENERATED WALK CALLS. They add nothing but the ordinal: a table is the same
        // properties and the same rule arrays the reflective path builds, so both forms reach the
        // same code below and cannot drift apart in behaviour.

        /// <summary> Run a type's class-level rules, addressed through its table. </summary>
        public void ObjectRules(RuleTable table, object target, RuleContext context)
            => ObjectRules(table.ObjectRules, target, table.Type, context,
                table.ObjectRulesTypeChecked);

        /// <summary> Run one property's rules, addressed by ordinal. </summary>
        public bool Check(RuleTable table, int ordinal, object value, RuleContext context)
            => Check(table.Properties[ordinal], table.Rules[ordinal], value, context,
                table.RulesTypeChecked[ordinal]);

        /// <summary> Walk into a property's value, in whatever shape it turns out to have. </summary>
        public void Descend(PropertyInfo property, object value, RuleContext context)
        {
            switch (value)
            {
                case null:
                    return;

                // A rank-1 array is an IList too and lands here; the paths are identical either way.
                case IList list:
                    DescendList(property, list, context);
                    return;

                case IDictionary dictionary:
                    DescendDictionary(property, dictionary, context);
                    return;
            }

            if (value.GetType().IsArray) DescendArray(property, (Array)value, context);
            else DescendOne(property, value, context);
        }

        // A NULL COLLECTION IS LEGAL. The generated walk calls these INSTEAD of Check, so RuleOptional's
        // short-circuit never reaches them (ThemeData.ColorNames threw here).

        /// <summary> Walks into a list, recording the index in the trace. </summary>
        public void DescendList(PropertyInfo property, IList list, RuleContext context)
        {
            if (list is null) return;

            for (var i = 0; i < list.Count; i++)
            {
                _trace.Add(new RulePath(property, BoxIndex(i)));
                Node(list[i], context);
                _trace.RemoveAt(_trace.Count - 1);
            }
        }

        // VALUES ONLY: a RulePath cannot say "the key itself", so an issue on a key would be repaired into
        // the value. Keys are id structs, never a [RuleContainer]; key/value agreement is the graph pass's.

        /// <summary> Walks into a dictionary, recording the key in the trace. </summary>
        public void DescendDictionary(PropertyInfo property, IDictionary dictionary, RuleContext context)
        {
            if (dictionary is null) return;

            foreach (DictionaryEntry entry in dictionary)
            {
                _trace.Add(new RulePath(property, entry.Key));
                Node(entry.Value, context);
                _trace.RemoveAt(_trace.Count - 1);
            }
        }

        /// <summary> Walks into an array, recording the index in the trace. </summary>
        public void DescendArray(PropertyInfo property, Array array, RuleContext context)
        {
            if (array is null) return;

            for (var i = 0; i < array.Length; i++)
            {
                _trace.Add(new RulePath(property, BoxIndex(i)));
                Node(array.GetValue(i), context);
                _trace.RemoveAt(_trace.Count - 1);
            }
        }

        /// <summary> Walks into a single value - the ONE point a generated walk and the reflective one meet. </summary>
        public void DescendOne(PropertyInfo property, object value, RuleContext context)
        {
            _trace.Add(new RulePath(property));
            Node(value, context);
            _trace.RemoveAt(_trace.Count - 1);
        }

        private static object BoxIndex(int index)
            => index < BoxedIndexCount ? BoxedIndexes[index] : index;

        // Cached because a Mono custom-attribute lookup allocates a fresh attribute instance on every
        // call - the same reason RuleContainer is not queried per node.
        private static readonly ConcurrentDictionary<PropertyInfo, bool> OptionalCache = new();

        private static bool IsOptional(PropertyInfo property)
        {
            if (property == null) return false;
            if (OptionalCache.TryGetValue(property, out var optional)) return optional;
            optional = property.GetCustomAttribute<RuleOptionalAttribute>() != null;
            OptionalCache[property] = optional;
            return optional;
        }
    }
}