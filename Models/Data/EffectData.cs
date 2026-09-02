using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Data
{
    /// <summary>
    /// A whole particle system definition, stored once in Level.Resources.Effects and referenced by
    /// any number of EffectObject placements. The object says where/when it plays, this says what it
    /// looks like - so reusing one effect across a level costs one placement, not one copy.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.EffectData, 1, 0)]
    [GenerateModel]
    public sealed partial class EffectData : IModel<EffectData>, IUpdatable<EffectData>
    {
        /// <summary> Identity of this effect resource and the key EffectObject.EffectId points at. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.EffectId)]
        public EffectId EffectId { get; set; }

        /// <summary> Editor-facing label of the effect. Cosmetic - EffectId is the identity. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public string Name { get; set; }

        /// <summary> Whether StopLocalFrame is honoured at all. Off means the emitter keeps spawning
        /// for as long as its object lives. </summary>
        [JsonProperty(Names.HasStopLocalFrame)]
        public bool HasStopLocalFrame { get; set; }

        // Bounded on its own scale, not as a level frame. It counts from the emitter's own start, so
        // validating it against the level timeline tied an effect's internal duration to wherever it
        // happened to be placed - and made an EffectData validated on its own fail outright.

        /// <summary> Frame, counted from the object's own span start (local, not level time), where
        /// emission stops - already-spawned particles still finish their lifetime. </summary>
        [RuleInRange(EffectRules.StopLocalFrame_Min, EffectRules.StopLocalFrame_Max)]
        [JsonProperty(Names.StopLocalFrame)]
        public int StopLocalFrame { get; set; }

        /// <summary> Emission basics: particle count, lifetime, looping, texture, pivot - everything
        /// that decides how many particles exist and how long. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Core)]
        public EffectObjectCore Core { get; set; }

        /// <summary> What moves a particle after it spawns: gravity, velocity, orbital and linear
        /// force. Complements Shape, which only decides where it starts. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Forces)]
        public EffectObjectForces Forces { get; set; }

        /// <summary> Spawn volume (Point/Circle/Rectangle/Line/Cone/Torus); rim shapes additionally
        /// nest an IEffectShapeSpread deciding where along the rim the next particle lands. </summary>
        [RuleNotNull(typeof(EffectShapePoint))]
        [JsonProperty(Names.Shape)]
        public IEffectShape Shape { get; set; }

        /// <summary> Particle rotation over its life - one of the five polymorphic variants
        /// (Value / CurvesOverLife / CurvesBySpeed / RandomUniform / RandomPerComponent). </summary>
        [RuleNotNull(typeof(EffectAngleValue))]
        [JsonProperty(Names.Angle)]
        public IEffectAngle Angle { get; set; }

        /// <summary> Particle size over its life, same five-variant family as Angle. </summary>
        [RuleNotNull(typeof(EffectScaleValue))]
        [JsonProperty(Names.Scale)]
        public IEffectScale Scale { get; set; }

        /// <summary> Particle tint over its life - gradient-based variants instead of curve-based,
        /// but otherwise the same five-way split as Angle/Scale. </summary>
        [RuleNotNull(typeof(EffectColorValue))]
        [JsonProperty(Names.Color)]
        public IEffectColor Color { get; set; }

        public EffectData()
        {
            EffectId = EffectId.Null;
            Name = string.Empty;
            HasStopLocalFrame = EffectRules.HasStopLocalFrame_Default;
            StopLocalFrame = EffectRules.StopLocalFrame_Default;
            Core = new EffectObjectCore();
            Forces = new EffectObjectForces();
            Shape = new EffectShapePoint();
            Angle = new EffectAngleValue();
            Scale = new EffectScaleValue();
            Color = new EffectColorValue();
        }
        public EffectData(EffectId effectId, string name, bool hasStopLocalFrame, int stopLocalFrame, EffectObjectCore core,
            EffectObjectForces forces, IEffectShape shape, IEffectAngle angle, IEffectScale scale, IEffectColor color)
        {
            EffectId = effectId;
            Name = name;
            HasStopLocalFrame = hasStopLocalFrame;
            StopLocalFrame = stopLocalFrame;
            Core = core;
            Forces = forces;
            Shape = shape;
            Angle = angle;
            Scale = scale;
            Color = color;
        }
    }
}