using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// The "how many, how long, what do they look like" half of an EffectData - everything that
    /// exists before a single force is applied. EffectObjectForces is the other half.
    /// </summary>
    [RuleContainer]
    public class EffectObjectCore : IModel<EffectObjectCore>, IUpdatable<EffectObjectCore>
    {
        /// <summary> Whether particles are drawn at all. Off keeps the system simulating - useful
        /// when only its side effects matter. </summary>
        [JsonProperty(Names.Render)]
        public bool Render { get; set; }

        /// <summary> Whether emission restarts once the batch is spent, instead of running once.
        /// NOTE: serialized under the "local" key - a legacy name, not a second meaning. </summary>
        [JsonProperty(Names.Local)]
        public bool Loop { get; set; }

        // For user-space it's always Local

        /// <summary> How many particles the system may have alive at once - the main cost knob, and
        /// what a level's capacity hint ultimately counts. </summary>
        [RuleInRange(EffectRules.Core.ParticleCount_Min, EffectRules.Core.ParticleCount_Max)]
        [JsonProperty(Names.ParticleCount)]
        public uint ParticleCount { get; set; }

        /// <summary> Min/max seconds a particle lives, drawn per particle - the spread is what keeps
        /// a burst from dying all at once. </summary>
        [RuleNotNull, RuleIVector2Ordered]
        [RuleIVector2InRange(EffectRules.Core.LifetimeBounds_Min, EffectRules.Core.LifetimeBounds_Max)]
        [JsonProperty(Names.Lifetime)]
        public IVector2 LifetimeBounds { get; set; }
        
        /// <summary> Geometry each particle is drawn with, out of the same shape pool ShapeObject
        /// draws from. Null draws NOTHING, exactly like ShapeObject.ShapeId; the quad is an
        /// ordinary value (ShapeId.Square.Fill) and is this field's default. </summary>
        [JsonProperty(Names.ShapeId)]
        public ShapeId ParticleShapeId { get; set; }

        /// <summary> Image each particle draws - the same resource pool ShapeObject draws from. </summary>
        [RuleReferenceExists(ResourceReferenceKind.Texture, allowNull: true)]
        [JsonProperty(Names.TextureResourceId)]
        public TextureResourceId TextureResourceId { get; set; }

        /// <summary> Point of the particle quad that sits on its position, and that it rotates
        /// around - the per-particle counterpart of RectObject.Pivots. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ParticlePivot)]
        public Alignment ParticlePivot { get; set; }

        public EffectObjectCore()
        {
            Render = EffectRules.Core.Render_Default;
            Loop = EffectRules.Core.Loop_Default;
            ParticleCount = EffectRules.Core.ParticleCount_Default;
            LifetimeBounds = new Vector2Value(
                EffectRules.Core.LifetimeBounds_X_Default,
                EffectRules.Core.LifetimeBounds_Y_Default);
            TextureResourceId = EffectRules.Core.TextureResourceId_Default;
            ParticleShapeId = EffectRules.Core.ParticleShapeId_Default;
            ParticlePivot = new Alignment(new Vector2Value(
                EffectRules.Core.Pivot_X_Default,
                EffectRules.Core.Pivot_Y_Default));
        }
        public EffectObjectCore(bool render, bool loop, uint particleCount,
            IVector2 lifetimeBounds, TextureResourceId textureResourceId, ShapeId particleShapeId,
            Alignment particlePivot)
        {
            Render = render;
            Loop = loop;
            ParticleCount = particleCount;
            LifetimeBounds = lifetimeBounds;
            TextureResourceId = textureResourceId;
            ParticleShapeId = particleShapeId;
            ParticlePivot = particlePivot;
        }
        public void Reset()
        {
            Render = EffectRules.Core.Render_Default;
            Loop = EffectRules.Core.Loop_Default;
            ParticleCount = EffectRules.Core.ParticleCount_Default;
            LifetimeBounds = new Vector2Value(
                EffectRules.Core.LifetimeBounds_X_Default,
                EffectRules.Core.LifetimeBounds_Y_Default);
            TextureResourceId = EffectRules.Core.TextureResourceId_Default;
            ParticleShapeId = EffectRules.Core.ParticleShapeId_Default;
            ParticlePivot = new Alignment(new Vector2Value(
                EffectRules.Core.Pivot_X_Default,
                EffectRules.Core.Pivot_Y_Default));
        }

        public void Update(EffectObjectCore src)
        {
            Render = src.Render;
            Loop = src.Loop;
            ParticleCount = src.ParticleCount;
            LifetimeBounds = src.LifetimeBounds.Copy();
            TextureResourceId = src.TextureResourceId;
            ParticleShapeId = src.ParticleShapeId;
            ParticlePivot = src.ParticlePivot.Copy();
        }

        public object Clone() => Copy();
        public EffectObjectCore Copy() => new(Render, Loop, ParticleCount,
            LifetimeBounds.Copy(), TextureResourceId, ParticleShapeId, ParticlePivot.Copy());

        public void Pull(EffectObjectCore src)
        {
            Render = src.Render;
            Loop = src.Loop;
            ParticleCount = src.ParticleCount;
            LifetimeBounds = LifetimeBounds.PullFrom(src.LifetimeBounds);
            TextureResourceId = src.TextureResourceId;
            ParticleShapeId = src.ParticleShapeId;
            ParticlePivot.Pull(src.ParticlePivot);
        }

        public override bool Equals(object obj) => obj is EffectObjectCore value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Render, Loop, ParticleCount,
            LifetimeBounds, TextureResourceId, ParticleShapeId, ParticlePivot);

        public bool Equals(EffectObjectCore other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Render == other.Render
                         && Loop == other.Loop
                         && ParticleCount.Equals(other.ParticleCount)
                         && LifetimeBounds.Equals(other.LifetimeBounds)
                         && TextureResourceId.Equals(other.TextureResourceId)
                         && ParticleShapeId.Equals(other.ParticleShapeId)
                         && ParticlePivot.Equals(other.ParticlePivot);
            return result;
        }
    }
}