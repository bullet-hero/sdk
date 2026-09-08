using System;
using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Versions;
using BH.SDK.Versions.V0;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THE REGISTRY HAD NO FIXTURE OF ITS OWN until this one, and it is the piece the whole format
    // rests on: which type a generation names, and how an old instance walks up to today's. It was
    // covered only indirectly, through a full Level migration - so a wrong refusal or a chain that
    // stopped one step early looked like a serialization failure somewhere else entirely.

    /// <summary> That a generation resolves back to its own type, that an unknown one is refused rather than
    /// guessed at, and that the V0 chain still walks the whole way up. </summary>
    [TestFixture]
    public class VersionedTypeRegistryTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Resolve_AnswersTheTypeThatCarriesThatGeneration()
        {
            Assert.AreEqual(typeof(Level), VersionedTypeRegistry.Resolve(ModelDomains.Level, ModelGenerations.Release));
            Assert.AreEqual(typeof(LevelV0), VersionedTypeRegistry.Resolve(ModelDomains.Level, ModelGenerations.Test));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Resolve_RefusesAnUnknownDomainAndAnUnknownGeneration()
        {
            // Two different mistakes, and both have to be refusals rather than a fallback to the
            // current shape: reading a payload as a type it was not written in is silent corruption.
            Assert.Throws<NotSupportedException>(
                () => VersionedTypeRegistry.Resolve("not_a_domain", ModelGenerations.Release));
            Assert.Throws<NotSupportedException>(
                () => VersionedTypeRegistry.Resolve(ModelDomains.Level, 999));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void UpgradeToLatest_WalksTheWholeChainAndLeavesTodaysTypeBehind()
        {
            var old = MockData.CreateTestLevelV0();

            var upgraded = VersionedTypeRegistry.UpgradeToLatest(ModelDomains.Level, old, ModelGenerations.Test);

            Assert.IsInstanceOf<Level>(upgraded);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void UpgradeToLatest_IsANoOpAtTheCurrentGeneration()
        {
            var level = new Level();

            var upgraded = VersionedTypeRegistry.UpgradeToLatest(ModelDomains.Level, level, ModelGenerations.Release);

            Assert.AreSame(level, upgraded, "nothing to walk, so nothing is rebuilt");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GetLatestAttribute_AnswersTheNewestGenerationOfADomain()
        {
            var latest = VersionedTypeRegistry.GetLatestAttribute(ModelDomains.Level);

            Assert.AreEqual(ModelDomains.Level, latest.Domain);
            Assert.AreEqual(ModelGenerations.Release, latest.Generation);
            Assert.Throws<NotSupportedException>(() => VersionedTypeRegistry.GetLatestAttribute("not_a_domain"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CanConvert_IsTrueForABoundaryAndFalseForAnOrdinaryModel()
        {
            Assert.IsTrue(VersionedTypeRegistry.CanConvert(typeof(Level)));
            Assert.IsTrue(VersionedTypeRegistry.CanConvert(typeof(Prefab)));
            Assert.IsFalse(VersionedTypeRegistry.CanConvert(typeof(RectObject)));

            Assert.Throws<ArgumentException>(() => VersionedTypeRegistry.ThrowIfNoDomain(typeof(RectObject)));
            Assert.DoesNotThrow(() => VersionedTypeRegistry.ThrowIfNoDomain(typeof(Level)));
        }
    }
}
