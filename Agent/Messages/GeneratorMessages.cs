using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>generator.schema</c> request. </summary>
    public sealed class GeneratorSchemaRequest
    {
        /// <summary> A generator id, the first word of a <c>generator.list</c> line. </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    /// <summary> The <c>generator.schema</c> answer. </summary>
    public sealed class GeneratorSchemaResult : AgentResultBase
    {
        /// <summary> The generator id. </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary> What the generator is for, one sentence. </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary> The parameters: JSON schema with <c>x-</c> extras (section, unit, step, visibility). </summary>
        [JsonProperty("schema")]
        public JObject Schema { get; set; }
    }

    /// <summary> The <c>generator.run</c> request: one generator, one undo step. </summary>
    public sealed class GeneratorRunRequest
    {
        /// <summary> The revision of the read this run is based on. </summary>
        [JsonProperty("expectedRevision")]
        public int ExpectedRevision { get; set; }

        /// <summary> A generator id from <c>generator.list</c>. </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary> Parameter values by field name; every field left out keeps its default. </summary>
        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Parameters { get; set; }

        /// <summary> The first frame of the window the run fills; a time address. </summary>
        [JsonProperty("start")]
        public JToken Start { get; set; }

        /// <summary> The first frame AFTER the window; a time address. </summary>
        [JsonProperty("end")]
        public JToken End { get; set; }

        /// <summary> An object the run hangs its content under; absent for the scope root. </summary>
        [JsonProperty("parent", NullValueHandling = NullValueHandling.Ignore)]
        public int? Parent { get; set; }

        /// <summary> The effective layer the run starts at; the editor's default when absent. </summary>
        [JsonProperty("layer", NullValueHandling = NullValueHandling.Ignore)]
        public int? Layer { get; set; }

        /// <summary> The random seed; zero when absent, so a run repeats exactly. </summary>
        [JsonProperty("seed", NullValueHandling = NullValueHandling.Ignore)]
        public long? Seed { get; set; }

        /// <summary> The objects a modifier works on. A generator that needs a selection refuses without
        /// them. </summary>
        [JsonProperty("selection", NullValueHandling = NullValueHandling.Ignore)]
        public List<int> Selection { get; set; }

        /// <summary> Wraps everything a content generator creates in one object of this name. </summary>
        [JsonProperty("groupName", NullValueHandling = NullValueHandling.Ignore)]
        public string GroupName { get; set; }

        /// <summary> Gives every object a content generator creates its own layer. </summary>
        [JsonProperty("splitLayers", NullValueHandling = NullValueHandling.Ignore)]
        public bool? SplitLayers { get; set; }

        /// <summary> Allows a configuration the generator calls dangerous (it rewrites or deletes content).
        /// </summary>
        [JsonProperty("confirmDangerous", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ConfirmDangerous { get; set; }

        /// <summary> What the History tab shows beside the run. </summary>
        [JsonProperty("caption", NullValueHandling = NullValueHandling.Ignore)]
        public string Caption { get; set; }
    }

    /// <summary> The <c>generator.run</c> answer. </summary>
    public sealed class GeneratorRunResult : AgentResultBase
    {
        /// <summary> The generator that ran. </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary> How many objects the run created. </summary>
        [JsonProperty("createdCount")]
        public int CreatedCount { get; set; }

        /// <summary> The ids of those objects as runs, such as <c>41-104</c> or <c>106</c>. </summary>
        [JsonProperty("created")]
        public List<string> Created { get; set; } = new List<string>();

        /// <summary> Clamps and ignored values; never a failure. </summary>
        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
