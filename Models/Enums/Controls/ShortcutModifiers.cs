using System;

namespace BH.SDK.Models.Enums.Controls
{
    // Three modifiers qualify another key; unlike KeyBindingMask's Shift/Control/Alt, which are bound TO.
    // No Command/Super: the wire form is a name, so adding one later is free. Docs/ENUMS.md

    /// <summary>
    /// The qualifier keys a shortcut may carry, in the canonical order they are written in.
    /// </summary>
    [Flags]
    public enum ShortcutModifiers : byte
    {
        None = 0,

        Ctrl = 1 << 0,
        Shift = 1 << 1,
        Alt = 1 << 2,
    }
}
