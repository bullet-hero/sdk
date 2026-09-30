using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of UserSettingsV1, frozen because
    // generation 2 added the menu level browser's starting layout.

    /// <summary> The game's interface group as generation 1 (Release) of the user-settings domain wrote
    /// it - before <c>levels_layout</c>. A frozen snapshot - never edit it to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class InterfaceSettingsV1 : IModel<InterfaceSettingsV1>
    {
        /// <summary> Whether losing a run opens the result window instead of respawning. </summary>
        [JsonProperty("open_menu_on_lose")]
        public bool OpenMenuOnLose { get; set; }

        /// <summary> Whether the statistics overlay is drawn at all. </summary>
        [JsonProperty("stats_active")]
        public bool StatsActive { get; set; }

        /// <summary> Whether the overlay draws the per-frame buffer usage block. </summary>
        [JsonProperty("stats_frame_objects")]
        public bool StatsFrameObjects { get; set; }

        /// <summary> Whether the overlay draws the loaded level's own size block. </summary>
        [JsonProperty("stats_level_objects")]
        public bool StatsLevelObjects { get; set; }

        /// <summary> Whether the overlay draws the process memory block. </summary>
        [JsonProperty("stats_memory")]
        public bool StatsMemory { get; set; }

        /// <summary> Whether the overlay draws the profiling block. </summary>
        [JsonProperty("stats_profile")]
        public bool StatsProfiling { get; set; }

        /// <summary> Horizontal alignment of the statistics overlay. </summary>
        [JsonProperty("stats_alignment_x")]
        public float StatsAlignmentX { get; set; }

        /// <summary> Vertical alignment of the statistics overlay. </summary>
        [JsonProperty("stats_alignment_y")]
        public float StatsAlignmentY { get; set; }

        /// <summary> What the main menu draws behind its buttons. </summary>
        [JsonProperty("menu_background")]
        public MenuBackgroundKind MenuBackground { get; set; }

        /// <summary> Which way round the player asked the device to hold this game. </summary>
        [JsonProperty("screen_orientation")]
        public ScreenOrientationLock ScreenOrientation { get; set; }

        /// <summary> Whether the run progress bar is drawn. </summary>
        [JsonProperty("show_game_progress")]
        public bool ShowGameProgress { get; set; }

        /// <summary> Whether the pause button is drawn. </summary>
        [JsonProperty("show_game_pause")]
        public bool ShowGamePause { get; set; }

        /// <summary> The master switch over the game screen's HUD. </summary>
        [JsonProperty("show_game_iface")]
        public bool ShowGameInterface { get; set; }

        /// <summary> How visible the avatar's hitbox ring is. </summary>
        [JsonProperty("hitbox_ring_opacity")]
        public float HitboxRingOpacity { get; set; }

        /// <summary> Whether an uncaught exception opens the error window. </summary>
        [JsonProperty("alert_on_exception")]
        public bool AlertOnException { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public InterfaceSettingsV1()
        {
            OpenMenuOnLose = false;
            StatsActive = false;
            StatsFrameObjects = false;
            StatsLevelObjects = false;
            StatsMemory = false;
            StatsProfiling = false;
            StatsAlignmentX = 0f;
            StatsAlignmentY = 1f;
            MenuBackground = MenuBackgroundKind.Bot;
            ScreenOrientation = ScreenOrientationLock.Unlock;
            ShowGameProgress = true;
            ShowGamePause = true;
            ShowGameInterface = true;
            HitboxRingOpacity = 0.6f; // InterfaceSettings.DefaultHitboxRingOpacity at generation 1
            AlertOnException = true;
        }
    }
}
