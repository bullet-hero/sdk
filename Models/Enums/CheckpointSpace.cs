namespace BH.SDK.Models.Enums
{
    // World is 0 on purpose: a file from before a position existed reads as "respawn where the level says"
    // (so no bump). Camera vs CameraPosition differ by the camera's rotation - Docs/ENUMS.md

    /// <summary>
    /// How a <see cref="Events.Checkpoint"/>'s respawn position is interpreted.
    /// </summary>
    public enum CheckpointSpace : byte
    {
        /// <summary> Absolute level coordinates; the camera is ignored. </summary>
        World = 0,

        /// <summary> Relative to the camera, with the camera's rotation applied to the offset. </summary>
        Camera = 1,

        /// <summary> Relative to the camera's global position only; its rotation is ignored. </summary>
        CameraPosition = 2,
    }
}
