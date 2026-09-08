using System.Linq;
using BH.SDK.Generators;
using BH.SDK.Generators.Utility;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // AllowsEmptyRun exists for one failure that is invisible from inside the SDK: a host disables a
    // generator whose whole estimate is zero, so a generator writing something GeneratorCost cannot
    // measure is not "estimated wrong", it is unusable. That makes it worth pinning here rather than
    // leaving it to whoever next opens the generators panel.

    /// <summary> That a generator whose whole estimate is legitimately zero still declares itself runnable - a
    /// host greys out the ones that do not, so this failure looks like the generator being permanently off. </summary>
    public class AllowsEmptyRunTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CapacityHint_AllowsAnEmptyRun()
        {
            Assert.IsTrue(new CapacityHintGenerator().AllowsEmptyRun,
                "gen_capacity_hint writes a bare advisory field, so its estimate is always zero - " +
                "without this its button is permanently disabled");
        }

        // The font cache does NOT opt in, on purpose: it writes real dictionary entries, counts them
        // as Resources, and a zero there genuinely means there is nothing to build or drop.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FontCache_DoesNotAllowAnEmptyRun()
        {
            Assert.IsFalse(new FontCacheGenerator().AllowsEmptyRun);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryModifier_AllowsAnEmptyRun()
        {
            foreach (var generator in GeneratorRegistry.All.OfType<IScopeGenerator>()
                         .Where(g => g.Kind == GeneratorKind.Modifier))
                Assert.IsTrue(generator.AllowsEmptyRun,
                    $"{generator.NameKey}: a modifier edits what already exists, so zero is its normal estimate");
        }
    }
}
