using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.SettingGroups.GameEditor;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    // This is the one UserSettings group that has ever been RESTRUCTURED rather than added to, and it
    // is what once took the domain from (1,0) to (2,0). Sixteen flat properties had accumulated here,
    // and the audit that fed twenty new ones in (docs/issues/EDITOR_SETTINGS_HISTORY.md) would have
    // made it thirty-six - a constructor nobody can call correctly and a GetHashCode already folding
    // twice.
    //
    // Moving keys is the one change an additive default cannot cover, so it shipped with a snapshot
    // and a migrator. Both are deleted and the domain is back at (1,0) - root CLAUDE.md Rule 11, the
    // game is pre-release. The shape it moved to is GraphicsSettings' own - a root holding nothing but
    // sub-groups - except that this one keeps no loose properties at all: every field belongs to
    // exactly one of the groups below.

    /// <summary>
    /// Preferences for the in-game level editor, per device. Belongs to the person editing, never to
    /// the level being edited.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class GameEditorSettings : IModel<GameEditorSettings>, IMoveable<GameEditorSettings>
    {
        /// <summary> Autosave policy and how deep the operation history goes. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Savings)]
        public EditorSavingsSettings Savings { get; set; }

        /// <summary> How the viewport camera pans and zooms, and its zoom limits. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Camera)]
        public EditorCameraSettings Camera { get; set; }

        /// <summary> How the editor's preview player behaves when switched on. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Player)]
        public EditorPlayerSettings Player { get; set; }

        /// <summary> The viewport grid's cell size and line opacity. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Grid)]
        public EditorGridSettings Grid { get; set; }

        /// <summary> Whether the editor previews a level's post-processing effects. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Effects)]
        public EditorEffectsSettings Effects { get; set; }

        /// <summary> How objects are picked, and what a picked object shows about itself. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Selection)]
        public EditorSelectionSettings Selection { get; set; }

        /// <summary> The viewport's drag handles. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Gizmos)]
        public EditorGizmosSettings Gizmos { get; set; }

        /// <summary> What a newly created object is sized as, per type. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Creation)]
        public EditorCreationSettings Creation { get; set; }

        /// <summary> How the timelines respond to a pointer, and whether playback wraps. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Timeline)]
        public EditorTimelineSettings Timeline { get; set; }

        /// <summary> How the editor's own panels read and behave. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Interface)]
        public EditorInterfaceSettings Interface { get; set; }

        /// <summary> Which wire format each kind of file is written with. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Serialize)]
        public EditorSerializationSettings Serialization { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public GameEditorSettings()
        {
            Savings = new EditorSavingsSettings();
            Camera = new EditorCameraSettings();
            Player = new EditorPlayerSettings();
            Grid = new EditorGridSettings();
            Effects = new EditorEffectsSettings();
            Selection = new EditorSelectionSettings();
            Gizmos = new EditorGizmosSettings();
            Creation = new EditorCreationSettings();
            Timeline = new EditorTimelineSettings();
            Interface = new EditorInterfaceSettings();
            Serialization = new EditorSerializationSettings();
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public GameEditorSettings(EditorSavingsSettings savings, EditorCameraSettings camera,
            EditorPlayerSettings player, EditorGridSettings grid, EditorEffectsSettings effects,
            EditorSelectionSettings selection,
            EditorGizmosSettings gizmos, EditorCreationSettings creation, EditorTimelineSettings timeline,
            EditorInterfaceSettings interfaceSettings, EditorSerializationSettings serialization)
        {
            Savings = savings;
            Camera = camera;
            Player = player;
            Grid = grid;
            Effects = effects;
            Selection = selection;
            Gizmos = gizmos;
            Creation = creation;
            Timeline = timeline;
            Interface = interfaceSettings;
            Serialization = serialization;
        }

        // HashCode.Combine takes at most 8 values and this holds 10, so the tail folds into the
        // eighth slot - the same shape the flat version needed twice over for its sixteen.
    }
}