using System;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Statistics;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> That a stats/&lt;id&gt;.json 1.0.0 wrote still opens after RunProfile gained its
    /// collision switch - every record re-filed with the switch off - and that today's shape keeps
    /// a no-collision record apart from an ordinary one. </summary>
    [TestFixture]
    public class LevelStatisticsMigrationTests
    {
        private static SerializationService Service() => new(new SerializationSettings());

        private static BestRun Run(float progress) => new(progress, 600, 1, 20, 2, 5, new Version(1, 0),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AReleaseFile_KeepsItsRecordsWithTheSwitchOff()
        {
            var service = Service();
            var stats = new LevelStatistics(LevelId.NewId()) { Attempts = 9, Hits = 4 };
            stats.SetRecord(new RunProfile(3, 100, true, BotKind.None), Run(0.5f));
            stats.SetRecord(new RunProfile(0, 150, false, BotKind.Reflex), Run(0.8f));

            // A real write of today's file, stamped Release and with the new key taken out of every
            // profile - exactly what 1.0.0 put on disk.
            var root = JObject.Parse(service.SerializeData(stats));
            root["g"] = ModelGenerations.V1_AlphaRelease;
            foreach (var token in root.SelectTokens("$..no_collision").ToList())
                token.Parent.Remove();

            var read = service.DeserializeData<LevelStatistics>(root.ToString());

            Assert.AreEqual(9, read.Attempts);
            Assert.AreEqual(4, read.Hits);
            Assert.AreEqual(2, read.Records.Count);
            Assert.AreEqual(0.5f, read.GetRecord(new RunProfile(3, 100, true, BotKind.None)).Progress);
            Assert.AreEqual(0.8f, read.GetRecord(new RunProfile(0, 150, false, BotKind.Reflex)).Progress);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TodaysFile_KeepsANoCollisionRecordApart()
        {
            var service = Service();
            var stats = new LevelStatistics(LevelId.NewId());
            stats.SetRecord(new RunProfile(0, 100, true, BotKind.None), Run(0.4f));
            stats.SetRecord(new RunProfile(0, 100, true, BotKind.None, true), Run(1f));

            var json = service.SerializeData(stats);
            var read = service.DeserializeData<LevelStatistics>(json);

            Assert.AreEqual(ModelGenerations.V2_SimplifyEntrance, (int)JObject.Parse(json)["g"]);
            Assert.AreEqual(2, read.Records.Count);
            Assert.AreEqual(0.4f, read.GetRecord(new RunProfile(0, 100, true, BotKind.None)).Progress);
            Assert.AreEqual(1f, read.GetRecord(new RunProfile(0, 100, true, BotKind.None, true)).Progress);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheSwitch_IsPartOfTheKey()
            => Assert.AreNotEqual(new RunProfile(0, 100, false, BotKind.None),
                new RunProfile(0, 100, false, BotKind.None, true));
    }
}
