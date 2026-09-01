using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // TextureResource is the only resource carrying authored fields beyond its id, UV and sources,
    // and there are three of them now - Kind, Alpha and Wrap. All three are hand-written boilerplate
    // in six places (both constructors that take them, Reset, CopyImpl, Update, Pull, Equals,
    // GetHashCode), which is exactly the shape the SDK's own conventions call the easiest place to
    // introduce a silent bug: a field forgotten in Copy or Equals compiles, and the level simply
    // loses it on the next round trip.
    //
    // The additive-default property is pinned too. All three default to their zero value, which is
    // why LevelResources needed no migration and stays at (1, 0) - a level written before any of
    // them reads back as Auto/Auto/Clamp, which IS the behaviour it already had.
    public class TextureResourceTests
    {
        private static TextureResource Authored()
            => new(new TextureResourceId(-3), new Vector4Value(2f, 2f, 0.25f, 0.5f),
                TextureKind.Gradient, TextureAlpha.Opaque, TextureWrapKind.Mirror,
                new List<ResourceKey> { new(ResourceUriType.LevelPath, "art/sky.png") });

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreTheBehaviourThatAlreadyExisted()
        {
            var resource = new TextureResource();

            Assert.AreEqual(TextureKind.Auto, resource.Kind);
            Assert.AreEqual(TextureAlpha.Auto, resource.Alpha);
            Assert.AreEqual(TextureWrapKind.Clamp, resource.Wrap);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_RestoresThoseDefaults()
        {
            var resource = Authored();

            resource.Reset();

            Assert.AreEqual(TextureKind.Auto, resource.Kind);
            Assert.AreEqual(TextureAlpha.Auto, resource.Alpha);
            Assert.AreEqual(TextureWrapKind.Clamp, resource.Wrap);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CopyUpdateAndPull_CarryEveryAuthoredField()
        {
            var source = Authored();

            var copy = (TextureResource)source.Copy();

            var updated = new TextureResource();
            updated.Update(source);

            var pulled = new TextureResource();
            pulled.Pull(source);

            Assert.AreEqual(source, copy);
            Assert.AreEqual(source, updated);
            Assert.AreEqual(source, pulled);
        }

        // Equality is what every other check here leans on, so it is asserted per field rather than
        // trusted: a field missing from Equals makes every one of the tests above pass while the
        // field itself is silently dropped.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Equality_SeesEachAuthoredFieldOnItsOwn()
        {
            var baseline = Authored();

            var otherKind = Authored();
            otherKind.Kind = TextureKind.Photo;

            var otherAlpha = Authored();
            otherAlpha.Alpha = TextureAlpha.Auto;

            var otherWrap = Authored();
            otherWrap.Wrap = TextureWrapKind.Repeat;

            Assert.AreNotEqual(baseline, otherKind);
            Assert.AreNotEqual(baseline, otherAlpha);
            Assert.AreNotEqual(baseline, otherWrap);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsEveryAuthoredField()
        {
            var source = Authored();

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<TextureResource>(json);

            Assert.AreEqual(source.Kind, restored.Kind);
            Assert.AreEqual(source.Alpha, restored.Alpha);
            Assert.AreEqual(source.Wrap, restored.Wrap);
        }

        // The reason none of this needed a migration: the keys are simply absent from an older file,
        // so Newtonsoft leaves the constructor's values in place.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnOlderFileWithoutTheKeys_ReadsBackAsTheOldBehaviour()
        {
            var restored = JsonConvert.DeserializeObject<TextureResource>("{}");

            Assert.AreEqual(TextureKind.Auto, restored.Kind);
            Assert.AreEqual(TextureAlpha.Auto, restored.Alpha);
            Assert.AreEqual(TextureWrapKind.Clamp, restored.Wrap);
        }
    }
}
