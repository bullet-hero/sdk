using System;

namespace BH.SDK.Models.Enums.Controls
{
    // Three, and no more, because these are the three a keyboard shortcut is built out of on every
    // platform this ships to. Deliberately NOT the same list as KeyBindingMask's Shift/Control/Alt
    // members: those name keys a gameplay action is bound TO, and are read as ordinary presses,
    // while these qualify another key and are never a binding on their own - except on a held
    // shortcut, where the modifier IS the gesture (see ShortcutSyntax).
    //
    // Command/Super is absent on purpose. macOS is not a shipping target yet, and adding a fourth
    // bit later costs nothing: the wire form is a NAME, not this enum, so an old file carrying
    // "ctrl+c" keeps meaning what it meant.

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
