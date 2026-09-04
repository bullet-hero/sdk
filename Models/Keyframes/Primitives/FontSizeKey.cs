using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Keyframes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Plain font size key and the simplest member of the font-size family: one authored size, drawn
    /// as-is. The default member - a key an author adds without asking for anything is this one.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class FontSizeKey : Keyframe, IFontSizeKey, IModel<FontSizeKey>
    {
        /// <summary> Font size at this frame. </summary>
        [RuleNotNull(typeof(FloatValue))]
        [JsonProperty(Names.Float)]
        public IFloat Value { get; set; }

        public FontSizeKey()
        {
            Value = new FloatValue(TextRules.FontSize_Fallback);
        }
        public FontSizeKey(IFloat value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }

        public FontSizeKeyType GetModelType() => FontSizeKeyType.Value;
    }
}
