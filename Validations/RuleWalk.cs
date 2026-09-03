using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;

namespace BH.SDK.Validations
{
    // ONE ANALYZE CALL'S MUTABLE HALF: the trace, the findings and the settings, plus the one place
    // a child node is reached from. Everything that decides once per TYPE - which properties exist,
    // which rules sit on them, whether a type is a container at all - stays on RuleAnalyzer, which
    // is what makes this safe to allocate per call.
    //
    // A CLASS, NOT A ref struct, and the reason is the dispatch rather than taste: Node has to reach
    // a value's runtime walk through IValidatable, and a ref struct parameter on an interface method
    // would force `ref RuleWalk` through every generated signature, the reflective recursion and
    // every helper - ref plumbing everywhere to save ONE object per Analyze, not per node. Passed by
    // value a struct would copy several words across ~150k nodes; passed by ref it costs exactly
    // what a class reference costs.
    //
    // THE CONTEXT IS A PARAMETER, NEVER A FIELD. It changes per subtree - a prefab template rebases
    // frames and parent ids for everything below it - while everything else here lives for the whole
    // call. Holding it would mean saving and restoring it around every descent, which is the bug
    // that shape invites.
    //
    // TWO PHASES, AND THEY ARE NOT ONE. A node checks EVERY property's own rules first (Check), and
    // only then descends into every walkable one (Descend). Fusing them - checking a property and
    // immediately walking into it - is the natural refactor and it reorders the report for every
    // object with more than one walkable property, i.e. every RectObject. Issue order is RuleFixer's
    // input and its repairs are not commutative, so a reordering is a different repair.

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

        public RuleAnalyzerSettings Settings => _settings;

        /// <summary> The findings so far, in the order they were reported. </summary>
        public List<RuleIssue> Issues => _result;

        /// <summary> How deep the walk currently is; one entry per property, index or key. </summary>
        public int Depth => _trace.Count;

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
        /// Run one property's own rules, under the trace segment they are reported at. Returns
        /// whether the descent phase may walk into this value - a failing rule suppresses it, since
        /// what is below a value the rule already rejected is rarely worth reporting twice.
        /// </summary>
        public bool Check(PropertyInfo property, BasePropertyRuleAttribute[] rules, object value,
            RuleContext context, bool typeChecked = false)
        {
            if (rules.Length == 0) return value != null;

            // THE TRACE SEGMENT IS PUSHED LAZILY, and on a real level that is most of what this
            // method costs. A finding needs the property on the trace; a rule that passes needs
            // nothing, and nearly every rule passes - volcano's 19 341 objects report zero. Pushing
            // unconditionally cost one List.Add and one RemoveAt per property per node, on the order
            // of five million pairs for one level, to build a path nobody read.
            //
            // The throw path pushes first and never pops, exactly as before: the walk is abandoned,
            // and the walk object dies with the Analyze call that made it.
            var pushed = false;
            var hasInvalidRule = false;

            foreach (var rule in rules)
            {
                // ASKED ONCE PER TYPE, NOT PER NODE. Whether a rule may sit on a property is a fact
                // about the TYPE, and RuleTable (or PropertyEntry, on the reflective path) settles
                // it at build; typeChecked means every rule here already answered yes. It is not
                // hoisted out of the loop entirely, and must not be: an earlier FAILING rule breaks
                // this loop, so a later misapplied one is never reached and must not throw.
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

                // An array is an IList too, and a rank-1 one lands here rather than in the array
                // branch below. That is not an accident to tidy up: both index the same way, so the
                // paths come out identical, and the array branch is what a multi-dimensional or
                // non-generic array would take.
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

        public void DescendList(PropertyInfo property, IList list, RuleContext context)
        {
            for (var i = 0; i < list.Count; i++)
            {
                _trace.Add(new RulePath(property, BoxIndex(i)));
                Node(list[i], context);
                _trace.RemoveAt(_trace.Count - 1);
            }
        }

        // VALUES ONLY, DELIBERATELY. A RulePath addresses "this property, at this key", which is how
        // RuleFixer finds its way back to a value - there is no way to express "the key itself", so
        // an issue raised on a key would be repaired into the value instead. Keys stay safe without
        // the walk: every key in the format is an id struct, and a struct can never be a
        // [RuleContainer] anyway (it is boxed on the way in, so any Fix would write into a copy and
        // vanish). Anything a key needs checking for - above all whether it agrees with its value's
        // own id - is relational, and belongs to the graph pass rather than here.

        public void DescendDictionary(PropertyInfo property, IDictionary dictionary, RuleContext context)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                _trace.Add(new RulePath(property, entry.Key));
                Node(entry.Value, context);
                _trace.RemoveAt(_trace.Count - 1);
            }
        }

        public void DescendArray(PropertyInfo property, Array array, RuleContext context)
        {
            for (var i = 0; i < array.Length; i++)
            {
                _trace.Add(new RulePath(property, BoxIndex(i)));
                Node(array.GetValue(i), context);
                _trace.RemoveAt(_trace.Count - 1);
            }
        }

        public void DescendOne(PropertyInfo property, object value, RuleContext context)
        {
            _trace.Add(new RulePath(property));
            Node(value, context);
            _trace.RemoveAt(_trace.Count - 1);
        }

        private static object BoxIndex(int index)
            => index < BoxedIndexCount ? BoxedIndexes[index] : index;
    }
}