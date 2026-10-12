using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>raw.get</c> request. </summary>
    public sealed class RawGetRequest : AgentPagedRequest
    {
        /// <summary> A Raw path: <c>level</c> or <c>meta</c>, then <c>.key</c> members and <c>[key]</c> items,
        /// exactly as an earlier answer printed them. Empty lists the two roots. </summary>
        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }
    }

    /// <summary> The <c>raw.get</c> answer: one node and a page of its children. </summary>
    public sealed class RawGetResult : AgentResultBase
    {
        /// <summary> The node's own path. </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary> <c>Leaf</c>, <c>Object</c>, <c>List</c> or <c>Dictionary</c>. </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }

        /// <summary> The slot's declared C# type. </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary> The value in the file's own wire form - what <c>raw.set</c> takes back. Only a leaf
        /// carries it; a collection reports its count in <c>summary</c>. </summary>
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Value { get; set; }

        /// <summary> One line: the value, a count, or the object's type name. </summary>
        [JsonProperty("summary")]
        public string Summary { get; set; }

        /// <summary> How many children the node has. </summary>
        [JsonProperty("total")]
        public int Total { get; set; }

        /// <summary> The index of the first child listed. </summary>
        [JsonProperty("offset")]
        public int Offset { get; set; }

        /// <summary> One line per child: <c>path | C# name | kind | summary</c>. </summary>
        [JsonProperty("children")]
        public List<string> Children { get; set; } = new List<string>();
    }

    /// <summary> The <c>raw.set</c> request. </summary>
    public sealed class RawSetRequest
    {
        /// <summary> The revision of the read this write is based on. </summary>
        [JsonProperty("expectedRevision")]
        public int ExpectedRevision { get; set; }

        /// <summary> The node to replace, as <c>raw.get</c> printed it. </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary> The new value in the file's wire form (a number, a string, an array, an object). </summary>
        [JsonProperty("value")]
        public JToken Value { get; set; }
    }

    /// <summary> The <c>raw.set</c> answer. </summary>
    public sealed class RawSetResult : AgentResultBase
    {
        /// <summary> The node written. </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary> Findings the edit introduced: same rule and target absent before it. </summary>
        [JsonProperty("newIssueCount")]
        public int NewIssueCount { get; set; }

        /// <summary> One line per new finding, as <c>validate</c> prints them (capped). </summary>
        [JsonProperty("newIssues")]
        public List<string> NewIssues { get; set; } = new List<string>();

        /// <summary> The leaf as the file now holds it, read back after the write; absent for a node.
        /// </summary>
        [JsonProperty("stored", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Stored { get; set; }

        /// <summary> Things the edit left for the author to know; never a failure. </summary>
        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();
    }
}