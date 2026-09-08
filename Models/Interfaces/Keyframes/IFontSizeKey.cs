using BH.SDK.Models.Enums.Keyframes;

namespace BH.SDK.Models.Interfaces.Keyframes
{
    /// <summary> A keyframe holding a font size, which may be an absolute number or a fit rule. </summary>
    public interface IFontSizeKey : IKeyframe, IModel<IFontSizeKey>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public FontSizeKeyType GetModelType();
    }
}
