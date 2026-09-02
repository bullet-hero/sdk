using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Game
{
    /// <summary>
    /// The camera's animation tracks - a trimmed-down RectObject, kept as its own aggregate because
    /// the camera has no parent, no layer and no independent axes. The comment below lists exactly
    /// which object tracks were dropped or replaced and why.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.CameraEvents, 1, 0)]
    [GenerateModel]
    public sealed partial class CameraEvents : IModel<CameraEvents>
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
    }
}