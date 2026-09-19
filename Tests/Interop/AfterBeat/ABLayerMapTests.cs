using System.Collections.Generic;
using System.Linq;
using BH.SDK.Interop;
using BH.SDK.Interop.AfterBeat;
using BH.SDK.Interop.AfterBeat.Export;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models.Game;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Interop.AfterBeat
{
    // Draw order is the conversion with the most ways to be silently wrong: every one of these
    // failures still produces a level that loads, plays, and looks plausible until the moment two
    // objects that should not overlap do.
    //
    // The invariants worth stating once, since almost every test below is one of them:
    //
    //   THE PLAYER LINE - this format draws its avatar at layer 0.5, and Afterbeat draws its own
    //   in front of EVERY Default object (they all share sortingOrder 0 over there; the player is
    //   61; depth only separates them by draw distance) and behind every AbovePlayer one. So the
    //   whole Default band lands at layer <= ValueRules.LastLayerBehindPlayer, depth 0 included, and
    //   AbovePlayer at layer >= ValueRules.FirstLayerAbovePlayer.
    //
    //   THE BANDS never interleave: everything AbovePlayer is in front of everything Default, which
    //   is in front of everything Background, whatever depths any of them carry.
    //
    //   AUTO IS ORDER-PRESERVING - it may compress, never reorder. Anything OnlyDepth draws in
    //   front, Auto draws in front or level with.
    //
    //   ONE PLAN ORDERS THE WHOLE LEVEL - a prefab template's objects are materialized into the
    //   level and drawn against its own by depth alone, so both read one table and a depth means
    //   one layer everywhere in the file.

    /// <summary> Afterbeat's draw order against this project's parent-relative layers. </summary>
    public class ABLayerMapTests
    {
        #region Fixture

        private static VgdObject Obj(int depth, ABRenderLayer band = ABRenderLayer.Default,
            int editorLayer = 0, int editorBin = 0, string id = null)
            => new()
            {
                Id = id ?? $"d{depth}-b{(int)band}-l{editorLayer}-n{editorBin}",
                Depth = depth,
                RenderLayer = (int)band,
                Editor = new VgdObjectEditor { Layer = editorLayer, Bin = editorBin },
            };

        private static ABOptions Options(ABLayerImport mode)
            => new() { LayerImport = mode };

        private static int[] Resolve(IReadOnlyList<VgdObject> sources, ABLayerImport mode,
            InteropReport report = null)
            => ABLayerMap.Resolve(sources, Options(mode), report).Layers;

        #endregion

        #region OnlyDepth

        // The whole band layout in one case, because the three bands are only correct relative to
        // each other: 61 depths each, back to back, Default's frontmost sitting on layer 0 - the
        // last layer behind the player.
        [TestCase(ABRenderLayer.Default, 0, 0)]
        [TestCase(ABRenderLayer.Default, 1, -1)]
        [TestCase(ABRenderLayer.Default, 20, -20)]
        [TestCase(ABRenderLayer.Default, 60, -60)]
        [TestCase(ABRenderLayer.AbovePlayer, 0, 61)]
        [TestCase(ABRenderLayer.AbovePlayer, 60, 1)]
        [TestCase(ABRenderLayer.Background, 0, -61)]
        [TestCase(ABRenderLayer.Background, 60, -121)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void OnlyDepth_PlacesEachBandOnItsOwnStretch(ABRenderLayer band, int depth, int expected)
        {
            var layers = Resolve(new[] { Obj(depth, band) }, ABLayerImport.OnlyDepth);
            Assert.AreEqual(expected, layers[0]);
        }

        // Independent of what the level uses: a depth is an absolute statement under this mode, so a
        // level whose shallowest object is at depth 10 must not have that object promoted onto 0.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OnlyDepth_IsAbsolute_NotRelativeToWhatTheLevelUses()
        {
            var sources = new[] { Obj(10), Obj(20), Obj(30) };
            var layers = Resolve(sources, ABLayerImport.OnlyDepth);

            CollectionAssert.AreEqual(new[] { -10, -20, -30 }, layers);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OnlyDepth_DepthOutsideTheSourceRange_IsClampedIntoIt()
        {
            var layers = Resolve(new[] { Obj(-5), Obj(999) }, ABLayerImport.OnlyDepth);
            CollectionAssert.AreEqual(new[] { 0, -60 }, layers);
        }

        // The export is the inverse of this mode, so the two have to agree about where a band
        // starts; a level that came from Afterbeat has to go back to the depth it came from.
        [TestCase(ABRenderLayer.Default, 0)]
        [TestCase(ABRenderLayer.Default, 20)]
        [TestCase(ABRenderLayer.Default, 60)]
        [TestCase(ABRenderLayer.AbovePlayer, 0)]
        [TestCase(ABRenderLayer.AbovePlayer, 60)]
        [TestCase(ABRenderLayer.Background, 0)]
        [TestCase(ABRenderLayer.Background, 60)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OnlyDepth_RoundTripsThroughTheExport(ABRenderLayer band, int depth)
        {
            var layer = Resolve(new[] { Obj(depth, band) }, ABLayerImport.OnlyDepth)[0];

            var scope = new GameLevel();
            var imported = new ShapeObject { ObjectId = new ObjectId(1), Active = true, Layer = layer };
            scope.Objects[imported.ObjectId] = imported;

            var context = new ABExportContext(new ABOptions(), new InteropReport(), scope);
            var exported = ABObjectExporter.Export(imported, context, "objects[0]");

            Assert.AreEqual(depth, exported.Depth, "depth survives the round trip");
            Assert.AreEqual((int)band, exported.RenderLayer, "so does the band");
        }

        #endregion

        #region The player line

        [TestCase(ABLayerImport.Auto)]
        [TestCase(ABLayerImport.OnlyDepth)]
        [TestCase(ABLayerImport.OnlyEditor)]
        [TestCase(ABLayerImport.DepthAndEditor)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryMode_PutsTheWholeDefaultBandBehindThePlayer(ABLayerImport mode)
        {
            var sources = Enumerable.Range(0, ABLayerMap.DepthSpan)
                .Select(depth => Obj(depth, editorLayer: 1 + depth % 4))
                .ToArray();

            var layers = Resolve(sources, mode);

            for (var depth = 0; depth < layers.Length; depth++)
                Assert.LessOrEqual(layers[depth], ValueRules.LastLayerBehindPlayer,
                    $"depth {depth} is an ordinary object and draws behind the player");
        }

        [TestCase(ABLayerImport.Auto)]
        [TestCase(ABLayerImport.OnlyDepth)]
        [TestCase(ABLayerImport.OnlyEditor)]
        [TestCase(ABLayerImport.DepthAndEditor)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryMode_PutsTheAbovePlayerBandInFrontOfThePlayer(ABLayerImport mode)
        {
            var sources = new[]
            {
                Obj(0, ABRenderLayer.AbovePlayer, editorLayer: 1),
                Obj(60, ABRenderLayer.AbovePlayer, editorLayer: 2),
                Obj(20, ABRenderLayer.Default, editorLayer: 2),
            };

            var layers = Resolve(sources, mode);

            Assert.GreaterOrEqual(layers[0], ValueRules.FirstLayerAbovePlayer,
                "an above-player object draws in front of the player");
            Assert.GreaterOrEqual(layers[1], ValueRules.FirstLayerAbovePlayer,
                "however deep it is inside its own band");
            Assert.LessOrEqual(layers[2], ValueRules.LastLayerBehindPlayer,
                "and an ordinary one still does not");
        }

        [TestCase(ABLayerImport.Auto)]
        [TestCase(ABLayerImport.OnlyDepth)]
        [TestCase(ABLayerImport.OnlyEditor)]
        [TestCase(ABLayerImport.DepthAndEditor)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryMode_KeepsTheThreeBandsApart(ABLayerImport mode)
        {
            // Three editor groups rather than a realistic number of them, because DepthAndEditor
            // spends a whole 61-layer band on each and three bands of those is already 549 layers -
            // a level organised any more finely than this genuinely does run out under that mode,
            // which is what its own clamping test is for.
            var sources = new List<VgdObject>();
            foreach (var band in new[]
                     {
                         ABRenderLayer.Background,
                         ABRenderLayer.Default,
                         ABRenderLayer.AbovePlayer,
                     })
                for (var depth = 0; depth <= VgdObject.MaxDepth; depth += 12)
                for (var bin = 0; bin < 3; bin++)
                    sources.Add(Obj(depth, band, 1, bin));

            var layers = Resolve(sources, mode);

            var byBand = sources
                .Select((source, index) => (Band: ABLayerMap.ToBand(source), Layer: layers[index]))
                .GroupBy(pair => pair.Band)
                .ToDictionary(group => group.Key,
                    group => (Low: group.Min(p => p.Layer), High: group.Max(p => p.Layer)));

            Assert.Less(byBand[ABRenderLayer.Background].High,
                byBand[ABRenderLayer.Default].Low, "background is behind all content");
            Assert.Less(byBand[ABRenderLayer.Default].High,
                byBand[ABRenderLayer.AbovePlayer].Low, "above-player is in front of all content");
        }

        #endregion

        #region OnlyEditor

        // Smaller editor layer and smaller bin are further BACK, which is the author's answer and the
        // opposite of how depth reads - so this is exactly the direction that cannot be inferred.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OnlyEditor_OrdersByLayerThenBin_SmallerFurtherBack()
        {
            var sources = new[]
            {
                Obj(0, editorLayer: 1, editorBin: 0),
                Obj(0, editorLayer: 1, editorBin: 14),
                Obj(0, editorLayer: 2, editorBin: 0),
            };

            var layers = Resolve(sources, ABLayerImport.OnlyEditor);

            Assert.Less(layers[0], layers[1], "bin 0 is behind bin 14 of the same layer");
            Assert.Less(layers[1], layers[2], "a whole editor layer is coarser than a bin");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OnlyEditor_IgnoresDepthEntirely()
        {
            var sources = new[]
            {
                Obj(60, editorLayer: 1, editorBin: 3),
                Obj(0, editorLayer: 1, editorBin: 3),
            };

            var layers = Resolve(sources, ABLayerImport.OnlyEditor);
            Assert.AreEqual(layers[0], layers[1]);
        }

        // An object with no editor block at all belongs with editor layer 1, not with a group of its
        // own - a level where nobody sorted anything must not arrive spread across two bands.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OnlyEditor_MissingEditorBlock_ReadsAsTheFirstGroup()
        {
            var bare = new VgdObject { Id = "bare", Depth = 20, Editor = null };
            var sources = new[] { bare, Obj(20, editorLayer: 1, editorBin: 0) };

            var layers = Resolve(sources, ABLayerImport.OnlyEditor);
            Assert.AreEqual(layers[1], layers[0]);
        }

        #endregion

        #region DepthAndEditor

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void DepthAndEditor_GivesEachGroupItsOwnBand_DepthOrderingInsideIt()
        {
            var sources = new[]
            {
                Obj(20, editorLayer: 1, editorBin: 0),
                Obj(10, editorLayer: 1, editorBin: 0),
                Obj(60, editorLayer: 1, editorBin: 1),
            };

            var layers = Resolve(sources, ABLayerImport.DepthAndEditor);

            Assert.Less(layers[0], layers[1], "inside one group, shallower draws in front");
            Assert.Less(layers[1], layers[2],
                "the deepest object of the next group still draws in front of the shallowest of this one");
        }

        // The mode that deliberately does NOT pack: a level using two depths still costs two whole
        // bands, and the gap between them is what Auto exists to remove.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void DepthAndEditor_LeavesTheUnusedLayersEmpty()
        {
            var sources = new[]
            {
                Obj(20, editorLayer: 1, editorBin: 0),
                Obj(20, editorLayer: 1, editorBin: 1),
            };

            var layers = Resolve(sources, ABLayerImport.DepthAndEditor);
            Assert.AreEqual(ABLayerMap.DepthSpan, layers[1] - layers[0]);
        }

        #endregion

        #region Auto

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Auto_PacksTheUsedDepthsIntoConsecutiveLayers()
        {
            var sources = new[] { Obj(5), Obj(20), Obj(55) };
            var layers = Resolve(sources, ABLayerImport.Auto);

            CollectionAssert.AreEqual(new[] { 0, -1, -2 }, layers);
        }

        // What "flattened onto 0" means with the avatar sitting at 0.5: an ordinary level marks
        // nothing AbovePlayer, so its frontmost content is the last layer behind the player and
        // everything else steps down from there - never 200 layers down.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Auto_ALevelWithNothingInFrontOfThePlayer_TopsOutOnZero()
        {
            var sources = new[] { Obj(0), Obj(20), Obj(60) };
            var result = ABLayerMap.Resolve(sources, Options(ABLayerImport.Auto));

            Assert.AreEqual(0, result.Highest, "the frontmost content sits just behind the player");
            Assert.AreEqual(-2, result.Lowest, "three ordering keys, three consecutive layers");
        }

        // The gap is the price of the band surviving an export, and it is only ever paid by a level
        // that uses the Background band at all - see BuildAuto's header.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Auto_KeepsEachBandInsideTheStretchTheExportReadsItFrom()
        {
            var sources = new[]
            {
                Obj(0),
                Obj(60),
                Obj(20, ABRenderLayer.Background),
                Obj(20, ABRenderLayer.AbovePlayer),
            };

            var layers = Resolve(sources, ABLayerImport.Auto);

            Assert.AreEqual(0, layers[0], "the ordinary band starts at the player line");
            Assert.AreEqual(-1, layers[1]);
            Assert.AreEqual(-ABLayerMap.DepthSpan, layers[2],
                "the background band starts below every layer the ordinary one could reach");
            Assert.AreEqual(1, layers[3], "and the above-player band starts on the other side of the line");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Auto_TheFirstLayerInFrontOfThePlayerIsOne()
        {
            var sources = new[]
            {
                Obj(20),
                Obj(60, ABRenderLayer.AbovePlayer),
                Obj(0, ABRenderLayer.AbovePlayer),
            };

            var layers = Resolve(sources, ABLayerImport.Auto);

            Assert.AreEqual(0, layers[0], "the only ordinary object is the last one behind the player");
            Assert.AreEqual(1, layers[1], "and the backmost above-player one is the first in front");
            Assert.AreEqual(2, layers[2]);
        }

        // The source editor's grouping is not draw order over there - it never reaches sortingOrder -
        // so two objects sharing a depth share a layer here however differently they were filed.
        // This is what the -520 came from: separating them cost 6.3x the layers on a real level.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Auto_ObjectsSharingADepth_ShareALayer_WhateverTheirEditorGroup()
        {
            var sources = new[]
            {
                Obj(20, editorLayer: 1, editorBin: 0, id: "a"),
                Obj(20, editorLayer: 5, editorBin: 12, id: "b"),
                Obj(20, editorLayer: 2, editorBin: 3, id: "c"),
            };

            var result = ABLayerMap.Resolve(sources, Options(ABLayerImport.Auto));

            Assert.AreEqual(1, result.Layers.Distinct().Count(), "one depth is one layer");
            Assert.AreEqual(0, result.Lowest);
            Assert.AreEqual(0, result.Highest);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Auto_NeverReordersWhatOnlyDepthDrew()
        {
            var sources = new List<VgdObject>();
            for (var depth = 0; depth <= VgdObject.MaxDepth; depth += 7)
            for (var editorLayer = 1; editorLayer <= 4; editorLayer++)
                sources.Add(Obj(depth, ABRenderLayer.Default, editorLayer, editorLayer * 3));

            var depthOnly = Resolve(sources, ABLayerImport.OnlyDepth);
            var auto = Resolve(sources, ABLayerImport.Auto);

            for (var i = 0; i < sources.Count; i++)
            for (var j = 0; j < sources.Count; j++)
            {
                if (depthOnly[i] >= depthOnly[j]) continue;
                Assert.Less(auto[i], auto[j],
                    $"{sources[i].Id} drew behind {sources[j].Id} by depth and must still do so");
            }
        }

        // The bound that makes the mode usable at all: what Auto can spend is decided by the source
        // FORMAT - three bands of 61 depths - and not by how large or how finely organised the level
        // is. A level using every combination the format has still occupies 183 layers, which is why
        // no level can reach the ValueRules range and nothing here is ever clamped.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Auto_ALevelUsingEveryOrderingTheFormatHas_StillFitsInTheFormatsOwnSpan()
        {
            var sources = new List<VgdObject>();
            foreach (var band in new[]
                     {
                         ABRenderLayer.Background,
                         ABRenderLayer.Default,
                         ABRenderLayer.AbovePlayer,
                     })
                for (var depth = 0; depth <= VgdObject.MaxDepth; depth++)
                for (var editorLayer = 1; editorLayer <= 8; editorLayer++)
                for (var bin = 0; bin <= ABLayerMap.MaxEditorBin; bin += 7)
                    sources.Add(Obj(depth, band, editorLayer, bin));

            var report = new InteropReport();
            var result = ABLayerMap.Resolve(sources, Options(ABLayerImport.Auto), report);

            Assert.AreEqual(-(ABLayerMap.BandCount - 1) * ABLayerMap.DepthSpan
                + ValueRules.FirstLayerAbovePlayer, result.Lowest);
            Assert.AreEqual(ABLayerMap.DepthSpan, result.Highest);
            Assert.Greater(result.Lowest, ValueRules.MinLayer);
            Assert.Less(result.Highest, ValueRules.MaxLayer);
            Assert.IsFalse(report.Issues.Any(issue => issue.Code == "layers_clamped"),
                "packing is what keeps a level this fine inside the range");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Auto_LeavesNoGapBetweenTheLayersOneBandUses()
        {
            var sources = new List<VgdObject>();
            foreach (var band in new[] { ABRenderLayer.Background, ABRenderLayer.Default })
                for (var depth = 0; depth <= VgdObject.MaxDepth; depth += 9)
                    sources.Add(Obj(depth, band));

            var result = ABLayerMap.Resolve(sources, Options(ABLayerImport.Auto));

            foreach (var band in new[] { ABRenderLayer.Background, ABRenderLayer.Default })
            {
                var used = sources
                    .Select((source, index) => (source, layer: result.Layers[index]))
                    .Where(pair => ABLayerMap.ToBand(pair.source) == band)
                    .Select(pair => pair.layer)
                    .Distinct()
                    .OrderBy(layer => layer)
                    .ToArray();

                for (var i = 1; i < used.Length; i++)
                    Assert.AreEqual(1, used[i] - used[i - 1], $"{band}: packed means consecutive");
            }
        }

        #endregion

        #region Plan

        // A template's objects are copied into the level and drawn against its own by depth alone,
        // so the two lists cannot be ranked separately: a depth has to mean one layer across the
        // whole file, and a depth only the template uses has to take its place in the level's own
        // ordering rather than being packed away.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Plan_OrdersEveryListAgainstTheSameDepths()
        {
            var level = new[] { Obj(10, id: "level-10"), Obj(50, id: "level-50") };
            var template = new[] { Obj(30, id: "template-30"), Obj(10, id: "template-10") };

            var plan = ABLayerMap.Build(new[] { level, template }, Options(ABLayerImport.Auto));

            var levelLayers = ABLayerMap.Resolve(level, null, plan: plan);
            var templateLayers = ABLayerMap.Resolve(template, null, plan: plan);

            Assert.AreEqual(levelLayers.Layers[0], templateLayers.Layers[1],
                "one depth is one layer, wherever the object lives");
            Assert.AreEqual(0, levelLayers.Layers[0], "depth 10 is the frontmost of the three");
            Assert.AreEqual(-1, templateLayers.Layers[0], "depth 30 sits between them");
            Assert.AreEqual(-2, levelLayers.Layers[1], "depth 50 is the backmost");
            Assert.AreEqual(-2, plan.Lowest);
            Assert.AreEqual(0, plan.Highest);
        }

        // The property that makes a prefab-heavy level survivable: a level built out of a hundred
        // templates occupies exactly as many layers as it uses depths, and a placement adds none of
        // its own.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Plan_ManyListsCostNoMoreLayersThanTheDepthsTheyShare()
        {
            var lists = Enumerable.Range(0, 100)
                .Select(index => new[] { Obj(20, id: $"a{index}"), Obj(40, id: $"b{index}") })
                .ToArray();

            var plan = ABLayerMap.Build(lists, Options(ABLayerImport.Auto));

            Assert.AreEqual(0, plan.Highest);
            Assert.AreEqual(-1, plan.Lowest, "two depths, two layers, a hundred lists");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Plan_ASingleList_IsWhatAListResolvedOnItsOwnGets()
        {
            var sources = new[] { Obj(5), Obj(40) };

            var withPlan = ABLayerMap.Resolve(sources, null,
                plan: ABLayerMap.Build(sources, Options(ABLayerImport.Auto)));
            var without = ABLayerMap.Resolve(sources, Options(ABLayerImport.Auto));

            CollectionAssert.AreEqual(without.Layers, withPlan.Layers);
        }

        #endregion

        #region Range and reporting

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void DepthAndEditor_MoreGroupsThanThereIsRoomFor_IsClampedAndReported()
        {
            var sources = Enumerable.Range(1, 200)
                .Select(editorLayer => Obj(20, ABRenderLayer.Default, editorLayer))
                .ToArray();

            var report = new InteropReport();
            var result = ABLayerMap.Resolve(sources, Options(ABLayerImport.DepthAndEditor), report);

            Assert.GreaterOrEqual(result.Lowest, ValueRules.MinLayer);
            Assert.LessOrEqual(result.Highest, ValueRules.MaxLayer);
            Assert.IsTrue(report.Issues.Any(issue => issue.Code == "layers_clamped"),
                "an author whose level lost ordering is told so");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Resolve_AnEmptyList_AnswersAnEmptyRange()
        {
            var result = ABLayerMap.Resolve(new List<VgdObject>(), Options(ABLayerImport.Auto));

            Assert.IsEmpty(result.Layers);
            Assert.AreEqual(0, result.Lowest);
            Assert.AreEqual(0, result.Highest);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ToBand_AnUndefinedValue_ReadsAsDefault()
        {
            Assert.AreEqual(ABRenderLayer.Default,
                ABLayerMap.ToBand(new VgdObject { RenderLayer = 7 }));
            Assert.AreEqual(ABRenderLayer.Background,
                ABLayerMap.ToBand(new VgdObject { RenderLayer = (int)ABRenderLayer.Background }));
        }

        #endregion
    }
}