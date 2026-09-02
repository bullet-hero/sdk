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
    // A plain float rather than the polymorphic IFloat every other scalar track uses: this is a
    // fraction of a string, bounded to 0..1, and a RandomMinMax draw on it would mean the text
    // writing itself to a different length every keyframe rather than animating.
    //
    // Direction travels WITH the key, exactly like Ease, and is read the same way - see
    // Keyframe.Ease's own note. Between two keys the LATER one's direction wins, because a
    // direction describes the segment arriving at its key, so one track can start forward and
    // finish from the centre without being split in two.

    /// <summary>
    /// How much of a text object is written at one frame, and from which end.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class FillmentKey : Keyframe, IModel<FillmentKey>
    {
        /// <summary> Fraction of the text written at this frame. </summary>
        [RuleInRange(TextRules.MinFillment, TextRules.MaxFillment)]
        [JsonProperty(Names.Float)]
        public float Value { get; set; }

        /// <summary> Which end the text is written from on the way into this key. </summary>
        [RuleEnumValid(TextRules.FillDirection_Default)]
        [JsonProperty(Names.FillDirection)]
        public TextFillDirection Direction { get; set; }

        public FillmentKey()
        {
            Value = TextRules.Fillment_Fallback;
            Direction = TextRules.FillDirection_Default;
        }
        public FillmentKey(float value, int frame, TextFillDirection direction = TextRules.FillDirection_Default,
            EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
            Direction = direction;
        }
    }
}
