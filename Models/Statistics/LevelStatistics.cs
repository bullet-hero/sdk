using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // stats/<LevelId>.json, its own root: NOT a field of the level (progress would arrive already won), keyed
    // by LevelId, every timestamp UTC. Docs/STATISTICS.md

    /// <summary> Everything one player has done with one level. </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.LevelStatistics, ModelGenerations.V2_SimplifyEntrance)]
    [GenerateModel]
    public sealed partial class LevelStatistics : IModel<LevelStatistics>
    {
        #region Identity and time

        /// <summary> The level this describes. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.LevelId)]
        public LevelId LevelId { get; set; }

        // A SNAPSHOT, refreshed on every run, never resolved from the level. This file outlives the
        // level folder, and a profile screen listing an entry it cannot name is worse than one
        // showing a title that has since been edited.

        /// <summary> The name the level had when it was last played. </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(StatisticsRules.MaxLevelNameLength)]
        [JsonProperty(Names.Name)]
        public IString LevelName { get; set; }

        /// <summary> The version the level had when it was last played. </summary>
        [RuleNotNull(1, 0)]
        [JsonProperty(Names.Version)]
        public Version LevelVersion { get; set; }

        /// <summary> When this level was first played. </summary>
        [JsonProperty(Names.FirstPlayedUtc)]
        public DateTime FirstPlayedUtc { get; set; }

        /// <summary> When it was last played. </summary>
        [JsonProperty(Names.LastPlayedUtc)]
        public DateTime LastPlayedUtc { get; set; }

        // REAL seconds on the wall clock, not the level clock (bent by launch speed and the checkpoint ramp).

        /// <summary> Real seconds spent playing this level. </summary>
        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.RealSeconds)]
        public double TotalRealSeconds { get; set; }

        /// <summary> How many times the game screen was entered for this level - visits, not
        /// attempts. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Sessions)]
        public int SessionCount { get; set; }

        #endregion

        #region Attempts

        /// <summary> Runs started. Every restart is a new one. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Attempts)]
        public int Attempts { get; set; }

        /// <summary> Runs that reached the end of the level alive. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Clears)]
        public int Clears { get; set; }

        // NOT DERIVED FROM THE OUTCOME OF A RUN, because a lost run does not have to end: with the
        // default settings a death rewinds the level to its last checkpoint and play continues, so
        // one attempt legitimately contains many deaths.

        /// <summary> Deaths, across every run. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Deaths)]
        public int Deaths { get; set; }

        /// <summary> Hits taken, across every run. A death costs the last of them. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Hits)]
        public int Hits { get; set; }

        /// <summary> Dashes spent, across every run. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Dashes)]
        public int Dashes { get; set; }

        /// <summary> Times a run resumed from a checkpoint, by rewind or by the button. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.CheckpointRestarts)]
        public int CheckpointRestarts { get; set; }

        /// <summary> Runs abandoned - left or restarted before the level ended. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Quits)]
        public int Quits { get; set; }

        #endregion

        #region Progress

        /// <summary> The furthest frame ever reached. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.BestFrame)]
        public int BestFrame { get; set; }

        /// <summary> The same, as a fraction of the level. </summary>
        [RuleInRange(0f, 1f)]
        [JsonProperty(Names.BestProgress)]
        public float BestProgress { get; set; }

        /// <summary> When the level was first cleared. Default means never. </summary>
        [JsonProperty(Names.FirstClearUtc)]
        public DateTime FirstClearUtc { get; set; }

        #endregion

        // ONE RECORD PER SET OF CONDITIONS - see RunProfile. Three lives at half speed with a bot is
        // not the same achievement as one life at double speed, and a single "best" over all of them
        // would be meaningless in exactly the cases a player cares about.

        /// <summary> The best run under each set of launch conditions. </summary>
        [GenerateModelMerge]
        [RuleNotNull, RuleCollectionMaxCount(StatisticsRules.MaxRecordProfiles)]
        [JsonProperty(Names.Records)]
        public Dictionary<RunProfile, BestRun> Records { get; set; }

        /// <summary> Where in the level the player loses - the author-facing half. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Difficulty)]
        public DifficultyStatistics Difficulty { get; set; }

        /// <summary> What the author did to the level, as opposed to how it was played. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Editor)]
        public LevelEditorStatistics Editor { get; set; }

        /// <summary> Whether this file records anything at all. A freshly created one does not, and
        /// nothing should be shown for it. </summary>
        [JsonIgnore]
        public bool HasValue => Attempts > 0 || Editor.HasValue;

        /// <summary> Whether the level was ever finished. </summary>
        [JsonIgnore]
        public bool Cleared => Clears > 0;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public LevelStatistics()
        {
            LevelId = LevelId.Null;
            LevelName = new StringValue(string.Empty);
            LevelVersion = new Version(1, 0);
            FirstPlayedUtc = default;
            LastPlayedUtc = default;
            TotalRealSeconds = 0.0;
            SessionCount = 0;

            Attempts = 0;
            Clears = 0;
            Deaths = 0;
            Hits = 0;
            Dashes = 0;
            CheckpointRestarts = 0;
            Quits = 0;

            BestFrame = FrameRules.NoFrame;
            BestProgress = 0f;
            FirstClearUtc = default;

            Records = new Dictionary<RunProfile, BestRun>();
            Difficulty = new DifficultyStatistics();
            Editor = new LevelEditorStatistics();
        }

        /// <summary> Built from its id. </summary>
        public LevelStatistics(LevelId levelId) : this()
        {
            LevelId = levelId;
        }

        // Oldest first, by the record's own timestamp: a cap reached by a player who keeps nudging
        // the speed slider should cost them the record they set longest ago, not the one they are
        // about to beat. Returns whether room was made.
        private bool EvictOldestRecord()
        {
            var oldest = default(RunProfile);
            var found = false;

            foreach (var (profile, run) in Records)
            {
                if (found && run.TimeUtc >= Records[oldest].TimeUtc) continue;
                oldest = profile;
                found = true;
            }

            return found && Records.Remove(oldest);
        }

        /// <summary> Files a record under its profile, making room if the cap is reached. </summary>
        public void SetRecord(in RunProfile profile, BestRun run)
        {
            if (run == null) return;

            if (!Records.ContainsKey(profile) && Records.Count >= StatisticsRules.MaxRecordProfiles
                && !EvictOldestRecord())
                return;

            Records[profile] = run;
        }

        /// <summary> The record filed under a profile, or null. </summary>
        public BestRun GetRecord(in RunProfile profile)
            => Records.TryGetValue(profile, out var run) ? run : null;

        // Nested instances are kept, unlike Update above: a view bound to Difficulty or Editor must
        // survive this file being refreshed from disk. Records is merged key by key for the same
        // reason - the level screen holds the record of the profile it is showing.

        // Nested, because HashCode.Combine takes eight arguments and this has far more than eight
        // values - the same shape InterfaceSettings uses for the same reason.
    }
}
