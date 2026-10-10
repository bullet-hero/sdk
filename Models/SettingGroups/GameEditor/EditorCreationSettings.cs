using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // A new object's size is a seeded first keyframe (GameEditor's SizeSeed): an empty Sizes track reads
    // zero. Text is wider because its size is the glyph block. Docs/SETTING_GROUPS.md

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
