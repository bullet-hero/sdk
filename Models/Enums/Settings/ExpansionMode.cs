namespace BH.SDK.Models.Enums.Settings
{
    // Three rungs on one button; the middle resolves to GameEditorSettings.Interface.ExpansionMask. The cycle
    // keeps all three steps. Cycle order is the consumer's. Why: Docs/ENUMS.md

    /// <summary> How much of a folding surface is unfolded before any per-node exception. </summary>
    public enum ExpansionMode : byte
    {
        /// <summary> Every kind of object unfolded. </summary>
        Expanded = 0,

        /// <summary> Only the kinds the author's own mask names. </summary>
        Partial = 1,

        /// <summary> Nothing unfolded. </summary>
        Collapsed = 2,
    }
}
