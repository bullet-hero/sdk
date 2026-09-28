namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Which rotation axes a motion sensor mapped to the screen at generation 1. The live
    /// enum is gone - the tilt is read in screen space against gravity. A frozen snapshot - never edit
    /// it. </summary>
    public enum GyroAxisMappingV1 : byte
    {
        /// <summary> Turning drives X, tilting forward/back drives Y. </summary>
        YawPitch = 0,

        /// <summary> Rolling drives X, tilting forward/back drives Y. </summary>
        RollPitch = 1,
    }
}
