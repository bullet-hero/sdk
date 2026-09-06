using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THE GENERATED TABLE IS CHECKED AGAINST REFLECTION, which is the one thing reflection is still
    // allowed to do here: ModificationImplementations is written at compile time from every
    // [GenerateModel] type, and the claim that this covers the polymorphic families is exactly what
    // a runtime sweep can falsify. A test may scan the assembly freely - what may not is the
    // library, which has to survive an IL2CPP build where a type nothing references statically is
    // stripped and a scan would quietly come back short.
    //
    // WHAT THIS REPLACED was a hand-written list of implementations in ModificationUtils' static
    // constructor. It had gone stale in the way such lists do - three Color3 variants and both
    // keyframe families were absent - and the failure was invisible: a prefab override addressed at
    // one of them resolved to no registered property and was dropped without a word.
    public class ModificationRegistryTests
    {
        private const string InterfaceRoot = "BH.SDK.Models.Interfaces";

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryImplementationInTheAssembly_IsInTheGeneratedTable()
        {
            var missing = new List<string>();

            foreach (var contract in Families())
            {
                var listed = ModificationImplementations.Of(contract);

                foreach (var implementation in Implementations(contract))
                {
                    if (listed.Contains(implementation)) continue;
                    missing.Add($"{contract.Name} misses {implementation.FullName}");
                }
            }

            missing.Sort(StringComparer.Ordinal);
            CollectionAssert.IsEmpty(missing,
                "The generated table is behind the assembly. A polymorphic model that is not "
                + "[GenerateModel] would do this - see ModificationRegistryGenerator's header.");
        }

        // The families are what make the table worth generating, so an empty sweep would pass this
        // file while proving nothing - the count is asserted rather than the mere absence of misses.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheSweepFindsTheFamiliesAtAll()
        {
            var families = Families().ToArray();

            Assert.Greater(families.Length, 10, "The interface sweep found almost nothing.");
            CollectionAssert.IsNotEmpty(ModificationImplementations.Of(typeof(Models.Interfaces.Values.IVector2)));
            CollectionAssert.IsEmpty(ModificationImplementations.Of(typeof(IDisposable)),
                "Something outside the model families is being expanded.");
        }

        // THE CASE THAT USED TO BE DROPPED SILENTLY. A font size lives behind IFontSizeKey, which
        // the old hand-written list never named, so the walk registered nothing for FontSizeKey and
        // an override addressed at one resolved to no property at all - Apply returned and the
        // placement kept the template's value with nothing logged. It is an end-to-end check on
        // purpose: the table being right is only useful if the walk actually descends through it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Apply_ReachesThroughAnInterfaceTypedProperty()
        {
            var text = new TextObject { ObjectId = new ObjectId(1) };
            text.FontSizes.Add(new FontSizeKey(new FloatValue(10f), 0));

            var replacement = new FloatValue(42f);
            text.Apply(new Modification(new ModificationKey(text.ObjectId, "fontsize[0].flt"), replacement));

            var key = text.FontSizes[0] as FontSizeKey;
            Assert.IsNotNull(key, "The font size stopped being a FontSizeKey.");

            var written = key.Value as FloatValue;
            Assert.IsNotNull(written, "The font size stopped being a FloatValue.");
            Assert.AreEqual(42f, written.Value,
                "The override never landed - the walk did not descend through IFontSizeKey.");
        }

        /// <summary> Every polymorphic model interface, i.e. everything in a sub-namespace of
        /// BH.SDK.Models.Interfaces - never the root, which holds IModel and the other contracts
        /// every model implements. </summary>
        private static IEnumerable<Type> Families() => typeof(ModificationUtils).Assembly.GetTypes()
            .Where(type => type.IsInterface && !type.IsGenericType)
            .Where(type => type.Namespace != null
                           && type.Namespace.StartsWith(InterfaceRoot + ".", StringComparison.Ordinal));

        private static IEnumerable<Type> Implementations(Type contract) =>
            typeof(ModificationUtils).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && !type.IsInterface && !type.IsValueType)
                .Where(contract.IsAssignableFrom);
    }
}
