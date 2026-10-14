using System;
using BH.SDK.Models;
using BH.SDK.Models.Achievements;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    public class AchievementsRecordTests
    {
        private static readonly DateTime Early = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Late = new DateTime(2026, 10, 14, 18, 30, 0, DateTimeKind.Utc);

        private static AchievementsRecord Sample()
        {
            var record = new AchievementsRecord();
            record.Add("first_clear", Early);
            record.Add("not_in_any_catalog", Late);
            return record;
        }

        [TestCase(SerializationType.Json)]
        [TestCase(SerializationType.Blob)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ARecord_RoundTripsThroughItsEnvelope(SerializationType format)
        {
            var serialization = new SerializationService();
            var record = Sample();

            var read = serialization.DeserializeEnvelope<AchievementsRecord>(
                serialization.SerializeEnvelope(record, format), format);

            Assert.AreEqual(record, read);
            Assert.AreEqual(Early, read.Find("first_clear").UnlockedUtc);
        }

        // The file is read by builds whose catalog differs; an id one of them does not know must survive it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnUnknownId_SurvivesARoundTrip()
        {
            var serialization = new SerializationService();

            var read = serialization.DeserializeData<AchievementsRecord>(serialization.SerializeData(Sample()));

            Assert.IsTrue(read.IsUnlocked("not_in_any_catalog"));
            Assert.AreEqual(Late, read.Find("not_in_any_catalog").UnlockedUtc);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ARecord_IsWrittenUnderItsOwnDomainAndFullKeys()
        {
            var json = new SerializationService().SerializeData(Sample());

            StringAssert.StartsWith($"{{\"{Names.Generation}\":{ModelGenerations.V2_SimplifyEntrance},", json);
            StringAssert.Contains($"\"{Names.Unlocked}\":", json);
            StringAssert.Contains($"\"{Names.Id}\":\"first_clear\"", json);
            StringAssert.Contains($"\"{Names.UnlockedUtc}\":", json);
            Assert.AreEqual(ModelDomains.AchievementsRecord,
                VersionedTypeRegistry.GetDomain(typeof(AchievementsRecord)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Add_KeepsTheFirstUnlockOfAnId()
        {
            var record = new AchievementsRecord();

            Assert.IsTrue(record.Add("first_clear", Late));
            Assert.IsFalse(record.Add("first_clear", Early));

            Assert.AreEqual(1, record.Unlocked.Count);
            Assert.AreEqual(Late, record.Find("first_clear").UnlockedUtc);
        }
    }
}
