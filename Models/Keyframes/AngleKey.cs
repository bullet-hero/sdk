using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Rotation key. Bounded but deliberately not wrapped: a full turn is meaningful, since
    /// interpolating 0 -> 4pi spins twice while wrapping it to 0 would not move at all. The bound
    /// is therefore stated in whole turns (ValueRules.MaxRotationTurns), not in one revolution.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AngleKey : Keyframe, IModel<AngleKey>
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
    }
}