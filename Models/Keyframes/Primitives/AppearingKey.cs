using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Text;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    // Same shape and same reasoning as FillmentKey: a bounded fraction rather than a polymorphic
    // IFloat, and a mode that travels with the key the way Ease does - between two keys the LATER
    // one's mode wins, so a track can dissolve at random and then resolve left-to-right without
    // being split into two objects.

    /// <summary>
    /// How much of a text object hides behind its appearing mask at one frame, and in what order.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AppearingKey : Keyframe, IModel<AppearingKey>
    {
        /// <summary> Fraction of the characters hidden at this frame. </summary>
        [RuleInRange(TextRules.MinAppearing, TextRules.MaxAppearing)]
        [JsonProperty(Names.Float)]
        public float Value { get; set; }

        /// <summary> Which characters hide first on the way into this key. </summary>
        [RuleEnumValid(TextRules.AppearingMode_Default)]
        [JsonProperty(Names.AppearingMode)]
        public TextAppearingMode Mode { get; set; }

        public AppearingKey()
        {
            Value = TextRules.Appearing_Fallback;
            Mode = TextRules.AppearingMode_Default;
        }
        public AppearingKey(float value, int frame, TextAppearingMode mode = TextRules.AppearingMode_Default,
            EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
            Mode = mode;
        }
    }
}
