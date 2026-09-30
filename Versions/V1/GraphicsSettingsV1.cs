using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.SettingGroups.Graphics;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of UserSettingsV1, frozen because
    // generation 2 added the Colliders Only group. Its own sub-groups did not change and stay typed as
    // today's classes.

    /// <summary> The graphics group as generation 1 (Release) of the user-settings domain wrote it -
    /// before <c>colliders_mode</c>. A frozen snapshot - never edit it to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class GraphicsSettingsV1 : IModel<GraphicsSettingsV1>
    {
        /// <summary> Where the target framerate comes from. </summary>
        [JsonProperty("fps_target")]
        public FramerateTarget FpsTarget { get; set; }

        /// <summary> Explicit framerate cap. </summary>
        [JsonProperty("fps_fixed")]
        public int FpsFixed { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("audio")]
        public AudioGraphicsSettings Audio { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("eff")]
        public EffectsGraphicsSettings Effects { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("avatar")]
        public AvatarGraphicsSettings Avatar { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("postprocessing")]
        public PostProcessingGraphicsSettings PostProcessing { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("aa")]
        public AntiAliasingGraphicsSettings AntiAliasing { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("textures")]
        public TexturesGraphicsSettings Textures { get; set; }

        /// <summary> Unchanged at generation 2. </summary>
        [JsonProperty("display")]
        public DisplayGraphicsSettings Display { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public GraphicsSettingsV1()
        {
            FpsTarget = FramerateTarget.ScreenHz;
            FpsFixed = 60;
            Audio = new AudioGraphicsSettings();
            Effects = new EffectsGraphicsSettings();
            Avatar = new AvatarGraphicsSettings();
            PostProcessing = new PostProcessingGraphicsSettings();
            AntiAliasing = new AntiAliasingGraphicsSettings();
            Textures = new TexturesGraphicsSettings();
            Display = new DisplayGraphicsSettings();
        }
    }
}
