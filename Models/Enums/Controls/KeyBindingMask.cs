using System;

namespace BH.SDK.Models.Enums.Controls
{
    /// <summary>
    /// A small curated set of keyboard keys bindable to one gameplay action; a full key enum would be a
    /// second source of truth against the engine's own.
    /// </summary>
    [Flags]
    public enum KeyBindingMask : ushort
    {
        None = 0,

        Space = 1 << 0,
        Shift = 1 << 1,
        Control = 1 << 2,
        Alt = 1 << 3,
        Enter = 1 << 4,
        Tab = 1 << 5,

        KeyQ = 1 << 6,
        KeyE = 1 << 7,
        KeyF = 1 << 8,
        KeyR = 1 << 9,
        KeyZ = 1 << 10,
        KeyX = 1 << 11,
        KeyC = 1 << 12,
        KeyV = 1 << 13,
    }
}
