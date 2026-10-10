using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>validate</c> answer: a page of findings, one line each, and the totals by severity.
    /// </summary>
    public sealed class ValidateResult : AgentPagedResult
    {
        /// <summary> Findings that make the level unplayable. </summary>
        [JsonProperty("errors")]
        public int Errors { get; set; }

        /// <summary> Findings that play, but not as authored. </summary>
        [JsonProperty("warnings")]
        public int Warnings { get; set; }

        /// <summary> Findings that change nothing in playback. </summary>
        [JsonProperty("advice")]
        public int Advice { get; set; }
    }

    /// <summary> The <c>validate.fix</c> request. </summary>
    public sealed class ValidateFixRequest
    {
        /// <summary> The revision of the read this write is based on. </summary>
        [JsonProperty("expectedRevision")]
        public int ExpectedRevision { get; set; }

        /// <summary> Repair only findings of these rules (the rule name before the first space of a
        /// <c>validate</c> line); every repairable one when absent. </summary>
        [JsonProperty("rules", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Rules { get; set; }
    }

    /// <summary> The <c>validate.fix</c> answer. </summary>
    public sealed class ValidateFixResult : AgentResultBase
    {
        /// <summary> How many values the repair changed. </summary>
        [JsonProperty("fixedCount")]
        public int FixedCount { get; set; }

        /// <summary> One line per change: rule, path, old value, new value (capped). </summary>
        [JsonProperty("fixes")]
        public List<string> Fixes { get; set; } = new List<string>();

        /// <summary> Findings still standing after the repair. </summary>
        [JsonProperty("remaining")]
        public int Remaining { get; set; }
    }
}
