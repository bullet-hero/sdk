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
    // THE ENVELOPE IS THE ONE PART OF THE FORMAT NO MODEL OWNS, so nothing else pins its shape.
    // Two keys, both one character, and the generation is a NUMBER - it used to be the string
    // "1.0" under the same key the author's own level version uses, which is what this replaced.
    //
    // ZERO IS THE CASE WORTH A TEST OF ITS OWN. Every reader here asks "did we read a generation"
    // with a flag rather than by comparing the value, because the frozen snapshots are written at
    // generation 0 - a reader that treated zero as absent would refuse exactly the files the
    // migration path exists for, and would do it only for those.

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
            Assert.AreEqual(ModelGenerations.Release, envelope[Names.Generation].Value<int>());
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

            Assert.AreEqual(ModelGenerations.Release, settings![Names.Generation]!.Value<int>(),
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

            // READ BACK AS THE CURRENT TYPE, never as the snapshot: the converter resolves the
            // generation to LevelSettingsV0, reads that, and hands back what the migration chain
            // produced. Asking for the snapshot type is what throws - it is a deserialization
            // target, not something a caller ever holds.
            var read = service.DeserializeData<LevelSettings>(json);

            Assert.IsNotNull(read, "generation 0 resolves to the V0 snapshot and migrates, rather than being refused");
            Assert.AreEqual(61, read.Fps, "the value the V0 fixture carries, walked up by LevelSettingsV0ToV1");
        }

        // A NESTED domain at another generation is REFUSED, and this is the fixture that exists
        // because it used to be read silently: the payload was matched by property name against
        // today's class, every field missed, and the object came back as constructor defaults with
        // nothing thrown. The two read paths gave two different answers for the same file and the
        // quiet one was the default.
        //
        // Refusing is not the same as migrating, and is not meant to be - a generated codec reads
        // only itself. Docs/VERSIONING.md carries what closing it properly would take.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ANestedDomainAtAnotherGeneration_IsRefusedRatherThanReadAsDefaults()
        {
            var service = new SerializationService();
            var json = WithNestedSettingsGeneration(service, 0, "\"test_fps\":61");

            var thrown = Assert.Throws<JsonSerializationException>(
                () => service.DeserializeData<Level>(json));

            Assert.That(thrown.Message, Does.Contain("generation 0"));
            Assert.That(thrown.Message, Does.Contain(nameof(LevelSettings)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ANestedEnvelopeWithNoGenerationAtAll_IsRefusedToo()
        {
            // Every writer this format has had emits the tag, so its absence is a damaged or foreign
            // document rather than an old one - and accepting it would reopen the same hole.
            var service = new SerializationService();

            var json = service.SerializeData(new Level());
            var start = json.IndexOf("\"settings\":{", StringComparison.Ordinal);
            var open = json.IndexOf('{', start + 11);
            var end = MatchingBrace(json, open);
            json = json.Substring(0, open) + "{\"v\":{}}" + json.Substring(end + 1);

            var thrown = Assert.Throws<JsonSerializationException>(
                () => service.DeserializeData<Level>(json));

            Assert.That(thrown.Message, Does.Contain(Names.Generation));
        }

        /// <summary> The document a level is, with its nested settings envelope rewritten to claim
        /// another generation. Built from a real write, so only the one envelope under test differs. </summary>
        private static string WithNestedSettingsGeneration(SerializationService service, int generation, string payload)
        {
            var json = service.SerializeData(new Level());

            var start = json.IndexOf("\"settings\":{", StringComparison.Ordinal);
            var open = json.IndexOf('{', start + 11);
            var end = MatchingBrace(json, open);

            var replacement = "{\"" + Names.Generation + "\":" + generation + ",\"" + Names.Value + "\":{" + payload + "}}";
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
                    new EnvelopeData(ModelGenerations.Release, MockData.CreateTestLevel()));

                var envelope = serializer.DeserializeEnvelope(bytes, typeof(Level));

                Assert.AreEqual(ModelGenerations.Release, envelope.Generation, format.ToString());
            }
        }
    }
}
