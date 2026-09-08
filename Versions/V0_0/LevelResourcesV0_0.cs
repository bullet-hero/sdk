using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V0_0
{
    // ReSharper disable once InconsistentNaming
    

    /// <summary> The v0.0 generation of the resources domain. A frozen snapshot - never edit it to match
    /// today's shape. </summary>
    [DataVersion(DataDomains.LevelResources, 0, 0)]
    public class LevelResourcesV0_0
    {
        /// <summary> Placeholder data with no correspondence to today's shape, so nothing maps out of it. </summary>
        [JsonProperty("test_resources")]
        public Dictionary<int, object> Resources { get; set; }
    }
}