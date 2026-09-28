using BH.SDK.Models.Statistics;

namespace BH.SDK.Versions.V1.Migrations
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Device-wide statistics, generation 1 to generation 2 - every group carried as it is,
    /// and an empty tutorial group: no build before generation 2 had a tutorial to complete. </summary>
    public class GameStatisticsV1ToV2 : ModelMigration<GameStatisticsV1, GameStatistics>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override GameStatistics Migrate(GameStatisticsV1 from) => new()
        {
            Profile = from.Profile,
            Screens = from.Screens,
            Totals = from.Totals,
            Streaks = from.Streaks,
            Avatar = from.Avatar,
            Editor = from.Editor,
            Devices = from.Devices,
            Tutorial = new TutorialStatistics(),
        };
    }
}
