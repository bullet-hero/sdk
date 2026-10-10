using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Agent.Messages
{
    /// <summary> A short sentence attached to the result that triggered it. </summary>
    public sealed class AgentHint
    {
        /// <summary> One of <see cref="AgentHintKey"/>; the server suppresses a repeat by it. </summary>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary> The sentence itself. </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    /// <summary> What every result carries: where the level stands, and optionally a few hints. </summary>
    public class AgentResultBase
    {
        /// <summary> The session's revision at the moment of the read or write. </summary>
        [JsonProperty("revision")]
        public int Revision { get; set; }

        /// <summary> A new value for every opened level. </summary>
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        /// <summary> <c>level</c> or <c>prefab:&lt;id&gt;</c>. </summary>
        [JsonProperty("scope")]
        public string Scope { get; set; }

        /// <summary> Just-in-time context; absent when there is none. </summary>
        [JsonProperty("hints", NullValueHandling = NullValueHandling.Ignore)]
        public List<AgentHint> Hints { get; set; }
    }

    /// <summary> The paging every list request shares; both default to <see cref="AgentRules"/>. </summary>
    public class AgentPagedRequest
    {
        /// <summary> Items to skip. </summary>
        [JsonProperty("offset", NullValueHandling = NullValueHandling.Ignore)]
        public int? Offset { get; set; }

        /// <summary> Items to return, at most <see cref="AgentRules.MaxPageSize"/>. </summary>
        [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore)]
        public int? Limit { get; set; }
    }

    /// <summary> A list result: one text line per item. </summary>
    public class AgentPagedResult : AgentResultBase
    {
        /// <summary> Items matching, before paging. </summary>
        [JsonProperty("total")]
        public int Total { get; set; }

        /// <summary> Items skipped. </summary>
        [JsonProperty("offset")]
        public int Offset { get; set; }

        /// <summary> The page, one line per item. </summary>
        [JsonProperty("lines")]
        public List<string> Lines { get; set; } = new List<string>();
    }
}
