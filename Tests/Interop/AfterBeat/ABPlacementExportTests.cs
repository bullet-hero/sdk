using System.Collections.Generic;
using System.Linq;
using BH.SDK.Interop.AfterBeat;
using BH.SDK.Interop.AfterBeat.Export;
using BH.SDK.Interop.AfterBeat.Import;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Interop.AfterBeat
{
    // A placement crosses as a placement wherever Afterbeat can say the same thing, and as its
    // copies where it cannot - never both, or the source game draws the template twice. Inside the
    // SDK nothing materializes, so a "materialized" placement is built by hand: a copy in the level
    // that the placement's id table names.

    /// <summary> Prefab placements and templates on the way out. </summary>
    public class ABPlacementExportTests
    {
        #region Fixture

        private static Level CreateLevel(out PrefabObject placement, out ObjectId copyId)
        {
            var level = new Level();

            var template = new Prefab { PrefabId = PrefabId.NewId() };
            template.Root.Name = "Template";
            var inner = new ShapeObject
            {
                ObjectId = template.GetNextObjectId(),
                ShapeId = ShapeId.Square.Fill,
                Active = true,
                Name = "Inner",
                Layer = 1,
            };
            template.Objects.Add(inner.ObjectId, inner);
            level.Resources.Prefabs.Add(template.PrefabId, template);

            placement = new PrefabObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                PrefabId = template.PrefabId,
                Name = "Template",
                Active = true,
                Span = new FrameSpan(31, template.Root.Span.FrameDuration),
            };
            placement.Positions.Add(new PosKey(new Vector2Value(2f, -3f), FrameRules.MinFrame));
            level.Game.Objects.Add(placement.ObjectId, placement);

            var copy = (ShapeObject)inner.Copy();
            copy.ObjectId = level.Settings.GetNextObjectId();
            copy.ParentObjectId = placement.ObjectId;
            copy.Name = "copy";
            level.Game.Objects.Add(copy.ObjectId, copy);
            placement.ObjectIds[inner.ObjectId] = copy.ObjectId;
            copyId = copy.ObjectId;

            return level;
        }

        private static string[] Codes(ABLevelExporter.Result result)
            => result.Report.Issues.Select(i => i.Code).ToArray();

        #endregion

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Export_StaticPlacement_IsOnePlacementAndNoCopies()
        {
            var level = CreateLevel(out var placement, out _);

            var result = ABLevelExporter.Export(level, null);

            var exported = result.Level.PrefabPlacements.Single();
            Assert.AreEqual(ABExportContext.ToSourceId(placement.ObjectId), exported.Id);
            Assert.AreEqual(ABPlacementExporter.ToPrefabSourceId(placement.PrefabId), exported.PrefabId);
            Assert.AreEqual(result.Level.Prefabs.Single().Id, exported.PrefabId);
            Assert.AreEqual(ABTimeMap.ToSeconds(31, level.Settings.Fps), exported.StartTime, 1e-4f);
            Assert.AreEqual(2f, exported.GetValue(VgdPrefabPlacement.TrackIndex.Position, 0), 1e-4f);
            Assert.AreEqual(-3f, exported.GetValue(VgdPrefabPlacement.TrackIndex.Position, 1), 1e-4f);

            Assert.IsEmpty(result.Level.Objects,
                "neither the placement nor its copy is an object - Afterbeat rebuilds both from the placement");
            CollectionAssert.Contains(Codes(result), "placements_exported");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Export_AnimatedPlacement_IsFlattenedWithItsReason()
        {
            var level = CreateLevel(out var placement, out var copyId);
            placement.Positions.Add(new PosKey(new Vector2Value(9f, 9f), 20));

            var result = ABLevelExporter.Export(level, null);

            Assert.IsEmpty(result.Level.PrefabPlacements);
            CollectionAssert.AreEquivalent(
                new[] { ABExportContext.ToSourceId(placement.ObjectId), ABExportContext.ToSourceId(copyId) },
                result.Level.Objects.Select(o => o.Id).ToArray(),
                "the copy and the empty it hangs off, exactly as before");
            CollectionAssert.Contains(Codes(result), "placement_flattened:animated");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Export_TemplateObjectsParentedToTheRoot_ResolveWithoutAFinding()
        {
            var level = CreateLevel(out _, out _);
            var template = level.Resources.Prefabs.Values.Single();
            template.Objects.Values.Single().ParentObjectId = ObjectId.PrefabRoot;

            var identity = ABLevelExporter.Export(level, null);
            CollectionAssert.DoesNotContain(Codes(identity), "parent_unresolvable");
            var flat = identity.Level.Prefabs.Single().Objects.Single();
            Assert.AreEqual(string.Empty, flat.ParentId, "an identity root is no object - its children are the roots");

            template.Root.Positions.Add(new PosKey(new Vector2Value(1f, 1f), FrameRules.MinFrame));
            var moved = ABLevelExporter.Export(level, null);
            CollectionAssert.DoesNotContain(Codes(moved), "parent_unresolvable");

            var objects = moved.Level.Prefabs.Single().Objects;
            var holder = objects.Single(o => o.ObjectType == (int)ABObjectType.AlphaEmpty);
            Assert.AreEqual(holder.Id, objects.Single(o => o != holder).ParentId,
                "a root that moves is written as the empty its content hangs off");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Conversion_ImportExportImport_KeepsPlacementsAndTemplates()
        {
            var first = ABLevelImporter.Import(ABMockData.CreateFullLevel(), null, new ABOptions(60)).Level;
            var exported = ABLevelExporter.Export(first, null).Level;
            var second = ABLevelImporter.Import(exported, null, new ABOptions(60)).Level;

            int Placements(Level level) => level.Game.Objects.Values.OfType<PrefabObject>().Count();
            int TemplateObjects(Level level) => level.Resources.Prefabs.Values.Sum(p => p.Objects.Count);

            Assert.Greater(Placements(first), 0);
            Assert.AreEqual(Placements(first), Placements(second));
            Assert.AreEqual(first.Resources.Prefabs.Count, second.Resources.Prefabs.Count);
            Assert.AreEqual(TemplateObjects(first), TemplateObjects(second));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Export_PackedLayersPastTheSourceRange_MapMonotonicallyIntoItsDepths()
        {
            var level = new Level();
            for (var layer = -1; layer >= -120; layer--)
            {
                var obj = new ShapeObject
                {
                    ObjectId = level.Settings.GetNextObjectId(),
                    ShapeId = ShapeId.Square.Fill,
                    Active = true,
                    Name = layer.ToString(),
                    Layer = layer,
                };
                level.Game.Objects.Add(obj.ObjectId, obj);
            }

            // One layer below the fixed mapping's floor, so the level is ranked.
            var deepest = new ShapeObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                ShapeId = ShapeId.Square.Fill,
                Active = true,
                Name = "-200",
                Layer = -200,
            };
            level.Game.Objects.Add(deepest.ObjectId, deepest);

            var exported = ABLevelExporter.Export(level, null).Level.Objects
                .OrderByDescending(o => int.Parse(o.Name))
                .ToArray();

            Assert.That(exported.Select(o => o.Depth), Is.All.InRange(VgdObject.MinDepth, VgdObject.MaxDepth));
            Assert.That(exported.All(o => o.RenderLayer == (int)ABRenderLayer.Default),
                "a ranked level's content stays ordinary content");
            for (var i = 1; i < exported.Length; i++)
                Assert.GreaterOrEqual(exported[i].Depth, exported[i - 1].Depth,
                    $"layer {exported[i].Name} is not drawn in front of layer {exported[i - 1].Name}");
            Assert.AreEqual(VgdObject.MinDepth, exported[0].Depth);
            Assert.AreEqual(VgdObject.MaxDepth, exported[^1].Depth);

            var rows = exported.Select(o => ABLayerMap.ToEditorIndex(o)).ToArray();
            for (var i = 1; i < rows.Length; i++)
                Assert.GreaterOrEqual(rows[i], rows[i - 1], "the editor rows read top to bottom");
        }
    }
}
