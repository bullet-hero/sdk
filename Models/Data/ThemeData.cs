using System;
using System.Collections.Generic;
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
    /// A named palette of 64 colors that objects reference by slot instead of storing colors; swapping
    /// the active theme (ThemeKeyframe) recolors the whole level at once.
    /// </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.ThemeData, ModelGenerations.V1_AlphaRelease)]
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
        /// meanings are mapped out below. </summary>
        [RuleNotNull, RuleCollectionCount(ValueRules.ThemeCount)]
        [JsonProperty(Names.Matrix)]
        public Color4Value[] Matrix { get; set; }
        
        // Slot bands (1-based; 1 is the fallback colour): Docs/RESOURCE_DATA.md

        /// <summary> One author-written label per slot, or null when none was ever named (a real state,
        /// written as one null). A non-null list is exactly ThemeCount long; an empty or null entry
        /// means that slot is unnamed. Not derived from the band map. </summary>
        [RuleOptional, RuleCollectionCount(ValueRules.ThemeCount)]
        [JsonProperty(Names.ColorNames)]
        public List<string> ColorNames { get; set; }
        
        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ThemeData()
        {
            ThemeId = ThemeId.Null;
            Name = string.Empty;
            Matrix = new Color4Value[ValueRules.ThemeCount];
            Array.Fill(Matrix, Color4Value.white);
            ColorNames = null;
        }
        /// <summary> Built from its id and "". </summary>
        public ThemeData(ThemeId themeId, string name = "")
        {
            ThemeId = themeId;
            Name = name;
            Matrix = new Color4Value[ValueRules.ThemeCount];
            Array.Fill(Matrix, Color4Value.white);
            ColorNames = null;
        }
        /// <summary> Built from its id, name and matrix. </summary>
        public ThemeData(ThemeId themeId, string name, Color4Value[] matrix)
        {
            ThemeId = themeId;
            Name = name;
            Matrix = matrix;
            ColorNames = null;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public ThemeData(ThemeId themeId, string name, Color4Value[] matrix, List<string> colorNames)
        {
            ThemeId = themeId;
            Name = name;
            Matrix = matrix;
            ColorNames = colorNames;
        }
    }
}