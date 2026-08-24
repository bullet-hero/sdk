using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Events
{
    // A span rather than a keyframe track, and that is the whole design decision. A track of tempo
    // points has no way to express a HOLE - an intro with no percussion, a break, the tail after the
    // song ends - because its last point reaches to the end of the level no matter what. A list of
    // spans says "here the grid exists, at this tempo" and stays silent everywhere else.
    //
    // Segments must not overlap, and that is a relational invariant no property attribute can see
    // (an attribute is handed one property at a time), so it lives in LevelGraphAnalyzer as
    // GraphRule.BeatSegmentsOverlap. Like every graph finding it carries no repair: clipping the
    // left one and moving the right one are both content decisions the author has to make.
    //
    // Editor-only, exactly like Marker: it is saved, read back and used by generators/snapping, and
    // playback never looks at it.

    /// <summary>
    /// One stretch of constant tempo on the level timeline: where the beat grid exists, how fast it
    /// runs and where its phase sits.
    /// </summary>
    [RuleContainer]
    public class BeatSegment : IFrameBounds, INameable, IModel<BeatSegment>
    {
        // Anchors mean "this edge follows the parent span's edge" and a segment has no parent, so
        // the flags are stripped on the way in rather than validated after the fact. FrameSpan
        // carries them in its sign bits either way, and an anchored one would serialize as a
        // negative number nothing here would ever resolve.

        /// <summary> Half-open stretch of level frames the grid covers. Anchors are always None. </summary>
        [JsonProperty(Names.SpanShort)]
        public FrameSpan Span
        {
            get => _span;
            set => _span = value.WithAnchors(FrameAnchor.None);
        }
        private FrameSpan _span;

        /// <summary> Beats per minute. Converted to frames through the level's own framerate, so the
        /// same number spans a different frame count in a 30 fps and a 60 fps level - which is
        /// correct, a frame is a different length of time in each. </summary>
        [RuleInRange(LevelRules.MinBpm, LevelRules.MaxBpm)]
        [JsonProperty(Names.Bpm)]
        public float Bpm { get; set; }

        // Fractional on purpose. A song's first beat almost never lands on a frame boundary, and at
        // 30 fps a whole-frame phase is 33ms out - audible. Beat frames round on the way out (see
        // BeatMath), so nothing downstream has to deal with a fraction.

        /// <summary> Where the first beat sits inside the span, in frames from its start. </summary>
        [RuleInRange(LevelRules.MinBeatOffset, LevelRules.MaxBeatOffset)]
        [JsonProperty(Names.Offset)]
        public float Offset { get; set; }

        /// <summary> How many beats make one bar - which beats read as downbeats. Purely how the grid
        /// is grouped; it never changes where a beat falls. </summary>
        [RuleInRange(LevelRules.MinBeatsPerBar, LevelRules.MaxBeatsPerBar)]
        [JsonProperty(Names.BeatsPerBar)]
        public int BeatsPerBar { get; set; }

        /// <summary> Short label shown on the timeline ("drop", "build"). </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public string Name { get; set; }

        /// <summary> Segment color in the editor. Concrete Color4Value, not IColor4 - an editor
        /// annotation has nothing to do with the level's theme (same call as Marker.Color4). </summary>
        [RuleNotNull]
        [JsonProperty(Names.Color)]
        public Color4Value Color4 { get; set; }

        public BeatSegment()
        {
            Span = new FrameSpan();
            Bpm = LevelRules.DefaultBpm;
            Offset = 0f;
            BeatsPerBar = LevelRules.DefaultBeatsPerBar;
            Name = string.Empty;
            Color4 = new Color4Value();
        }
        public BeatSegment(FrameSpan span, float bpm, float offset, int beatsPerBar,
            string name, Color4Value color4)
        {
            Span = span;
            Bpm = bpm;
            Offset = offset;
            BeatsPerBar = beatsPerBar;
            Name = name;
            Color4 = color4;
        }
        public void Reset()
        {
            Span = new FrameSpan();
            Bpm = LevelRules.DefaultBpm;
            Offset = 0f;
            BeatsPerBar = LevelRules.DefaultBeatsPerBar;
            Name = string.Empty;
            Color4 = new Color4Value();
        }

        public object Clone() => Copy();
        public BeatSegment Copy() => new(Span, Bpm, Offset, BeatsPerBar, Name, Color4.Copy());

        public void Update(BeatSegment src)
        {
            Span = src.Span;
            Bpm = src.Bpm;
            Offset = src.Offset;
            BeatsPerBar = src.BeatsPerBar;
            Name = src.Name;
            Color4 = src.Color4.Copy();
        }

        public void Pull(BeatSegment src)
        {
            Span = src.Span;
            Bpm = src.Bpm;
            Offset = src.Offset;
            BeatsPerBar = src.BeatsPerBar;
            Name = src.Name;
            Color4.Pull(src.Color4);
        }

        public override bool Equals(object obj) => obj is BeatSegment value && Equals(value);
        public override int GetHashCode() =>
            HashCode.Combine(Span, Bpm, Offset, BeatsPerBar, Name, Color4);

        public bool Equals(BeatSegment other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Span.Equals(other.Span)
                         && Bpm.Equals(other.Bpm)
                         && Offset.Equals(other.Offset)
                         && BeatsPerBar.Equals(other.BeatsPerBar)
                         && Name.Equals(other.Name)
                         && Color4.Equals(other.Color4);
            return result;
        }
    }
}
