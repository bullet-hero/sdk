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

        // THE IN-POINT, AND IT MOVES EVERYTHING THE TEMPLATE CONTRIBUTES - every materialized copy's
        // span (PrefabMaterializer/PrefabVirtualizationUtils subtract it) and this placement's own
        // instance origin, which is where its root-copied tracks are read from (LevelStateBuilder).
        // Both halves have to move together or the placement's own animation slides against the
        // content it places.
        //
        // It exists so that a placement can be TRIMMED and CUT without the template restarting:
        // the two halves of a cut are one template played from two in-points, which is what makes a
        // cut need no per-child override at all. S - PlacementOffset is the number every frame this
        // placement contributes is measured from, and both gestures preserve it.
        //
        // The bound that is NOT here: PlacementOffset may not exceed Span.StartFrame - MinFrame.
        // See FrameRules.MinPlacementOffset - a property rule sees one number and never the span.

        /// <summary> How many frames into the template this placement starts playing. Zero plays it
        /// from its own first frame; negative leaves an empty head. </summary>
        [RuleInRange(FrameRules.MinPlacementOffset, FrameRules.MaxPlacementOffset)]
        [ModificationField(ModificationFields.PlacementOffset)]
        [JsonProperty(Names.Offset)]
        public int PlacementOffset { get; set; }

        // A LENGTH OF ITS OWN IS AN OVERRIDE, NOT A FIELD, and this property is how the override
        // machinery reaches one. PrefabRootUtils.ApplyRoot rewrites the duration from the template
        // root on every materialize, every resync and every load; ApplyModifications runs after it
        // in both consumers, so a Modification recorded against this field wins and survives while
        // an ordinary written duration would not. The template still owns the DEFAULT - a placement
        // that has never been trimmed still follows its template's length.
        //
        // SERIALIZED BY NEITHER CODEC, on purpose. The duration is already in Span, and a second
        // spelling of one number is what Prefab.FrameDuration was deleted for. [GenerateModelIgnore]
        // keeps it out of Copy/Equals/the blob/the generated JSON, and [JsonIgnore] keeps the
        // reflective writer off it - the two JSON writers have to agree byte for byte, so a member
        // one emits and the other skips is not a cosmetic difference.

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
