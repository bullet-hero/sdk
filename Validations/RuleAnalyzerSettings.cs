using System;
using BH.SDK.Rules;

namespace BH.SDK.Validations
{
    /// <summary> How thorough one analysis is, and how weak a finding may be before it stops being reported. </summary>
    [Serializable]
    public class RuleAnalyzerSettings
    {
        /// <summary> When analyzer find first invalid rule in property - it stop inspecting other rules in property.
        /// If you enable it - analyzer will check all rules in property no matter what</summary>
        public bool analyzeAllPropertyRules = false;

        /// <summary> When recursive check of property find invalid rule - it don't inspect deep into property value.
        /// If you enable it - analyzer will check all rules in property value no matter what</summary>
        public bool analyzeAllRecursiveRules = false;

        /// <summary> Weakest severity still reported. Error only reports what makes a level
        /// unplayable; Advice (the default) reports everything. Ordered by decreasing severity in
        /// RuleGroup itself, so this is an upper bound on the enum value, not a lower one. </summary>
        public RuleGroup weakestGroup = RuleGroup.Advice;

        // Exists for one caller: the parity test comparing both walks element by element (same shape as
        // SerializationSettings.useGeneratedCodecs). Also why the reflective walk is not deleted.

        /// <summary> Whether a model's own generated walk is used. Off falls back to reflection for
        /// everything, which must produce an identical report. </summary>
        public bool useGeneratedWalk = true;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public RuleAnalyzerSettings() { }

        /// <summary> Built from its all property rules and all recursive rules. </summary>
        public RuleAnalyzerSettings(bool analyzeAllPropertyRules, bool analyzeAllRecursiveRules)
        {
            this.analyzeAllPropertyRules = analyzeAllPropertyRules;
            this.analyzeAllRecursiveRules = analyzeAllRecursiveRules;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public RuleAnalyzerSettings(bool analyzeAllPropertyRules, bool analyzeAllRecursiveRules,
            RuleGroup weakestGroup)
        {
            this.analyzeAllPropertyRules = analyzeAllPropertyRules;
            this.analyzeAllRecursiveRules = analyzeAllRecursiveRules;
            this.weakestGroup = weakestGroup;
        }

        /// <summary> Whether a rule of this severity is worth reporting under these settings. None
        /// means "unclassified" and is always reported - silently dropping an issue because its
        /// author forgot to set a group would be worse than a false positive. </summary>
        public bool Reports(RuleGroup group)
            => group == RuleGroup.None || group <= weakestGroup;
    }
}
