using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BH.SDK.Rules.Attributes;

namespace BH.SDK.Validations
{
    // WHAT THE GENERATOR DELIBERATELY DOES NOT PRODUCE. A generated Validate supplies the VALUES and
    // the shape of the walk; the properties and the rules sitting on them are built here, by the
    // same expression RuleAnalyzer uses, once per type. Four things that buys, none of which a
    // generated table would:
    //
    // - RulePath.Property is the SAME PropertyInfo object the reflective path hands out, so a trace
    //   compares equal either way and RuleFixer writes through it unchanged.
    // - RuleIssue.Rule is the same attribute INSTANCE for every node of a type, as it has always
    //   been. Emitting `new RuleInRangeAttribute(0, 10)` per node would change both the allocation
    //   count and reference identity, which nothing currently forbids and something may rely on.
    // - The rule ARRAY keeps reflection's order. Under the default analyzeAllPropertyRules = false
    //   the first FAILING rule wins and the loop breaks, so a different order reports a different
    //   finding - not a reordered one. 145 properties carry two to five rules.
    // - Repeated AllowMultiple attributes keep reflection's order too (Color4MinMax carries four
    //   [RulePropertyOrder]), and that order is not contractually source order.
    //
    // THE ORDINAL CONTRACT IS CHECKED, LOUDLY, ONCE. The generator's whole correctness rests on its
    // property list being GetProperties' list in GetProperties' order; a silent divergence would
    // report the same findings under the wrong paths, in the wrong order, and RuleFixer repairs in
    // reverse. So the expected names travel with the ordinals and a mismatch throws at class-init
    // rather than at some node halfway through a level.

    /// <summary> One type's walked properties and their rules, resolved once. </summary>
    public sealed class RuleTable
    {
        /// <summary> Exactly what RuleAnalyzer walks, in exactly its order. </summary>
        public readonly PropertyInfo[] Properties;

        /// <summary> The rules on each property, by the same ordinal. </summary>
        public readonly BasePropertyRuleAttribute[][] Rules;

        public readonly BaseObjectRuleAttribute[] ObjectRules;

        // WHETHER EVERY RULE ON A PROPERTY ANSWERED IsValidType ONCE, AT BUILD. That check is a
        // reflective type test (typeof(ICollection).IsAssignableFrom(...), and so on) and it used to
        // run per rule, per node - millions of times for one level, to ask a question whose answer
        // is a property of the TYPE and cannot change. Where every rule passed it, Check skips it,
        // and skipping it is unobservable precisely because it would never have fired.
        //
        // Per property rather than per table: one misapplied rule must not slow every other
        // property down. Where one did fail, Check takes the original loop verbatim - the same
        // throw, at the same node, with the same path in the message, and with the same subtlety
        // that an earlier failing rule breaks the loop before a later misapplied one is reached.
        public readonly bool[] RulesTypeChecked;

        public readonly bool ObjectRulesTypeChecked;

        public readonly Type Type;

        private RuleTable(Type type, PropertyInfo[] properties, BasePropertyRuleAttribute[][] rules,
            bool[] rulesTypeChecked, BaseObjectRuleAttribute[] objectRules, bool objectRulesTypeChecked)
        {
            Type = type;
            Properties = properties;
            Rules = rules;
            RulesTypeChecked = rulesTypeChecked;
            ObjectRules = objectRules;
            ObjectRulesTypeChecked = objectRulesTypeChecked;
        }

        /// <summary>
        /// Build the table a generated <see cref="IValidatable.Validate"/> addresses by ordinal.
        /// <paramref name="expected"/> is what the generator believed the property list to be, as
        /// "DeclaringType.Property" per ordinal; a disagreement with reflection is fatal.
        /// </summary>
        public static RuleTable For(Type type, bool hasObjectRules, string[] expected)
        {
            // THE SAME EXPRESSION RuleAnalyzer.GetObjProperties USES, not a reimplementation of it:
            // the two paths have to hand out the same PropertyInfo objects in the same order, and
            // the cheapest way to guarantee that is for there to be only one way of asking.
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.CanWrite)
                .ToArray();

            if (properties.Length != expected.Length) throw Drift(type, properties, expected);

            for (var i = 0; i < properties.Length; i++)
            {
                // Declaring type AND name: a hidden pair shares a name, so a name-only check would
                // pass on exactly the case that reorders the ordinals.
                if (Describe(properties[i]) == expected[i]) continue;
                throw Drift(type, properties, expected);
            }

            var rules = new BasePropertyRuleAttribute[properties.Length][];
            var checkedRules = new bool[properties.Length];
            for (var i = 0; i < properties.Length; i++)
            {
                rules[i] = properties[i].GetCustomAttributes<BasePropertyRuleAttribute>(true).ToArray();

                checkedRules[i] = true;
                foreach (var rule in rules[i])
                    checkedRules[i] &= rule.IsValidType(properties[i]);
            }

            var objectRules = type.GetCustomAttributes<BaseObjectRuleAttribute>(true).ToArray();
            if (hasObjectRules != objectRules.Length > 0)
            {
                throw new InvalidOperationException($"Generated validation for {type.FullName} "
                    + (hasObjectRules
                        ? "expects object rules that reflection does not report"
                        : "elides an object-rule pass this type actually has"));
            }

            var checkedObjectRules = true;
            foreach (var rule in objectRules) checkedObjectRules &= rule.IsValidType(type);

            return new RuleTable(type, properties, rules, checkedRules,
                objectRules, checkedObjectRules);
        }

        private static string Describe(PropertyInfo property)
            => property.DeclaringType?.Name + "." + property.Name;

        private static InvalidOperationException Drift(Type type, PropertyInfo[] properties,
            string[] expected)
        {
            var builder = new StringBuilder();
            builder.Append("Generated validation for ").Append(type.FullName)
                .AppendLine(" is out of step with reflection.");
            builder.Append("  generator: ").AppendLine(string.Join(", ", expected));
            builder.Append("  reflection: ")
                .AppendLine(string.Join(", ", properties.Select(Describe)));
            builder.Append("Rebuild BH.SDK.Roslyn (Tools > BH.SDK.Roslyn > Build Analyzer) - a stale ")
                .Append("analyzer is by far the likeliest cause.");

            return new InvalidOperationException(builder.ToString());
        }
    }
}
