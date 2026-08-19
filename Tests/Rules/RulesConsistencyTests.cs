using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // The limit tables are ~400 hand-written constants across eight classes, most of them in
    // Min/Max/Default triples transcribed from Unity's own audio and URP parameter ranges. A
    // transposed pair or an out-of-range default there is invisible - the code compiles, the level
    // validates, and the value is simply wrong. These tests read the tables reflectively and check
    // the relationships the naming already implies.

    /// <summary>
    /// Cross-checks inside Rules/: every Min/Max pair is ordered and every Default sits inside its
    /// own pair.
    /// </summary>
    public class RulesConsistencyTests
    {
        // Three naming shapes are in use, all three intentional: per-effect parameters use
        // "Param_Min", generic value limits use "MinParam", post-processing uses "ParamMin".
        private static readonly (string min, string max, string def)[] Shapes =
        {
            ("{0}_Min", "{0}_Max", "{0}_Default"),
            ("Min{0}", "Max{0}", "Default{0}"),
            ("{0}Min", "{0}Max", "{0}Default"),
        };

        private static IEnumerable<Type> RuleTables => typeof(ValueRules).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(ValueRules).Namespace)
            .Where(type => type.IsAbstract && type.IsSealed); // static class

        private static Dictionary<string, double> NumericConstants(Type table)
        {
            var result = new Dictionary<string, double>();

            foreach (var field in table.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.IsLiteral || field.IsInitOnly) continue;

                var value = field.GetRawConstantValue();
                if (value is null or string or bool || value.GetType().IsEnum) continue;

                result[field.Name] = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            return result;
        }

        private static IEnumerable<(string table, string name, double min, double max, double? def)> Triples()
        {
            foreach (var table in RuleTables)
            {
                var constants = NumericConstants(table);

                foreach (var shape in Shapes)
                {
                    var prefixLength = shape.min.IndexOf("{0}", StringComparison.Ordinal);

                    foreach (var constant in constants)
                    {
                        var name = ExtractName(constant.Key, shape.min, prefixLength);
                        if (string.IsNullOrEmpty(name)) continue;
                        if (string.Format(shape.min, name) != constant.Key) continue;

                        if (!constants.TryGetValue(string.Format(shape.max, name), out var max)) continue;

                        double? def = constants.TryGetValue(string.Format(shape.def, name), out var d) ? d : null;
                        yield return (table.Name, name, constant.Value, max, def);
                    }
                }
            }
        }

        private static string ExtractName(string fieldName, string shape, int prefixLength)
        {
            var suffixLength = shape.Length - prefixLength - "{0}".Length;
            if (fieldName.Length <= prefixLength + suffixLength) return null;

            return fieldName.Substring(prefixLength, fieldName.Length - prefixLength - suffixLength);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestEveryPairIsOrdered()
        {
            var broken = Triples().Where(triple => triple.min > triple.max)
                .Select(triple => $"{triple.table}.{triple.name}: min {triple.min} > max {triple.max}")
                .ToList();

            CollectionAssert.IsEmpty(broken, string.Join("\n", broken));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestEveryDefaultIsInsideItsPair()
        {
            var broken = Triples()
                .Where(triple => triple.def.HasValue)
                .Where(triple => triple.def.Value < triple.min || triple.def.Value > triple.max)
                .Select(triple => $"{triple.table}.{triple.name}: default {triple.def.Value} " +
                                  $"outside [{triple.min}, {triple.max}]")
                .ToList();

            CollectionAssert.IsEmpty(broken, string.Join("\n", broken));
        }

        // Enough pairs must actually be discovered for the two tests above to mean anything - a
        // refactor that renames the constants would otherwise turn them into silent no-ops.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestPairDiscoveryIsNotEmpty()
        {
            Assert.Greater(Triples().Count(), 50);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestFrameBoundsAgree()
        {
            Assert.AreEqual(FrameRules.MaxFrameDuration - 1, FrameRules.MaxFrame,
                "The last playable frame is one below the length, because FrameDuration is a count");
            Assert.AreEqual(FrameRules.MaxFrameDuration, PrefabRules.MaxFrameDuration,
                "A template's timeline is measured in the same frames as a level's");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestPrefabBudgetsFollowLevel()
        {
            Assert.AreEqual(LevelRules.MaxObjects, PrefabRules.MaxObjects);
            Assert.AreEqual(PrefabRules.MaxObjects, PrefabRules.MaxObjectIdRemaps,
                "A placement cannot remap more objects than a template can hold");
        }

        // The authored layer band must stay below the editor's own overlay bands, or a level object
        // could draw on top of the selection outline and the gizmo handles.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestLayerBandsDoNotOverlap()
        {
            Assert.Less(ValueRules.MaxLayer, ValueRules.MinLayerSelection);
            Assert.Less(ValueRules.MinLayerSelection, ValueRules.MinLayerGrid);
            Assert.Less(ValueRules.MinLayerGrid, ValueRules.MinLayerColliders);
            Assert.Less(ValueRules.MinLayerColliders, ValueRules.MinLayerGizmos);
            Assert.Less(ValueRules.MinLayerGizmos, ValueRules.MaxCameraLayer);
        }
    }
}
