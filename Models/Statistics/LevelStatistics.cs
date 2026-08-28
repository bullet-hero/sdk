using System;
using System.Collections.Generic;
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
    // ONE LEVEL, AS THE PLAYER HAS EXPERIENCED IT: stats/<LevelId>.json, its own serialization root.
    // A fourth top-level document beside a level, its metadata and the player's settings, and the
    // only one of the four describing the relationship between a player and a level rather than
    // either of them alone.
    //
    // NOT A FIELD OF THE LEVEL, and that is what the whole file turns on. A level is a portable
    // document - zipped, shared, unzipped, played by someone else - and progress that travelled with
    // it would arrive already won. It also has to survive the level being deleted, which is why this
    // lives under stats/ next to backups/ rather than inside the level folder.
    //
    // KEYED BY LevelId, the identifier LevelMeta itself declares scores and progress attach to: a
    // title is edited, translated and duplicated, an id is not. The known cost is that copying a
    // level folder produces two levels sharing one id and therefore one statistics file; that is
    // this identifier's contract ("the same id is the same level") rather than a defect here.
    //
    // EVERY TIMESTAMP IS UTC. A statistics file outlives time zones, travels between machines and is
    // read by a person - so it stores an absolute instant in a readable form, and every writer uses
    // DateTime.UtcNow. Unix seconds were the alternative and were rejected for the reason the file
    // is JSON at all: a number nobody can read is worse than a longer string.

    /// <summary> Everything one player has done with one level. </summary>
    [RuleContainer]
    [DataVersion(DataDomains.LevelStatistics, 1, 0)]
    public class LevelStatistics : IModel<LevelStatistics>
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

        // REAL seconds, measured on the wall clock rather than on the level clock: the level clock
        // is bent by the launch speed and by the checkpoint ramp, so it answers "how much level was
        // played" and not "how long the player sat there". The second question is the one a
        // playtime number is asked.

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

        public LevelStatistics()
        {
            Reset();
        }

        public LevelStatistics(LevelId levelId) : this()
        {
            LevelId = levelId;
        }

        public void Reset()
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

            BestFrame = 0;
            BestProgress = 0f;
            FirstClearUtc = default;

            Records = new Dictionary<RunProfile, BestRun>();
            Difficulty = new DifficultyStatistics();
            Editor = new LevelEditorStatistics();
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

        public object Clone() => Copy();

        public LevelStatistics Copy()
        {
            var copy = new LevelStatistics(LevelId)
            {
                LevelName = LevelName.Copy(),
                LevelVersion = (Version)LevelVersion.Clone(),
                FirstPlayedUtc = FirstPlayedUtc,
                LastPlayedUtc = LastPlayedUtc,
                TotalRealSeconds = TotalRealSeconds,
                SessionCount = SessionCount,

                Attempts = Attempts,
                Clears = Clears,
                Deaths = Deaths,
                Hits = Hits,
                Dashes = Dashes,
                CheckpointRestarts = CheckpointRestarts,
                Quits = Quits,

                BestFrame = BestFrame,
                BestProgress = BestProgress,
                FirstClearUtc = FirstClearUtc,

                Records = Records.CopyDictionary(),
                Difficulty = Difficulty.Copy(),
                Editor = Editor.Copy(),
            };
            return copy;
        }

        public void Update(LevelStatistics src)
        {
            LevelId = src.LevelId;
            LevelName = src.LevelName.Copy();
            LevelVersion = (Version)src.LevelVersion.Clone();
            FirstPlayedUtc = src.FirstPlayedUtc;
            LastPlayedUtc = src.LastPlayedUtc;
            TotalRealSeconds = src.TotalRealSeconds;
            SessionCount = src.SessionCount;

            Attempts = src.Attempts;
            Clears = src.Clears;
            Deaths = src.Deaths;
            Hits = src.Hits;
            Dashes = src.Dashes;
            CheckpointRestarts = src.CheckpointRestarts;
            Quits = src.Quits;

            BestFrame = src.BestFrame;
            BestProgress = src.BestProgress;
            FirstClearUtc = src.FirstClearUtc;

            Records = src.Records.CopyDictionary();
            Difficulty = src.Difficulty.Copy();
            Editor = src.Editor.Copy();
        }

        // Nested instances are kept, unlike Update above: a view bound to Difficulty or Editor must
        // survive this file being refreshed from disk. Records is merged key by key for the same
        // reason - the level screen holds the record of the profile it is showing.
        public void Pull(LevelStatistics source)
        {
            if (ReferenceEquals(this, source)) return;

            LevelId = source.LevelId;
            LevelName = LevelName.PullFrom(source.LevelName);
            LevelVersion = (Version)source.LevelVersion.Clone();
            FirstPlayedUtc = source.FirstPlayedUtc;
            LastPlayedUtc = source.LastPlayedUtc;
            TotalRealSeconds = source.TotalRealSeconds;
            SessionCount = source.SessionCount;

            Attempts = source.Attempts;
            Clears = source.Clears;
            Deaths = source.Deaths;
            Hits = source.Hits;
            Dashes = source.Dashes;
            CheckpointRestarts = source.CheckpointRestarts;
            Quits = source.Quits;

            BestFrame = source.BestFrame;
            BestProgress = source.BestProgress;
            FirstClearUtc = source.FirstClearUtc;

            Records.PullDictionary(source.Records);
            Difficulty.Pull(source.Difficulty);
            Editor.Pull(source.Editor);
        }

        public override bool Equals(object obj) => obj is LevelStatistics value && Equals(value);

        public bool Equals(LevelStatistics other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return LevelId.Equals(other.LevelId)
                   && Equals(LevelName, other.LevelName)
                   && Equals(LevelVersion, other.LevelVersion)
                   && FirstPlayedUtc.Equals(other.FirstPlayedUtc)
                   && LastPlayedUtc.Equals(other.LastPlayedUtc)
                   && TotalRealSeconds.Equals(other.TotalRealSeconds)
                   && SessionCount == other.SessionCount
                   && Attempts == other.Attempts
                   && Clears == other.Clears
                   && Deaths == other.Deaths
                   && Hits == other.Hits
                   && Dashes == other.Dashes
                   && CheckpointRestarts == other.CheckpointRestarts
                   && Quits == other.Quits
                   && BestFrame == other.BestFrame
                   && BestProgress.Equals(other.BestProgress)
                   && FirstClearUtc.Equals(other.FirstClearUtc)
                   && Records.DictionaryEquals(other.Records)
                   && Difficulty.Equals(other.Difficulty)
                   && Editor.Equals(other.Editor);
        }

        // Nested, because HashCode.Combine takes eight arguments and this has far more than eight
        // values - the same shape InterfaceSettings uses for the same reason.
        public override int GetHashCode() =>
            HashCode.Combine(
                LevelId,
                HashCode.Combine(FirstPlayedUtc, LastPlayedUtc, TotalRealSeconds, SessionCount),
                HashCode.Combine(Attempts, Clears, Deaths, Hits),
                HashCode.Combine(Dashes, CheckpointRestarts, Quits),
                HashCode.Combine(BestFrame, BestProgress, FirstClearUtc),
                Records.GetDictionaryHashCode(),
                Difficulty.GetHashCode(),
                Editor.GetHashCode());
    }
}
