namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    /// <summary> How the leading control device was chosen at generation 1. The live enum is gone -
    /// the leader is always the most recently used device now. A frozen snapshot - never edit it. </summary>
    public enum DeviceSelectionV1 : byte
    {
        /// <summary> The most recently used device leads. </summary>
        Auto = 0,

        /// <summary> The player pinned one. </summary>
        Manual = 1,
    }
}
