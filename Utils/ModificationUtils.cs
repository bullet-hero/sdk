using System.Reflection;
using BH.SDK.Models.Objects;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;

namespace BH.SDK.Utils
{
    // WHAT THIS USED TO BE was 377 lines of reflection: a static constructor walking ~200 types to
    // build a dictionary keyed on ([JsonProperty] name, declaring type), sixteen generic
    // AddProperties overloads feeding it, a hand-written parser for dotted/indexed paths, and an
    // Apply that resolved a path per override and returned in silence when it did not resolve -
    // with the LogWarning commented out. Three of the twenty-eight live paths never resolved, and
    // nothing in the project could see it. All of that is one generated switch now.
    //
    // The file stays rather than folding into ModificationTable because the generated table answers
    // structural questions only - which field, what type, where to write it. Whether a value is
    // ALLOWED there is a rules question, and the rules live on the property as attributes that no
    // generator consumes. That is why PropertyOf exists and why this is the only caller of it.

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

        // A PER-INSTANCE OVERRIDE IS THE ONE WRITE THAT REACHES A MODEL WITH NOTHING TO JUDGE IT.
        // It bypasses every rule the target property carries, so an override can park a frame past
        // the end of the timeline or a colour outside 0..1 while the level it belongs to validates
        // clean - the one hole the validation standard cannot close from the model side, since the
        // value never passes through the property's own setter contract.

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