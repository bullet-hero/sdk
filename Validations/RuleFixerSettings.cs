namespace BH.SDK.Validations
{
    /// <summary> How many repair passes are worth attempting before a finding is called unfixable. </summary>
    public class RuleFixerSettings
    {
        // A repair can legitimately create a new violation elsewhere: assigning an id to an unset
        // resource makes it disagree with the dictionary key it is filed under, padding a collection
        // to its minimum introduces entries with their own rules. Reverse-order fixing handles the
        // nested case, but not this one - the second issue does not exist yet when the first is
        // fixed. So a full repair re-analyzes and goes again, a few times at most: anything still
        // reported after that is either unfixable by design (a dangling reference) or a genuine
        // repair loop, and both deserve to be reported rather than ground against forever.

        /// <summary> Ceiling on those passes. Whatever still reports after them is unfixable by design or a
        /// repair loop, and both deserve reporting rather than grinding. </summary>
        public int maxPasses = 4;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public RuleFixerSettings() { }

        /// <summary> Built from its passes. </summary>
        public RuleFixerSettings(int maxPasses)
        {
            this.maxPasses = maxPasses;
        }
    }
}
