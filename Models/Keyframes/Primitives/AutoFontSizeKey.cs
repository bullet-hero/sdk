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
    // Only ever SHRINKS: MaxValue is the authored size, the renderer steps down until the text fits the rect.
    // min > max is legal and the renderer resolves it. Models/Keyframes/CLAUDE.md

    /// <summary>
    /// Auto-sizing font key: a band the text is fitted into rather than a size it is drawn at. Both
    /// bounds are ordinary values, so either can be randomized or animated like any other.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AutoFontSizeKey : Keyframe, IFontSizeKey, IModel<AutoFontSizeKey>
    {
        /// <summary> Smallest size the text may be shrunk to before it is simply allowed to
        /// overflow. </summary>
        [RuleNotNull(typeof(FloatValue))]
        [JsonProperty(Names.Min)]
        public IFloat MinValue { get; set; }

        /// <summary> Size the text is drawn at while it fits, and the size fitting starts from. </summary>
        [RuleNotNull(typeof(FloatValue))]
        [JsonProperty(Names.Max)]
        public IFloat MaxValue { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AutoFontSizeKey()
        {
            MinValue = new FloatValue(TextRules.AutoFontSize_Min_Default);
            MaxValue = new FloatValue(TextRules.AutoFontSize_Max_Default);
        }
        /// <summary> Built from its value, value, frame and default ease. </summary>
        public AutoFontSizeKey(IFloat minValue, IFloat maxValue, int frame, EaseType ease = DefaultEase)
            : base(frame, ease)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public FontSizeKeyType GetModelType() => FontSizeKeyType.Auto;
    }
}
