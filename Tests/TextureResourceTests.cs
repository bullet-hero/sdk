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
    // and there are six of them now - Kind, Alpha, Sampling, Compression, WrapU and WrapV. Every one
    // is carried through the constructors, Reset, Copy, Update, Pull, Equals and GetHashCode, which is
    // exactly the shape the SDK's own conventions call the easiest place to introduce a silent bug: a
    // field forgotten in Copy or Equals compiles, and the level simply loses it on the next round
    // trip. That is generated code today rather than hand-written, and the point of asserting it per
    // field is that a member the generator cannot see fails HERE rather than in somebody's level.
    //
    // The additive-default property is pinned too: every one of the six defaults to its zero value,
    // which is why LevelResources needed no migration and stays at generation 1 - a level written
    // before them reads back as Auto/Auto/Auto/Auto/Clamp/Clamp, which IS the behaviour it had.

    /// <summary> The six authored fields on TextureResource, each carried through seven bodies - and
    /// that all six default to their zero value is why the resources domain needed no migration.
    /// </summary>
    public class TextureResourceTests
    {
        private static TextureResource Authored()
            => new(new TextureResourceId(-3), new Vector4Value(2f, 2f, 0.25f, 0.5f),
                TextureKind.Gradient, TextureAlpha.Opaque, TextureSampling.Sharp,
                TextureCompressionKind.Allow, TextureWrapKind.Mirror, TextureWrapKind.Repeat,
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
            Assert.AreEqual(TextureSampling.Auto, resource.Sampling);
            Assert.AreEqual(TextureCompressionKind.Auto, resource.Compression);
            Assert.AreEqual(TextureWrapKind.Clamp, resource.WrapU);
            Assert.AreEqual(TextureWrapKind.Clamp, resource.WrapV);
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
            Assert.AreEqual(TextureSampling.Auto, resource.Sampling);
            Assert.AreEqual(TextureCompressionKind.Auto, resource.Compression);
            Assert.AreEqual(TextureWrapKind.Clamp, resource.WrapU);
            Assert.AreEqual(TextureWrapKind.Clamp, resource.WrapV);
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

            var otherSampling = Authored();
            otherSampling.Sampling = TextureSampling.Smooth;

            var otherCompression = Authored();
            otherCompression.Compression = TextureCompressionKind.Refuse;

            var otherWrapU = Authored();
            otherWrapU.WrapU = TextureWrapKind.Clamp;

            // The one the per-axis split exists for: two resources differing only in what happens
            // past the TOP edge are different resources, and a single Wrap could not say so.
            var otherWrapV = Authored();
            otherWrapV.WrapV = TextureWrapKind.Clamp;

            Assert.AreNotEqual(baseline, otherKind);
            Assert.AreNotEqual(baseline, otherAlpha);
            Assert.AreNotEqual(baseline, otherSampling);
            Assert.AreNotEqual(baseline, otherCompression);
            Assert.AreNotEqual(baseline, otherWrapU);
            Assert.AreNotEqual(baseline, otherWrapV);
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
            Assert.AreEqual(source.Sampling, restored.Sampling);
            Assert.AreEqual(source.Compression, restored.Compression);
            Assert.AreEqual(source.WrapU, restored.WrapU);
            Assert.AreEqual(source.WrapV, restored.WrapV);
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
            Assert.AreEqual(TextureSampling.Auto, restored.Sampling);
            Assert.AreEqual(TextureCompressionKind.Auto, restored.Compression);
            Assert.AreEqual(TextureWrapKind.Clamp, restored.WrapU);
            Assert.AreEqual(TextureWrapKind.Clamp, restored.WrapV);
        }

        // The two axes are two KEYS, and a file carrying only one of them is what a hand edit and an
        // older writer both look like. Neither may bleed into the other: reading wrap_u into both is
        // the shortcut this split exists to refuse.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OneWrapAxisInTheFile_LeavesTheOtherAtItsDefault()
        {
            var restored = JsonConvert.DeserializeObject<TextureResource>("{\"wrap_u\":1}");

            Assert.AreEqual(TextureWrapKind.Repeat, restored.WrapU);
            Assert.AreEqual(TextureWrapKind.Clamp, restored.WrapV);
        }
    }
}
