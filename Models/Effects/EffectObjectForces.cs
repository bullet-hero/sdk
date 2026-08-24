using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Everything that moves a particle after it spawns. Split in two kinds: the Start* fields are
    /// drawn once at birth (min/max pairs), the rest act continuously for the particle's whole life.
    /// </summary>
    [RuleContainer]
    public class EffectObjectForces : IModel<EffectObjectForces>, IUpdatable<EffectObjectForces>
    {
        /// <summary> Lower bound of the per-particle gravity draw; negative values float upward. </summary>
        [RuleNotNull]
        [JsonProperty(Names.GravityMin)]
        public IFloat StartGravityMin { get; set; }

        /// <summary> Upper bound of the per-particle gravity draw. </summary>
        [RuleNotNull]
        [JsonProperty(Names.GravityMax)]
        public IFloat StartGravityMax { get; set; }

        /// <summary> Lower bound of the initial velocity draw, per axis. </summary>
        [RuleNotNull]
        [JsonProperty(Names.VelocityMin)]
        public IVector2 StartVelocityMin { get; set; }

        /// <summary> Upper bound of the initial velocity draw. Spreading these two is what turns a
        /// clean burst into a spray. </summary>
        [RuleNotNull]
        [JsonProperty(Names.VelocityMax)]
        public IVector2 StartVelocityMax { get; set; }

        /// <summary> Lower bound of the initial spin draw, in degrees per second. </summary>
        [RuleNotNull]
        [JsonProperty(Names.AngularVelocityMin)]
        public IFloat StartAngularVelocityMin { get; set; }

        /// <summary> Upper bound of the initial spin draw. </summary>
        [RuleNotNull]
        [JsonProperty(Names.AngularVelocityMax)]
        public IFloat StartAngularVelocityMax { get; set; }

        /// <summary> Constant drift added every frame, on top of whatever velocity the particle was
        /// born with - wind, not an impulse. </summary>
        [RuleNotNull]
        [JsonProperty(Names.LinearVelocity)]
        public IVector2 LinearVelocity { get; set; }

        /// <summary> Rotation of particles around a center rather than around themselves; the Z
        /// component is the one that matters in a 2D scene. </summary>
        [RuleNotNull]
        [JsonProperty(Names.OrbitalVelocity)]
        public IVector3 OrbitalVelocity { get; set; }

        /// <summary> Where that orbit center sits relative to the emitter - offsetting it makes the
        /// swirl lopsided. </summary>
        [RuleNotNull]
        [JsonProperty(Names.OrbitalCenterOffset)]
        public IVector3 OrbitalCenterOffset { get; set; }

        /// <summary> Multiplier over the particle's whole velocity - one dial to slow down or speed
        /// up a finished effect without re-tuning every field above. </summary>
        [RuleNotNull]
        [JsonProperty(Names.VelocitySpeed)]
        public IFloat VelocitySpeed { get; set; }

        /// <summary> Constant acceleration (force, not velocity), so its effect compounds over the
        /// particle's life instead of staying flat. </summary>
        [RuleNotNull]
        [JsonProperty(Names.LinearForce)]
        public IVector2 LinearForce { get; set; }

        public EffectObjectForces()
        {
            StartGravityMin = new FloatValue(EffectRules.Forces.StartGravityMin_Default);
            StartGravityMax = new FloatValue(EffectRules.Forces.StartGravityMax_Default);
            StartVelocityMin = new Vector2Value(
                EffectRules.Forces.StartVelocityMin_X_Default,
                EffectRules.Forces.StartVelocityMin_Y_Default);
            StartVelocityMax = new Vector2Value(
                EffectRules.Forces.StartVelocityMax_X_Default,
                EffectRules.Forces.StartVelocityMax_Y_Default);
            StartAngularVelocityMin = new FloatValue(EffectRules.Forces.StartAngularVelocityMin_Default);
            StartAngularVelocityMax = new FloatValue(EffectRules.Forces.StartAngularVelocityMax_Default);
            LinearVelocity = new Vector2Value(EffectRules.Forces.LinearVelocity_X_Default,
                EffectRules.Forces.LinearVelocity_Y_Default);
            OrbitalVelocity = new Vector3Value(
                EffectRules.Forces.OrbitalVelocity_X_Default,
                EffectRules.Forces.OrbitalVelocity_Y_Default,
                EffectRules.Forces.OrbitalVelocity_Z_Default);
            OrbitalCenterOffset = new Vector3Value(
                EffectRules.Forces.OrbitalCenterOffset_X_Default,
                EffectRules.Forces.OrbitalCenterOffset_Y_Default,
                EffectRules.Forces.OrbitalCenterOffset_Z_Default);
            VelocitySpeed = new FloatValue(EffectRules.Forces.VelocitySpeed_Default);
            LinearForce = new Vector2Value(
                EffectRules.Forces.LinearForce_X_Default,
                EffectRules.Forces.LinearForce_Y_Default);
        }
        public EffectObjectForces(IFloat startGravityMin, IFloat startGravityMax, 
            IVector2 startVelocityMin, IVector2 startVelocityMax, 
            IFloat startAngularVelocityMin, IFloat startAngularVelocityMax, 
            IVector2 linearVelocity, IVector3 orbitalVelocity, IVector3 orbitalCenterOffset, 
            IFloat velocitySpeed, IVector2 linearForce)
        {
            StartGravityMin = startGravityMin;
            StartGravityMax = startGravityMax;
            StartVelocityMin = startVelocityMin;
            StartVelocityMax = startVelocityMax;
            StartAngularVelocityMin = startAngularVelocityMin;
            StartAngularVelocityMax = startAngularVelocityMax;
            LinearVelocity = linearVelocity;
            OrbitalVelocity = orbitalVelocity;
            OrbitalCenterOffset = orbitalCenterOffset;
            VelocitySpeed = velocitySpeed;
            LinearForce = linearForce;
        }
        public void Reset()
        {
            StartGravityMin = new FloatValue(EffectRules.Forces.StartGravityMin_Default);
            StartGravityMax = new FloatValue(EffectRules.Forces.StartGravityMax_Default);
            StartVelocityMin = new Vector2Value(
                EffectRules.Forces.StartVelocityMin_X_Default,
                EffectRules.Forces.StartVelocityMin_Y_Default);
            StartVelocityMax = new Vector2Value(
                EffectRules.Forces.StartVelocityMax_X_Default,
                EffectRules.Forces.StartVelocityMax_Y_Default);
            StartAngularVelocityMin = new FloatValue(EffectRules.Forces.StartAngularVelocityMin_Default);
            StartAngularVelocityMax = new FloatValue(EffectRules.Forces.StartAngularVelocityMax_Default);
            LinearVelocity = new Vector2Value(EffectRules.Forces.LinearVelocity_X_Default,
                EffectRules.Forces.LinearVelocity_Y_Default);
            OrbitalVelocity = new Vector3Value(
                EffectRules.Forces.OrbitalVelocity_X_Default,
                EffectRules.Forces.OrbitalVelocity_Y_Default,
                EffectRules.Forces.OrbitalVelocity_Z_Default);
            OrbitalCenterOffset = new Vector3Value(
                EffectRules.Forces.OrbitalCenterOffset_X_Default,
                EffectRules.Forces.OrbitalCenterOffset_Y_Default,
                EffectRules.Forces.OrbitalCenterOffset_Z_Default);
            VelocitySpeed = new FloatValue(EffectRules.Forces.VelocitySpeed_Default);
            LinearForce = new Vector2Value(
                EffectRules.Forces.LinearForce_X_Default,
                EffectRules.Forces.LinearForce_Y_Default);
        }

        public object Clone() => Copy();
        public EffectObjectForces Copy() => new(StartGravityMin.Copy(), StartGravityMax.Copy(), StartVelocityMin.Copy(),
            StartVelocityMax.Copy(), StartAngularVelocityMin.Copy(), StartAngularVelocityMax.Copy(), LinearVelocity.Copy(),
            OrbitalVelocity.Copy(), OrbitalCenterOffset.Copy(), VelocitySpeed.Copy(), LinearForce.Copy());

        public void Update(EffectObjectForces src)
        {
            StartGravityMin = src.StartGravityMin.Copy();
            StartGravityMax = src.StartGravityMax.Copy();
            StartVelocityMin = src.StartVelocityMin.Copy();
            StartVelocityMax = src.StartVelocityMax.Copy();
            StartAngularVelocityMin = src.StartAngularVelocityMin.Copy();
            StartAngularVelocityMax = src.StartAngularVelocityMax.Copy();
            LinearVelocity = src.LinearVelocity.Copy();
            OrbitalVelocity = src.OrbitalVelocity.Copy();
            OrbitalCenterOffset = src.OrbitalCenterOffset.Copy();
            VelocitySpeed = src.VelocitySpeed.Copy();
            LinearForce = src.LinearForce.Copy();
        }

        public void Pull(EffectObjectForces src)
        {
            StartGravityMin = StartGravityMin.PullFrom(src.StartGravityMin);
            StartGravityMax = StartGravityMax.PullFrom(src.StartGravityMax);
            StartVelocityMin = StartVelocityMin.PullFrom(src.StartVelocityMin);
            StartVelocityMax = StartVelocityMax.PullFrom(src.StartVelocityMax);
            StartAngularVelocityMin = StartAngularVelocityMin.PullFrom(src.StartAngularVelocityMin);
            StartAngularVelocityMax = StartAngularVelocityMax.PullFrom(src.StartAngularVelocityMax);
            LinearVelocity = LinearVelocity.PullFrom(src.LinearVelocity);
            OrbitalVelocity = OrbitalVelocity.PullFrom(src.OrbitalVelocity);
            OrbitalCenterOffset = OrbitalCenterOffset.PullFrom(src.OrbitalCenterOffset);
            VelocitySpeed = VelocitySpeed.PullFrom(src.VelocitySpeed);
            LinearForce = LinearForce.PullFrom(src.LinearForce);
        }

        public override bool Equals(object obj) => obj is EffectObjectForces value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(StartGravityMin);
            hashCode.Add(StartGravityMax);
            hashCode.Add(StartVelocityMin);
            hashCode.Add(StartVelocityMax);
            hashCode.Add(StartAngularVelocityMin);
            hashCode.Add(StartAngularVelocityMax);
            hashCode.Add(LinearVelocity);
            hashCode.Add(OrbitalVelocity);
            hashCode.Add(OrbitalCenterOffset);
            hashCode.Add(VelocitySpeed);
            hashCode.Add(LinearForce);
            return hashCode.ToHashCode();
        }

        public bool Equals(EffectObjectForces other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = StartGravityMin.Equals(other.StartGravityMin)
                         && StartGravityMax.Equals(other.StartGravityMax)
                         && StartVelocityMin.Equals(other.StartVelocityMin)
                         && StartVelocityMax.Equals(other.StartVelocityMax)
                         && StartAngularVelocityMin.Equals(other.StartAngularVelocityMin)
                         && StartAngularVelocityMax.Equals(other.StartAngularVelocityMax)
                         && LinearVelocity.Equals(other.LinearVelocity)
                         && OrbitalVelocity.Equals(other.OrbitalVelocity)
                         && OrbitalCenterOffset.Equals(other.OrbitalCenterOffset)
                         && VelocitySpeed.Equals(other.VelocitySpeed)
                         && LinearForce.Equals(other.LinearForce);
            return result;
        }
    }
}