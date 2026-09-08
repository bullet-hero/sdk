using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;

namespace BH.SDK.Validations
{
    /// <summary> One violated rule: which rule, on which property of which object, and the context it fired in -
    /// which is what a repair replays against. </summary>
    public readonly struct RuleIssue
    {
        /// <summary> The rule that refused the value. </summary>
        public readonly BaseRuleAttribute Rule;

        // The context as it was WHERE the rule fired, not as it is at the root - inside a prefab
        // template that means the template's own timeline and scope. RuleFixer replays the fix
        // against this exact context, so a repair lands on the same bounds the check used.

        /// <summary> The context the rule fired in - which is what a repair replays against, not the root. </summary>
        public readonly RuleContext Context;

        /// <summary> The route from the analysis root down to the property that failed. </summary>
        public readonly List<RulePath> Trace;

        /// <summary> The object the analysis started from. </summary>
        public object Root => Context.Root;

        /// <summary> Built from its rule, context and trace. </summary>
        public RuleIssue(BaseRuleAttribute rule, RuleContext context, List<RulePath> trace)
        {
            Rule = rule;
            Context = context;
            Trace = trace;
        }

        /// <summary> The value that failed, walked back down the trace. </summary>
        public object GetValue()
        {
            var result = Root;
            foreach (var path in Trace)
            {
                result = path.Property.GetValue(result);
                if (path.HasKey) result = GetCollectionItem(result, path);
            }
            return result;
        }

        /// <summary> The object holding the failed property, and the property itself. </summary>
        public (object, PropertyInfo) GetContextAndProperty()
        {
            var result = Root;
            for (var i = 0; i < Trace.Count - 1; i++)
            {
                var path = Trace[i];
                result = path.Property.GetValue(result);
                if (path.HasKey) result = GetCollectionItem(result, path);
            }
            var resultProperty = Trace[^1].Property;
            return (result, resultProperty);
        }

        private static object GetCollectionItem(object collection, RulePath path)
        {
            return collection switch
            {
                IDictionary dictionary => dictionary[path.Key],
                IList list => list[(int)path.Key],
                _ when collection.GetType().IsArray => ((Array)collection).GetValue((int)path.Key),
                _ => collection,
            };
        }

        // Applying the repair is the issue's own job, because only it knows which kind of rule it
        // holds: a property rule needs the owner plus the PropertyInfo, an object rule needs the
        // object itself and has no property at all.

        /// <summary> Runs the rule's own repair, against the context the check ran in. </summary>
        public void ApplyFix()
        {
            switch (Rule)
            {
                case BasePropertyRuleAttribute propertyRule:
                {
                    var (target, property) = GetContextAndProperty();
                    propertyRule.Fix(target, property, Context);
                    break;
                }
                case BaseObjectRuleAttribute objectRule:
                {
                    objectRule.Fix(GetValue(), Context);
                    break;
                }
            }
        }

        /// <summary> The trace as the dotted, indexed path a person reads. </summary>
        public string GetPath() => Trace.GetPath();
        /// <summary> One line, for a log. </summary>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("Issue, Rule: ");
            builder.Append(Rule.GetType().Name);
            builder.Append(", Trace: ");
            Trace.BuildTracePath(builder);
            return builder.ToString();
        }
    }
}
