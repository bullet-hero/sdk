using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Everything that moves a particle after it spawns. Split in two kinds: the Start* fields are
    /// drawn once at birth (min/max pairs), the rest act continuously for the particle's whole life.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectObjectForces : IModel<EffectObjectForces>, IUpdatable<EffectObjectForces>
    {
        /// <summary> Lower bound of the per-particle gravity draw; negative values float upward. </summary>
        [RuleOptional]
        [JsonProperty(Names.MinStartGravity)]
        public IFloat StartGravityMin { get; set; }

        /// <summary> Upper bound of the per-particle gravity draw. </summary>
        [RuleOptional]
        [JsonProperty(Names.MaxStartGravity)]
        public IFloat StartGravityMax { get; set; }

        /// <summary> Lower bound of the initial velocity draw, per axis. </summary>
        [RuleOptional]
        [JsonProperty(Names.MinStartVelocity)]
        public IVector2 StartVelocityMin { get; set; }

        /// <summary> Upper bound of the initial velocity draw. Spreading these two is what turns a
        /// clean burst into a spray. </summary>
        [RuleOptional]
        [JsonProperty(Names.MaxStartVelocity)]
        public IVector2 StartVelocityMax { get; set; }

        /// <summary> Lower bound of the initial spin draw, in degrees per second. </summary>
        [RuleOptional]
        [JsonProperty(Names.MinStartAngularVelocity)]
        public IFloat StartAngularVelocityMin { get; set; }

        /// <summary> Upper bound of the initial spin draw. </summary>
        [RuleOptional]
        [JsonProperty(Names.MaxStartAngularVelocity)]
        public IFloat StartAngularVelocityMax { get; set; }

        /// <summary> Constant drift added every frame, on top of whatever velocity the particle was
        /// born with - wind, not an impulse. </summary>
        [RuleOptional]
        [JsonProperty(Names.LinearVelocity)]
        public IVector2 LinearVelocity { get; set; }

        /// <summary> Rotation of particles around a center rather than around themselves; the Z
        /// component is the one that matters in a 2D scene. </summary>
        [RuleOptional]
        [JsonProperty(Names.OrbitalVelocity)]
        public IVector3 OrbitalVelocity { get; set; }

        /// <summary> Where that orbit center sits relative to the emitter - offsetting it makes the
        /// swirl lopsided. </summary>
        [RuleOptional]
        [JsonProperty(Names.OrbitalCenterOffset)]
        public IVector3 OrbitalCenterOffset { get; set; }

        /// <summary> Multiplier over the particle's whole velocity - one dial to slow down or speed
        /// up a finished effect without re-tuning every field above. </summary>
        [RuleOptional]
        [JsonProperty(Names.VelocitySpeed)]
        public IFloat VelocitySpeed { get; set; }

        /// <summary> Constant acceleration (force, not velocity), so its effect compounds over the
        /// particle's life instead of staying flat. </summary>
        [RuleOptional]
        [JsonProperty(Names.LinearForce)]
        public IVector2 LinearForce { get; set; }

        // THE ELEVEN FORCES ARE BORN EMPTY. Each was a polymorphic value constructed at its own
        // neutral - 318 bytes, 45% of a default EffectData - written for every effect whether or
        // not it applies a force, and most do not. Null means "not set" and reads back as the
        // neutral, which is what EffectForcesState.Default already spells out in one place.
        //
        // Unlike LevelTrackEffects this is COMPRESSION rather than a third state: 0 gravity is both
        // the neutral and a legal authored value, so absent and zero behave identically. What it
        // buys is that an effect only carries the forces it actually uses.

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EffectObjectForces()
        {
        }

        /// <summary> A fully populated instance at every neutral - what the parameterless
        /// constructor used to build. Returns a FRESH one each call, never a shared instance:
        /// whoever needs a value to show or to edit gets one it may keep. </summary>
        public static EffectObjectForces CreateDefaults() => new()
        {
            StartGravityMin = new FloatValue(EffectRules.Forces.StartGravityMin_Default),
            StartGravityMax = new FloatValue(EffectRules.Forces.StartGravityMax_Default),
            StartVelocityMin = new Vector2Value(
                EffectRules.Forces.StartVelocityMin_X_Default,
                EffectRules.Forces.StartVelocityMin_Y_Default),
            StartVelocityMax = new Vector2Value(
                EffectRules.Forces.StartVelocityMax_X_Default,
                EffectRules.Forces.StartVelocityMax_Y_Default),
            StartAngularVelocityMin = new FloatValue(EffectRules.Forces.StartAngularVelocityMin_Default),
            StartAngularVelocityMax = new FloatValue(EffectRules.Forces.StartAngularVelocityMax_Default),
            LinearVelocity = new Vector2Value(
                EffectRules.Forces.LinearVelocity_X_Default,
                EffectRules.Forces.LinearVelocity_Y_Default),
            OrbitalVelocity = new Vector3Value(
                EffectRules.Forces.OrbitalVelocity_X_Default,
                EffectRules.Forces.OrbitalVelocity_Y_Default,
                EffectRules.Forces.OrbitalVelocity_Z_Default),
            OrbitalCenterOffset = new Vector3Value(
                EffectRules.Forces.OrbitalCenterOffset_X_Default,
                EffectRules.Forces.OrbitalCenterOffset_Y_Default,
                EffectRules.Forces.OrbitalCenterOffset_Z_Default),
            VelocitySpeed = new FloatValue(EffectRules.Forces.VelocitySpeed_Default),
            LinearForce = new Vector2Value(
                EffectRules.Forces.LinearForce_X_Default,
                EffectRules.Forces.LinearForce_Y_Default),
        };

        /// <summary> Every member at once, in declaration order. </summary>
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
    }
}