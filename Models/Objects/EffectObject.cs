using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Objects
{
    /// <summary>
    /// Places a particle system in the scene. Deliberately thin - it only points at an EffectData
    /// resource; its inherited transform tracks decide where and when the emitter lives, the resource
    /// decides what it emits.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectObject : RectObject, IModel<EffectObject>, IUpdatable<EffectObject>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public override ObjectType GetModelType() => ObjectType.EffectObject;

        /// <summary> Which EffectData of Level.Resources.Effects to play. Several objects sharing one
        /// id share one definition, not one running instance. </summary>
        [ModificationField(ModificationFields.EffectId)]
        [JsonProperty(Names.EffectId)]
        public EffectId EffectId { get; set; }
        
        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EffectObject()
        {
            EffectId = EffectId.Null;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public EffectObject(ObjectId objectId, ObjectId parentObjectId, string name, bool active, FrameSpan span, int layer, List<PosKey> positions, List<AngleKey> rotations, List<ScaKey> scales, List<ScaKey> sizes,
            List<AlignmentKey> anchorsMin, List<AlignmentKey> anchorsMax, List<AlignmentKey> pivots, EffectId effectId)
            : base(objectId, parentObjectId, name, active, span, layer,
                positions, rotations, scales, sizes, anchorsMin, anchorsMax, pivots)
        {
            EffectId = effectId;
        }
    }
}