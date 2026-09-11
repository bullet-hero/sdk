using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BH.SDK.Versions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // WHAT A GENERATION IS CANNOT BE CHECKED BY READING ONE FILE, which is why this fixture exists
    // at all: a generation is a number written in twenty-five places, and the invariants that make
    // it mean anything are all relations between them. Current has to be the newest one actually
    // carried; a domain has to be spelled once; two types may not claim the same (domain,
    // generation), or resolving one back to a type is a coin toss.
    //
    // LIVE VERSUS FROZEN IS DECIDED BY NAMESPACE, not by the number. Reading it off the number
    // would assume the answer - that Test is 0 and Release is 1 - which is exactly what these
    // assertions are for.

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
        public void EveryFrozenSnapshot_ReadsTheTestGeneration()
        {
            var frozen = Tagged().Where(pair => IsFrozen(pair.Type)).ToList();

            Assert.IsNotEmpty(frozen, "Versions/V0 is the only proof the migration path still works");
            foreach (var pair in frozen)
            {
                Assert.AreEqual(ModelGenerations.Test, pair.Attribute.Generation, pair.Type.Name);
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
            Assert.Greater(ModelGenerations.Release, ModelGenerations.Test);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NothingIsRegisteredAtTheFabricatedGeneration()
        {
            // THE GUARD UNDER EVERY FORWARD FIXTURE. They claim a generation no build has ever
            // written, and they are only testing the forward direction for as long as that stays
            // true - the day a domain reaches the fabricated number, every one of them quietly
            // becomes a backward test that still passes and no longer covers what it was written for.
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
