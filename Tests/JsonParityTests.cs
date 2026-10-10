using System;
using System.Linq;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Json;
using BH.SDK.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Anything that changes how a level is READ is locked by the SAME BYTES through the old and the new
    // path, never a self-consistent round trip. BH.SDK/Docs/TESTING.md, "Reading and writing".


    /// <summary> The generated JSON path against the reflective one: writing compared byte for byte, reading
    /// compared through Level.Equals on bytes neither path produced. </summary>
    [TestFixture]
    public class JsonParityTests
    {
        /// <summary> The reflective path: contract resolver, thirty-five converters, no generated
        /// code. What every level on disk was written by. </summary>
        private static readonly SerializationService Reflective =
            SerializationService.CreateWithoutGeneratedCodecs();

        /// <summary> What the game uses now. </summary>
        private static readonly SerializationService Generated = new();

        #region Writing

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ATestLevel_IsWrittenByteForByte()
        {
            var level = MockData.CreateTestLevel();

            Assert.AreEqual(
                Reflective.SerializeData(level),
                Generated.SerializeData(level));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ATestLevelMeta_IsWrittenByteForByte()
        {
            var meta = MockData.CreateTestLevelMeta();

            Assert.AreEqual(
                Reflective.SerializeData(meta),
                Generated.SerializeData(meta));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ValidSettings_AreWrittenByteForByte()
        {
            var settings = MockData.CreateValidTestSettings();

            Assert.AreEqual(
                Reflective.SerializeData(settings),
                Generated.SerializeData(settings));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ALevelOfEveryShape_IsWrittenByteForByte()
        {
            // The fixture factory reaches as much field surface as it can while staying rule-valid,
            // which is exactly what a format comparison wants: the members nobody sets are the ones
            // a generated writer would be free to get wrong.
            var level = MockData.CreateLargeTestLevel(120, 6, 5);

            Assert.AreEqual(
                Reflective.SerializeData(level),
                Generated.SerializeData(level));
        }

        #endregion

        #region Reading

        // The id remap table is written as {k,v} pairs; both paths once disagreed on the CASE of the letters.

        // A PrefabObject is not a serialization ROOT (no [ModelGeneration]), so both halves below go
        // through the level that holds it - which is also the only shape the format ever writes one in.

        /// <summary> The pair table both paths write, compared byte for byte. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void APrefabIdRemapTable_IsWrittenByteForByte()
        {
            var level = LevelWithPlacement();

            var written = Generated.SerializeData(level);
            Assert.IsTrue(written.Contains("\"k\":"), "the fixture carries no pair table: " + written);
            Assert.AreEqual(Reflective.SerializeData(level), written);
        }

        /// <summary> Every level already on disk spells the pair keys with capitals, and a reader that
        /// stopped answering them would drop the whole table to one default entry without a word. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void APrefabIdRemapTable_StillReadsTheCapitalSpelling()
        {
            var lower = Generated.SerializeData(LevelWithPlacement());
            // Only inside the pair itself: a blanket ,"v": would also rewrite the envelope's own
            // payload key and every keyframe value in the level, and the document would stop parsing.
            var upper = Regex.Replace(lower, "\\{\"k\":(-?\\d+),\"v\":(-?\\d+)\\}", "{\"K\":$1,\"V\":$2}");
            Assert.AreNotEqual(lower, upper, "the fixture carries no pair table, so this proves nothing");

            var fromUpper = Generated.DeserializeData<Level>(upper);
            var placement = (PrefabObject)fromUpper.Game.Objects[new ObjectId(7)];

            Assert.AreEqual(3, placement.ObjectIds.Count, "a collapsed table reads back as one entry");
            Assert.AreEqual(new ObjectId(261), placement.ObjectIds[new ObjectId(1)]);
            Assert.AreEqual(new ObjectId(263), placement.ObjectIds[new ObjectId(3)]);
        }

        private static Level LevelWithPlacement()
        {
            var level = new Level();
            var placement = MockData.CreateTestPrefabPlacement();
            level.Game.Objects.Add(placement.ObjectId, placement);
            return level;
        }


        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ALevel_ReadsToTheSameGraphAsTheReflectivePath()
        {
            var text = Reflective.SerializeData(MockData.CreateLargeTestLevel(120, 6, 5));

            var reflective = Reflective.DeserializeData<Level>(text);
            var generated = Generated.DeserializeData<Level>(text);

            Assert.IsTrue(reflective.Equals(generated),
                "the two readers disagree about what the same bytes mean");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void AMeta_ReadsToTheSameGraphAsTheReflectivePath()
        {
            var text = Reflective.SerializeData(MockData.CreateTestLevelMeta());

            Assert.IsTrue(Reflective.DeserializeData<LevelMeta>(text)
                .Equals(Generated.DeserializeData<LevelMeta>(text)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AnUnknownProperty_IsSkippedRatherThanRefused()
        {
            // What makes an additive member free in both directions: a file from a NEWER build has
            // properties this one has never heard of, and it still has to open.
            var text = Generated.SerializeData(MockData.CreateTestLevel())
                .Replace("\"settings\":{", "\"nobody_knows_this\":[1,2,{\"a\":null}],\"settings\":{");

            Assert.DoesNotThrow(() => Generated.DeserializeData<Level>(text));
        }

        // A degraded read is bytes neither path would write, which the corpus cannot supply: both stacks
        // must give the SAME MODEL and the SAME SUBSTITUTIONS (the report says which reason).

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ANewerGeneration_IsRefusedTheSameWayByBothStacks()
        {
            // A newer generation is no longer a degraded read but a refusal, and the two stacks must
            // agree on WHAT they refused - the domain and the generation - not merely that they threw.
            var text = Generated.SerializeData(MockData.CreateTestLevel())
                .Replace("{\"g\":1,\"v\":{\"fps\"", "{\"g\":" + MockData.FabricatedGeneration + ",\"v\":{\"fps\"");

            var fromReflective = Assert.Throws<NewerGenerationException>(() => Reflective.DeserializeData<Level>(text));
            var fromGenerated = Assert.Throws<NewerGenerationException>(() => Generated.DeserializeData<Level>(text));

            Assert.AreEqual(ModelDomains.LevelSettings, fromReflective.Domain);
            Assert.AreEqual(fromReflective.Domain, fromGenerated.Domain);
            Assert.AreEqual(MockData.FabricatedGeneration, fromReflective.FileGeneration);
            Assert.AreEqual(fromReflective.FileGeneration, fromGenerated.FileGeneration);
            Assert.AreEqual(fromReflective.BuildGeneration, fromGenerated.BuildGeneration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AnUnknownTag_DegradesTheSameWayThroughBothStacks()
        {
            // Retagging every keyframe value to an unclaimed tag: the model is identical, the report is
            // the only evidence.
            var text = Generated.SerializeData(MockData.CreateTestLevel())
                .Replace("\"v\":[0,", "\"v\":[" + MockData.FabricatedTag + ",");

            var reflective = new SerializationReport();
            var generated = new SerializationReport();

            Level fromReflective, fromGenerated;
            using (SerializationReport.Begin(reflective)) fromReflective = Reflective.DeserializeData<Level>(text);
            using (SerializationReport.Begin(generated)) fromGenerated = Generated.DeserializeData<Level>(text);

            Assert.IsTrue(fromReflective.Equals(fromGenerated), "the two stacks degraded to different levels");
            Assert.IsNotEmpty(generated.Entries, "the generated stack reported nothing");
            CollectionAssert.AreEquivalent(Described(reflective), Described(generated),
                "the two stacks reported different substitutions");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void AV0Level_MigratesToTheSameLevelThroughBothStacks()
        {
            // The backward direction through the same lens. It could not be written before either
            // half of it existed: a snapshot had no codec, so the generated stack had nothing to read
            // one WITH and the comparison had only one side.
            var text = Generated.SerializeData(MockData.CreateTestLevelV0());

            var fromReflective = Reflective.DeserializeData<Level>(text);
            var fromGenerated = Generated.DeserializeData<Level>(text);

            Assert.IsTrue(fromReflective.Equals(fromGenerated), "the two stacks migrated to different levels");
        }

        /// <summary> A report as text, so two of them can be compared without depending on the order
        /// two readers happen to meet the same substitutions in. </summary>
        private static string[] Described(SerializationReport report)
            => report.Entries.Select(e => e.ToString()).ToArray();

        #endregion

        #region The member list itself

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryModel_WritesTheMembersItsContractDeclares()
        {
            // Asks Newtonsoft itself what it would write, for every model: catches members no fixture sets.
            var resolver = new SerializationService.ContractResolver(new SerializationSettings());
            var failures = new System.Collections.Generic.List<string>();
            var swept = 0;

            foreach (var type in typeof(IJsonModel).Assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface) continue;
                if (!typeof(IJsonModel).IsAssignableFrom(type)) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                var contract = resolver.ResolveContract(type) as JsonObjectContract;
                if (contract == null) continue;

                var instance = (IJsonModel)Activator.CreateInstance(type);

                // A null SUB-MODEL is omitted by both writers, name included - that is what makes
                // null affordable as a third state. So the contract's answer for THIS instance is
                // what the reflective path would write for it, not every property it declares.
                var expected = contract.Properties
                    .Where(p => !p.Ignored)
                    .Where(p => p.NullValueHandling != NullValueHandling.Ignore
                                || p.ValueProvider?.GetValue(instance) != null)
                    .Select(p => p.PropertyName)
                    .ToList();

                var actual = Written(instance);
                swept++;

                if (!expected.SequenceEqual(actual))
                    failures.Add($"{type.Name}\n    contract:  {string.Join(", ", expected)}"
                                 + $"\n    generated: {string.Join(", ", actual)}");
            }

            Assert.Greater(swept, 150, "the reflection filter matched almost nothing");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        /// <summary> The property names a model's generated writer emits, in order. </summary>
        private static System.Collections.Generic.List<string> Written(IJsonModel model)
        {
            var names = new System.Collections.Generic.List<string>();
            using var text = new System.IO.StringWriter();
            using var writer = new JsonTextWriter(text);

            model.WriteJson(writer);

            using var reader = new JsonTextReader(new System.IO.StringReader(text.ToString()));
            var depth = 0;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray) depth++;
                else if (reader.TokenType == JsonToken.EndObject || reader.TokenType == JsonToken.EndArray) depth--;
                // Only this object's own properties - anything deeper belongs to a member.
                else if (reader.TokenType == JsonToken.PropertyName && depth == 1)
                    names.Add((string)reader.Value);
            }

            return names;
        }

        #endregion
    }
}