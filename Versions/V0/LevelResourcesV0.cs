using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V0
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Generation 0 of the resources domain. A frozen snapshot - never edit it to match
    /// today's shape. </summary>
    [ModelGeneration(ModelDomains.LevelResources, ModelGenerations.Test)]
    [GenerateModel]
    public sealed partial class LevelResourcesV0 : IModel<LevelResourcesV0>
    {
        // Not a Dictionary<int, object>: a snapshot has a generated codec, which encodes only what it can
        // name. LevelResourcesV0ToV1 maps none of it.

        /// <summary> Placeholder data with no correspondence to today's shape, so nothing maps out of it. </summary>
        [JsonProperty("test_resources")]
        public Dictionary<int, string> Resources { get; set; }

        /// <summary> Every member at the value generation 0 would have read into it. </summary>
        public LevelResourcesV0() => Resources = new Dictionary<int, string>();
    }
}
