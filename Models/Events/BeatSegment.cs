using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Events
{
    // A span, not a keyframe track: a track of tempo points cannot express a HOLE (an intro, a break).
    // Non-overlap is LevelGraphAnalyzer's (GraphRule.BeatSegmentsOverlap). Editor-only, like Marker.

    /// <summary>
    /// One stretch of constant tempo on the level timeline: where the beat grid exists, how fast it
    /// runs and where its phase sits.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class BeatSegment : IFrameBounds, INameable, IModel<BeatSegment>
    {
        // A segment has no parent span, so the anchor flags are stripped on the way in: an anchored one
        // would serialize as a negative number nothing here resolves.

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
        [JsonProperty(Names.BPM)]
        public float BPM { get; set; }

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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public BeatSegment()
        {
            Span = new FrameSpan();
            BPM = LevelRules.DefaultBpm;
            Offset = 0f;
            BeatsPerBar = LevelRules.DefaultBeatsPerBar;
            Name = string.Empty;
            Color4 = new Color4Value();
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public BeatSegment(FrameSpan span, float bpm, float offset, int beatsPerBar,
            string name, Color4Value color4)
        {
            Span = span;
            BPM = bpm;
            Offset = offset;
            BeatsPerBar = beatsPerBar;
            Name = name;
            Color4 = color4;
        }
    }
}
