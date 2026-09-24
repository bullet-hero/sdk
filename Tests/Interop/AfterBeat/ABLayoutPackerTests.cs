using System.Collections.Generic;
using System.Linq;
using BH.SDK.Interop;
using BH.SDK.Interop.AfterBeat;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Interop.AfterBeat
{
    // The packer's promises, one per test: time decides the row, depth decides the order, a subtree
    // is a block of rows 1..N above its root, and a band never leaves its side of the player line.

    /// <summary> Laying a scope out on rows, packed in time and ordered by depth. </summary>
    public class ABLayoutPackerTests
    {
        #region Fixture

        private static ABLayoutPacker.Entry Entry(string id, int start, int end, int depth = 20,
            string parent = null, ABLayoutPacker.Band band = ABLayoutPacker.Band.Default, int order = 0)
            => new()
            {
                Id = id,
                ParentId = parent,
                Start = start,
                End = end,
                Depth = depth,
                Band = band,
                Order = order,
            };

        private static ABLayoutPacker.Result Pack(params ABLayoutPacker.Entry[] entries)
            => ABLayoutPacker.Pack(entries, ABLayoutPacker.Scope.Level);

        #endregion

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Pack_SameDepthNeverOverlapping_ShareOneRow()
        {
            var result = Pack(Entry("a", 1, 10), Entry("b", 10, 20, order: 1), Entry("c", 30, 40, order: 2));

            Assert.AreEqual(1, result.EffectiveLayers.Values.Distinct().Count());
            Assert.AreEqual(ValueRules.LastLayerBehindPlayer, result.EffectiveLayers["a"]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Pack_SameDepthOverlapping_TakeDistinctRowsAndReuseThemOnceFree()
        {
            var result = Pack(
                Entry("a", 1, 10, order: 0),
                Entry("b", 2, 10, order: 1),
                Entry("c", 3, 10, order: 2),
                Entry("d", 12, 20, order: 3),
                Entry("e", 13, 20, order: 4));

            CollectionAssert.AllItemsAreUnique(new[] { "a", "b", "c" }.Select(id => result.EffectiveLayers[id]));
            Assert.AreEqual(result.EffectiveLayers["a"], result.EffectiveLayers["d"], "the first free row is reused");
            Assert.AreEqual(result.EffectiveLayers["b"], result.EffectiveLayers["e"], "and then the next one");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Pack_OverlappingDifferentDepths_LayerOrderFollowsDepth()
        {
            var result = Pack(
                Entry("far", 1, 20, depth: 40, order: 0),
                Entry("near", 5, 15, depth: 5, order: 1),
                Entry("mid", 3, 18, depth: 20, order: 2));

            Assert.Greater(result.EffectiveLayers["near"], result.EffectiveLayers["mid"]);
            Assert.Greater(result.EffectiveLayers["mid"], result.EffectiveLayers["far"]);
            Assert.AreEqual(0, result.Approximated);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Pack_Children_TakeOwnLayersOneToNAndNestedOnesStack()
        {
            var result = Pack(
                Entry("root", 1, 100, depth: 20),
                Entry("c1", 1, 100, depth: 30, parent: "root", order: 1),
                Entry("c2", 1, 100, depth: 20, parent: "root", order: 2),
                Entry("c3", 1, 100, depth: 10, parent: "root", order: 3));

            Assert.AreEqual(1, result.OwnLayers["c1"], "farthest sibling first");
            Assert.AreEqual(2, result.OwnLayers["c2"]);
            Assert.AreEqual(3, result.OwnLayers["c3"]);

            var nested = Pack(
                Entry("root", 1, 100),
                Entry("a", 1, 100, depth: 30, parent: "root", order: 1),
                Entry("a1", 1, 100, parent: "a", order: 2),
                Entry("a2", 1, 100, parent: "a", order: 3),
                Entry("b", 1, 100, depth: 10, parent: "root", order: 4));

            var rows = new[] { "root", "a", "a1", "a2", "b" }.Select(id => nested.EffectiveLayers[id]).ToArray();
            CollectionAssert.AllItemsAreUnique(rows, "a nested subtree stacks instead of sharing a cousin's row");
            Assert.That(new[] { "a", "a1", "a2", "b" }.Select(id => nested.OwnLayers[id]), Is.All.GreaterThan(0));
            Assert.AreEqual(4, nested.OwnLayers["b"], "b sits above a's whole block: 1 + (height(a) + 1)");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Pack_Bands_StayOnTheirSideOfThePlayerLine()
        {
            var entries = new List<ABLayoutPacker.Entry>();
            for (var i = 0; i < 6; i++)
            {
                entries.Add(Entry($"d{i}", 1, 50, depth: i * 5, order: i));
                entries.Add(Entry($"d{i}-child", 1, 50, parent: $"d{i}", order: 100 + i));
                entries.Add(Entry($"a{i}", 1, 50, depth: i * 5, band: ABLayoutPacker.Band.AbovePlayer, order: 200 + i));
            }

            var result = ABLayoutPacker.Pack(entries, ABLayoutPacker.Scope.Level);

            for (var i = 0; i < 6; i++)
            {
                Assert.LessOrEqual(result.EffectiveLayers[$"d{i}-child"], ValueRules.LastLayerBehindPlayer,
                    "a Default block never crosses above the player");
                Assert.GreaterOrEqual(result.EffectiveLayers[$"a{i}"], ValueRules.FirstLayerAbovePlayer);
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Pack_Placement_BlocksNearerUnitsFromItsRenderRangeNotItsRow()
        {
            var placement = Entry("p", 1, 50, depth: 30);
            placement.IsPlacement = true;
            placement.FarDepth = 30;
            placement.RenderHeight = 5;

            var result = Pack(placement, Entry("near", 1, 50, depth: 10, order: 1),
                Entry("same", 1, 50, depth: 30, order: 2));

            Assert.LessOrEqual(result.EffectiveLayers["p"] + 5, ValueRules.LastLayerBehindPlayer,
                "its template draws five rows above it, and that range may not cross the player line");
            Assert.Greater(result.EffectiveLayers["near"], result.EffectiveLayers["p"] + 5,
                "a nearer unit sits above everything the placement draws");
            Assert.AreNotEqual(result.EffectiveLayers["p"], result.EffectiveLayers["same"],
                "the placement's own row is taken");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Pack_EmptyWithOneChild_SharesItsRow()
        {
            var pivot = Entry("pivot", 1, 50);
            pivot.Rendered = false;
            var result = Pack(pivot, Entry("shape", 1, 50, parent: "pivot", order: 1));

            Assert.AreEqual(0, result.OwnLayers["shape"],
                "an empty draws nothing, so its only child may share its row");

            var drawnParent = Pack(Entry("parent", 1, 50), Entry("shape", 1, 50, parent: "parent", order: 1));
            Assert.AreEqual(1, drawnParent.OwnLayers["shape"], "a parent that draws keeps its child above it");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Pack_CollidersAbovePlayer_LiftsOnlyWhatCanHurt()
        {
            var hit = Entry("hit", 1, 50);
            hit.Collides = true;
            var decor = Entry("decor", 1, 50, order: 1);

            var faithful = ABLayoutPacker.Pack(new[] { hit, decor }, ABLayoutPacker.Scope.Level);
            Assert.LessOrEqual(faithful.EffectiveLayers["hit"], ValueRules.LastLayerBehindPlayer,
                "off by default: Afterbeat draws a hitting ordinary object behind its player");

            var lifted = ABLayoutPacker.Pack(new[] { hit, decor }, ABLayoutPacker.Scope.Level,
                collidersAbovePlayer: true);
            Assert.GreaterOrEqual(lifted.EffectiveLayers["hit"], ValueRules.FirstLayerAbovePlayer);
            Assert.LessOrEqual(lifted.EffectiveLayers["decor"], ValueRules.LastLayerBehindPlayer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Pack_Overflow_SharesRowsInsteadOfPilingOnTheFloor()
        {
            var entries = new List<ABLayoutPacker.Entry>();
            for (var i = 0; i < 1100; i++) entries.Add(Entry($"e{i}", 1, 10, order: i));
            var report = new InteropReport();

            var result = ABLayoutPacker.Pack(entries, ABLayoutPacker.Scope.Level, report, "objects");

            Assert.AreEqual(0, result.Clamped, "nothing is pushed past the range");
            Assert.That(result.EffectiveLayers.Values,
                Is.All.InRange(ValueRules.MinLayer, ValueRules.LastLayerBehindPlayer));
            var perRow = result.EffectiveLayers.Values.GroupBy(l => l).Max(g => g.Count());
            Assert.LessOrEqual(perRow, 2, "1100 objects over 1001 rows share at most two to a row");
            Assert.IsTrue(report.Issues.Any(i => i.Code == "rows_shared"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Pack_Template_PacksUpFromOneAndReportsItsBand()
        {
            var result = ABLayoutPacker.Pack(new[]
            {
                Entry("a", 1, 10, depth: 20),
                Entry("b", 1, 10, depth: 20, order: 1),
                Entry("c", 1, 10, depth: 5, band: ABLayoutPacker.Band.AbovePlayer, order: 2),
            }, ABLayoutPacker.Scope.Template);

            Assert.That(result.EffectiveLayers.Values, Is.All.GreaterThanOrEqualTo(1));
            Assert.AreEqual(ABLayoutPacker.Band.Default, result.MajorityBand);
            Assert.IsTrue(result.MixedBands);
            Assert.AreEqual(3, result.Highest);
        }
    }
}