using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Effects
{
    // The format carries NO world/local switch: level particles are always local to their object.
    // The host's graph must honour it (EffectGraphSpaceTests). Why: Docs/RESOURCE_DATA.md

    /// <summary>
    /// The "how many, how long, what do they look like" half of an EffectData - everything that
    /// exists before a single force is applied. EffectObjectForces is the other half.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectObjectCore : IModel<EffectObjectCore>, IUpdatable<EffectObjectCore>
    {
        /// <summary> Whether particles are drawn at all. Off keeps the system simulating - useful
        /// when only its side effects matter. </summary>
        [JsonProperty(Names.RenderShort)]
        public bool Render { get; set; }

        /// <summary> Whether the system emits continuously, instead of one batch that is never
        /// refilled. Off, it has nothing left to draw past its own LifetimeBounds. </summary>
        [JsonProperty(Names.LoopShort)]
        public bool Loop { get; set; }


        // A RATE, not a population: alive count settles at count x average lifetime, and nothing here
        // bounds that product. Why: Docs/RESOURCE_DATA.md

        /// <summary> Particles emitted per second - the main cost knob. What is alive at once is
        /// this times the average lifetime, in both modes: looping spawns at this rate, and a
        /// one-shot bursts that whole product at once. </summary>
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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
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

        /// <summary> Every member at once, in declaration order. </summary>
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
    }
}