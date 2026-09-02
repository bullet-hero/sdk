using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Events;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Game
{
    /// <summary>
    /// Level-global event tracks that belong to no single object: annotations, respawn points, screen
    /// framing, background color and the active theme. Mixed in kind on purpose - Markers/Checkpoints
    /// are one-shot flat lists, ScreenLimits/Backgrounds/Themes are real interpolated keyframe tracks.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.GameEvents, 1, 0)]
    [GenerateModel]
    public sealed partial class GameEvents : IModel<GameEvents>
    {
        /// <summary> Named timeline bookmarks for the mapper. Zero gameplay effect - the player
        /// deserializes them and ignores them. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxMarkerEvents)]
        [RuleCollectionUnique(nameof(Marker.Frame))]
        [JsonProperty(Names.Markers)]
        public List<Marker> Markers { get; set; }

        /// <summary> Where the beat grid exists and at what tempo - one entry per stretch of constant
        /// tempo, never overlapping (LevelGraphAnalyzer's GraphRule.BeatSegmentsOverlap). Editor-only
        /// like Markers: generators and timeline snapping read it, playback never does. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxBeatEvents)]
        [JsonProperty(Names.Beats)]
        public List<BeatSegment> Beats { get; set; }

        /// <summary> Frames a death rewinds playback to. Unlike Markers these are real gameplay
        /// state - one-shot points, not an animated track. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxCheckpointEvents)]
        [RuleCollectionUnique(nameof(Checkpoint.Frame))]
        [JsonProperty(Names.Checkpoints)]
        public List<Checkpoint> Checkpoints { get; set; }

        /// <summary> How the visible area is constrained over time (None / fixed aspect / aspect
        /// bounds), so a level authored for one aspect ratio stays playable on any device
        /// (limitations for screen will be chosen by mappers) </summary>
        [RuleNotNull(typeof(ScreenLimitFixed)), RuleCollectionMaxCount(LevelRules.MaxScreenLimitEvents)]
        [RuleCollectionUnique(nameof(ScreenLimitKey.Frame))]
        [JsonProperty(Names.ScreenLimits)]
        public List<ScreenLimitKey> ScreenLimits { get; set; }

        /// <summary> Camera clear color over time. RGB only (no alpha) and themeable, so a background
        /// can follow the active ThemeData instead of a literal color. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxBackgroundEvents)]
        [RuleCollectionUnique(nameof(Color3Key.Frame))]
        [JsonProperty(Names.Backgrounds)]
        public List<Color3Key> Backgrounds { get; set; }

        /// <summary> Which ThemeData is active over time - the outer half of the theme indirection
        /// (this picks the palette, a ColorType.ThemeRef picks a slot inside it). </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxThemeEvents)]
        [RuleCollectionUnique(nameof(ThemeKeyframe.Frame))]
        [JsonProperty(Names.Themes)]
        public List<ThemeKeyframe> Themes { get; set; }

        public GameEvents()
        {
            Markers = new List<Marker>();
            Beats = new List<BeatSegment>();
            Checkpoints = new List<Checkpoint>();
            ScreenLimits = new List<ScreenLimitKey>();
            Backgrounds = new List<Color3Key>();
            Themes = new List<ThemeKeyframe>();
        }
        public GameEvents(List<Marker> markers, List<BeatSegment> beats, List<Checkpoint> checkpoints,
            List<ScreenLimitKey> screenLimits, List<Color3Key> backgrounds, List<ThemeKeyframe> themes)
        {
            Markers = markers;
            Beats = beats;
            Checkpoints = checkpoints;
            ScreenLimits = screenLimits;
            Backgrounds = backgrounds;
            Themes = themes;
        }
    }
}