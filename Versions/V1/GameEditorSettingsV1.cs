using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.SettingGroups.GameEditor;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of UserSettingsV1, frozen because
    // generation 2 added the publishing group and changed the interface group. The other sub-groups did
    // not change and stay typed as today's classes.

    /// <summary> The editor group as generation 1 (Release) of the user-settings domain wrote it -
    /// before <c>publishing</c>. A frozen snapshot - never edit it to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class GameEditorSettingsV1 : IModel<GameEditorSettingsV1>
    {
        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("savings")]
        public EditorSavingsSettings Savings { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("camera")]
        public EditorCameraSettings Camera { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("player")]
        public EditorPlayerSettings Player { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("grid")]
        public EditorGridSettings Grid { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("eff")]
        public EditorEffectsSettings Effects { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("selection")]
        public EditorSelectionSettings Selection { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("gizmos")]
        public EditorGizmosSettings Gizmos { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("creation")]
        public EditorCreationSettings Creation { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("timeline")]
        public EditorTimelineSettings Timeline { get; set; }

        /// <summary> Changed at generation 2: gained <c>levels_layout</c>. </summary>
        [JsonProperty("iface")]
        public EditorInterfaceSettingsV1 Interface { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("serialize")]
        public EditorSerializationSettings Serialization { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public GameEditorSettingsV1()
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
            Interface = new EditorInterfaceSettingsV1();
            Serialization = new EditorSerializationSettings();
        }
    }
}
