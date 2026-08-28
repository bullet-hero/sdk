using System;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Statistics;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    [TestFixture]
    public class StatisticsModelTests
    {
        private static LevelStatistics Populated()
        {
            var stats = new LevelStatistics(LevelId.NewId())
            {
                LevelName = new StringValue("Name"),
                LevelVersion = new Version(1, 4),
                Attempts = 5,
                Clears = 1,
                BestFrame = 100,
                BestProgress = 0.5f,
            };

            stats.SetRecord(new RunProfile(3, 100, true, BotKind.None),
                new BestRun(0.5f, 100, 2, 3, 1, 42, new Version(1, 4), DateTime.UtcNow));
            stats.Difficulty.SyncFrameDuration(200);
            stats.Difficulty.AddDeath(1);
            stats.Editor.Saves = 3;

            return stats;
        }

        // THE ONE-LINE BOILERPLATE THIS PROJECT CALLS ITS EASIEST SILENT BUG: Equals(object obj)
        // reads `obj is T` and is pasted between sibling classes, so a stale type name compiles
        // fine and just makes every comparison through the non-generic path return false.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EqualsObject_MatchesItsOwnType()
        {
            // One instance against its own copy, never two calls to Populated: that factory mints a
            // fresh LevelId and stamps DateTime.UtcNow, so two of them are genuinely different.
            var populated = Populated();
            AssertEqualsObject(populated, populated.Copy());
            AssertEqualsObject(new GameStatistics(), new GameStatistics());
            AssertEqualsObject(new ProfileStatistics(), new ProfileStatistics());
            AssertEqualsObject(new ScreenTimeStatistics(), new ScreenTimeStatistics());
            AssertEqualsObject(new TotalsStatistics(), new TotalsStatistics());
            AssertEqualsObject(new StreakStatistics(), new StreakStatistics());
            AssertEqualsObject(new AvatarStatistics(), new AvatarStatistics());
            AssertEqualsObject(new EditorTotalsStatistics(), new EditorTotalsStatistics());
            AssertEqualsObject(new DeviceTimeStatistics(), new DeviceTimeStatistics());
            AssertEqualsObject(new DifficultyStatistics(), new DifficultyStatistics());
            AssertEqualsObject(new LevelEditorStatistics(), new LevelEditorStatistics());
            AssertEqualsObject(new BestRun(), new BestRun());
            AssertEqualsObject(new CheckpointDeaths(1, 2), new CheckpointDeaths(1, 2));
        }

        private static void AssertEqualsObject(object left, object right)
        {
            Assert.IsTrue(left.Equals(right),
                $"{left.GetType().Name}.Equals(object) failed against its own type");
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode(),
                $"{left.GetType().Name}.GetHashCode disagrees with Equals");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Copy_IsDeep()
        {
            var source = Populated();
            var copy = source.Copy();

            Assert.AreEqual(source, copy);
            Assert.AreNotSame(source.Difficulty, copy.Difficulty);
            Assert.AreNotSame(source.Editor, copy.Editor);
            Assert.AreNotSame(source.Records, copy.Records);

            copy.Difficulty.AddDeath(1);
            copy.Editor.Saves = 99;
            copy.Attempts = 100;

            Assert.AreEqual(5, source.Attempts);
            Assert.AreEqual(3, source.Editor.Saves);
            Assert.AreEqual(1, source.Difficulty.DeathsByBucket[1]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Reset_ReturnsToAFreshInstance()
        {
            var stats = Populated();
            stats.Reset();

            var fresh = new LevelStatistics();

            Assert.AreEqual(fresh, stats);
        }

        // Pull is what a live statistics object is refreshed through, so every nested instance has
        // to survive it - a view bound to Difficulty or to one record must not be left pointing at
        // an object nothing writes to any more.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_KeepsNestedInstances()
        {
            var target = Populated();
            var difficulty = target.Difficulty;
            var editor = target.Editor;
            var records = target.Records;

            var source = Populated();
            source.Attempts = 77;
            source.Difficulty.AddDeath(2);

            target.Pull(source);

            Assert.AreSame(difficulty, target.Difficulty);
            Assert.AreSame(editor, target.Editor);
            Assert.AreSame(records, target.Records);
            Assert.AreEqual(77, target.Attempts);
            Assert.AreEqual(1, target.Difficulty.DeathsByBucket[2]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Pull_KeepsGroupInstancesOnTheGlobalRoot()
        {
            var target = new GameStatistics();
            var profile = target.Profile;
            var devices = target.Devices;

            var source = new GameStatistics();
            source.Profile.AppLaunches = 12;
            source.Devices.Add(ControlDevice.Gamepad, 5.0);

            target.Pull(source);

            Assert.AreSame(profile, target.Profile);
            Assert.AreSame(devices, target.Devices);
            Assert.AreEqual(12, target.Profile.AppLaunches);
            Assert.AreEqual(5.0, target.Devices.Get(ControlDevice.Gamepad));
        }

        // The histograms describe a fraction of the level, so a level that changed length makes
        // every bucket name a different moment of the music. Clearing is the honest answer.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SyncFrameDuration_ClearsBucketsOnlyWhenTheLengthMoved()
        {
            var difficulty = new DifficultyStatistics();
            difficulty.SyncFrameDuration(600);
            difficulty.AddDeath(3);
            difficulty.AddHit(3);
            difficulty.AddCheckpointDeath(120, true);

            Assert.IsFalse(difficulty.SyncFrameDuration(600));
            Assert.AreEqual(1, difficulty.DeathsByBucket[3]);
            Assert.AreEqual(1, difficulty.DeathsByCheckpoint.Count);

            Assert.IsTrue(difficulty.SyncFrameDuration(900));
            Assert.AreEqual(0, difficulty.DeathsByBucket[3]);
            Assert.AreEqual(0, difficulty.HitsByBucket[3]);
            Assert.AreEqual(0, difficulty.DeathsByCheckpoint.Count);
            Assert.AreEqual(900, difficulty.BucketFrameDuration);
        }

        // A frameDuration of zero is a level that has not been measured yet, not a length: clearing
        // on it would drop the histograms every time something asked before the level was loaded.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void SyncFrameDuration_IgnoresNonPositiveLengths()
        {
            var difficulty = new DifficultyStatistics();
            difficulty.SyncFrameDuration(600);
            difficulty.AddDeath(0);

            Assert.IsFalse(difficulty.SyncFrameDuration(0));
            Assert.IsFalse(difficulty.SyncFrameDuration(-1));
            Assert.AreEqual(600, difficulty.BucketFrameDuration);
            Assert.AreEqual(1, difficulty.DeathsByBucket[0]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AddDeath_IgnoresBucketsOutsideTheHistogram()
        {
            var difficulty = new DifficultyStatistics();

            Assert.DoesNotThrow(() => difficulty.AddDeath(-1));
            Assert.DoesNotThrow(() => difficulty.AddDeath(StatisticsRules.BucketCount));
            Assert.DoesNotThrow(() => difficulty.AddHit(StatisticsRules.BucketCount + 100));
        }

        // The cap has to bite by dropping the OLDEST record, not by refusing the new one: a player
        // nudging the speed slider would otherwise be locked out of recording anything ever again.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void SetRecord_EvictsTheOldestWhenFull()
        {
            var stats = new LevelStatistics(LevelId.NewId());
            var epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < StatisticsRules.MaxRecordProfiles; i++)
                stats.SetRecord(new RunProfile(1, 100 + i, true, BotKind.None),
                    new BestRun(0.1f, 1, 0, 0, 0, i, new Version(1, 0), epoch.AddMinutes(i)));

            Assert.AreEqual(StatisticsRules.MaxRecordProfiles, stats.Records.Count);
            var oldest = new RunProfile(1, 100, true, BotKind.None);
            Assert.IsTrue(stats.Records.ContainsKey(oldest));

            var newcomer = new RunProfile(9, 999, false, BotKind.Warm);
            stats.SetRecord(newcomer, new BestRun(0.2f, 2, 0, 0, 0, 1, new Version(1, 0), epoch.AddYears(1)));

            Assert.AreEqual(StatisticsRules.MaxRecordProfiles, stats.Records.Count);
            Assert.IsTrue(stats.Records.ContainsKey(newcomer));
            Assert.IsFalse(stats.Records.ContainsKey(oldest), "The oldest record should have been evicted");
        }

        // Replacing an existing profile is not a new entry and must never evict anything.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SetRecord_ReplacingAProfileDoesNotEvict()
        {
            var stats = new LevelStatistics(LevelId.NewId());
            var epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < StatisticsRules.MaxRecordProfiles; i++)
                stats.SetRecord(new RunProfile(1, 100 + i, true, BotKind.None),
                    new BestRun(0.1f, 1, 0, 0, 0, i, new Version(1, 0), epoch.AddMinutes(i)));

            var existing = new RunProfile(1, 100, true, BotKind.None);
            stats.SetRecord(existing, new BestRun(0.9f, 500, 0, 0, 0, 5, new Version(1, 0), epoch));

            Assert.AreEqual(StatisticsRules.MaxRecordProfiles, stats.Records.Count);
            Assert.AreEqual(0.9f, stats.GetRecord(existing).Progress);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Streaks_BreakOnAnythingButAClear()
        {
            var streaks = new StreakStatistics();

            streaks.ReportOutcome(true);
            streaks.ReportOutcome(true);
            streaks.ReportOutcome(true);
            Assert.AreEqual(3, streaks.CurrentClearStreak);
            Assert.AreEqual(3, streaks.LongestClearStreak);

            streaks.ReportOutcome(false);
            Assert.AreEqual(0, streaks.CurrentClearStreak);
            Assert.AreEqual(3, streaks.LongestClearStreak);

            streaks.ReportOutcome(true);
            Assert.AreEqual(1, streaks.CurrentClearStreak);
            Assert.AreEqual(3, streaks.LongestClearStreak);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ReportAttempts_KeepsTheLeaderByComparison()
        {
            var streaks = new StreakStatistics();
            var a = LevelId.NewId();
            var b = LevelId.NewId();

            streaks.ReportAttempts(a, 10);
            streaks.ReportAttempts(b, 4);

            Assert.AreEqual(a, streaks.MostPlayedLevelId);
            Assert.AreEqual(10, streaks.MostPlayedAttempts);
            Assert.AreEqual(b, streaks.LastPlayedLevelId);

            streaks.ReportAttempts(b, 11);
            Assert.AreEqual(b, streaks.MostPlayedLevelId);
            Assert.AreEqual(11, streaks.MostPlayedAttempts);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DeviceTime_ChargesOneDeviceAtATime()
        {
            var devices = new DeviceTimeStatistics();

            devices.Add(ControlDevice.Touchscreen, 2.5);
            devices.Add(ControlDevice.Touchscreen, 2.5);
            devices.Add(ControlDevice.Gamepad, 1.0);

            Assert.AreEqual(5.0, devices.Get(ControlDevice.Touchscreen));
            Assert.AreEqual(1.0, devices.Get(ControlDevice.Gamepad));
            Assert.AreEqual(0.0, devices.Get(ControlDevice.KeyboardMouse));
            Assert.AreEqual(0.0, devices.Get(ControlDevice.DeviceGyro));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void HasValue_IsFalseForAFreshFile()
        {
            var stats = new LevelStatistics(LevelId.NewId());
            Assert.IsFalse(stats.HasValue);
            Assert.IsFalse(stats.Cleared);

            stats.Attempts = 1;
            Assert.IsTrue(stats.HasValue);

            var editorOnly = new LevelStatistics(LevelId.NewId());
            editorOnly.Editor.EditorOpens = 1;
            Assert.IsTrue(editorOnly.HasValue, "A level only ever edited still has a file worth keeping");
        }
    }
}
