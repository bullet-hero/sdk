using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// RGBA counterpart of Color3ThemeRef - alpha comes from the referenced palette slot too, so a
    /// theme can make a whole class of objects translucent without touching them.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Color4ThemeRef : IColor4, IModel<Color4ThemeRef>
    {
        /// <summary> Slot inside the active ThemeData.Matrix (0-63), resolved per frame. </summary>
        [RuleInRange(ValueRules.MinThemeIndex, ValueRules.MaxThemeIndex)]
        [JsonProperty(Names.ThemeIndex)]
        public int ThemeColorIndex { get; set; }
        
        public ColorType GetModelType() => ColorType.ThemeRef;

        public Color4ThemeRef()
        {
            ThemeColorIndex = ValueRules.MinThemeIndex;
        }
        public Color4ThemeRef(int themeColorIndex)
        {
            ThemeColorIndex = themeColorIndex;
        }
    }
}