using System;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    public class CloudManifestTests
    {
        private static CloudManifest Sample()
        {
            var manifest = new CloudManifest();
            manifest.Levels.Add(new CloudLevelEntry(LevelId.NewId(), "my-level",
                new DateTime(2026, 10, 14, 12, 30, 0, DateTimeKind.Utc), false));
            manifest.Levels.Add(new CloudLevelEntry(LevelId.NewId(), "gone",
                new DateTime(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc), true));
            return manifest;
        }

        [TestCase(SerializationType.Json)]
        [TestCase(SerializationType.Blob)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AManifest_RoundTripsThroughItsEnvelope(SerializationType format)
        {
            var serialization = new SerializationService();
            var manifest = Sample();

            var read = serialization.DeserializeEnvelope<CloudManifest>(
                serialization.SerializeEnvelope(manifest, format), format);

            Assert.AreEqual(manifest, read);
            Assert.IsTrue(read.Levels[1].Deleted);
            Assert.AreEqual(manifest.Levels[0].ModifiedUtc, read.Levels[0].ModifiedUtc);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AManifest_IsWrittenUnderItsOwnDomainAndFullKeys()
        {
            var json = new SerializationService().SerializeData(Sample());

            StringAssert.StartsWith($"{{\"{Names.Generation}\":{ModelGenerations.V2_SimplifyEntrance},", json);
            StringAssert.Contains($"\"{Names.Levels}\":", json);
            StringAssert.Contains($"\"{Names.LevelId}\":", json);
            StringAssert.Contains($"\"{Names.Folder}\":\"my-level\"", json);
            StringAssert.Contains($"\"{Names.Deleted}\":true", json);
            Assert.AreEqual(ModelDomains.CloudManifest, VersionedTypeRegistry.GetDomain(typeof(CloudManifest)));
        }
    }
}
