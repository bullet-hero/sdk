using Newtonsoft.Json;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>prefab_mode.enter</c> request. </summary>
    public sealed class PrefabModeEnterRequest
    {
        /// <summary> The template to open: the GUID <c>resources.list</c> prints for a prefab, or the one an
        /// outline line shows after <c>prefab:</c>. </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    /// <summary> The <c>prefab_mode.exit</c> request. </summary>
    public sealed class PrefabModeExitRequest
    {
        /// <summary> Leave every open template at once instead of the innermost one. </summary>
        [JsonProperty("all", NullValueHandling = NullValueHandling.Ignore)]
        public bool? All { get; set; }
    }

    /// <summary> The answer of both Prefab Mode calls; <c>scope</c> is the scope now active. </summary>
    public sealed class PrefabModeResult : AgentResultBase
    {
        /// <summary> How many templates are open, zero at the level. </summary>
        [JsonProperty("depth")]
        public int Depth { get; set; }
    }

    /// <summary> The <c>prefab.overrides</c> request. </summary>
    public sealed class PrefabOverridesRequest : AgentPagedRequest
    {
        /// <summary> A prefab placement, or an object materialized from one, in the active scope. </summary>
        [JsonProperty("id")]
        public int Id { get; set; }
    }
}
