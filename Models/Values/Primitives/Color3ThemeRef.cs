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
    /// An RGB color deferred to the palette: "whatever sits in slot N of the theme active right now".
    /// Lets one recolor of a ThemeData restyle a whole level at once, instead of editing every object.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Color3ThemeRef : IColor3, IModel<Color3ThemeRef>
    {
        /// <summary> Slot inside the active ThemeData.Matrix (0-63). Deliberately a raw index, not a
        /// ThemeId - which theme is active is decided elsewhere, by ThemeKeyframe. </summary>
        [RuleInRange(ValueRules.MinThemeIndex, ValueRules.MaxThemeIndex)]
        [JsonProperty(Names.ThemeIndex)]
        public int ThemeColorIndex { get; set; }

        public ColorType GetModelType() => ColorType.ThemeRef;

        public Color3ThemeRef()
        {
            ThemeColorIndex = ValueRules.MinThemeIndex;
        }
        public Color3ThemeRef(int themeColorIndex)
        {
            ThemeColorIndex = themeColorIndex;
        }
    }
}
