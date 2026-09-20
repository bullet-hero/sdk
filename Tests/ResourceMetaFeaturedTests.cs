using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MetaAuthor = BH.SDK.Models.Meta.Author;
using ResourceMeta = BH.SDK.Models.Meta.ResourceMeta;

namespace BH.SDK.Tests
{
    // ResourceFeatured is the newest member on this model and the one a CLIENT decides a whole
    // screen from - a level card names the musician when a record says it is worth naming. So the
    // cases here are the ones the generated contract cannot state for itself: that the flag is off
    // until somebody turns it on, and that a record written before the field existed reads back off
    // rather than throwing. The second one is what makes the field additive - no generation bump on
    // LevelMeta, no migrator - and it is the same claim AuthorTests makes for Author.Credit.
    //
    // The two models are aliased rather than imported by namespace: NUnit's own [Author] attribute
    // is on every method here, and a plain `using` of BH.SDK.Models.Meta makes the name ambiguous.

    /// <summary> ResourceMeta.ResourceFeatured: off by default, off out of a record that predates it,
    /// and it survives a round trip. </summary>
    public class ResourceMetaFeaturedTests
    {
        private static ResourceMeta Audio() => new(
            ResourceType.Audio,
            TypedResourceId.Null,
            new StringValue("Title"),
            new StringValue(string.Empty),
            "https://example.com",
            new NoSpecifiedLicense(),
            new List<IString>(),
            new List<MetaAuthor>());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreNotFeatured()
        {
            Assert.IsFalse(new ResourceMeta().ResourceFeatured);
        }

        // The eight-argument call every site made before the field existed still compiles, and what
        // it builds is the same "not featured" the default constructor builds.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ConstructedWithoutTheFlag_IsNotFeatured()
        {
            Assert.IsFalse(Audio().ResourceFeatured);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RecordWrittenBeforeTheField_ReadsBackNotFeatured()
        {
            var service = new SerializationService(new SerializationSettings());
            var source = Audio();
            source.ResourceFeatured = true;

            var token = (JObject)JToken.FromObject(source, service.Serializer);
            Assert.IsTrue(token.Remove("featured"), "The flag is not written under the expected key");
            var restored = token.ToObject<ResourceMeta>(service.Serializer);

            Assert.IsFalse(restored.ResourceFeatured);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Featured_SurvivesAJsonRoundTrip()
        {
            var service = new SerializationService(new SerializationSettings());
            var source = Audio();
            source.ResourceFeatured = true;

            var token = JToken.FromObject(source, service.Serializer);
            var restored = token.ToObject<ResourceMeta>(service.Serializer);

            Assert.IsTrue(restored.ResourceFeatured);
            Assert.IsTrue(source.Equals(restored));
        }
    }
}
