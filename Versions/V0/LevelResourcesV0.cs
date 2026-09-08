using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V0
{
    // ReSharper disable once InconsistentNaming
    

    /// <summary> Generation 0 of the resources domain. A frozen snapshot - never edit it to match
    /// today's shape. </summary>
    [ModelGeneration(ModelDomains.LevelResources, ModelGenerations.Test)]
    public class LevelResourcesV0
    {
        /// <summary> Placeholder data with no correspondence to today's shape, so nothing maps out of it. </summary>
        [JsonProperty("test_resources")]
        public Dictionary<int, object> Resources { get; set; }
    }
}