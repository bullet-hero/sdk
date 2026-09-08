namespace BH.SDK.Models.Interfaces
{
    /// <summary> Sits on exactly one frame of a timeline - a keyframe, a marker, a checkpoint. </summary>
    public interface IFrame
    {
        /// <summary> The frame it sits on, as a cell index rather than a boundary. </summary>
        public int Frame { get; set; }
    }
}