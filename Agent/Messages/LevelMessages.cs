using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>level.list</c> request. </summary>
    public sealed class LevelListRequest : AgentPagedRequest
    {
        /// <summary> A name fragment, case-insensitive. </summary>
        [JsonProperty("filter", NullValueHandling = NullValueHandling.Ignore)]
        public string Filter { get; set; }
    }

    /// <summary> The <c>level.open</c> request. </summary>
    public sealed class LevelOpenRequest
    {
        /// <summary> The level id, as <c>level.list</c> prints it. </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    /// <summary> The <c>level.create</c> request; every member but the name has a default. </summary>
    public sealed class LevelCreateRequest
    {
        /// <summary> The level's name. </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary> A short description. </summary>
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        /// <summary> Frames per second. </summary>
        [JsonProperty("framerate", NullValueHandling = NullValueHandling.Ignore)]
        public int? Framerate { get; set; }

        /// <summary> A duration, as <c>TimeAddress</c> reads one: <c>"s:180"</c> or <c>"f:600"</c>.
        /// </summary>
        [JsonProperty("duration", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Duration { get; set; }
    }

    /// <summary> The answer of <c>level.open</c> and <c>level.create</c>. </summary>
    public sealed class LevelOpenResult : AgentResultBase
    {
        /// <summary> The level's id. </summary>
        [JsonProperty("levelId")]
        public string LevelId { get; set; }

        /// <summary> The level's name. </summary>
        [JsonProperty("levelName")]
        public string LevelName { get; set; }
    }

    /// <summary> The <c>level.save</c> answer. </summary>
    public sealed class LevelSaveResult : AgentResultBase
    {
        /// <summary> The save landed. </summary>
        [JsonProperty("saved")]
        public bool Saved { get; set; }
    }

    /// <summary> The <c>level.outline</c> request: the hierarchy, paged, one indented line per object.
    /// </summary>
    public sealed class LevelOutlineRequest : AgentPagedRequest
    {
        /// <summary> The object to start under; absent for the roots. </summary>
        [JsonProperty("parent", NullValueHandling = NullValueHandling.Ignore)]
        public int? Parent { get; set; }

        /// <summary> Levels of children to descend; absent for all. </summary>
        [JsonProperty("depth", NullValueHandling = NullValueHandling.Ignore)]
        public int? Depth { get; set; }

        /// <summary> Only this object type (<c>rect</c>, <c>shape</c>, <c>text</c>, <c>effect</c>,
        /// <c>prefab</c>). </summary>
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary> A name fragment, case-insensitive. </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary> Only objects playing at or after this time. </summary>
        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        public JToken From { get; set; }

        /// <summary> Only objects playing at or before this time. </summary>
        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)]
        public JToken To { get; set; }
    }
}
