using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>object.query</c> request: objects by time, name, type or parent. </summary>
    public sealed class ObjectQueryRequest : AgentPagedRequest
    {
        /// <summary> Only objects playing at or after this time. </summary>
        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        public JToken From { get; set; }

        /// <summary> Only objects playing at or before this time. </summary>
        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)]
        public JToken To { get; set; }

        /// <summary> A name fragment, case-insensitive. </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary> Only this object type. </summary>
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary> Only direct children of this object. </summary>
        [JsonProperty("parent", NullValueHandling = NullValueHandling.Ignore)]
        public int? Parent { get; set; }
    }

    /// <summary> The <c>object.get</c> request. </summary>
    public sealed class ObjectGetRequest
    {
        /// <summary> The objects to read, at most <see cref="AgentRules.MaxPageSize"/>. </summary>
        [JsonProperty("ids")]
        public List<int> Ids { get; set; } = new List<int>();
    }

    /// <summary> The <c>object.get</c> answer: each object as readable JSON, keyframes summarised per track.
    /// </summary>
    public sealed class ObjectGetResult : AgentResultBase
    {
        /// <summary> One entry per found id, in request order. </summary>
        [JsonProperty("objects")]
        public List<JObject> Objects { get; set; } = new List<JObject>();

        /// <summary> Requested ids that do not exist. </summary>
        [JsonProperty("missing", NullValueHandling = NullValueHandling.Ignore)]
        public List<int> Missing { get; set; }
    }

    /// <summary> The <c>keys.get</c> request; paging is per track. </summary>
    public sealed class KeysGetRequest : AgentPagedRequest
    {
        /// <summary> The owning object. </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary> Tracks to read, such as <c>position</c>; absent for every track the object has.
        /// </summary>
        [JsonProperty("tracks", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Tracks { get; set; }

        /// <summary> Only keys at or after this global frame. </summary>
        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        public JToken From { get; set; }

        /// <summary> Only keys at or before this global frame. </summary>
        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)]
        public JToken To { get; set; }
    }

    /// <summary> One keyframe as the AI sees it. </summary>
    public sealed class AgentKey
    {
        /// <summary> The GLOBAL frame, counting from <c>FrameRules.MinFrame</c>. </summary>
        [JsonProperty("frame")]
        public int Frame { get; set; }

        /// <summary> The ease name. </summary>
        [JsonProperty("ease")]
        public string Ease { get; set; }

        /// <summary> The value, as <c>AgentValueCodec</c> writes it. </summary>
        [JsonProperty("value")]
        public JToken Value { get; set; }
    }

    /// <summary> The <c>keys.get</c> answer. </summary>
    public sealed class KeysGetResult : AgentResultBase
    {
        /// <summary> Track name to its keys in frame order. </summary>
        [JsonProperty("tracks")]
        public Dictionary<string, List<AgentKey>> Tracks { get; set; } = new Dictionary<string, List<AgentKey>>();

        /// <summary> Track name to the keys it holds in range, before paging. </summary>
        [JsonProperty("totals")]
        public Dictionary<string, int> Totals { get; set; } = new Dictionary<string, int>();
    }

    /// <summary> The <c>events.get</c> request; paging is per kind. </summary>
    public sealed class EventsGetRequest : AgentPagedRequest
    {
        /// <summary> Any of <c>markers</c>, <c>beats</c>, <c>checkpoints</c>, <c>audio</c>; absent for all
        /// four. </summary>
        [JsonProperty("kinds", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Kinds { get; set; }
    }

    /// <summary> The <c>events.get</c> answer: one line per event, grouped by kind. </summary>
    public sealed class EventsGetResult : AgentResultBase
    {
        /// <summary> Kind to its lines. </summary>
        [JsonProperty("events")]
        public Dictionary<string, List<string>> Events { get; set; } = new Dictionary<string, List<string>>();

        /// <summary> Kind to the events it holds, before paging. </summary>
        [JsonProperty("totals")]
        public Dictionary<string, int> Totals { get; set; } = new Dictionary<string, int>();
    }

    /// <summary> The <c>resources.list</c> request. </summary>
    public sealed class ResourcesListRequest : AgentPagedRequest
    {
        /// <summary> <c>textures</c>, <c>fonts</c>, <c>audios</c>, <c>prefabs</c>, <c>themes</c> or
        /// <c>effects</c>. </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }
    }

    /// <summary> The <c>shapes.find</c> request: by form, sector and thickness, or a name fragment.
    /// </summary>
    public sealed class ShapesFindRequest : AgentPagedRequest
    {
        /// <summary> The geometric form, as the shape catalog names it. </summary>
        [JsonProperty("form", NullValueHandling = NullValueHandling.Ignore)]
        public string Form { get; set; }

        /// <summary> The sector, as the shape catalog names it. </summary>
        [JsonProperty("sector", NullValueHandling = NullValueHandling.Ignore)]
        public string Sector { get; set; }

        /// <summary> The thickness, as the shape catalog names it. </summary>
        [JsonProperty("thickness", NullValueHandling = NullValueHandling.Ignore)]
        public string Thickness { get; set; }

        /// <summary> A name fragment, case-insensitive. </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }
}
