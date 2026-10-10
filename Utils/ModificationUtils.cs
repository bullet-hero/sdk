using System.Reflection;
using BH.SDK.Models.Objects;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;

namespace BH.SDK.Utils
{
    // Generated ModificationTable answers structure only; whether a value is ALLOWED is a rules question on
    // the property's attributes, which is why PropertyOf exists and this is its only caller.

    /// <summary> Applying a prefab override onto a materialized object, plainly or against the target property's own rules. </summary>
    public static class ModificationUtils
    {
        /// <summary> Writes one override onto an object. False = it did not land, which is silent by
        /// design: a stale ObjectId is already swept by the materializer, and the remaining ways to
        /// fail are not the caller's business at this layer. </summary>
        public static bool Apply(this RectObject obj, Modification mod)
        {
            if (mod is null) return false;

            return ModificationTable.Apply(obj, mod.Key.Field, mod.Key.Index, mod.Value);
        }

        // A per-instance override is the one write that reaches a model with nothing to judge it: it
        // bypasses every property rule, so this checked write is what keeps an out-of-range value out.

        /// <summary> Whether writing this value at this field would satisfy the target property's own rules. </summary>
        public static bool IsValueAllowed(int field, object value, RuleContext context)
        {
            var property = ModificationTable.PropertyOf(field);
            if (property is null) return false;

            foreach (var rule in property.GetCustomAttributes<BasePropertyRuleAttribute>(true))
            {
                if (!rule.IsValid(value, context)) return false;
            }

            return true;
        }

        /// <summary> Write only if the value satisfies the target property's rules; otherwise change
        /// nothing and report the refusal. </summary>
        public static bool SetValueChecked(this RectObject obj, Modification mod, RuleContext context)
        {
            if (mod is null) return false;

            // Addressing one ELEMENT of a collection is exempt, and deliberately: the rules on such
            // a member are about the collection (count, uniqueness of a frame across it), and none
            // of them can be judged from a single item in isolation.
            if (mod.Key.Index == Models.Primitives.ModificationKey.WholeField
                && !IsValueAllowed(mod.Key.Field, mod.Value, context))
            {
                return false;
            }

            return obj.Apply(mod);
        }
    }
}