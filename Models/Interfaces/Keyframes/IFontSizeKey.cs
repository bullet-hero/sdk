using BH.SDK.Models.Enums.Keyframes;

namespace BH.SDK.Models.Interfaces.Keyframes
{
    public interface IFontSizeKey : IKeyframe, IModel<IFontSizeKey>
    {
        public FontSizeKeyType GetModelType();
    }
}
