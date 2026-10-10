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
    /// A placement of a Prefab template. Its children are materialized as ordinary objects in the hosting
    /// scope when PrefabId is set; this object keeps the bookkeeping to re-sync them with the template.
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

        // The in-point: it moves every copy's span AND the placement's own instance origin, which must
        // move together. Bound (not an attribute): FrameRules.MinPlacementOffset. Models/Objects/CLAUDE.md

        /// <summary> How many frames into the template this placement starts playing. Zero plays it
        /// from its own first frame; negative leaves an empty head. </summary>
        [RuleInRange(FrameRules.MinPlacementOffset, FrameRules.MaxPlacementOffset)]
        [ModificationField(ModificationFields.PlacementOffset)]
        [JsonProperty(Names.Offset)]
        public int PlacementOffset { get; set; }

        // A length of its own is an OVERRIDE, not a field: ApplyRoot rewrites it from the template, and
        // ApplyModifications runs after. Serialized by NEITHER codec (it is already in Span); the two JSON
        // writers must agree byte for byte. Models/Objects/CLAUDE.md

        /// <summary> The placement's own timeline length - the half of <see cref="RectObject.Span"/>
        /// a per-instance override may diverge in. </summary>
        [JsonIgnore]
        [GenerateModelIgnore]
        [ModificationField(ModificationFields.PlacementDuration)]
        public int PlacementDuration
        {
            get => Span.FrameDuration;
            set => Span = Span.WithDuration(value);
        }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public PrefabObject()
        {
            PrefabId = PrefabId.Null;
            ObjectIds = new Dictionary<ObjectId, ObjectId>();
            Modifications = new Dictionary<ModificationKey, Modification>();
            PlacementOffset = 0;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public PrefabObject(ObjectId objectId, ObjectId parentObjectId, string name, bool active, FrameSpan span, int layer,
            List<PosKey> positions, List<AngleKey> rotations, List<ScaKey> scales, List<ScaKey> sizes,
            List<AlignmentKey> anchorsMin, List<AlignmentKey> anchorsMax, List<AlignmentKey> pivots,
            PrefabId prefabId, Dictionary<ObjectId, ObjectId> objectIds, Dictionary<ModificationKey, Modification> modifications,
            int placementOffset)
            : base(objectId, parentObjectId, name, active, span, layer,
                positions, rotations, scales, sizes, anchorsMin, anchorsMax, pivots)
        {
            PrefabId = prefabId;
            ObjectIds = objectIds;
            Modifications = modifications;
            PlacementOffset = placementOffset;
        }
    }
}
