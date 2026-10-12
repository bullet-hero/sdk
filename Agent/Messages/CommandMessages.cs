using Newtonsoft.Json;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>command.list</c> request. </summary>
    public sealed class CommandListRequest : AgentPagedRequest
    {
        /// <summary> True to list the excluded commands too, each with the tool to use instead. </summary>
        [JsonProperty("all", NullValueHandling = NullValueHandling.Ignore)]
        public bool? All { get; set; }
    }

    /// <summary> The <c>command.run</c> request. </summary>
    public sealed class CommandRunRequest
    {
        /// <summary> The revision of the read this write is based on. </summary>
        [JsonProperty("expectedRevision")]
        public int ExpectedRevision { get; set; }

        /// <summary> A command id that <c>command.list</c> marks <c>allowed</c>. </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    /// <summary> The <c>command.run</c> answer. </summary>
    public sealed class CommandRunResult : AgentResultBase
    {
        /// <summary> The command that ran. </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary> False when the command ran and changed nothing (an empty selection, say). </summary>
        [JsonProperty("changed")]
        public bool Changed { get; set; }
    }
}
