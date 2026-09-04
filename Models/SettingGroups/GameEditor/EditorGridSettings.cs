using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // WHETHER THE LINES ARE DRAWN RIGHT NOW is not here, and that is the split: the current view is
    // session state, like the active gizmo, and lives in Services.GameEditor's GridModeService. What
    // the STARTING state is describes how the author works, so that half is remembered - the same
    // split, and the same pair of names, as the preview player's EditorPlayerSettings.ActiveDefault.
    // How big a cell is and how loud the lines are are remembered for the same reason: a level
    // authored on a half-unit grid stays authored on one across sessions.
    //
    // Alpha is deliberately the ONLY thing authored about the colour: the hue is the inverse of
    // whatever the camera is showing on the current frame, so the one decision left is how far the
    // lines fade into it. The camera's OWN alpha never takes part - a level fading its background out
    // would otherwise take the grid with it, and a guide that disappears while the content it guides
    // is still on screen is worse than no guide.

    /// <summary>
    /// The editor viewport's grid: how big one cell is and how loud its lines are.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorGridSettings : IModel<EditorGridSettings>, IMoveable<EditorGridSettings>
    {
        // TRUE rather than the zero value, and both halves of that matter. A grid is what the
        // viewport is missing before anything is placed in it, so switching it on by hand every
        // session is a step nobody chooses to keep; and a settings file written before this field
        // existed reads back as the field's default, so making that default true is also what keeps
        // this additive - no DataVersion moved and there is no migrator, the same call
        // LevelOrientation.Horizontal makes about not being its enum's zero.

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

        public EditorGridSettings()
        {
            ResetOwn();
        }
        public EditorGridSettings(bool activeDefault, float size, float opacity)
        {
            ActiveDefault = activeDefault;
            Size = size;
            Opacity = opacity;
        }
        private void ResetOwn()
        {
            ActiveDefault = true;
            Size = 1f;
            Opacity = 0.25f;
        }
    }
}
