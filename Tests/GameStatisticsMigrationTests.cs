using BH.SDK.Models;
using BH.SDK.Models.Statistics;
using BH.SDK.Serialization;
using BH.SDK.Versions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> That a statistics.json 1.0.0 wrote still opens after the tutorial group arrived - its
    /// groups carried, the tutorial never completed - and that today's shape keeps the group. </summary>
    [TestFixture]
    public class GameStatisticsMigrationTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AReleaseFile_MigratesWithItsGroupsAndNoTutorial()
        {
            var service = new SerializationService();

            var stats = new GameStatistics();
            stats.Profile.AppLaunches = 42;
            stats.Totals.DistinctLevelsPlayed = 7;

            var root = JObject.Parse(service.SerializeData(stats));
            root["g"] = ModelGenerations.V1_AlphaRelease;
            ((JObject)root["v"]).Remove(Names.Tutorial);

            var migrated = service.DeserializeData<GameStatistics>(root.ToString());

            Assert.AreEqual(42, migrated.Profile.AppLaunches);
            Assert.AreEqual(7, migrated.Totals.DistinctLevelsPlayed);
            Assert.IsNotNull(migrated.Tutorial);
            Assert.AreEqual(0, migrated.Tutorial.Completions);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TodaysFile_KeepsTheTutorialGroup()
        {
            var service = new SerializationService();
            var stats = new GameStatistics();
            stats.Tutorial.Completions = 2;

            var json = service.SerializeData(stats);

            Assert.AreEqual(ModelGenerations.V2_SimplifyEntrance, (int)JObject.Parse(json)["g"]);
            Assert.AreEqual(2, service.DeserializeData<GameStatistics>(json).Tutorial.Completions);
        }
    }
}
