using BH.SDK.Serialization.Serializers;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1_0
{
    // ReSharper disable once InconsistentNaming

    // Sixteen flat properties, which is what GameEditorSettings was until it became nine nested
    // groups. It carries NO [DataVersion]: this group never had an envelope of its own - it was
    // always a plain nested object inside UserSettings - so the containing domain's migrator is what
    // builds the current type out of these raw fields, exactly as AudioLevelV0_0's own note describes.
    //
    // Keys are LITERALS on purpose. Names.cs tracks CURRENT naming and no longer holds a single one
    // of these; a snapshot that read from it would silently follow the next rename and stop matching
    // the files it exists to read.

    public class GameEditorSettingsV1_0
    {
        [JsonProperty("autosave")]
        public bool Autosave { get; set; }

        [JsonProperty("autosave_rate")]
        public float AutosaveRate { get; set; }

        [JsonProperty("max_autosave_files")]
        public int MaxAutosaveFiles { get; set; }

        [JsonProperty("camera_min_size")]
        public float CameraMinSize { get; set; }

        [JsonProperty("camera_max_size")]
        public float CameraMaxSize { get; set; }

        [JsonProperty("player_active_default")]
        public bool PlayerActiveDefault { get; set; }

        [JsonProperty("gizmos_reset_on_player")]
        public bool GizmosResetOnPlayer { get; set; }

        [JsonProperty("multi_selection_requires_hold")]
        public bool MultiSelectRequiresHold { get; set; }

        [JsonProperty("preview_collider_on_selection")]
        public bool PreviewColliderOnSelect { get; set; }

        [JsonProperty("pick_invisible_aabb")]
        public bool PickInvisibleAABB { get; set; }

        [JsonProperty("render_inframes")]
        public bool RenderInframes { get; set; }

        [JsonProperty("grid_size")]
        public float GridSize { get; set; }

        [JsonProperty("grid_opacity")]
        public float GridOpacity { get; set; }

        [JsonProperty("level_serialize_mode")]
        public SerializationType LevelSerializeMode { get; set; }

        [JsonProperty("resources_serialize_mode")]
        public SerializationType ResourcesSerializeMode { get; set; }

        [JsonProperty("copy_serialize_mode")]
        public SerializationType CopySerializeMode { get; set; }
    }
}
