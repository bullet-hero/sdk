using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // WHERE A LEVEL KILLS PEOPLE. This is the half of the statistics aimed at the AUTHOR rather than
    // at the player: a level is content someone is still editing, and "which passage do players
    // actually die in" is a question the game can answer and a playtest cannot.
    //
    // Buckets are a fixed COUNT, not a fixed duration (StatisticsRules.BucketCount): a bucket is a
    // percentage of the level, so two levels of different lengths produce histograms that can be
    // read side by side, and a short array stays readable in a file a person may open.
    //
    // BucketFrameDuration IS WHAT KEEPS THE DATA HONEST. A histogram means "deaths at this fraction
    // of the level", and that claim silently becomes false the moment the level changes length -
    // every bucket then names a different moment of the music. So the length the buckets were built
    // against is stored beside them, and a change clears both histograms rather than letting them go
    // on quietly lying. What it cannot catch is content REARRANGED inside the same length; that is
    // the known limit of the cheap version of this.

    /// <summary> Where in a level the player loses, accumulated across every run. </summary>
    [RuleContainer]
    public class DifficultyStatistics : IModel<DifficultyStatistics>
    {
        /// <summary> Deaths per bucket of level progress. Always
        /// <see cref="StatisticsRules.BucketCount"/> long. </summary>
        [RuleNotNull]
        [JsonProperty(Names.DeathsByBucket)]
        public int[] DeathsByBucket { get; set; }

        /// <summary> Hits per bucket of level progress, same layout. </summary>
        [RuleNotNull]
        [JsonProperty(Names.HitsByBucket)]
        public int[] HitsByBucket { get; set; }

        /// <summary> The level length both histograms were built against. A different length clears
        /// them. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.BucketFrameDuration)]
        public int BucketFrameDuration { get; set; }

        // ITS OWN FIELD RATHER THAN A KEY OF -1 IN THE MAP BELOW. -1 is the project's one reserved
        // frame number (FrameSpan.LastFrame), and giving that digit a second meaning here is the
        // exact class of mistake FrameSpan's own header exists to prevent.

        /// <summary> Deaths that happened before any checkpoint was reached. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.DeathsBeforeCheckpoint)]
        public int DeathsBeforeCheckpoint { get; set; }

        /// <summary> Deaths per checkpoint, keyed by that checkpoint's own frame. </summary>
        [RuleNotNull, RuleCollectionMaxCount(StatisticsRules.MaxCheckpointDeaths)]
        [JsonProperty(Names.DeathsByCheckpoint)]
        public Dictionary<int, CheckpointDeaths> DeathsByCheckpoint { get; set; }

        /// <summary> Whether anything was ever recorded here. </summary>
        [JsonIgnore]
        public bool HasValue => BucketFrameDuration > 0;

        public DifficultyStatistics()
        {
            Reset();
        }

        public void Reset()
        {
            DeathsByBucket = new int[StatisticsRules.BucketCount];
            HitsByBucket = new int[StatisticsRules.BucketCount];
            BucketFrameDuration = 0;
            DeathsBeforeCheckpoint = 0;
            DeathsByCheckpoint = new Dictionary<int, CheckpointDeaths>();
        }

        /// <summary> Drops both histograms when the level is no longer the length they describe.
        /// Returns whether anything was cleared. </summary>
        public bool SyncFrameDuration(int frameDuration)
        {
            if (frameDuration <= 0) return false;
            if (BucketFrameDuration == frameDuration) return false;

            ClearBuckets();
            BucketFrameDuration = frameDuration;
            return true;
        }

        public void ClearBuckets()
        {
            for (var i = 0; i < DeathsByBucket.Length; i++) DeathsByBucket[i] = 0;
            for (var i = 0; i < HitsByBucket.Length; i++) HitsByBucket[i] = 0;
            DeathsBeforeCheckpoint = 0;
            DeathsByCheckpoint.Clear();
        }

        public void AddDeath(int bucket)
        {
            if (bucket < 0 || bucket >= DeathsByBucket.Length) return;
            DeathsByBucket[bucket]++;
        }

        public void AddHit(int bucket)
        {
            if (bucket < 0 || bucket >= HitsByBucket.Length) return;
            HitsByBucket[bucket]++;
        }

        /// <summary> Records a death against the checkpoint last reached, or against the start of
        /// the level when none was. </summary>
        public void AddCheckpointDeath(int checkpointFrame, bool hasCheckpoint)
        {
            if (!hasCheckpoint)
            {
                DeathsBeforeCheckpoint++;
                return;
            }

            if (DeathsByCheckpoint.TryGetValue(checkpointFrame, out var entry))
            {
                entry.Deaths++;
                return;
            }

            if (DeathsByCheckpoint.Count >= StatisticsRules.MaxCheckpointDeaths) return;
            DeathsByCheckpoint.Add(checkpointFrame, new CheckpointDeaths(checkpointFrame, 1));
        }

        public object Clone() => Copy();

        public DifficultyStatistics Copy() =>
            new()
            {
                DeathsByBucket = (int[])DeathsByBucket.Clone(),
                HitsByBucket = (int[])HitsByBucket.Clone(),
                BucketFrameDuration = BucketFrameDuration,
                DeathsBeforeCheckpoint = DeathsBeforeCheckpoint,
                DeathsByCheckpoint = DeathsByCheckpoint.CopyDictionary(),
            };

        public void Update(DifficultyStatistics src)
        {
            DeathsByBucket = (int[])src.DeathsByBucket.Clone();
            HitsByBucket = (int[])src.HitsByBucket.Clone();
            BucketFrameDuration = src.BucketFrameDuration;
            DeathsBeforeCheckpoint = src.DeathsBeforeCheckpoint;
            DeathsByCheckpoint = src.DeathsByCheckpoint.CopyDictionary();
        }

        // The arrays and the dictionary are written into rather than replaced, unlike Update above:
        // this is the aggregate a live statistics object is refreshed through, and anything holding
        // one of its entries must not lose the instance under it.
        public void Pull(DifficultyStatistics source)
        {
            if (ReferenceEquals(this, source)) return;

            CopyInto(source.DeathsByBucket, DeathsByBucket);
            CopyInto(source.HitsByBucket, HitsByBucket);
            BucketFrameDuration = source.BucketFrameDuration;
            DeathsBeforeCheckpoint = source.DeathsBeforeCheckpoint;
            DeathsByCheckpoint.PullDictionary(source.DeathsByCheckpoint);
        }

        // A file can carry an array of the wrong length - it is a number in a text document, and
        // BucketCount is free to change between builds. Copying by the shorter of the two and
        // zeroing the tail keeps this total instead of throwing on data nobody can repair by hand.
        private static void CopyInto(int[] source, int[] target)
        {
            var count = source.Length < target.Length ? source.Length : target.Length;
            for (var i = 0; i < count; i++) target[i] = source[i];
            for (var i = count; i < target.Length; i++) target[i] = 0;
        }

        public override bool Equals(object obj) => obj is DifficultyStatistics value && Equals(value);

        public bool Equals(DifficultyStatistics other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return BucketFrameDuration == other.BucketFrameDuration
                   && DeathsBeforeCheckpoint == other.DeathsBeforeCheckpoint
                   && DeathsByBucket.ArrayEquals(other.DeathsByBucket)
                   && HitsByBucket.ArrayEquals(other.HitsByBucket)
                   && DeathsByCheckpoint.DictionaryEquals(other.DeathsByCheckpoint);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = BucketFrameDuration * 31 + DeathsBeforeCheckpoint;
                hash = hash * 31 + DeathsByBucket.GetArrayHashCode();
                hash = hash * 31 + HitsByBucket.GetArrayHashCode();
                return hash * 31 + DeathsByCheckpoint.GetDictionaryHashCode();
            }
        }
    }
}
