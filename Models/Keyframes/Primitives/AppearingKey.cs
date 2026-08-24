using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Text;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

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
    public class AppearingKey : Keyframe, IModel<AppearingKey>
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
        public override void Reset()
        {
            base.Reset();
            Value = TextRules.Appearing_Fallback;
            Mode = TextRules.AppearingMode_Default;
        }

        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        AppearingKey ICopyable<AppearingKey>.Copy() => CopyImpl();

        private AppearingKey CopyImpl() => new(Value, Frame, Mode, Ease);

        public void Update(AppearingKey src)
        {
            base.Update(src);

            Value = src.Value;
            Mode = src.Mode;
        }

        public void Pull(AppearingKey src)
        {
            base.Pull(src);

            Value = src.Value;
            Mode = src.Mode;
        }

        public override bool Equals(object obj) => obj is AppearingKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Value, (int)Mode);

        public bool Equals(AppearingKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Value.Equals(other.Value) && Mode == other.Mode;
            return result;
        }
    }
}
