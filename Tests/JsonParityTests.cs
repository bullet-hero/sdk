using System;
using System.Linq;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Json;
using BH.SDK.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THE RULE A WITHDRAWN READER BOUGHT THIS PROJECT: anything that changes how a level is READ is
    // locked by a test comparing the SAME BYTES through the old path and the new one. Not a round
    // trip, not self-consistency - the change that shipped a game unable to open a level passed
    // 4494 tests, every one of them self-consistent, because a reader that writes and reads its own
    // mistakes agrees with itself perfectly.
    //
    // So both halves are here. WRITING is compared byte for byte against the reflective path, which
    // is the only proof that a level written by this build is the file the last one wrote. READING
    // is compared through Level.Equals over the whole graph, on bytes neither path produced.


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

        // A DEGRADED READ IS A READ OF BYTES NEITHER PATH WOULD WRITE, which is exactly the shape
        // this fixture was built for, and exactly what the corpus could never supply: every file it
        // holds sits at one generation and carries no unknown tag, so it has nothing to disagree
        // about. That is how a real divergence between the two stacks survived 4494 self-consistent
        // tests - the same nested LevelSettings reading back fps=60 one way and fps=61 the other.
        //
        // So both stacks must degrade to the SAME MODEL and report the SAME SUBSTITUTIONS. Agreeing
        // about the second is the stronger half: two readers can arrive at identical defaults for
        // opposite reasons, and only the report says which reason it was.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AnUnknownGeneration_DegradesTheSameWayThroughBothStacks()
        {
            var text = Generated.SerializeData(MockData.CreateTestLevel())
                .Replace("{\"g\":1,\"v\":{\"fps\"", "{\"g\":" + MockData.FabricatedGeneration + ",\"v\":{\"fps\"");

            var reflective = new SerializationReport();
            var generated = new SerializationReport();

            Level fromReflective, fromGenerated;
            using (SerializationReport.Begin(reflective)) fromReflective = Reflective.DeserializeData<Level>(text);
            using (SerializationReport.Begin(generated)) fromGenerated = Generated.DeserializeData<Level>(text);

            Assert.IsTrue(fromReflective.Equals(fromGenerated), "the two stacks degraded to different levels");
            CollectionAssert.AreEquivalent(Described(reflective), Described(generated),
                "the two stacks reported different substitutions");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AnUnknownTag_DegradesTheSameWayThroughBothStacks()
        {
            // `[tag, payload]` is how every polymorphic value is written, so `"v":[0,` is a keyframe's
            // own value at its family's lowest tag. Retagging them to a number nothing claims leaves
            // the payloads untouched - the model must come back identical, and the report is the only
            // evidence anything happened at all.
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
            // The check that does not need a fixture to reach a member. A byte comparison only sees
            // what MockData happens to populate; this asks Newtonsoft itself, for every model, what
            // it would have written and in which order - and compares that against what the
            // generator emitted. A member the fixtures never touch is caught here or nowhere.
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
