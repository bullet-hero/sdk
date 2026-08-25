using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.SettingGroups.GameEditor;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    // This is the one UserSettings group that has ever been RESTRUCTURED rather than added to, and it
    // is what took the domain from (1,0) to (2,0). Sixteen flat properties had accumulated here, and
    // the audit that fed twenty new ones in (docs/issues/EDITOR_SETTINGS_HISTORY.md) would have made
    // it thirty-six - a constructor nobody can call correctly and a GetHashCode already folding twice.
    //
    // Moving keys is the one change an additive default cannot cover, so unlike every other group
    // here it ships with a snapshot (UserSettingsV1_0) and a migrator beside it. The shape it moved
    // to is GraphicsSettings' own - a root holding nothing but sub-groups - except that this one keeps
    // no loose properties at all: every field belongs to exactly one of the nine.

    /// <summary>
    /// Preferences for the in-game level editor, per device. Belongs to the person editing, never to
    /// the level being edited.
    /// </summary>
    [RuleContainer]
    public class GameEditorSettings : IModel<GameEditorSettings>, IMoveable<GameEditorSettings>
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

        /// <summary> How objects are picked, and what a picked object shows about itself. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Selection)]
        public EditorSelectionSettings Selection { get; set; }

        /// <summary> The viewport's drag handles. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Gizmos)]
        public EditorGizmosSettings Gizmos { get; set; }

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

        public GameEditorSettings()
        {
            Savings = new EditorSavingsSettings();
            Camera = new EditorCameraSettings();
            Player = new EditorPlayerSettings();
            Grid = new EditorGridSettings();
            Selection = new EditorSelectionSettings();
            Gizmos = new EditorGizmosSettings();
            Timeline = new EditorTimelineSettings();
            Interface = new EditorInterfaceSettings();
            Serialization = new EditorSerializationSettings();
        }

        public GameEditorSettings(EditorSavingsSettings savings, EditorCameraSettings camera,
            EditorPlayerSettings player, EditorGridSettings grid, EditorSelectionSettings selection,
            EditorGizmosSettings gizmos, EditorTimelineSettings timeline,
            EditorInterfaceSettings interfaceSettings, EditorSerializationSettings serialization)
        {
            Savings = savings;
            Camera = camera;
            Player = player;
            Grid = grid;
            Selection = selection;
            Gizmos = gizmos;
            Timeline = timeline;
            Interface = interfaceSettings;
            Serialization = serialization;
        }

        public void Reset()
        {
            Savings.Reset();
            Camera.Reset();
            Player.Reset();
            Grid.Reset();
            Selection.Reset();
            Gizmos.Reset();
            Timeline.Reset();
            Interface.Reset();
            Serialization.Reset();
        }

        public object Clone() => Copy();

        public GameEditorSettings Copy() => new(Savings.Copy(), Camera.Copy(), Player.Copy(),
            Grid.Copy(), Selection.Copy(), Gizmos.Copy(), Timeline.Copy(), Interface.Copy(),
            Serialization.Copy());

        public void Pull(GameEditorSettings source)
        {
            Savings.Pull(source.Savings);
            Camera.Pull(source.Camera);
            Player.Pull(source.Player);
            Grid.Pull(source.Grid);
            Selection.Pull(source.Selection);
            Gizmos.Pull(source.Gizmos);
            Timeline.Pull(source.Timeline);
            Interface.Pull(source.Interface);
            Serialization.Pull(source.Serialization);
        }

        public void Update(GameEditorSettings src)
        {
            Savings = src.Savings.Copy();
            Camera = src.Camera.Copy();
            Player = src.Player.Copy();
            Grid = src.Grid.Copy();
            Selection = src.Selection.Copy();
            Gizmos = src.Gizmos.Copy();
            Timeline = src.Timeline.Copy();
            Interface = src.Interface.Copy();
            Serialization = src.Serialization.Copy();
        }

        public override bool Equals(object obj) => obj is GameEditorSettings value && Equals(value);

        // HashCode.Combine takes at most 8 values and this holds 9, so the tail folds into the
        // eighth slot - the same shape the flat version needed twice over for its sixteen.
        public override int GetHashCode() => HashCode.Combine(Savings, Camera, Player, Grid,
            Selection, Gizmos, Timeline, HashCode.Combine(Interface, Serialization));

        public bool Equals(GameEditorSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Savings.Equals(other.Savings)
                   && Camera.Equals(other.Camera)
                   && Player.Equals(other.Player)
                   && Grid.Equals(other.Grid)
                   && Selection.Equals(other.Selection)
                   && Gizmos.Equals(other.Gizmos)
                   && Timeline.Equals(other.Timeline)
                   && Interface.Equals(other.Interface)
                   && Serialization.Equals(other.Serialization);
        }
    }
}