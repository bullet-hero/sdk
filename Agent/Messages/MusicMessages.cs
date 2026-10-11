using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>music.analyze</c> request. </summary>
    public sealed class MusicAnalyzeRequest
    {
        /// <summary> The audio resource id; absent for the resource of the level's first audio track.
        /// </summary>
        [JsonProperty("resource", NullValueHandling = NullValueHandling.Ignore)]
        public int? Resource { get; set; }
    }

    /// <summary> One section the analysis found, in level frames. </summary>
    public sealed class MusicSection
    {
        /// <summary> Similarity letter: equal letters sound alike, <c>A'</c> is a variation of <c>A</c>.
        /// </summary>
        [JsonProperty("letter")]
        public string Letter { get; set; }

        /// <summary> <c>low</c>, <c>mid</c> or <c>high</c> against the loudest section. </summary>
        [JsonProperty("energy")]
        public string Energy { get; set; }

        /// <summary> First frame. </summary>
        [JsonProperty("start")]
        public int Start { get; set; }

        /// <summary> Frames it covers. </summary>
        [JsonProperty("duration")]
        public int Duration { get; set; }

        /// <summary> <c>bar.beat</c> of the first frame on the level's beat grid; absent off the grid.
        /// </summary>
        [JsonProperty("bar", NullValueHandling = NullValueHandling.Ignore)]
        public string Bar { get; set; }

        /// <summary> Mean band loudness, low/mid/high, each 0-9. </summary>
        [JsonProperty("bands")]
        public string Bands { get; set; }
    }

    /// <summary> The <c>music.analyze</c> answer: a summary, never the raw envelopes or onsets. </summary>
    public sealed class MusicAnalyzeResult : AgentResultBase
    {
        /// <summary> The audio resource analysed. </summary>
        [JsonProperty("resource")]
        public int Resource { get; set; }

        /// <summary> How clip seconds became level frames: the track used, or why none was. </summary>
        [JsonProperty("placement")]
        public string Placement { get; set; }

        /// <summary> Length of the clip, seconds. </summary>
        [JsonProperty("durationSeconds")]
        public double DurationSeconds { get; set; }

        /// <summary> Tempo as the level plays it (the clip's tempo times the track speed); 0 when none was
        /// found. </summary>
        [JsonProperty("bpm")]
        public double Bpm { get; set; }

        /// <summary> The frame of the first beat. </summary>
        [JsonProperty("phaseFrame")]
        public int PhaseFrame { get; set; }

        /// <summary> 0-1: how far the tempo stands above the others tried. </summary>
        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        /// <summary> 0-1: how close half or double the tempo came; high means the bpm may be off by 2x.
        /// </summary>
        [JsonProperty("ambiguity")]
        public double Ambiguity { get; set; }

        /// <summary> How far the beat slides between the first and last third, milliseconds. </summary>
        [JsonProperty("driftMs")]
        public double DriftMs { get; set; }

        /// <summary> Beats per bar the downbeat was searched with. </summary>
        [JsonProperty("beatsPerBar")]
        public int BeatsPerBar { get; set; }

        /// <summary> The frame of the first downbeat. </summary>
        [JsonProperty("downbeatFrame")]
        public int DownbeatFrame { get; set; }

        /// <summary> 0-1: how far the downbeat stands above the next candidate. </summary>
        [JsonProperty("downbeatConfidence")]
        public double DownbeatConfidence { get; set; }

        /// <summary> Sections in time order. </summary>
        [JsonProperty("sections")]
        public List<MusicSection> Sections { get; set; } = new List<MusicSection>();

        /// <summary> Onsets per band: <c>low</c>, <c>mid</c>, <c>high</c>. </summary>
        [JsonProperty("onsets")]
        public Dictionary<string, int> Onsets { get; set; } = new Dictionary<string, int>();

        /// <summary> What to doubt and why. </summary>
        [JsonProperty("notes")]
        public List<string> Notes { get; set; } = new List<string>();

        /// <summary> Whether the analysis came from the cache. </summary>
        [JsonProperty("cached")]
        public bool Cached { get; set; }
    }

    /// <summary> The <c>music.table</c> request: one row per bar or beat between two times. </summary>
    public sealed class MusicTableRequest : AgentPagedRequest
    {
        /// <summary> The first time, a <c>TimeAddress</c>; the level's first frame when absent. </summary>
        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        public JToken From { get; set; }

        /// <summary> The last time, a <c>TimeAddress</c>; the level's last frame when absent. </summary>
        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)]
        public JToken To { get; set; }

        /// <summary> <c>bar</c> (default) or <c>beat</c>. </summary>
        [JsonProperty("by", NullValueHandling = NullValueHandling.Ignore)]
        public string By { get; set; }

        /// <summary> The audio resource id; absent for the level's first audio track. </summary>
        [JsonProperty("resource", NullValueHandling = NullValueHandling.Ignore)]
        public int? Resource { get; set; }
    }

    /// <summary> The <c>music.table</c> answer: paged rows and what their columns mean. </summary>
    public sealed class MusicTableResult : AgentPagedResult
    {
        /// <summary> <c>level</c> when rows follow the level's beat grid, <c>analysis</c> when they follow
        /// the analysed tempo because the level has no grid there. </summary>
        [JsonProperty("grid")]
        public string Grid { get; set; }

        /// <summary> What each column of a row means. </summary>
        [JsonProperty("legend")]
        public string Legend { get; set; }
    }

    /// <summary> The <c>music.strip</c> request. </summary>
    public sealed class MusicStripRequest
    {
        /// <summary> The first time, a <c>TimeAddress</c>; the level's first frame when absent. </summary>
        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        public JToken From { get; set; }

        /// <summary> The last time, a <c>TimeAddress</c>; the level's last frame when absent. </summary>
        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)]
        public JToken To { get; set; }

        /// <summary> Width in pixels, capped by the game's image limit. </summary>
        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public int? Size { get; set; }

        /// <summary> The audio resource id; absent for the level's first audio track. </summary>
        [JsonProperty("resource", NullValueHandling = NullValueHandling.Ignore)]
        public int? Resource { get; set; }
    }

    /// <summary> The <c>music.strip</c> answer. </summary>
    public sealed class MusicStripResult : AgentImageResult
    {
        /// <summary> The first frame drawn. </summary>
        [JsonProperty("from")]
        public int From { get; set; }

        /// <summary> The last frame drawn. </summary>
        [JsonProperty("to")]
        public int To { get; set; }
    }

    /// <summary> The <c>music.resolve</c> request. </summary>
    public sealed class MusicResolveRequest
    {
        /// <summary> Any <c>TimeAddress</c>: a frame, <c>s:</c>, <c>bar:</c>, <c>beat:</c>, <c>marker:</c>.
        /// </summary>
        [JsonProperty("time")]
        public JToken Time { get; set; }
    }

    /// <summary> The <c>music.resolve</c> answer. </summary>
    public sealed class MusicResolveResult : AgentResultBase
    {
        /// <summary> The global frame. </summary>
        [JsonProperty("frame")]
        public int Frame { get; set; }

        /// <summary> Seconds from the level's start to the start of that frame. </summary>
        [JsonProperty("seconds")]
        public double Seconds { get; set; }

        /// <summary> <c>bar.beat</c> on the level's beat grid (<c>17.3+0.5</c> between beats); absent off the
        /// grid. </summary>
        [JsonProperty("bar", NullValueHandling = NullValueHandling.Ignore)]
        public string Bar { get; set; }

        /// <summary> The global beat number (<c>beat:</c> spells it); absent off the grid. </summary>
        [JsonProperty("beat", NullValueHandling = NullValueHandling.Ignore)]
        public double? Beat { get; set; }

        /// <summary> Why there is no bar; absent when there is one. </summary>
        [JsonProperty("note", NullValueHandling = NullValueHandling.Ignore)]
        public string Note { get; set; }
    }

    /// <summary> The <c>music.check</c> request. </summary>
    public sealed class MusicCheckRequest : AgentPagedRequest
    {
        /// <summary> Frames a start may sit from the nearest beat; see
        /// <see cref="AgentRules.DefaultBeatTolerance"/>. </summary>
        [JsonProperty("tolerance", NullValueHandling = NullValueHandling.Ignore)]
        public int? Tolerance { get; set; }

        /// <summary> The first time checked, a <c>TimeAddress</c>; the level's start when absent. </summary>
        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        public JToken From { get; set; }

        /// <summary> The last time checked, a <c>TimeAddress</c>; the level's end when absent. </summary>
        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)]
        public JToken To { get; set; }
    }

    /// <summary> The <c>music.check</c> answer: one line per frame off the grid. </summary>
    public sealed class MusicCheckResult : AgentPagedResult
    {
        /// <summary> The tolerance used, frames. </summary>
        [JsonProperty("tolerance")]
        public int Tolerance { get; set; }

        /// <summary> Frames checked (object starts and keyframes inside the grid and the range). </summary>
        [JsonProperty("checked")]
        public int Checked { get; set; }

        /// <summary> Frames in the range that no beat segment covers, so they were not checked. </summary>
        [JsonProperty("outsideGrid")]
        public int OutsideGrid { get; set; }
    }

    /// <summary> The <c>music.play</c> request. </summary>
    public sealed class MusicPlayRequest
    {
        /// <summary> Where playback starts, a <c>TimeAddress</c>. </summary>
        [JsonProperty("from")]
        public JToken From { get; set; }

        /// <summary> Where it stops, a <c>TimeAddress</c> (that frame is the last one played). </summary>
        [JsonProperty("to")]
        public JToken To { get; set; }
    }

    /// <summary> The <c>music.play</c> answer, sent once the range has played. </summary>
    public sealed class MusicPlayResult : AgentResultBase
    {
        /// <summary> The first frame played. </summary>
        [JsonProperty("from")]
        public int From { get; set; }

        /// <summary> The last frame played. </summary>
        [JsonProperty("to")]
        public int To { get; set; }

        /// <summary> Wall time the playback took, seconds. </summary>
        [JsonProperty("seconds")]
        public double Seconds { get; set; }

        /// <summary> Where the playhead is now. </summary>
        [JsonProperty("playhead")]
        public int Playhead { get; set; }

        /// <summary> What happened to the playhead and the sound, in one sentence. </summary>
        [JsonProperty("note")]
        public string Note { get; set; }
    }

    /// <summary> One beat segment as <c>beatgrid.propose</c> suggests it and <c>beatgrid.apply</c> takes it.
    /// </summary>
    public sealed class BeatgridSegment
    {
        /// <summary> First frame, a <c>TimeAddress</c> (not a musical one: the grid is what is being made).
        /// </summary>
        [JsonProperty("start")]
        public JToken Start { get; set; }

        /// <summary> Frames covered, a <c>TimeAddress</c> duration. </summary>
        [JsonProperty("duration")]
        public JToken Duration { get; set; }

        /// <summary> Tempo as the level plays it. </summary>
        [JsonProperty("bpm")]
        public float Bpm { get; set; }

        /// <summary> Frames from <c>start</c> to beat 0, which is a downbeat; may be negative (a pickup).
        /// </summary>
        [JsonProperty("offset")]
        public float Offset { get; set; }

        /// <summary> Beats in a bar. </summary>
        [JsonProperty("beatsPerBar")]
        public int BeatsPerBar { get; set; }
    }

    /// <summary> The <c>beatgrid.propose</c> request. </summary>
    public sealed class BeatgridProposeRequest
    {
        /// <summary> The audio resource id; absent for the level's first audio track. </summary>
        [JsonProperty("resource", NullValueHandling = NullValueHandling.Ignore)]
        public int? Resource { get; set; }

        /// <summary> Where the grid starts, a frame or seconds; the track's start when absent. </summary>
        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        public JToken From { get; set; }

        /// <summary> Its last frame; the end of the track or the level, whichever is first, when absent.
        /// </summary>
        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)]
        public JToken To { get; set; }
    }

    /// <summary> The <c>beatgrid.propose</c> answer: segments ready for <c>beatgrid.apply</c>. </summary>
    public sealed class BeatgridProposeResult : AgentResultBase
    {
        /// <summary> The suggested segments; empty when no tempo was found. </summary>
        [JsonProperty("segments")]
        public List<BeatgridSegment> Segments { get; set; } = new List<BeatgridSegment>();

        /// <summary> The tempo fit's confidence, 0-1. </summary>
        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        /// <summary> What to doubt, and which existing segments an apply would cut or remove. </summary>
        [JsonProperty("notes")]
        public List<string> Notes { get; set; } = new List<string>();
    }

    /// <summary> The <c>beatgrid.apply</c> request: one undo step. </summary>
    public sealed class BeatgridApplyRequest
    {
        /// <summary> The revision the caller last read; anything else is <c>stale</c>. </summary>
        [JsonProperty("expectedRevision")]
        public int ExpectedRevision { get; set; }

        /// <summary> The segments to write, not overlapping one another. Existing segments they overlap are
        /// trimmed, split or removed first. </summary>
        [JsonProperty("segments")]
        public List<BeatgridSegment> Segments { get; set; }

        /// <summary> What the History tab shows. </summary>
        [JsonProperty("caption", NullValueHandling = NullValueHandling.Ignore)]
        public string Caption { get; set; }
    }

    /// <summary> The <c>beatgrid.apply</c> answer: the level's segments as they now are. </summary>
    public sealed class BeatgridApplyResult : AgentResultBase
    {
        /// <summary> Every beat segment of the level after the edit, one line each. </summary>
        [JsonProperty("lines")]
        public List<string> Lines { get; set; } = new List<string>();

        /// <summary> What was cut or removed to make room, and anything clamped. </summary>
        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
