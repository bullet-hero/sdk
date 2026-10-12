using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>view.capture</c> request. </summary>
    public sealed class ViewCaptureRequest
    {
        /// <summary> The frame to render, a <c>TimeAddress</c>. </summary>
        [JsonProperty("frame")]
        public JToken Frame { get; set; }

        /// <summary> The longest side in pixels, capped by the game's image limit; the level camera's aspect
        /// decides the other. </summary>
        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public int? Size { get; set; }

        /// <summary> A world rect to frame instead of the level camera's view, for a close look; the image takes
        /// its aspect. Nothing in the level changes. </summary>
        [JsonProperty("region", NullValueHandling = NullValueHandling.Ignore)]
        public AgentRegion Region { get; set; }
    }

    /// <summary> The <c>view.capture</c> answer: the frame as the player would see it, overlays hidden.
    /// </summary>
    public sealed class ViewCaptureResult : AgentImageResult
    {
        /// <summary> The global frame rendered. </summary>
        [JsonProperty("frame")]
        public int Frame { get; set; }

        /// <summary> Where the playhead is after the capture. </summary>
        [JsonProperty("playhead")]
        public int Playhead { get; set; }
    }

    /// <summary> The <c>view.filmstrip</c> request: either <c>frames</c>, or <c>from</c> and <c>to</c> with
    /// an optional <c>count</c>. </summary>
    public sealed class ViewFilmstripRequest
    {
        /// <summary> The first frame of a range, a <c>TimeAddress</c>. </summary>
        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        public JToken From { get; set; }

        /// <summary> The last frame of a range, a <c>TimeAddress</c>. </summary>
        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)]
        public JToken To { get; set; }

        /// <summary> Frames spread evenly over the range, both ends included; see
        /// <see cref="AgentRules.DefaultFilmstripFrames"/>. </summary>
        [JsonProperty("count", NullValueHandling = NullValueHandling.Ignore)]
        public int? Count { get; set; }

        /// <summary> Exact frames, <c>TimeAddress</c>es, in the order to show them. </summary>
        [JsonProperty("frames", NullValueHandling = NullValueHandling.Ignore)]
        public List<JToken> Frames { get; set; }

        /// <summary> The longest side of the whole sheet in pixels, capped by the game's image limit.
        /// </summary>
        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public int? Size { get; set; }
    }

    /// <summary> The <c>view.filmstrip</c> answer: one sheet, a cell per frame, its frame written under it.
    /// </summary>
    public sealed class ViewFilmstripResult : AgentImageResult
    {
        /// <summary> The global frames rendered, in reading order. </summary>
        [JsonProperty("frames")]
        public List<int> Frames { get; set; } = new List<int>();

        /// <summary> Cells per row. </summary>
        [JsonProperty("columns")]
        public int Columns { get; set; }

        /// <summary> Where the playhead is after the strip. </summary>
        [JsonProperty("playhead")]
        public int Playhead { get; set; }

        /// <summary> Why the frames differ from the request; absent when they do not. </summary>
        [JsonProperty("note", NullValueHandling = NullValueHandling.Ignore)]
        public string Note { get; set; }
    }

    /// <summary> The <c>playhead.set</c> request. </summary>
    public sealed class PlayheadSetRequest
    {
        /// <summary> The frame to move to, a <c>TimeAddress</c>. </summary>
        [JsonProperty("frame")]
        public JToken Frame { get; set; }
    }

    /// <summary> The <c>playback.set</c> request. </summary>
    public sealed class PlaybackSetRequest
    {
        /// <summary> True to play, false to pause. </summary>
        [JsonProperty("playing")]
        public bool Playing { get; set; }

        /// <summary> Where to start or stop, a <c>TimeAddress</c>; absent to stay where the playhead is.
        /// </summary>
        [JsonProperty("frame")]
        public JToken Frame { get; set; }
    }

    /// <summary> The <c>playhead.get</c>, <c>playhead.set</c> and <c>playback.set</c> answer. </summary>
    public sealed class PlayheadResult : AgentResultBase
    {
        /// <summary> The global frame under the playhead. </summary>
        [JsonProperty("frame")]
        public int Frame { get; set; }

        /// <summary> Whether the level is playing. </summary>
        [JsonProperty("playing")]
        public bool Playing { get; set; }

        /// <summary> Whether the author is busy: a capture or a move now would be refused. </summary>
        [JsonProperty("authorBusy")]
        public bool AuthorBusy { get; set; }
    }
}
