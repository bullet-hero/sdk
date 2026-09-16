namespace BH.SDK.Models.Enums.Controls
{
    // None is LAST rather than zero, and that is the whole reason it has a value of its own: Left is
    // the default a hold button arrives at, and moving it off zero would silently repoint every
    // settings file and every blob byte already written for one.

    /// <summary>
    /// Which mouse button carries a control role - holding the "follow the cursor" state, or asking
    /// for a dash.
    /// </summary>
    public enum MouseButton : byte
    {
        Left = 0,
        Right = 1,
        Middle = 2,

        /// <summary>Any of the three counts as held.</summary>
        Any = 3,

        /// <summary>No button at all - the role is not bound to the mouse.</summary>
        None = 4,
    }
}
