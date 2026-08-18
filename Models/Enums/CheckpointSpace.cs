namespace BH.SDK.Models.Enums
{
    // World is 0 on purpose, so a file written before Checkpoint carried a position deserializes
    // into it and reads as "respawn where the level says", which is what every existing checkpoint
    // already meant. That is also why GameEvents never bumped its DataVersion - "absent" and
    // "World at (0, 0)" have to mean the same thing for this to need no migration.
    //
    // Camera and CameraPosition differ by exactly one term and the difference is the point of
    // having both: a respawn pinned to a rotating camera rides that rotation, which is what an
    // author wants for a point framed inside a tilted shot, and is exactly what they do NOT want
    // for a point that should stay upright while the camera rolls.

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
