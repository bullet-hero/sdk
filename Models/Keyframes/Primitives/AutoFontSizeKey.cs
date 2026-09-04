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
    // AUTO SIZING ONLY EVER SHRINKS, and that is what makes MaxValue the authored size rather than a
    // ceiling bolted onto one: the renderer starts at MaxValue and steps down until the text fits the
    // object's rect, never above it. So switching a key from FontSizeKey to this one with
    // MaxValue = the old Value cannot change how a text that already fitted looks.
    //
    // The box it is fitted into is the object's own rect - the Sizes track - so there is nothing here
    // naming one. Word wrap changes what "fits" means (unwrapped text is measured as one line), which
    // is why the two settings are worth thinking about together and are still authored apart.
    //
    // Neither bound is checked against the other. min > max is legal authored data and the renderer
    // resolves it; a rule would have to be a graph finding to see two properties at once, and graph
    // findings here never carry a repair - so it would report an author's own arrangement forever.

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

        public AutoFontSizeKey()
        {
            MinValue = new FloatValue(TextRules.AutoFontSize_Min_Default);
            MaxValue = new FloatValue(TextRules.AutoFontSize_Max_Default);
        }
        public AutoFontSizeKey(IFloat minValue, IFloat maxValue, int frame, EaseType ease = DefaultEase)
            : base(frame, ease)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public FontSizeKeyType GetModelType() => FontSizeKeyType.Auto;
    }
}
