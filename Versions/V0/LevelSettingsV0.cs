using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V0
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Generation 0 of the level-settings domain. A frozen snapshot - never edit it to match
    /// today's shape. </summary>
    [ModelGeneration(ModelDomains.LevelSettings, ModelGenerations.Test)]
    [GenerateModel]
    public sealed partial class LevelSettingsV0 : IModel<LevelSettingsV0>
    {
        /// <summary> The only setting that existed at generation 0; the timeline length is derived from it on migration. </summary>
        [JsonProperty("test_fps")]
        public int Framerate { get; set; }
    }
}