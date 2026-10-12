using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Agent.Messages
{
    /// <summary> Who made a change; the values of <see cref="AgentChange.Source"/>. </summary>
    public static class AgentChangeSource
    {
        /// <summary> A batch the bridge itself executed. </summary>
        public const string Ai = "ai";

        /// <summary> Anything else: the author's own edit, undo or redo. </summary>
        public const string Author = "author";
    }

    /// <summary> One entry of the session's change log. </summary>
    public sealed class AgentChange
    {
        /// <summary> The revision this change produced. </summary>
        [JsonProperty("revision")]
        public int Revision { get; set; }

        /// <summary> <see cref="AgentChangeSource.Ai"/> or <see cref="AgentChangeSource.Author"/>. </summary>
        [JsonProperty("source")]
        public string Source { get; set; }

        /// <summary> The localization key of the operation's name. </summary>
        [JsonProperty("nameKey", NullValueHandling = NullValueHandling.Ignore)]
        public string NameKey { get; set; }

        /// <summary> The operation's name in English, as the History tab words it. </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary> The caption of an AI batch. </summary>
        [JsonProperty("caption", NullValueHandling = NullValueHandling.Ignore)]
        public string Caption { get; set; }

        /// <summary> True when this change took the step back: the entry names the node it left. </summary>
        [JsonProperty("undo", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Undo { get; set; }
    }

    /// <summary> The <c>changes.since</c> request. </summary>
    public sealed class ChangesSinceRequest
    {
        /// <summary> The last revision the caller has seen. </summary>
        [JsonProperty("revision")]
        public int Revision { get; set; }
    }

    /// <summary> The <c>changes.since</c> answer. </summary>
    public sealed class ChangesSinceResult : AgentResultBase
    {
        /// <summary> Changes after the requested revision, oldest first. </summary>
        [JsonProperty("changes")]
        public List<AgentChange> Changes { get; set; } = new List<AgentChange>();

        /// <summary> The log no longer reaches back that far; re-read what matters. </summary>
        [JsonProperty("truncated")]
        public bool Truncated { get; set; }
    }

    /// <summary> The <c>history.get</c> request. </summary>
    public sealed class HistoryGetRequest : AgentPagedRequest
    {
    }

    /// <summary> The <c>history.undo</c> and <c>history.redo</c> request. </summary>
    public sealed class HistoryStepRequest
    {
        /// <summary> The revision the step is based on; absent to step from whatever the level is at.
        /// </summary>
        [JsonProperty("expectedRevision", NullValueHandling = NullValueHandling.Ignore)]
        public int? ExpectedRevision { get; set; }
    }

    /// <summary> The <c>history.undo</c> and <c>history.redo</c> answer. </summary>
    public sealed class HistoryStepResult : AgentResultBase
    {
        /// <summary> A step was undone or redone; false when there was nothing to do. </summary>
        [JsonProperty("applied")]
        public bool Applied { get; set; }

        /// <summary> The step, as <c>history.get</c> words it: <c>#41 ai op_group_ai-batch "Chorus 1"</c>.
        /// </summary>
        [JsonProperty("step", NullValueHandling = NullValueHandling.Ignore)]
        public string Step { get; set; }
    }

    /// <summary> The <c>selection.get</c> and <c>selection.set</c> answer. </summary>
    public sealed class SelectionResult : AgentResultBase
    {
        /// <summary> The selected object ids. </summary>
        [JsonProperty("ids")]
        public List<int> Ids { get; set; } = new List<int>();
    }

    /// <summary> The <c>selection.set</c> request. </summary>
    public sealed class SelectionSetRequest
    {
        /// <summary> The objects to select; empty clears the selection. </summary>
        [JsonProperty("ids")]
        public List<int> Ids { get; set; } = new List<int>();
    }
}