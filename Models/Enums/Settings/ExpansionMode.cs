namespace BH.SDK.Models.Enums.Settings
{
    // THREE RUNGS ON ONE BUTTON, and the middle one is the only one an author configures - it
    // resolves to GameEditorSettings.Interface.ExpansionMask, while the two ends resolve to every
    // bit and to no bit whatever that mask says.
    //
    // The cycle keeps all three steps even when the configured mask is All or None and the middle
    // rung therefore lands on the same rows as a neighbour. A cycle that silently dropped a step
    // would make the button's behaviour depend on a setting two screens away, and an author counting
    // presses would be wrong without being told why.
    //
    // It lives here rather than beside the surfaces that fold, because two settings now spell a
    // STARTING rung in the file - GameEditorSettings.Interface.HierarchyExpansion and
    // .TimelineExpansion - and a serialized member may not be typed by a consumer's enum. Which
    // rung a surface is currently ON stays session state in the consumer, exactly as the grid's
    // ActiveDefault/current split does. The cycle order itself is the consumer's
    // (Services.GameEditor's ExpansionModeExtensions.Next): it is how a button behaves, not what a
    // file stores.

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
