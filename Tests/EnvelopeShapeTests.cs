using System;
using BH.SDK.Models;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The envelope is the one part no model owns: two one-character keys, generation a NUMBER. Zero is
    // read with a presence flag because frozen snapshots are generation 0.

    /// <summary> That the envelope on disk is <c>{"g": int, "v": ...}</c>, and that generation zero is read as a
    /// value rather than as an absent one. </summary>
    [TestFixture]
    public class EnvelopeShapeTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AnEnvelope_IsTheGenerationAsANumberBesideItsPayload()
        {
            var json = new SerializationService().SerializeData(new Level());

            var envelope = JObject.Parse(json);

            Assert.AreEqual(2, envelope.Count, "an envelope carries the generation and the payload, nothing else");
            Assert.AreEqual(JTokenType.Integer, envelope[Names.Generation].Type,
                "a number, never the \"major.minor\" string this replaced");
            Assert.AreEqual(ModelGenerations.V1_AlphaRelease, envelope[Names.Generation].Value<int>());
            Assert.AreEqual(JTokenType.Object, envelope[Names.Value].Type);
            Assert.IsNull(envelope[Names.Version], "\"vrs\" is the AUTHOR's version of a level, never the envelope's");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ANestedDomain_CarriesItsOwnEnvelope()
        {
            var json = new SerializationService().SerializeData(new Level());

            var settings = JObject.Parse(json)[Names.Value]![Names.Settings];

            Assert.AreEqual(ModelGenerations.V1_AlphaRelease, settings![Names.Generation]!.Value<int>(),
                "a domain nested inside another is wrapped by whoever holds it - the generator's own branch");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GenerationZero_IsReadAsAValueRatherThanAsAMissingOne()
        {
            var service = new SerializationService();

            // Written from the real V0 snapshot, so the tag is generation 0 because the type says
            // so - not because a literal in this file says so.
            var json = service.SerializeData(MockData.CreateTestLevelSettingsV0());
            Assert.AreEqual(ModelGenerations.Test, JObject.Parse(json)[Names.Generation]!.Value<int>());

            // Read back as the CURRENT type: the snapshot type is a deserialization target only.
            var read = service.DeserializeData<LevelSettings>(json);

            Assert.IsNotNull(read, "generation 0 resolves to the V0 snapshot and migrates, rather than being refused");
            Assert.AreEqual(61, read.Fps, "the value the V0 fixture carries, walked up by LevelSettingsV0ToV1");
        }

        // A NESTED domain at another generation is reported, not read silently into today's class
        // (Docs/Issues/FORWARD_COMPATIBILITY_HISTORY.md).

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ANestedDomainAtAKnownGeneration_MigratesRatherThanRefusing()
        {
            var service = new SerializationService();
            var json = WithNestedSettingsGeneration(service, ModelGenerations.Test, "\"test_fps\":61");

            var report = new SerializationReport();
            Level level;
            using (SerializationReport.Begin(report)) level = service.DeserializeData<Level>(json);

            // Generation 0 resolves to LevelSettingsV0, whose migrator carries the framerate up.
            Assert.AreEqual(61, level.Settings.Fps);
            Assert.That(report.Entries, Has.Some.Matches<SerializationSubstitution>(e =>
                e.Kind == SubstitutionKind.MigratedGeneration
                && e.Domain == ModelDomains.LevelSettings));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ANestedDomainAtANewerGeneration_RefusesTheWholeFile()
        {
            var service = new SerializationService();
            var json = WithNestedSettingsGeneration(service, MockData.FabricatedGeneration, "\"fps\":61");

            // Nothing can migrate a shape no build has ever seen, and reading it by property name
            // would open a level silently missing whatever moved - so the nested envelope refuses
            // the level around it rather than degrading alone.
            var refused = Assert.Throws<NewerGenerationException>(() => service.DeserializeData<Level>(json));

            Assert.AreEqual(ModelDomains.LevelSettings, refused.Domain);
            Assert.AreEqual(MockData.FabricatedGeneration, refused.FileGeneration);
            Assert.AreEqual(VersionedTypeRegistry.GetLatestAttribute(ModelDomains.LevelSettings).Generation,
                refused.BuildGeneration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ANestedEnvelopeWithNoGenerationAtAll_DegradesAndIsReported()
        {
            var service = new SerializationService();

            var json = service.SerializeData(new Level());
            var start = json.IndexOf("\"settings\":{", StringComparison.Ordinal);
            var open = json.IndexOf('{', start + 11);
            var end = MatchingBrace(json, open);
            json = json.Substring(0, open) + "{\"v\":{\"fps\":61}}" + json.Substring(end + 1);

            var report = new SerializationReport();
            Level level;
            using (SerializationReport.Begin(report)) level = service.DeserializeData<Level>(json);

            Assert.AreEqual(61, level.Settings.Fps);
            Assert.That(report.Entries, Has.Some.Matches<SerializationSubstitution>(e =>
                e.Kind == SubstitutionKind.AbsentGeneration
                && e.Domain == ModelDomains.LevelSettings));
        }

        /// <summary> The document a level is, with its nested settings envelope rewritten to claim
        /// another generation. Built from a real write, so only the one envelope under test differs. </summary>
        private static string WithNestedSettingsGeneration(SerializationService service, int generation, string payload)
        {
            var json = service.SerializeData(new Level());

            var start = json.IndexOf("\"settings\":{", StringComparison.Ordinal);
            var open = json.IndexOf('{', start + 11);
            var end = MatchingBrace(json, open);

            var replacement = "{\"" + Names.Generation + "\":" + generation + ",\"" + Names.Value + "\":{" + payload +
                              "}}";
            return json.Substring(0, open) + replacement + json.Substring(end + 1);
        }

        private static int MatchingBrace(string json, int open)
        {
            var depth = 0;
            for (var i = open; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}' && --depth == 0) return i;
            }

            Assert.Fail("unbalanced document");
            return -1;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TheGenerationSurvivesARoundTripThroughEitherFormat()
        {
            var service = new SerializationService();

            foreach (var format in new[] { SerializationType.Json, SerializationType.Blob })
            {
                var serializer = service.GetDataSerializer(format);
                var bytes = serializer.SerializeEnvelope(ModelDomains.Level,
                    new EnvelopeData(ModelGenerations.V1_AlphaRelease, MockData.CreateTestLevel()));

                var envelope = serializer.DeserializeEnvelope(bytes, typeof(Level));

                Assert.AreEqual(ModelGenerations.V1_AlphaRelease, envelope.Generation, format.ToString());
            }
        }
    }
}