using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Data
{
    /// <summary>
    /// A named palette of 64 colors that objects reference by slot instead of storing colors of
    /// their own. Swapping which theme is active (ThemeKeyframe) then recolors the whole level at
    /// once - the reason ColorType.ThemeRef exists.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.ThemeData, 1, 0)]
    [GenerateModel]
    public sealed partial class ThemeData : IModel<ThemeData>
    {
        /// <summary> Identity of this palette, what ThemeKeyframe selects. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.ThemeId)]
        public ThemeId ThemeId { get; set; }

        /// <summary> Editor-facing label of the palette. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public string Name { get; set; }

        /// <summary> The 64 slots, fixed length so a ThemeRef index is always resolvable. Slot
        /// meanings follow Project Arrhythmya's layout, mapped out below. </summary>
        [RuleNotNull, RuleCollectionCount(ValueRules.ThemeCount)]
        [JsonProperty(Names.Matrix)]
        public Color4Value[] Matrix { get; set; }
        
        // Theme - is map of colors, level can refer to color via index
        // Theme is a predefined array in runtime
        // Now it's 64 or 8x8 grid. If you see PA and this game, what indexes means (starts with 1)
        // 1 - fallback color, if index is not founded
        // 2 - GUI (PA)
        // 3 - Background (PA)
        // 4-7 - Players (PA)
        // 8 - Tail (PA)
        // 9-16 - free space
        // 17-25 - objects (PA)
        // 26-32 - free
        // 33-41 - parallax (PA)
        // 42-48 - free
        // 49-57 - effects (PA)
        // 58-64 - free
        
        public ThemeData()
        {
            ThemeId = ThemeId.Null;
            Name = string.Empty;
            Matrix = new Color4Value[ValueRules.ThemeCount];
            Array.Fill(Matrix, Color4Value.white);
        }
        public ThemeData(ThemeId themeId, string name = "")
        {
            ThemeId = themeId;
            Name = name;
            Matrix = new Color4Value[ValueRules.ThemeCount];
            Array.Fill(Matrix, Color4Value.white);
        }
        public ThemeData(ThemeId themeId, string name, Color4Value[] matrix)
        {
            ThemeId = themeId;
            Name = name;
            Matrix = matrix;
        }
    }
}