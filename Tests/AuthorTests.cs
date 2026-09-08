using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MetaAuthor = BH.SDK.Models.Meta.Author;

namespace BH.SDK.Tests
{
    // Credit is the third field on this model and the only one that can be empty on a complete
    // record - a level says who, not always what. So the tests it needs are the ones the generated
    // contract cannot state for itself: that an empty credit is a real StringValue rather than a
    // null waiting to throw, and that a record written before the field reads back as one.
    //
    // The model is aliased because NUnit's own [Author] attribute is on every method here, and a
    // plain `using` of its namespace would make the name ambiguous between the two.


    /// <summary> MetaAuthor's optional credit: that an empty one is a real value rather than a null, and that a
    /// record written before the field reads back as one. </summary>
    public class AuthorTests
    {
        private static MetaAuthor Authored() => new(new StringValue("vertoker"),
            "https://example.com", new StringValue("music"));

        private static string Text(IString value) => ((StringValue)value).Value;

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreAnEmptyCredit()
        {
            var author = new MetaAuthor();

            Assert.IsNotNull(author.Credit);
            Assert.IsEmpty(Text(author.Credit));
        }

        // The two-argument call every site made before the field existed still compiles, and what it
        // builds has to be the same "no credit" a default constructor builds - not a null.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ConstructedWithoutACredit_StillCarriesAnEmptyOne()
        {
            var author = new MetaAuthor(new StringValue("vertoker"), "https://example.com");

            Assert.IsNotNull(author.Credit);
            Assert.IsEmpty(Text(author.Credit));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Copy_DoesNotShareTheCreditInstance()
        {
            var source = Authored();

            var copy = source.Copy();

            Assert.AreEqual("music", Text(copy.Credit));
            Assert.AreNotSame(source.Credit, copy.Credit);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Equals_DistinguishesTheCredit()
        {
            var source = Authored();
            var other = source.Copy();
            other.Credit = new StringValue("cover art");

            Assert.IsTrue(source.Equals(source.Copy()));
            Assert.IsFalse(source.Equals(other));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsTheCredit()
        {
            var service = new SerializationService(new SerializationSettings());
            var source = Authored();

            var token = JToken.FromObject(source, service.Serializer);
            var restored = token.ToObject<MetaAuthor>(service.Serializer);

            Assert.AreEqual("music", Text(restored.Credit));
            Assert.IsTrue(source.Equals(restored));
        }

        // The claim that made the field additive: no DataVersion bump on LevelMeta, no migrator. An
        // absent key leaves the constructor's empty StringValue standing, which is what a record
        // written before credits existed means.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AuthorWrittenBeforeTheField_ReadsBackAnEmptyCredit()
        {
            var service = new SerializationService(new SerializationSettings());

            var token = (JObject)JToken.FromObject(Authored(), service.Serializer);
            Assert.IsTrue(token.Remove("credit"), "The credit is not written under the expected key");
            var restored = token.ToObject<MetaAuthor>(service.Serializer);

            Assert.IsNotNull(restored.Credit);
            Assert.IsEmpty(Text(restored.Credit));
            Assert.AreEqual("vertoker", Text(restored.Name));
        }
    }
}
