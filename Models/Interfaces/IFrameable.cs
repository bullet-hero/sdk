using BH.SDK.Models.Enums.Settings;

namespace BH.SDK.Models.Interfaces
{
    /// <summary> Carries a framerate preference: a target mode plus the number the fixed mode uses. </summary>
    public interface IFrameable
    {
        /// <summary> Which framerate policy applies - screen refresh, uncapped, or the fixed number below. </summary>
        public FramerateTarget FpsTarget { get; set; }
        
        /// <summary> The framerate used when the target is the fixed one, ignored otherwise. </summary>
        public int FpsFixed { get; set; }
    }
}