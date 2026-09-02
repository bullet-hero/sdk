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
    public sealed partial class PrefabObject : RectObject, IModel<PrefabObject>, IUpdatable<PrefabObject>
    {
        public override ObjectType GetModelType() => ObjectType.PrefabObject;

        // Deliberately NOT [RuleIPrimitiveGuidNotNull], unlike most IPrimitiveGuid properties here:
        // Null is a real state, not an unset reference. The ctor defaults to it and GameEditor's
        // OpLevelCreatePrefabObject creates every placement empty on purpose (the author picks the
        // target Prefab afterward, which runs OpLevelObjectPrefabId's null -> X transition). The
        // rule's Fix would assign a random Guid, pointing the placement at a Prefab that doesn't
        // exist - strictly worse than an empty placement, since materialization then finds nothing
        // to copy and the dangling reference persists in the saved file.

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

        // Per-instance field overrides on this placement's own materialized children, keyed by
        // ModificationKey (TEMPLATE's inner ObjectId + field Path) - see
        // BH.Core.Services.PrefabMaterializer.ApplyModifications (re-applied after every
        // materialize/resync, on top of the fresh template copy) and GameEditor's
        // EditObjectOperation.RecordModification (what records one here whenever a direct edit lands
        // on a materialized child outside Prefab Mode). One Modification per (object, field) pair -
        // a child can have several fields overridden at once, but only one override per field.

        /// <summary> Per-placement field overrides, keyed by (template object, field path). </summary>
        [GenerateModelKeyed(nameof(Modification.Key))]
        [RuleNotNull, RuleCollectionMaxCount(PrefabRules.MaxModifications)]
        [JsonProperty(Names.Mod)]
        public Dictionary<ModificationKey, Modification> Modifications { get; set; }

        public PrefabObject()
        {
            PrefabId = PrefabId.Null;
            ObjectIds = new Dictionary<ObjectId, ObjectId>();
            Modifications = new Dictionary<ModificationKey, Modification>();
        }
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
