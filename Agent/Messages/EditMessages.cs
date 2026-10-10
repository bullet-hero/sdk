using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>edit.batch</c> request: one undo step for the author. </summary>
    public sealed class EditBatchRequest
    {
        /// <summary> The revision of the read this batch is based on. </summary>
        [JsonProperty("expectedRevision")]
        public int ExpectedRevision { get; set; }

        /// <summary> What the History tab shows beside the batch. </summary>
        [JsonProperty("caption")]
        public string Caption { get; set; }

        /// <summary> The operations, at most <see cref="AgentRules.MaxBatchOps"/>; each has an <c>op</c>
        /// verb. </summary>
        [JsonProperty("ops")]
        public List<JObject> Ops { get; set; } = new List<JObject>();
    }

    /// <summary> The <c>edit.batch</c> answer. </summary>
    public sealed class EditBatchResult : AgentResultBase
    {
        /// <summary> Each <c>ref</c> the batch declared to the object id it created. </summary>
        [JsonProperty("created")]
        public Dictionary<string, int> Created { get; set; } = new Dictionary<string, int>();

        /// <summary> Each <c>ref</c> of an <c>audiotrack</c> to the audio id it created. </summary>
        [JsonProperty("createdAudio", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, int> CreatedAudio { get; set; }

        /// <summary> Each <c>ref</c> of a <c>prefab_pack</c> to the template id (a GUID) it created; the ref
        /// itself names the placement. </summary>
        [JsonProperty("createdPrefabs", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> CreatedPrefabs { get; set; }

        /// <summary> Clamps and legal oddities; never a failure. </summary>
        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
