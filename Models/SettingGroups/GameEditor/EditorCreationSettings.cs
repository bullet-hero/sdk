using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // WHAT A NEW OBJECT MEASURES HAS TO BE SAID SOMEWHERE, because an empty Sizes track no longer
    // says it: that fallback is zero - a group node with no extent of its own - so a type that draws
    // something is given an explicit first size keyframe at creation instead (GameEditor's SizeSeed).
    // A seed can be deleted by the author, which is the whole reason it beats a per-type fallback.
    //
    // It belongs to the PERSON rather than to the level, like everything else under UserSettings: two
    // authors can disagree about what a new shape should start at and neither is editing the other's
    // levels differently for it. Nothing here is ever read at playback - once the keyframe is
    // written, the level carries the number and this group has no further say.
    //
    // TEXT IS WIDER THAN TALL, AND NOT BY TASTE: a TextObject's Size is the BLOCK its glyphs lay out
    // in rather than the glyphs themselves, so a one-cell square holds a single character - the same
    // measurement the Afterbeat import has to estimate on its way in (ABObjectImporter.ApplyTextSize
    // gives a block one cell per character and one line tall). A shape has no such second meaning:
    // its size IS its rect, and the unit square is what the whole shape library is drawn inside.
    //
    // Four floats rather than two vectors, which is the shape every other settings group here takes
    // (see EditorCameraSettings' MoveSensitivityX/Y): a settings group is a flat list of scalars an
    // author reads in a file and edits in a row of fields.

    /// <summary>
    /// What a newly created object is sized as, per type - the first size keyframe the editor writes
    /// for it.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorCreationSettings : IModel<EditorCreationSettings>,
        IMoveable<EditorCreationSettings>
    {
        /// <summary> Width a newly created ShapeObject is given, in world units. </summary>
        [RuleInRange(ValueRules.MinSca, ValueRules.MaxSca)]
        [JsonProperty(Names.ShapeSizeX)]
        public float ShapeSizeX { get; set; }

        /// <summary> Height a newly created ShapeObject is given, in world units. </summary>
        [RuleInRange(ValueRules.MinSca, ValueRules.MaxSca)]
        [JsonProperty(Names.ShapeSizeY)]
        public float ShapeSizeY { get; set; }

        /// <summary> Width of the block a newly created TextObject lays its glyphs out in. </summary>
        [RuleInRange(ValueRules.MinSca, ValueRules.MaxSca)]
        [JsonProperty(Names.TextSizeX)]
        public float TextSizeX { get; set; }

        /// <summary> Height of the block a newly created TextObject lays its glyphs out in. </summary>
        [RuleInRange(ValueRules.MinSca, ValueRules.MaxSca)]
        [JsonProperty(Names.TextSizeY)]
        public float TextSizeY { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorCreationSettings()
        {
            ResetOwn();
        }
        /// <summary> Built from both sizes, width then height. </summary>
        public EditorCreationSettings(float shapeSizeX, float shapeSizeY, float textSizeX, float textSizeY)
        {
            ShapeSizeX = shapeSizeX;
            ShapeSizeY = shapeSizeY;
            TextSizeX = textSizeX;
            TextSizeY = textSizeY;
        }
        private void ResetOwn()
        {
            ShapeSizeX = 1f;
            ShapeSizeY = 1f;
            TextSizeX = 4f;
            TextSizeY = 2f;
        }
    }
}
