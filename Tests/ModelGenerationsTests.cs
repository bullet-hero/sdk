using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BH.SDK.Versions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // A generation is a number in twenty-five places; the invariants are relations between them. LIVE
    // versus FROZEN is decided by namespace, not by the number.

    /// <summary> That the generation constants and the twenty-five attributes carrying them still agree. </summary>
    [TestFixture]
    public class ModelGenerationsTests
    {
        private const string FrozenNamespacePrefix = "BH.SDK.Versions.V";

        private static IEnumerable<(Type Type, ModelGenerationAttribute Attribute)> Tagged()
        {
            foreach (var type in typeof(ModelGenerations).Assembly.GetTypes())
            {
                var attribute = type.GetCustomAttribute<ModelGenerationAttribute>(false);
                if (attribute != null) yield return (type, attribute);
            }
        }

        private static bool IsFrozen(Type type) =>
            type.Namespace != null && type.Namespace.StartsWith(FrozenNamespacePrefix, StringComparison.Ordinal);

        private static List<(Type Type, ModelGenerationAttribute Attribute)> Live() =>
            Tagged().Where(pair => !IsFrozen(pair.Type)).ToList();

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Current_IsTheNewestGenerationAnyLiveDomainCarries()
        {
            var newest = Live().Max(pair => pair.Attribute.Generation);

            Assert.AreEqual(newest, ModelGenerations.Current,
                "Current is what UI and reports read - a domain bumped past it would be invisible there");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryLiveDomain_IsNamedOnceInModelDomains()
        {
            var declared = typeof(ModelDomains)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue())
                .ToList();

            var carried = Live().Select(pair => pair.Attribute.Domain).ToList();

            CollectionAssert.AreEquivalent(declared, carried,
                "ModelDomains and the live attributes are the same set, spelled once each");
            Assert.AreEqual(carried.Count, carried.Distinct().Count(), "one live type per domain");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void NoTwoTypes_ClaimTheSameDomainAndGeneration()
        {
            // VersionedTypeRegistry keys on exactly this pair, and a Dictionary indexer silently
            // keeps whichever type it scanned last - so a collision does not throw, it decides.
            var duplicates = Tagged()
                .GroupBy(pair => (pair.Attribute.Domain, pair.Attribute.Generation))
                .Where(group => group.Count() > 1)
                .Select(group => group.Key + ": " + string.Join(", ", group.Select(pair => pair.Type.Name)))
                .ToList();

            Assert.IsEmpty(duplicates, string.Join("\n", duplicates));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryFrozenSnapshot_IsOlderThanItsLiveDomain()
        {
            var frozen = Tagged().Where(pair => IsFrozen(pair.Type)).ToList();

            Assert.IsNotEmpty(frozen, "Versions/V0 is the only proof the migration path still works");
            foreach (var pair in frozen)
            {
                var live = VersionedTypeRegistry.GetLatestAttribute(pair.Attribute.Domain).Generation;
                Assert.Less(pair.Attribute.Generation, live, pair.Type.Name);
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Invalid_IsNegativeSoZeroStaysARealGeneration()
        {
            // The whole reason the sentinel is not zero: the frozen snapshots are written at zero,
            // so a reader treating it as "no generation" would refuse the migration path outright.
            Assert.Less(ModelGenerations.Invalid, 0);
            Assert.AreEqual(0, ModelGenerations.Test);
            Assert.Greater(ModelGenerations.V1_AlphaRelease, ModelGenerations.Test);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NothingIsRegisteredAtTheFabricatedGeneration()
        {
            // The guard under every forward fixture: the day a domain reaches the number they go backward.
            Assert.Greater(MockData.FabricatedGeneration, ModelGenerations.Current);

            foreach (var domain in Declared())
                Assert.IsNull(VersionedTypeRegistry.TryResolve(domain, MockData.FabricatedGeneration), domain);
        }

        /// <summary> Every domain name ModelDomains declares. </summary>
        private static IEnumerable<string> Declared() => typeof(ModelDomains)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue());
    }
}
