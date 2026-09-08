using BH.SDK.Models.Enums;

namespace BH.SDK.Models.Interfaces
{
    /// <summary> A frame carrying an authored value, plus how the value approaches it from the previous key. </summary>
    public interface IKeyframe : IFrame
    {
        /// <summary> The curve used to blend from the previous keyframe into this one. </summary>
        public EaseType Ease { get; set; }
    }
}