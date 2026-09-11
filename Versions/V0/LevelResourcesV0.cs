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
        // IT WAS A Dictionary<int, object> AND COULD NOT STAY ONE. A snapshot carries its own
        // generated codec now, and the generator encodes what it can name - `object` is not that.
        // Retyping cost nothing: the migrator out of this domain (LevelResourcesV0ToV1) maps none of
        // it, because the placeholder never corresponded to anything today's shape holds.

        /// <summary> Placeholder data with no correspondence to today's shape, so nothing maps out of it. </summary>
        [JsonProperty("test_resources")]
        public Dictionary<int, string> Resources { get; set; }

        /// <summary> Every member at the value generation 0 would have read into it. </summary>
        public LevelResourcesV0() => Resources = new Dictionary<int, string>();
    }
}
