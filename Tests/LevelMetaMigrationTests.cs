using System;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Serialization;
using BH.SDK.Versions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> That a metadata.json 1.0.0 wrote still opens after ResourceMeta gained its guid slot -
    /// every record carried with the slot empty - that a generation 2 one opens unrated, and that today's
    /// shape keeps a data resource's guid across a round trip. </summary>
    [TestFixture]
    public class LevelMetaMigrationTests
    {
        private static SerializationService Service() => new(new SerializationSettings());

        private static ResourceMeta FileRecord(int id, string title)
            => new(ResourceType.Texture, new TypedResourceId(id), new StringValue(title), new StringValue(),
                string.Empty, new NoSpecifiedLicense(), new(), new());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AReleaseFile_KeepsItsRecordsWithAnEmptyGuid()
        {
            var service = Service();
            var meta = new LevelMeta();
            meta.ResourcesMeta.Add(FileRecord(-1, "cover"));
            meta.ResourcesMeta.Add(FileRecord(-2, "song"));
            meta.LevelTags.Add("hard");
            meta.LevelDuration = 42f;

            // A real write of today's file, stamped Release and with the new key taken out of every
            // record - exactly what 1.0.0 put on disk.
            var root = JObject.Parse(service.SerializeData(meta));
            root["g"] = ModelGenerations.V1_AlphaRelease;
            foreach (var token in root.SelectTokens("$..resource_guid").ToList())
                token.Parent.Remove();

            var read = service.DeserializeData<LevelMeta>(root.ToString());

            Assert.AreEqual(meta.LevelId, read.LevelId);
            Assert.AreEqual(42f, read.LevelDuration);
            CollectionAssert.AreEqual(new[] { "hard" }, read.LevelTags);
            Assert.AreEqual(2, read.ResourcesMeta.Count);
            Assert.AreEqual(-1, read.ResourcesMeta[0].ResourceId.value);
            Assert.AreEqual(-2, read.ResourcesMeta[1].ResourceId.value);
            Assert.IsTrue(read.ResourcesMeta.All(r => r.ResourceGuid == Guid.Empty));
        }

        // A 1.0.0 file said nothing about AI, and the migration must not say "no" on its behalf. The
        // source is written with Yes everywhere and the keys then stripped, so a reader that copied
        // anything through instead of stating NotSpecified would fail here.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AReleaseFile_DeclaresNothingAboutAi_AndGetsNoCoverRecord()
        {
            var service = Service();
            var meta = new LevelMeta { LevelAiGenerated = AiGeneration.Yes };
            meta.ResourcesMeta.Add(FileRecord(-1, "song"));
            meta.ResourcesMeta[0].ResourceAiGenerated = AiGeneration.Yes;

            var root = JObject.Parse(service.SerializeData(meta));
            root["g"] = ModelGenerations.V1_AlphaRelease;
            foreach (var token in root.SelectTokens("$..resource_guid").ToList())
                token.Parent.Remove();
            foreach (var token in root.SelectTokens("$..ai_generated").ToList())
                token.Parent.Remove();

            var read = service.DeserializeData<LevelMeta>(root.ToString());

            Assert.AreEqual(AiGeneration.NotSpecified, read.LevelAiGenerated);
            Assert.AreEqual(1, read.ResourcesMeta.Count);
            Assert.AreEqual(AiGeneration.NotSpecified, read.ResourcesMeta[0].ResourceAiGenerated);
            Assert.IsFalse(read.ResourcesMeta.Any(r => r.ResourceType == ResourceType.LevelLogo));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TodaysFile_KeepsADataResourcesGuid()
        {
            var service = Service();
            var themeId = Guid.NewGuid();
            var meta = new LevelMeta();
            meta.ResourcesMeta.Add(new ResourceMeta(ResourceType.Theme, TypedResourceId.Null,
                new StringValue("palette"), new StringValue(), string.Empty, new NoSpecifiedLicense(),
                new(), new(), resourceGuid: themeId));

            var json = service.SerializeData(meta);
            var read = service.DeserializeData<LevelMeta>(json);

            Assert.AreEqual(ModelGenerations.V3_ImproveAppearance, (int)JObject.Parse(json)["g"]);
            Assert.AreEqual(ResourceType.Theme, read.ResourcesMeta[0].ResourceType);
            Assert.AreEqual(themeId, read.ResourcesMeta[0].ResourceGuid);
        }

        // The source is rated, then the key is stripped: a reader copying anything through would fail.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AGeneration2File_ReadsUnrated_WithEveryOtherMemberEqual()
        {
            var service = Service();
            var meta = MockData.CreateTestLevelMeta();
            meta.LevelTags.Add("hard");
            meta.LevelDuration = 42f;
            meta.LevelDifficulty = 7;

            var root = JObject.Parse(service.SerializeData(meta));
            root["g"] = ModelGenerations.V2_SimplifyEntrance;
            Assert.IsTrue(((JObject)root[Names.Value]).Remove(Names.Difficulty));

            var read = service.DeserializeData<LevelMeta>(root.ToString());

            var expected = meta.Copy();
            expected.LevelDifficulty = DifficultyRules.Undefined;
            Assert.AreEqual(DifficultyRules.Undefined, read.LevelDifficulty);
            Assert.AreEqual(expected, read);
        }

        // The source carries a demand, then the key is stripped: a generation 2 file never measured one.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AGeneration2File_ReadsUnmeasured()
        {
            var service = Service();
            var meta = MockData.CreateTestLevelMeta();
            meta.LevelDemand = new LevelDemand();
            meta.LevelDemand.Windows.Add(new DemandWindow(0.25f, new float[DemandDimensions.Count]));

            var root = JObject.Parse(service.SerializeData(meta));
            root["g"] = ModelGenerations.V2_SimplifyEntrance;
            Assert.IsTrue(((JObject)root[Names.Value]).Remove(Names.Demand));
            ((JObject)root[Names.Value]).Remove(Names.Difficulty);

            var read = service.DeserializeData<LevelMeta>(root.ToString());

            Assert.IsNull(read.LevelDemand);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TodaysFile_KeepsItsDemand_AndOmitsAnAbsentOne()
        {
            var service = Service();
            var meta = MockData.CreateTestLevelMeta();
            var written = (JObject)JObject.Parse(service.SerializeData(meta))[Names.Value];
            Assert.IsFalse(written.ContainsKey(Names.Demand), "a null demand writes no key");

            meta.LevelDemand = new LevelDemand { Exact = false };
            var values = new float[DemandDimensions.Count];
            values[(int)DemandDimension.Glyphs] = 12.5f;
            meta.LevelDemand.Windows.Add(new DemandWindow(1.75f, values));

            var read = service.DeserializeData<LevelMeta>(service.SerializeData(meta));

            Assert.AreEqual(meta.LevelDemand, read.LevelDemand);
            Assert.IsFalse(read.LevelDemand.Exact);
            Assert.AreEqual(12.5f, read.LevelDemand.Windows[0].Get(DemandDimension.Glyphs));
        }
    }
}