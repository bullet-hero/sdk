using System;
using System.Linq;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Statistics;
using BH.SDK.Rules;
using BH.SDK.Services.Profile;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    /// <summary> Merging two copies of a player's statistics: idempotent, never inflating, a record the
    /// better of two real runs, histograms only merged against the same level length. </summary>
    public class StatisticsMergeTests
    {
        private static readonly DateTime Early = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Late = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly RunProfile Normal = new RunProfile(3, 100, true, BotKind.None);

        private static LevelStatistics Level(int attempts, int deaths, DateTime first, DateTime last)
        {
            var level = new LevelStatistics(new LevelId(new Guid("11111111-1111-1111-1111-111111111111")))
            {
                Attempts = attempts,
                Deaths = deaths,
                FirstPlayedUtc = first,
                LastPlayedUtc = last,
                TotalRealSeconds = attempts * 10.0,
            };
            level.Difficulty.SyncFrameDuration(600);
            level.Difficulty.AddDeath(3);
            return level;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void MergingAFileWithItsOwnCopy_ChangesNothing()
        {
            var level = Level(5, 3, Early, Late);
            level.SetRecord(Normal, new BestRun(0.5f, 300, 2, 1, 1, 7, new Version(1, 0), Early));
            var copy = level.Copy();

            StatisticsMerge.Merge(level, copy);

            Assert.AreEqual(copy, level);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Counters_TakeTheLarger_EitherWayRound()
        {
            var a = Level(5, 9, Early, Early);
            var b = Level(8, 2, Late, Late);

            var ab = a.Copy();
            StatisticsMerge.Merge(ab, b);
            var ba = b.Copy();
            StatisticsMerge.Merge(ba, a);

            Assert.AreEqual(8, ab.Attempts);
            Assert.AreEqual(9, ab.Deaths);
            Assert.AreEqual(ab.Attempts, ba.Attempts);
            Assert.AreEqual(ab.Deaths, ba.Deaths);
            Assert.AreEqual(ab.TotalRealSeconds, ba.TotalRealSeconds);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Dates_FirstIsTheEarlierSet_LastIsTheLater_DefaultMeansNever()
        {
            var local = Level(1, 0, default, Early);
            var incoming = Level(1, 0, Late, Late);

            StatisticsMerge.Merge(local, incoming);

            Assert.AreEqual(Late, local.FirstPlayedUtc);
            Assert.AreEqual(Late, local.LastPlayedUtc);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Records_AreUnited_AndTheBetterRunWins()
        {
            var fast = new RunProfile(1, 200, false, BotKind.None);

            var local = Level(1, 0, Early, Early);
            local.SetRecord(Normal, new BestRun(0.5f, 300, 4, 0, 1, 1, new Version(1, 0), Early));

            var incoming = Level(1, 0, Late, Late);
            incoming.SetRecord(Normal, new BestRun(0.5f, 300, 2, 0, 1, 1, new Version(1, 0), Late));
            incoming.SetRecord(fast, new BestRun(0.1f, 60, 0, 0, 1, 1, new Version(1, 0), Late));

            StatisticsMerge.Merge(local, incoming);

            Assert.AreEqual(2, local.GetRecord(Normal).Hits, "same frame, fewer hits wins");
            Assert.IsNotNull(local.GetRecord(fast));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Records_NeverExceedTheCap()
        {
            var local = Level(1, 0, Early, Early);
            var incoming = Level(1, 0, Late, Late);

            for (var i = 0; i < StatisticsRules.MaxRecordProfiles; i++)
            {
                local.SetRecord(new RunProfile(1, 100 + i, true, BotKind.None),
                    new BestRun(0.1f, 10, 0, 0, 1, 1, new Version(1, 0), Early.AddMinutes(i)));
                incoming.SetRecord(new RunProfile(2, 100 + i, true, BotKind.None),
                    new BestRun(0.1f, 10, 0, 0, 1, 1, new Version(1, 0), Late.AddMinutes(i)));
            }

            StatisticsMerge.Merge(local, incoming);

            Assert.AreEqual(StatisticsRules.MaxRecordProfiles, local.Records.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Histograms_OfTheSameLength_MergeBucketByBucket()
        {
            var local = Level(1, 0, Early, Early);
            var incoming = Level(1, 0, Late, Late);
            incoming.Difficulty.AddDeath(3);
            incoming.Difficulty.AddDeath(5);

            StatisticsMerge.Merge(local, incoming);

            Assert.AreEqual(2, local.Difficulty.DeathsByBucket[3]);
            Assert.AreEqual(1, local.Difficulty.DeathsByBucket[5]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Histograms_OfDifferentLengths_ComeWholeFromTheSideThatPlayedLast()
        {
            var local = Level(1, 0, Early, Early);
            var incoming = Level(1, 0, Late, Late);
            incoming.Difficulty.SyncFrameDuration(900);
            incoming.Difficulty.AddDeath(10);

            StatisticsMerge.Merge(local, incoming);

            Assert.AreEqual(900, local.Difficulty.BucketFrameDuration);
            Assert.AreEqual(1, local.Difficulty.DeathsByBucket[10]);
            Assert.AreEqual(0, local.Difficulty.DeathsByBucket[3]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Global_TakesTheLarger_AndRecountsFromTheMergedLevels()
        {
            var local = new GameStatistics();
            local.Totals.TotalAttempts = 4;
            local.Profile.LastPlayedUtc = Early;
            local.Streaks.CurrentClearStreak = 1;

            var incoming = new GameStatistics();
            incoming.Totals.TotalAttempts = 9;
            incoming.Profile.LastPlayedUtc = Late;
            incoming.Streaks.CurrentClearStreak = 7;

            var levels = new[] { Level(12, 0, Early, Late), Level(0, 0, default, default) };
            levels[0].Clears = 1;

            StatisticsMerge.Merge(local, incoming, levels);

            Assert.AreEqual(9, local.Totals.TotalAttempts);
            Assert.AreEqual(7, local.Streaks.CurrentClearStreak, "the latest state comes from whoever played last");
            Assert.AreEqual(1, local.Totals.DistinctLevelsPlayed);
            Assert.AreEqual(1, local.Totals.DistinctLevelsCleared);
            Assert.AreEqual(12, local.Streaks.MostPlayedAttempts);
            Assert.AreEqual(levels[0].LevelId, local.Streaks.MostPlayedLevelId);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Global_MergedWithItsOwnCopy_ChangesNothing()
        {
            var stats = new GameStatistics();
            stats.Totals.TotalAttempts = 4;
            stats.Screens.MenuSeconds = 12.5;
            stats.Profile.FirstPlayedUtc = Early;
            stats.Profile.LastPlayedUtc = Late;
            var copy = stats.Copy();

            StatisticsMerge.Merge(stats, copy);

            Assert.AreEqual(copy, stats);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void BestRunOrder_FollowsTheTieBreaksInOrder()
        {
            BestRun Run(int frame, int hits, int lives, int dashes, DateTime time) =>
                new BestRun(0f, frame, hits, dashes, lives, 0, new Version(1, 0), time);

            Assert.Greater(BestRunOrder.Compare(Run(10, 9, 0, 9, Late), Run(9, 0, 3, 0, Early)), 0);
            Assert.Greater(BestRunOrder.Compare(Run(10, 1, 0, 9, Late), Run(10, 2, 3, 0, Early)), 0);
            Assert.Greater(BestRunOrder.Compare(Run(10, 1, 2, 9, Late), Run(10, 1, 1, 0, Early)), 0);
            Assert.Greater(BestRunOrder.Compare(Run(10, 1, 1, 0, Late), Run(10, 1, 1, 1, Early)), 0);
            Assert.Greater(BestRunOrder.Compare(Run(10, 1, 1, 1, Early), Run(10, 1, 1, 1, Late)), 0);
            Assert.AreEqual(0, BestRunOrder.Compare(Run(10, 1, 1, 1, Early), Run(10, 1, 1, 1, Early)));
        }
    }
}
