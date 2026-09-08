using System;

namespace BH.SDK.Models.Enums.Settings
{
    /// <summary> Which surfaces something is drawn on - the game, the editor, or both. </summary>
    [Flags]
    public enum RenderStatus : byte
    {
        None = 0,
        Player = 1 << 0,
        Editor = 1 << 1,
        All = 255,
    }
}