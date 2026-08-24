using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Rotation key. Bounded but deliberately not wrapped: a full turn is meaningful, since
    /// interpolating 0 -> 4pi spins twice while wrapping it to 0 would not move at all. The bound
    /// is therefore stated in whole turns (ValueRules.MaxRotationTurns), not in one revolution.
    /// </summary>
    [RuleContainer]
    public class AngleKey : Keyframe, IModel<AngleKey>
    {
        /// <summary> Target rotation in RADIANS at this frame, around the object's pivot - degrees
        /// exist only at the consumer's inspector boundary. </summary>
        [RuleNotNull(typeof(FloatValue))]
        [RuleIFloatInRange(ValueRules.MinRotation, ValueRules.MaxRotation)]
        [JsonProperty(Names.Float)]
        public IFloat Angle { get; set; }

        public AngleKey()
        {
            Angle = new FloatValue();
        }
        public AngleKey(IFloat value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Angle = value;
        }
        public override void Reset()
        {
            base.Reset();
            Angle = new FloatValue();
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        AngleKey ICopyable<AngleKey>.Copy() => CopyImpl();
        
        private AngleKey CopyImpl() => new(Angle.Copy(), Frame, Ease);

        public void Update(AngleKey src)
        {
            base.Update(src);

            Angle = src.Angle.Copy();
        }

        public void Pull(AngleKey src)
        {
            base.Pull(src);

            Angle = Angle.PullFrom(src.Angle);
        }

        public override bool Equals(object obj) => obj is AngleKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Angle);

        public bool Equals(AngleKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Angle.Equals(other.Angle);
            return result;
        }
    }
}