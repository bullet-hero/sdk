namespace BH.SDK.Models.Enums.Controls.Modes
{
    // One mode enum per device so an unsupported mode is unrepresentable; values line up with ControlMode
    // at 0/1/2 and RuleEnumValid validates against this device's own set.
    /// How keyboard and mouse drive the avatar.
    /// </summary>
    public enum KeyboardMouseControlMode : byte
    {
        /// <summary>The mouse position is the cursor - the PC default.</summary>
        Absolute = 0,

        /// <summary>The OS cursor is locked and its delta moves the in-world cursor.</summary>
        Relative = 1,

        /// <summary>WASD, no cursor.</summary>
        Direction = 2,
    }
}
