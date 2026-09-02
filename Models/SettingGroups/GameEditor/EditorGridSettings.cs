using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // The grid's own VISIBILITY is not here, and that is the split: whether the lines are drawn right
    // now is the current view, like the active gizmo, and lives in the session (Services.GameEditor's
    // GridModeService). How big a cell is and how loud the lines are describe how the author works -
    // a level authored on a half-unit grid stays authored on one across sessions - so only those are
    // remembered.
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
        public EditorGridSettings(float size, float opacity)
        {
            Size = size;
            Opacity = opacity;
        }
        private void ResetOwn()
        {
            Size = 1f;
            Opacity = 0.25f;
        }
    }
}
