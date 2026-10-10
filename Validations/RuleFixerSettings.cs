namespace BH.SDK.Validations
{
    /// <summary> How many repair passes are worth attempting before a finding is called unfixable. </summary>
    public class RuleFixerSettings
    {
        // A repair can create a new violation elsewhere, which reverse order cannot catch (that issue does
        // not exist yet): a full repair re-analyzes a few times. What remains is unfixable by design or a
        // repair loop; both deserve a report.

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
