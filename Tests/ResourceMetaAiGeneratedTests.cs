using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MetaAuthor = BH.SDK.Models.Meta.Author;
using ResourceMeta = BH.SDK.Models.Meta.ResourceMeta;

namespace BH.SDK.Tests
{
    // The AI declaration is a three-state value on two models, and what the generated contract cannot
    // state for itself is the part a client decides a screen from: that every state survives both
    // codecs, that a record with no key reads "unknown" rather than "no", and that "contains AI" is
    // answered by Yes alone.
    //
    // The two models are aliased rather than imported by namespace: NUnit's own [Author] attribute
    // is on every method here, and a plain `using` of BH.SDK.Models.Meta makes the name ambiguous.

    /// <summary> LevelMeta.LevelAiGenerated and ResourceMeta.ResourceAiGenerated: unspecified by
    /// default and out of a file without the key, every value survives JSON and the blob, and
    /// ContainsAiContent counts Yes only. </summary>
    public class ResourceMetaAiGeneratedTests
    {
        private static readonly AiGeneration[] AllValues =
            { AiGeneration.NotSpecified, AiGeneration.No, AiGeneration.Yes };

        private static SerializationService Service() => new(new SerializationSettings());

        private static ResourceMeta Audio(AiGeneration ai = AiGeneration.NotSpecified) => new(
            ResourceType.Audio,
            new TypedResourceId(-1),
            new StringValue("Title"),
            new StringValue(string.Empty),
            "https://example.com",
            new NoSpecifiedLicense(),
            new List<IString>(),
            new List<MetaAuthor>(),
            resourceAiGenerated: ai);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreNotSpecified()
        {
            Assert.AreEqual(AiGeneration.NotSpecified, new ResourceMeta().ResourceAiGenerated);
            Assert.AreEqual(AiGeneration.NotSpecified, Audio().ResourceAiGenerated);
            Assert.AreEqual(AiGeneration.NotSpecified, new LevelMeta().LevelAiGenerated);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryValue_SurvivesAJsonRoundTrip([ValueSource(nameof(AllValues))] AiGeneration value)
        {
            var service = Service();
            var meta = new LevelMeta { LevelAiGenerated = value };
            meta.ResourcesMeta.Add(Audio(value));

            var read = service.DeserializeData<LevelMeta>(service.SerializeData(meta));

            Assert.AreEqual(value, read.LevelAiGenerated);
            Assert.AreEqual(value, read.ResourcesMeta[0].ResourceAiGenerated);
            Assert.IsTrue(meta.Equals(read));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryValue_SurvivesABlobRoundTrip([ValueSource(nameof(AllValues))] AiGeneration value)
        {
            var blob = new SerializationService().GetDataSerializer(SerializationType.Blob);
            var meta = new LevelMeta { LevelAiGenerated = value };
            meta.ResourcesMeta.Add(Audio(value));

            var bytes = blob.SerializeEnvelope(ModelDomains.LevelMeta,
                new EnvelopeData(ModelGenerations.Current, meta));
            var read = blob.DeserializeEnvelope(bytes, typeof(LevelMeta)).GetPayload<LevelMeta>();

            Assert.AreEqual(value, read.LevelAiGenerated);
            Assert.AreEqual(value, read.ResourcesMeta[0].ResourceAiGenerated);
            Assert.IsTrue(meta.Equals(read));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AMissingKey_ReadsBackNotSpecified()
        {
            var service = Service();
            var meta = new LevelMeta { LevelAiGenerated = AiGeneration.Yes };
            meta.ResourcesMeta.Add(Audio(AiGeneration.No));

            var root = JObject.Parse(service.SerializeData(meta));
            var tokens = root.SelectTokens("$..ai_generated").ToList();
            foreach (var token in tokens)
                token.Parent.Remove();

            Assert.AreEqual(2, tokens.Count, "The declaration is not written under the expected key");
            var read = service.DeserializeData<LevelMeta>(root.ToString());

            Assert.AreEqual(AiGeneration.NotSpecified, read.LevelAiGenerated);
            Assert.AreEqual(AiGeneration.NotSpecified, read.ResourcesMeta[0].ResourceAiGenerated);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ContainsAiContent_CountsYesAlone()
        {
            var nothing = new LevelMeta();
            nothing.ResourcesMeta.Add(Audio());

            var allNo = new LevelMeta { LevelAiGenerated = AiGeneration.No };
            allNo.ResourcesMeta.Add(Audio(AiGeneration.No));

            var levelYes = new LevelMeta { LevelAiGenerated = AiGeneration.Yes };
            levelYes.ResourcesMeta.Add(Audio(AiGeneration.No));

            var recordYes = new LevelMeta { LevelAiGenerated = AiGeneration.No };
            recordYes.ResourcesMeta.Add(Audio());
            recordYes.ResourcesMeta.Add(new ResourceMeta
            {
                ResourceType = ResourceType.LevelLogo,
                ResourceAiGenerated = AiGeneration.Yes,
            });

            Assert.IsFalse(nothing.ContainsAiContent());
            Assert.IsFalse(allNo.ContainsAiContent());
            Assert.IsTrue(levelYes.ContainsAiContent());
            Assert.IsTrue(recordYes.ContainsAiContent());
        }
    }
}