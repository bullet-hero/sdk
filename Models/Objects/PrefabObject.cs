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
    /// A placement of a Prefab template. Its children are not resolved at load time - they were
    /// materialized as ordinary objects in the hosting scope the moment PrefabId was set, and this
    /// object only keeps the bookkeeping needed to re-sync them when the template changes.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class PrefabObject : RectObject, IModel<PrefabObject>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public override ObjectType GetModelType() => ObjectType.PrefabObject;
        
        /// <summary> Which template this placement instantiates. Null means the placement is empty -
        /// created but not yet pointed at a template, so it materializes nothing. </summary>
        [JsonProperty(Names.PrefabId)]
        public PrefabId PrefabId { get; set; } // reference into Level.Resources.Prefabs

        /// <summary> Remap table from template-inner ids to the real ids this placement's copies got
        /// in the hosting scope - how a resync finds the objects it already owns instead of
        /// duplicating them. </summary>
        [RuleNotNull, RuleCollectionMaxCount(PrefabRules.MaxObjectIdRemaps)]
        [JsonProperty(Names.ObjectIds)]
        public Dictionary<ObjectId, ObjectId> ObjectIds { get; set; } // inner id -> this instance's outer id
        
        /// <summary> Per-placement field overrides, keyed by (template object, field, element). </summary>
        [GenerateModelKeyed(nameof(Modification.Key))]
        [RuleNotNull, RuleCollectionMaxCount(PrefabRules.MaxModifications)]
        [JsonProperty(Names.Mod)]
        public Dictionary<ModificationKey, Modification> Modifications { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public PrefabObject()
        {
            PrefabId = PrefabId.Null;
            ObjectIds = new Dictionary<ObjectId, ObjectId>();
            Modifications = new Dictionary<ModificationKey, Modification>();
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public PrefabObject(ObjectId objectId, ObjectId parentObjectId, string name, bool active, FrameSpan span, int layer,
            List<PosKey> positions, List<AngleKey> rotations, List<ScaKey> scales, List<ScaKey> sizes,
            List<AlignmentKey> anchorsMin, List<AlignmentKey> anchorsMax, List<AlignmentKey> pivots,
            PrefabId prefabId, Dictionary<ObjectId, ObjectId> objectIds, Dictionary<ModificationKey, Modification> modifications)
            : base(objectId, parentObjectId, name, active, span, layer,
                positions, rotations, scales, sizes, anchorsMin, anchorsMax, pivots)
        {
            PrefabId = prefabId;
            ObjectIds = objectIds;
            Modifications = modifications;
        }
    }
}
