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
    // TODO activate for player when add events
    
    /// <summary>
    /// A point that pushes or pulls the player at a given frame. Not wired into gameplay yet (see
    /// the TODO above) - the format reserves the shape, the player does not read it.
    /// </summary>
    [RuleContainer]
    public class VelocityPoint : Keyframe, IModel<VelocityPoint>
    {
        /// <summary> Where the force radiates from. </summary>
        [RuleNotNull(typeof(Vector2Value))]
        [JsonProperty(Names.Center)]
        public IVector2 Center { get; set; }

        /// <summary> Strength of the push; negative pulls toward Center instead. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Force)]
        public float Force { get; set; }

        public VelocityPoint()
        {
            Center = new Vector2Value();
            Force = 1f;
        }
        public VelocityPoint(IVector2 center, float force, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Center = center;
            Force = force;
        }
        public override void Reset()
        {
            base.Reset();
            Center = new Vector2Value();
            Force = 1f;
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        VelocityPoint ICopyable<VelocityPoint>.Copy() => CopyImpl();
        
        private VelocityPoint CopyImpl() => new(Center.Copy(), Force, Frame, Ease);

        public void Update(VelocityPoint src)
        {
            base.Update(src);

            Center = src.Center.Copy();
            Force = src.Force;
        }

        public void Pull(VelocityPoint src)
        {
            base.Pull(src);

            Center = Center.PullFrom(src.Center);
            Force = src.Force;
        }

        public override bool Equals(object obj) => obj is VelocityPoint value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Center, Force);

        public bool Equals(VelocityPoint other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Center.Equals(other.Center)
                         && Force.Equals(other.Force);
            return result;
        }
    }
}