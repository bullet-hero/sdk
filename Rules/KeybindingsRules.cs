namespace BH.SDK.Rules
{
    // Limits for the keybindings map, in the same "Max" shape PrefabRules uses for its collection
    // caps. Nothing here clamps at runtime: an override the game does not recognize resolves to the
    // catalog's own default, so a hand-edited file with a wilder number costs the author that one
    // entry rather than the whole map.

    /// <summary>
    /// Bounds for <see cref="BH.SDK.Models.SettingGroups.KeybindingsSettings"/>.
    /// </summary>
    public static class KeybindingsRules
    {
        // Comfortably above the catalog's own size, so a player who rebinds literally everything
        // still fits, and low enough that a hostile file cannot ship a map the settings screen has
        // to lay a row out for.

        /// <summary> How many overridden shortcuts one settings file may carry. </summary>
        public const int MaxOverrides = 512;

        // Two, and the second is a genuine alternate rather than a chord: a player who wants Redo on
        // both Ctrl+Y and Ctrl+Shift+Z is the whole use case, and a third slot has no consumer and
        // no room in a settings row.

        /// <summary> How many keys one shortcut may answer to. </summary>
        public const int MaxAlternates = 2;

        /// <summary> Longest legal value string, alternates and separators included. </summary>
        public const int MaxBindingLength = 64;

        /// <summary> Longest legal shortcut id - the dictionary's key. </summary>
        public const int MaxShortcutIdLength = 64;
    }
}
