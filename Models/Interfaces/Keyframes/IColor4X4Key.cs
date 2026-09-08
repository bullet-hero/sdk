using BH.SDK.Models.Enums.Keyframes;

namespace BH.SDK.Models.Interfaces.Keyframes
{
    /// <summary> A keyframe holding four RGBA colours - one theme-capable corner set. </summary>
    public interface IColor4X4Key : IKeyframe, IModel<IColor4X4Key>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public Color4X4KeyType GetModelType();
    }
}