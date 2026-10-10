using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // The current view is session state (GridModeService); the STARTING state is remembered. Alpha is the
    // only authored colour, and the camera's own alpha never takes part. Docs/SETTING_GROUPS.md

    /// <summary>
    /// The editor viewport's grid: how big one cell is and how loud its lines are.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorGridSettings : IModel<EditorGridSettings>, IMoveable<EditorGridSettings>
    {
        // False: on a phone the lines read as content, and reaching for the grid is one press.

        /// <summary> Whether the editor's viewport grid starts switched on. </summary>
        [JsonProperty(Names.ActiveDefault)]
        public bool ActiveDefault { get; set; }

        /// <summary> Side of one cell of the editor's viewport grid, in world units. </summary>
        [RuleMinValue(ValueRules.MinGridSize)]
        [JsonProperty(Names.Size)]
        public float Size { get; set; }

        /// <summary> Opacity of the editor viewport grid's lines. </summary>
        [RuleInRange(0f, 1f)]
        [JsonProperty(Names.Opacity)]
        public float Opacity { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorGridSettings()
        {
            ResetOwn();
        }
        /// <summary> Built from its default, size and opacity. </summary>
        public EditorGridSettings(bool activeDefault, float size, float opacity)
        {
            ActiveDefault = activeDefault;
            Size = size;
            Opacity = opacity;
        }
        private void ResetOwn()
        {
            ActiveDefault = false;
            Size = 1f;
            Opacity = 0.25f;
        }
    }
}
