namespace BH.SDK.Models.Enums.Controls.Modes
{
    /// <summary>
    /// How the device's own motion sensor drives the avatar. There is no Relative: angular velocity has
    /// no absolute up and drifts. The values still line up with ControlMode, hence the gap at 1.
    /// </summary>
    public enum DeviceGyroControlMode : byte
    {
        /// <summary>Tilt angle is the cursor's position.</summary>
        Absolute = 0,

        /// <summary>Tilt is the direction, no cursor - the default here, since a phone is held rather
        /// than aimed.</summary>
        Direction = 2,
    }
}
