using System;
using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Game
{
    /// <summary>
    /// The camera's animation tracks - a trimmed-down RectObject, kept as its own aggregate because
    /// the camera has no parent, no layer and no independent axes. The comment below lists exactly
    /// which object tracks were dropped or replaced and why.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.CameraEvents, 1, 0)]
    public class CameraEvents : IModel<CameraEvents>
    {
        // Camera - is a unique form of default RectObject. It cut some data, here's all
        // Positions - unchanged
        // Layers - camera is not an ShapeObject, you can't see it, it's otherwise, remove
        // Rotations - unchanged
        // Scales - always (1f, 1f) because of aspect, remove
        // Size - camera aspect is locked by user screen, also Level.Settings provide tool for
        // manual setup aspect (IScreenLimit). Because of that
        // changed to Zooms - Size with 1 value applied to xy simultaneously
        // AnchorsMin/AnchorsMax - camera doesn't have any parents, remove
        // Pivot - unchanged
        
        /// <summary> Where the camera looks, in level space - it has no parent to be relative to. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxCameraKeys)]
        [RuleCollectionUnique(nameof(PosKey.Frame))]
        [JsonProperty(Names.Position)]
        public List<PosKey> Positions { get; set; }

        /// <summary> Camera roll in degrees. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxCameraKeys)]
        [RuleCollectionUnique(nameof(AngleKey.Frame))]
        [JsonProperty(Names.Rotation)]
        public List<AngleKey> Rotations { get; set; }

        /// <summary> Single-axis stand-in for an object's two-axis Sizes track - the aspect ratio is
        /// the device's, so only uniform zoom is authorable. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxCameraKeys)]
        [RuleCollectionUnique(nameof(ZoomKey.Frame))]
        [JsonProperty(Names.Zoom)]
        public List<ZoomKey> Zooms { get; set; }

        /// <summary> The point rotation and zoom happen around. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(AlignmentKey.Frame))]
        [JsonProperty(Names.Pivot)]
        public List<AlignmentKey> Pivots { get; set; }

        /// <summary> Procedural shake layered over Positions - the one track with no RectObject
        /// counterpart, since only the camera shakes. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxCameraKeys)]
        [RuleCollectionUnique(nameof(ShakeKey.Frame))]
        [JsonProperty(Names.Shake)]
        public List<ShakeKey> Shakes { get; set; }

        public CameraEvents()
        {
            Positions = new List<PosKey>();
            Rotations = new List<AngleKey>();
            Zooms = new List<ZoomKey>();
            Pivots = new List<AlignmentKey>();
            Shakes = new List<ShakeKey>();
        }
        public CameraEvents(List<PosKey> positions, List<AngleKey> rotations,
            List<ZoomKey> zooms, List<AlignmentKey> pivots, List<ShakeKey> shakes)
        {
            Positions = positions;
            Rotations = rotations;
            Zooms = zooms;
            Pivots = pivots;
            Shakes = shakes;
        }
        public void Reset()
        {
            Positions.Clear();
            Rotations.Clear();
            Zooms.Clear();
            Pivots.Clear();
            Shakes.Clear();
        }

        public object Clone() => Copy();
        public CameraEvents Copy() => new(Positions.CopyList(), Rotations.CopyList(),
            Zooms.CopyList(), Pivots.CopyList(), Shakes.CopyList());

        public void Update(CameraEvents src)
        {
            Positions = src.Positions.CopyList();
            Rotations = src.Rotations.CopyList();
            Zooms = src.Zooms.CopyList();
            Pivots = src.Pivots.CopyList();
            Shakes = src.Shakes.CopyList();
        }

        public void Pull(CameraEvents src)
        {
            Positions = src.Positions.CopyList();
            Rotations = src.Rotations.CopyList();
            Zooms = src.Zooms.CopyList();
            Pivots = src.Pivots.CopyList();
            Shakes = src.Shakes.CopyList();
        }

        public override bool Equals(object obj) => obj is CameraEvents value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Positions.GetListHashCode(),
            Rotations.GetListHashCode(), Zooms.GetListHashCode(), Pivots.GetListHashCode(), Shakes.GetListHashCode());

        public bool Equals(CameraEvents other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Positions.ListEquals(other.Positions)
                         && Rotations.ListEquals(other.Rotations)
                         && Zooms.ListEquals(other.Zooms)
                         && Pivots.ListEquals(other.Pivots)
                         && Shakes.ListEquals(other.Shakes);
            return result;
        }
    }
}