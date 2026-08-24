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
    // The directional half of the pair - VelocityPoint is the radial one. Both describe a force the
    // LEVEL applies to the player, which is a thing the player's own input cannot express: wind
    // across a section, a shove on a beat, a pull toward something.
    //
    // Zero is the neutral value and the default, so a track that exists but says nothing changes
    // nothing. That matters because the force is authored on a level-global track: a keyframe with
    // no value written is a keyframe that lets the player move normally, not one that stops them.

    /// <summary>
    /// A force pushing the player in a direction at a given frame. Not wired into gameplay yet - the
    /// format carries it, the player does not read it.
    /// </summary>
    [RuleContainer]
    public class Velocity : Keyframe, IModel<Velocity>
    {
        /// <summary> How hard and which way the player is pushed. Zero leaves them alone. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Force)]
        public IVector2 Force { get; set; }

        public Velocity()
        {
            Force = new Vector2Value();
        }
        public Velocity(IVector2 force, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Force = force;
        }
        public override void Reset()
        {
            base.Reset();
            Force = new Vector2Value();
        }

        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        Velocity ICopyable<Velocity>.Copy() => CopyImpl();

        private Velocity CopyImpl() => new(Force.Copy(), Frame, Ease);

        public void Update(Velocity src)
        {
            base.Update(src);

            Force = src.Force.Copy();
        }

        public void Pull(Velocity src)
        {
            base.Pull(src);

            Force = Force.PullFrom(src.Force);
        }

        public override bool Equals(object obj) => obj is Velocity value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Force);

        public bool Equals(Velocity other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Force.Equals(other.Force);
            return result;
        }
    }
}
