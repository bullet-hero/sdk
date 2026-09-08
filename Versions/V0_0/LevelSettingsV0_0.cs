using Newtonsoft.Json;

namespace BH.SDK.Versions.V0_0
{
    // ReSharper disable once InconsistentNaming


    /// <summary> The v0.0 generation of the level-settings domain. A frozen snapshot - never edit it to match
    /// today's shape. </summary>
    [DataVersion(DataDomains.LevelSettings, 0, 0)]
    public class LevelSettingsV0_0
    {
        /// <summary> The only setting that existed at v0.0; the timeline length is derived from it on migration. </summary>
        [JsonProperty("test_fps")]
        public int Framerate { get; set; }
    }
}