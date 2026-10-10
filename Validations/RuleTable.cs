using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BH.SDK.Rules.Attributes;

namespace BH.SDK.Validations
{
    // What the generator deliberately does NOT produce: the properties and rule arrays, built once per
    // type by the expression RuleAnalyzer uses, so PropertyInfo, rule instances and reflection ORDER are
    // identical on both paths (order decides which finding wins). Ordinals are checked at class init.

    /// <summary> One type's walked properties and their rules, resolved once. </summary>
    public sealed class RuleTable
    {
        /// <summary> Exactly what RuleAnalyzer walks, in exactly its order. </summary>
        public readonly PropertyInfo[] Properties;

        /// <summary> The rules on each property, by the same ordinal. </summary>
        public readonly BasePropertyRuleAttribute[][] Rules;

        /// <summary> The whole-object rules written on this type. </summary>
        public readonly BaseObjectRuleAttribute[] ObjectRules;

        // Whether every rule on a property passed IsValidType once, at build: a reflective test that used
        // to run per rule per node. Per property so one misapplied rule slows nobody else; where one
        // failed, Check runs the original loop verbatim (same throw, node and path).

        /// <summary> Whether each property rule may sit where it does - asked once per type, not per value. </summary>
        public readonly bool[] RulesTypeChecked;

        /// <summary> The same answer for the object rules. </summary>
        public readonly bool ObjectRulesTypeChecked;

        /// <summary> The type this table describes. </summary>
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
