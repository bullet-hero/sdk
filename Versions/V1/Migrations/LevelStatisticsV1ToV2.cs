using BH.SDK.Models.Statistics;

namespace BH.SDK.Versions.V1.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Level statistics, generation 1 to generation 2 - everything carried as it is, and
    /// every record re-filed under a profile whose collision switch is off: before generation 2 no
    /// run could be launched without collisions, so none of them was. </summary>
    public class LevelStatisticsV1ToV2 : ModelMigration<LevelStatisticsV1, LevelStatistics>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override LevelStatistics Migrate(LevelStatisticsV1 from)
        {
            var to = new LevelStatistics(from.LevelId)
            {
                LevelName = from.LevelName,
                LevelVersion = from.LevelVersion,
                FirstPlayedUtc = from.FirstPlayedUtc,
                LastPlayedUtc = from.LastPlayedUtc,
                TotalRealSeconds = from.TotalRealSeconds,
                SessionCount = from.SessionCount,
                Attempts = from.Attempts,
                Clears = from.Clears,
                Deaths = from.Deaths,
                Hits = from.Hits,
                Dashes = from.Dashes,
                CheckpointRestarts = from.CheckpointRestarts,
                Quits = from.Quits,
                BestFrame = from.BestFrame,
                BestProgress = from.BestProgress,
                FirstClearUtc = from.FirstClearUtc,
                Difficulty = from.Difficulty,
                Editor = from.Editor,
            };

            foreach (var (profile, run) in from.Records)
                to.Records[new RunProfile(profile.LifeCount, profile.SpeedCenti, profile.UseCheckpoints,
                    profile.Bot)] = run;

            return to;
        }
    }
}
