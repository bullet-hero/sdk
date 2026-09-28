namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    /// <summary> The motion sensor's modes at generation 1, with <c>Relative</c>, which generation 2
    /// removed. A frozen snapshot - never edit it. </summary>
    public enum DeviceGyroControlModeV1 : byte
    {
        /// <summary> Tilt angle is the cursor's position. </summary>
        Absolute = 0,

        /// <summary> Angular velocity moves the cursor. Gone at generation 2. </summary>
        Relative = 1,

        /// <summary> Tilt is the direction, no cursor. </summary>
        Direction = 2,
    }
}
