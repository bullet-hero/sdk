using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Objects
{
    /// <summary>
    /// Base of every placeable object in a level: identity, parenting, lifetime and the seven shared
    /// transform tracks. Usable on its own as an invisible node - a pure transform other objects
    /// parent to. Subclasses only add what they draw.
    /// <br/><br/>
    /// Every track may legitimately be empty; that means "use the engine default for this field",
    /// not "data is missing".
    /// </summary>
    [RuleContainer]
    public class RectObject : IFrameBounds, INameable, IModel<RectObject>, IUpdatable<RectObject>
    {
        public virtual ObjectType GetModelType() => ObjectType.RectObject;

        /// <summary> Identity within its own scope (a level or one prefab template) - the key of the
        /// Objects dictionary holding it. </summary>
        [RuleObjectIdValid]
        [JsonProperty(Names.ObjectId)]
        public ObjectId ObjectId { get; set; }

        /// <summary> Whose transform this one is relative to. Null means level space; reserved
        /// negative ids attach to the camera, the local player, or a prefab's root. </summary>
        [RuleParentObjectIdValid]
        [JsonProperty(Names.ParentObjectId)]
        public ObjectId ParentObjectId { get; set; }

        /// <summary> Editor-facing label. Not unique and not an identity - ObjectId is. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public string Name { get; set; }

        // This IS the disable switch, unlike the Visible flag it replaced. That one gated rendering
        // only, so an invisible object still hit the player - a trap, since nothing in its name said
        // so. "Not drawn but still solid" is now expressed where it belongs: a Null ShapeId with a
        // real ColliderId. Inactive means the object contributes nothing to either path, while
        // staying alive as a parent for whatever hangs off it.

        /// <summary> Whether the object participates at all - drawn and collided against. Applies
        /// down the hierarchy: an inactive parent takes its whole subtree with it. </summary>
        [JsonProperty(Names.Active)]
        public bool Active { get; set; }

        /// <summary> Half-open lifetime [Start, End) on the owning scope's timeline. Outside it the
        /// object is not simulated at all, which is what keeps a long level cheap. </summary>
        [JsonProperty(Names.SpanShort)]
        public FrameSpan Span { get; set; }

        /// <summary> Draw order among siblings - higher draws in front. Static here; LayerKey
        /// animates it where a track is wired up. </summary>
        [RuleInRange(ValueRules.MinLayer, ValueRules.MaxLayer)]
        [JsonProperty(Names.Layer)]
        public int Layer { get; set; }

        // Rect content

        /// <summary> Position track, in parent space. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(PosKey.Frame))]
        [JsonProperty(Names.Position)]
        public List<PosKey> Positions { get; set; }

        /// <summary> Rotation track, in degrees around the pivot. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(AngleKey.Frame))]
        [JsonProperty(Names.Rotation)]
        public List<AngleKey> Rotations { get; set; }

        /// <summary> Scale track - a multiplier applied on top of Sizes, and the one that also
        /// scales children. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(ScaKey.Frame))]
        [JsonProperty(Names.Scale)]
        public List<ScaKey> Scales { get; set; }

        /// <summary> Size track - the rect's own extents before scaling, in the parent's units. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(ScaKey.Frame))]
        [JsonProperty(Names.Size)]
        public List<ScaKey> Sizes { get; set; }

        /// <summary> Lower anchor track: which point of the parent's rect this object's lower corner
        /// sticks to. Together with AnchorsMax this is what makes layouts survive aspect changes. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(AlignmentKey.Frame))]
        [JsonProperty(Names.AnchorMin)]
        public List<AlignmentKey> AnchorsMin { get; set; }

        /// <summary> Upper anchor track. Equal to AnchorsMin means "follow that point"; different
        /// means "stretch between the two". </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(AlignmentKey.Frame))]
        [JsonProperty(Names.AnchorMax)]
        public List<AlignmentKey> AnchorsMax { get; set; }

        /// <summary> Pivot track: the point rotation and scaling happen around, and the origin the
        /// position refers to. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(AlignmentKey.Frame))]
        [JsonProperty(Names.Pivot)]
        public List<AlignmentKey> Pivots { get; set; }

        public RectObject()
        {
            ObjectId = ObjectId.Null;
            ParentObjectId = ObjectId.Null;
            Name = string.Empty;
            Active = true;
            Span = new FrameSpan();
            Layer = ValueRules.DefaultLayer;
            
            Positions = new List<PosKey>();
            Rotations = new List<AngleKey>();
            Scales = new List<ScaKey>();
            Sizes = new List<ScaKey>();
            AnchorsMin = new List<AlignmentKey>();
            AnchorsMax = new List<AlignmentKey>();
            Pivots = new List<AlignmentKey>();
        }
        public RectObject(ObjectId objectId, ObjectId parentObjectId, string name, bool active, FrameSpan span, int layer,
            List<PosKey> positions, List<AngleKey> rotations, List<ScaKey> scales, List<ScaKey> sizes,
            List<AlignmentKey> anchorsMin, List<AlignmentKey> anchorsMax, List<AlignmentKey> pivots)
        {
            ObjectId = objectId;
            ParentObjectId = parentObjectId;
            Name = name;
            Active = active;
            Span = span;
            Layer = layer;
            
            Positions = positions;
            Rotations = rotations;
            Scales = scales;
            Sizes = sizes;
            AnchorsMin = anchorsMin;
            AnchorsMax = anchorsMax;
            Pivots = pivots;
        }
        public virtual void Reset()
        {
            ObjectId = ObjectId.Null;
            ParentObjectId = ObjectId.Null;
            Name = string.Empty;
            Active = true;
            Span = new FrameSpan();
            Layer = ValueRules.DefaultLayer;
            
            Positions.Clear();
            Rotations.Clear();
            Scales.Clear();
            Sizes.Clear();
            AnchorsMin.Clear();
            AnchorsMax.Clear();
            Pivots.Clear();
        }

        public virtual object Clone() => CopyImpl();
        public virtual RectObject Copy() => CopyImpl();
        
        private RectObject CopyImpl() => new(ObjectId, ParentObjectId, Name, Active, Span, Layer,
            Positions.CopyList(), Rotations.CopyList(), Scales.CopyList(), Sizes.CopyList(),
            AnchorsMin.CopyList(), AnchorsMax.CopyList(), Pivots.CopyList());
        
        public void Update(RectObject src)
        {
            ObjectId = src.ObjectId;
            ParentObjectId = src.ParentObjectId;
            Name = src.Name;
            Active = src.Active;
            Span = src.Span;
            Layer = src.Layer;
            
            Positions = src.Positions.CopyList();
            Rotations = src.Rotations.CopyList();
            Scales = src.Scales.CopyList();
            Sizes = src.Sizes.CopyList();
            AnchorsMin = src.AnchorsMin.CopyList();
            AnchorsMax = src.AnchorsMax.CopyList();
            Pivots = src.Pivots.CopyList();
        }

        public void Pull(RectObject src)
        {
            ObjectId = src.ObjectId;
            ParentObjectId = src.ParentObjectId;
            Name = src.Name;
            Active = src.Active;
            Span = src.Span;
            Layer = src.Layer;
            Positions = src.Positions.CopyList();
            Rotations = src.Rotations.CopyList();
            Scales = src.Scales.CopyList();
            Sizes = src.Sizes.CopyList();
            AnchorsMin = src.AnchorsMin.CopyList();
            AnchorsMax = src.AnchorsMax.CopyList();
            Pivots = src.Pivots.CopyList();
        }

        public override bool Equals(object obj) => obj is RectObject value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            
            hashCode.Add(ObjectId);
            hashCode.Add(ParentObjectId);
            hashCode.Add(Name);
            hashCode.Add(Active);
            hashCode.Add(Span);
            hashCode.Add(Layer);
            
            hashCode.Add(Positions.GetListHashCode());
            hashCode.Add(Rotations.GetListHashCode());
            hashCode.Add(Scales.GetListHashCode());
            hashCode.Add(Sizes.GetListHashCode());
            hashCode.Add(AnchorsMin.GetListHashCode());
            hashCode.Add(AnchorsMax.GetListHashCode());
            hashCode.Add(Pivots.GetListHashCode());
            
            return hashCode.ToHashCode();
        }

        public virtual bool Equals(RectObject other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            
            var result = EqualsObject(other);
            return result;
        }
        
        protected bool EqualsObject(RectObject other)
        {
            var result = ObjectId.Equals(other.ObjectId)
                         && ParentObjectId.Equals(other.ParentObjectId)
                         && Name.Equals(other.Name)
                         && Active == other.Active
                         && Span.Equals(other.Span)
                         // rect content
                         && Positions.ListEquals(other.Positions)
                         && Layer.Equals(other.Layer)
                         && Rotations.ListEquals(other.Rotations)
                         && Scales.ListEquals(other.Scales)
                         && Sizes.ListEquals(other.Sizes)
                         && AnchorsMin.ListEquals(other.AnchorsMin)
                         && AnchorsMax.ListEquals(other.AnchorsMax)
                         && Pivots.ListEquals(other.Pivots);
            return result;
        }
    }
}