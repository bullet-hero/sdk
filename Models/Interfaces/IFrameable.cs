using BH.SDK.Models.Enums.Settings;

namespace BH.SDK.Models.Interfaces
{
    public interface IFrameable
    {
        public FramerateTarget FpsTarget { get; set; }
        
        public int FpsFixed { get; set; }
    }
}