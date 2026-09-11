namespace BH.SDK.Models.Interfaces
{
    /// <summary> Sits on exactly one frame of a timeline - a keyframe, a marker, a checkpoint. </summary>
    public interface IFrame
    {
        /// <summary> The frame it sits on, as a CELL rather than a boundary - counted from
        /// FrameRules.MinFrame like every frame in the format. </summary>
        public int Frame { get; set; }
    }
}