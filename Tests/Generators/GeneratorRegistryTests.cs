using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BH.SDK.Generators;
using BH.SDK.Models.Interfaces.Values;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // These are the whole-system invariants: every generator the SDK ships is discoverable, named
    // uniquely, and describes its own form completely. They cost nothing per generator and catch
    // the two mistakes that would otherwise only surface in a host's UI - a duplicate key silently
    // shadowing a generator, and a field the form renders in an unpredictable position.

    /// <summary> The whole-system invariants: every shipped generator is discoverable, uniquely named, and
    /// describes its own form completely - the two mistakes that would otherwise surface only in a host's UI. </summary>
    public class GeneratorRegistryTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Registry_FindsShippedGenerators()
        {
            Assert.IsNotEmpty(GeneratorRegistry.All);
            Assert.IsTrue(GeneratorRegistry.TryGet("gen_level_empty", out var empty));
            Assert.IsInstanceOf<EmptyLevelGenerator>(empty);
            Assert.AreEqual(GeneratorKind.Level, empty.Kind);
        }

        // Test fixtures live in this assembly precisely so they stay out of a host's list.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Registry_ScansOnlyTheSdkAssembly()
        {
            Assert.IsFalse(GeneratorRegistry.TryGet("gen_test_spawn", out _));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void NameKeys_AreUnique()
        {
            var keys = GeneratorRegistry.All.Select(generator => generator.NameKey).ToList();
            CollectionAssert.AllItemsAreUnique(keys);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void NameKeys_FollowTheKindPrefixConvention()
        {
            foreach (var generator in GeneratorRegistry.All)
            {
                var expected = generator.Kind switch
                {
                    GeneratorKind.Level => "gen_level_",
                    GeneratorKind.Modifier => "mod_",
                    _ => "gen_",
                };
                Assert.IsTrue(generator.NameKey.StartsWith(expected),
                    $"{generator.GetType().Name}: '{generator.NameKey}' should start with '{expected}'");
            }
        }

        // Type.GetFields() order is unspecified by the CLI, so an unlisted field is a form whose
        // layout can change on a recompile - see GeneratorHints' header.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void HintsOrder_CoversEveryParameterField()
        {
            foreach (var generator in GeneratorRegistry.All)
                AssertOrderCoversFields(generator);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void HintsOrder_CoversEveryParameterField_ForTestFixtures()
        {
            IGenerator[] fixtures =
            {
                new SpawnTestGenerator(), new RenameTestModifier(),
                new CameraFlashTestGenerator(), new ScatterTestGenerator(),
            };
            foreach (var generator in fixtures)
                AssertOrderCoversFields(generator);
        }

        // A field left out of every Section still renders - in the default section, at the bottom -
        // so the mistake is invisible in a host until someone wonders why a Main field sank.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void HintsSections_CoverEveryParameterField()
        {
            foreach (var generator in GeneratorRegistry.All)
            {
                foreach (var field in ParameterFields(generator))
                {
                    Assert.IsTrue(generator.Hints.Sections.ContainsKey(field.Name),
                        $"{generator.NameKey}: field '{field.Name}' is in no Hints.Section");
                }
            }
        }

        // An unbounded number is a level the format rejects one keystroke later: a host clamps
        // writes against Hints.Ranges and has nothing to clamp against without one. Types that carry
        // their own bounds (bool, enum, ids) are exempt; everything an author can type a number into
        // is not.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryNumericField_HasARange()
        {
            foreach (var generator in GeneratorRegistry.All)
            {
                foreach (var field in ParameterFields(generator))
                {
                    if (!NeedsRange(field.FieldType)) continue;
                    Assert.IsTrue(generator.Hints.TryGetRange(field.Name, out _),
                        $"{generator.NameKey}: field '{field.Name}' has no Hints.Range");
                }
            }
        }

        private static bool NeedsRange(Type type)
            => type == typeof(int) || type == typeof(uint) || type == typeof(float) || type == typeof(double)
               || typeof(IFloat).IsAssignableFrom(type) || typeof(IInt).IsAssignableFrom(type)
               || typeof(IVector2).IsAssignableFrom(type) || typeof(IVector3).IsAssignableFrom(type);

        // A parameters class hiding an inherited field of the same name breaks the whole
        // name-keyed design: two FieldInfos answer to one name, so a form binds one of them at
        // random and every hint keyed on that name hits both. ShapeObjectsGenerator did exactly
        // this with SpawnParameters.Texture before its own image field was renamed to Image.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ParameterFields_DoNotShadowAnInheritedField()
        {
            foreach (var generator in GeneratorRegistry.All)
            {
                var names = ParameterFields(generator).Select(field => field.Name).ToList();
                CollectionAssert.AllItemsAreUnique(names,
                    $"{generator.NameKey}: a parameter field shadows an inherited one");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CreateDefaultParameters_ReturnsParametersType_AndAFreshInstanceEachTime()
        {
            foreach (var generator in GeneratorRegistry.All)
            {
                var first = generator.CreateDefaultParameters();
                var second = generator.CreateDefaultParameters();

                Assert.IsInstanceOf(generator.ParametersType, first);
                Assert.AreNotSame(first, second,
                    $"{generator.NameKey} must not hand out one shared parameters instance");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Hints_AreNeverNull()
        {
            foreach (var generator in GeneratorRegistry.All)
                Assert.IsNotNull(generator.Hints, generator.NameKey);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void OfKind_FiltersByKind()
        {
            foreach (var generator in GeneratorRegistry.OfKind(GeneratorKind.Level))
            {
                Assert.AreEqual(GeneratorKind.Level, generator.Kind);
                Assert.IsInstanceOf<ILevelGenerator>(generator);
            }
            foreach (var generator in GeneratorRegistry.OfKind(GeneratorKind.Content))
                Assert.IsInstanceOf<IScopeGenerator>(generator);
        }

        private static IEnumerable<FieldInfo> ParameterFields(IGenerator generator)
            => generator.ParametersType.GetFields(BindingFlags.Public | BindingFlags.Instance);

        private static void AssertOrderCoversFields(IGenerator generator)
        {
            var fields = ParameterFields(generator).Select(field => field.Name);
            var ordered = new HashSet<string>(generator.Hints.Order);

            foreach (var field in fields)
            {
                Assert.IsTrue(ordered.Contains(field),
                    $"{generator.NameKey}: field '{field}' is missing from Hints.Order");
            }
        }
    }
}
