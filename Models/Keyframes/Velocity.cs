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
    // The directional half (VelocityPoint is the radial one): a force the LEVEL applies, which the player's
    // own input cannot express. Zero is neutral and the default, so a key with no value changes nothing.

    /// <summary>
    /// A force pushing the player in a direction at a given frame. Not wired into gameplay yet - the
    /// format carries it, the player does not read it.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Velocity : Keyframe, IModel<Velocity>
    {
        /// <summary> How hard and which way the player is pushed. Zero leaves them alone. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Force)]
        public IVector2 Force { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Velocity()
        {
            Force = new Vector2Value();
        }
        /// <summary> Built from its force, frame and default ease. </summary>
        public Velocity(IVector2 force, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Force = force;
        }
    }
}
