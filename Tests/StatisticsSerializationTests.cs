using System;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Statistics;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    [TestFixture]
    public class StatisticsSerializationTests
    {
        private static SerializationService Service() => new(new SerializationSettings());

        private static LevelStatistics SampleLevel()
        {
            var stats = new LevelStatistics(LevelId.NewId())
            {
                LevelName = new StringValue("Test Level"),
                LevelVersion = new Version(2, 3),
                FirstPlayedUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                LastPlayedUtc = new DateTime(2026, 5, 6, 7, 8, 9, DateTimeKind.Utc),
                TotalRealSeconds = 1234.5,
                SessionCount = 7,
                Attempts = 42,
                Clears = 3,
                Deaths = 39,
                Hits = 51,
                Dashes = 300,
                CheckpointRestarts = 12,
                Quits = 5,
                BestFrame = 5400,
                BestProgress = 0.9f,
                FirstClearUtc = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc),
            };

            stats.SetRecord(new RunProfile(3, 100, true, BotKind.None),
                new BestRun(1f, 6000, 0, 120, 3, 777, new Version(2, 3),
                    new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc)));
            stats.SetRecord(new RunProfile(1, 200, false, BotKind.None),
                new BestRun(0.6f, 3600, 1, 90, 0, 778, new Version(2, 3),
                    new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc)));
            stats.SetRecord(new RunProfile(0, 50, true, BotKind.Reflex),
                new BestRun(0.3f, 1800, 4, 10, 0, 779, new Version(2, 2),
                    new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc)));

            stats.Difficulty.SyncFrameDuration(6000);
            stats.Difficulty.AddDeath(0);
            stats.Difficulty.AddDeath(StatisticsRules.BucketCount - 1);
            stats.Difficulty.AddHit(5);
            stats.Difficulty.AddCheckpointDeath(0, false);
            stats.Difficulty.AddCheckpointDeath(1200, true);
            stats.Difficulty.AddCheckpointDeath(1200, true);
            stats.Difficulty.AddCheckpointDeath(3600, true);

            stats.Editor.EditorOpens = 4;
            stats.Editor.TotalEditSeconds = 900.25;
            stats.Editor.LastEditedUtc = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
            stats.Editor.Saves = 11;
            stats.Editor.Autosaves = 22;
            stats.Editor.Operations = 333;

            return stats;
        }

        private static GameStatistics SampleGame()
        {
            var stats = new GameStatistics();

            stats.Profile.FirstPlayedUtc = new DateTime(2025, 12, 31, 23, 0, 0, DateTimeKind.Utc);
            stats.Profile.LastPlayedUtc = new DateTime(2026, 6, 1, 1, 2, 3, DateTimeKind.Utc);
            stats.Profile.AppLaunches = 91;
            stats.Profile.TotalAppSeconds = 98765.5;

            stats.Screens.MenuSeconds = 100.5;
            stats.Screens.GameSeconds = 200.25;
            stats.Screens.EditorSeconds = 300.125;
            stats.Screens.LoadingSeconds = 40.0;

            stats.Totals.TotalAttempts = 500;
            stats.Totals.TotalClears = 60;
            stats.Totals.TotalDeaths = 440;
            stats.Totals.TotalHits = 900;
            stats.Totals.DistinctLevelsPlayed = 17;
            stats.Totals.DistinctLevelsCleared = 9;
            stats.Totals.TotalFramesSimulated = 12000000000L;

            stats.Streaks.ReportOutcome(true);
            stats.Streaks.ReportOutcome(true);
            stats.Streaks.ReportAttempts(LevelId.NewId(), 42);

            stats.Avatar.TotalDashes = 9001L;
            stats.Avatar.TotalDistanceMoved = 12345.75;

            stats.Editor.LevelsCreated = 6;
            stats.Editor.LevelsDeleted = 2;
            stats.Editor.ObjectsCreated = 4000;
            stats.Editor.OperationsExecuted = 8000;
            stats.Editor.GeneratorsRun = 30;
            stats.Editor.TotalResources = 45;

            stats.Devices.Add(ControlDevice.KeyboardMouse, 1000.5);
            stats.Devices.Add(ControlDevice.Gamepad, 20.25);

            return stats;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void LevelStatistics_RoundTrips()
        {
            var service = Service();
            var source = SampleLevel();

            var json = service.SerializeData(source);
            var result = service.DeserializeData<LevelStatistics>(json);

            Assert.AreEqual(source, result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void GameStatistics_RoundTrips()
        {
            var service = Service();
            var source = SampleGame();

            var json = service.SerializeData(source);
            var result = service.DeserializeData<GameStatistics>(json);

            Assert.AreEqual(source, result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void LevelStatistics_RoundTripsThroughBlob()
        {
            var service = Service();
            var source = SampleLevel();

            var bytes = service.SerializeEnvelope(source, SerializationType.Blob);
            var result = service.DeserializeEnvelope<LevelStatistics>(bytes, SerializationType.Blob);

            Assert.AreEqual(source, result);
        }

        // Every record has to survive, keys included: the dictionary is written as an array of
        // pairs, and the failure this pins is losing one silently rather than throwing.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Records_KeepEveryProfileAcrossARoundTrip()
        {
            var service = Service();
            var source = SampleLevel();

            var result = service.DeserializeData<LevelStatistics>(service.SerializeData(source));

            Assert.AreEqual(source.Records.Count, result.Records.Count);
            foreach (var (profile, run) in source.Records)
            {
                Assert.IsTrue(result.Records.TryGetValue(profile, out var restored),
                    $"Profile {profile} was lost");
                Assert.AreEqual(run, restored);
            }
        }

        // The key-from-value form: DeathsByCheckpoint writes a plain array and rebuilds its keys
        // from the Frame each entry carries, so a wrong GetKey shows up as a shifted map rather
        // than as an exception.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void DeathsByCheckpoint_WritesAFlatArrayAndRebuildsKeys()
        {
            var service = Service();
            var source = SampleLevel();

            var json = service.SerializeData(source);
            var token = JObject.Parse(json)["value"]?["difficulty"]?[Names.DeathsByCheckpoint];

            Assert.IsNotNull(token);
            Assert.AreEqual(JTokenType.Array, token.Type);

            var result = service.DeserializeData<LevelStatistics>(json);
            foreach (var (frame, entry) in result.Difficulty.DeathsByCheckpoint)
                Assert.AreEqual(frame, entry.Frame, "Key and value disagree after a round trip");

            Assert.AreEqual(2, result.Difficulty.DeathsByCheckpoint[1200].Deaths);
            Assert.AreEqual(1, result.Difficulty.DeathsBeforeCheckpoint);
        }

        // MemberSerialization.OptIn does NOT actually filter members in this project - the contract
        // resolver sets it after CreateProperties has already run - so a public member carrying
        // neither [JsonProperty] nor [JsonIgnore] silently enters the format under its C# name and
        // stays part of it forever. This is what catches that.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void LevelStatistics_WritesExactlyTheDeclaredKeys()
        {
            var service = Service();
            var json = service.SerializeData(SampleLevel());
            var value = (JObject)JObject.Parse(json)[Names.Value];

            var expected = new[]
            {
                Names.LevelId, Names.Name, Names.Version, Names.FirstPlayedUtc, Names.LastPlayedUtc,
                Names.RealSeconds, Names.Sessions, Names.Attempts, Names.Clears, Names.Deaths,
                Names.Hits, Names.Dashes, Names.CheckpointRestarts, Names.Quits, Names.BestFrame,
                Names.BestProgress, Names.FirstClearUtc, Names.Records, Names.Difficulty,
                Names.Editor,
            };

            CollectionAssert.AreEquivalent(expected, value.Properties().Select(p => p.Name).ToArray());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void GameStatistics_WritesExactlyTheDeclaredKeys()
        {
            var service = Service();
            var json = service.SerializeData(SampleGame());
            var value = (JObject)JObject.Parse(json)[Names.Value];

            var expected = new[]
            {
                Names.Profile, Names.Screens, Names.Totals, Names.Streaks, Names.Avatar,
                Names.Editor, Names.Devices,
            };

            CollectionAssert.AreEquivalent(expected, value.Properties().Select(p => p.Name).ToArray());
        }

        // A statistics file travels between machines and time zones, so an instant has to come back
        // as the same instant. Newtonsoft reads dates as Local by default, which would quietly shift
        // every timestamp by the offset of whoever reads it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Timestamps_SurviveAsTheSameInstant()
        {
            var service = Service();
            var source = SampleLevel();

            var result = service.DeserializeData<LevelStatistics>(service.SerializeData(source));

            Assert.AreEqual(source.FirstPlayedUtc.ToUniversalTime(), result.FirstPlayedUtc.ToUniversalTime());
            Assert.AreEqual(source.LastPlayedUtc.ToUniversalTime(), result.LastPlayedUtc.ToUniversalTime());
            Assert.AreEqual(source.FirstClearUtc.ToUniversalTime(), result.FirstClearUtc.ToUniversalTime());
            Assert.AreEqual(source.Editor.LastEditedUtc.ToUniversalTime(),
                result.Editor.LastEditedUtc.ToUniversalTime());
        }

        // A file written before a group existed has no key for it, and must read back as a zeroed
        // group rather than as null - that is what keeps this domain additive at (1, 0).
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GameStatistics_MissingGroupsReadBackAsDefaults()
        {
            var service = Service();
            var source = new GameStatistics();
            source.Profile.AppLaunches = 3;

            // Written whole, then stripped down to one group, so the fixture cannot drift away from
            // whatever the envelope currently looks like.
            var document = JObject.Parse(service.SerializeData(source));
            var value = (JObject)document[Names.Value];
            foreach (var name in value.Properties().Select(p => p.Name).ToArray())
                if (name != Names.Profile)
                    value.Remove(name);

            var result = service.DeserializeData<GameStatistics>(document.ToString());

            Assert.AreEqual(3, result.Profile.AppLaunches);
            Assert.IsNotNull(result.Screens);
            Assert.IsNotNull(result.Totals);
            Assert.IsNotNull(result.Streaks);
            Assert.IsNotNull(result.Avatar);
            Assert.IsNotNull(result.Editor);
            Assert.IsNotNull(result.Devices);
            Assert.AreEqual(0, result.Totals.TotalAttempts);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EmptyStatistics_RoundTrip()
        {
            var service = Service();

            var level = new LevelStatistics(LevelId.NewId());
            var game = new GameStatistics();

            Assert.AreEqual(level, service.DeserializeData<LevelStatistics>(service.SerializeData(level)));
            Assert.AreEqual(game, service.DeserializeData<GameStatistics>(service.SerializeData(game)));
        }
    }
}
