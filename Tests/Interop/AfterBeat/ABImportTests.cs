using System.Linq;
using BH.SDK.Interop;
using BH.SDK.Interop.AfterBeat;
using BH.SDK.Interop.AfterBeat.Import;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Interop.AfterBeat
{
    // Whole-document import. The assertions here are deliberately about the four conversions
    // ABObjectImporter's own header calls out - keyframe locality, rotation, parent-relative
    // layers, and which of size/scale an Afterbeat "scale" becomes - because each of those produces
    // a level that loads and plays wrongly rather than one that fails.

    /// <summary> The import's ordinary path: a document in, a level out, with the structure it described. </summary>
    public class ABImportTests
    {
        private const int Framerate = 60;

        private static ABOptions Options() => new(Framerate);

        private static ABOptions Options(ABLayerImport layerImport)
        {
            var options = new ABOptions(Framerate);
            options.LayerImport = layerImport;
            return options;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_MinimalLevel_ProducesObjectsThemesAndEvents()
        {
            var result = ABLevelImporter.Import(ABMockData.CreateLevel(), null, Options());

            Assert.IsNotNull(result.Level);
            Assert.AreEqual(Framerate, result.Level.Settings.Fps);
            Assert.AreEqual(1, result.Level.Game.Objects.Count);
            Assert.AreEqual(1, result.Level.Resources.Themes.Count);
            Assert.AreEqual(1, result.Level.Game.Events.Markers.Count);
            Assert.AreEqual(1, result.Level.Game.Events.Checkpoints.Count);
        }

        // The one that is invisible in every other check: an object placed correctly whose keys sit
        // past its own death simply never moves.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_KeyframeFrames_StayLocalToTheObject()
        {
            var result = ABLevelImporter.Import(ABMockData.CreateLevel(), null, Options());
            var imported = result.Level.Game.Objects.Values.First();

            Assert.AreEqual(FrameRules.MinFrame + 60, imported.Span.StartFrame, "object starts at 1s");
            CollectionAssert.AreEqual(new[] { 0, 120 },
                imported.Positions.Select(k => k.Frame - FrameRules.MinFrame).ToArray());

            // Autokill Last Keyframe means the object dies AS it reaches its final keyframe, so
            // that keyframe lands exactly on the span's end boundary rather than inside it. It is
            // still needed: it is what every frame before it interpolates towards. Anything PAST
            // the boundary would be the real bug.
            foreach (var key in imported.Positions)
                Assert.LessOrEqual(key.Frame,
                    FrameRules.EndBoundaryOf(imported.Span.FrameDuration),
                    "a key past the span's own end would be unreachable in both formats");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_Rotation_IsAbsoluteRadians()
        {
            var level = new VgdLevel();
            level.Objects.Add(ABMockData.CreateRotatingObject());

            var result = ABLevelImporter.Import(level, null, Options());
            var imported = result.Level.Game.Objects.Values.First();

            var angles = imported.Rotations
                .OrderBy(k => k.Frame)
                .Select(k => ((FloatValue)k.Angle).Value)
                .ToArray();

            Assert.AreEqual(2, angles.Length);
            Assert.AreEqual(System.Math.PI / 2.0, angles[0], 1e-3f);
            Assert.AreEqual(System.Math.PI, angles[1], 1e-3f, "the second key is 90 more, not 90 again");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_AfterbeatScale_BecomesSizeNotScale()
        {
            var result = ABLevelImporter.Import(ABMockData.CreateLevel(), null, Options());
            var imported = result.Level.Game.Objects.Values.First();

            Assert.AreEqual(1, imported.Sizes.Count);
            Assert.AreEqual(0, imported.Scales.Count);

            var size = (Vector2Value)imported.Sizes[0].Scale;
            Assert.AreEqual(2f, size.X, 1e-4f);
            Assert.AreEqual(3f, size.Y, 1e-4f);
        }

        // Depth is absolute there and Layer is parent-relative here, so a child at depth 15 under a
        // parent at depth 20 must store 5, not the -15 its own effective layer equals.
        //
        // OnlyDepth rather than the default Auto: this asserts the SUBTRACTION, and Auto packs the
        // depths a level does not use out of the way, so under it the same pair is one layer apart -
        // correct, and no longer a statement about depth at all.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ChildLayer_IsRelativeToItsParent()
        {
            var result = ABLevelImporter.Import(ABMockData.CreateFullLevel(), null,
                Options(ABLayerImport.OnlyDepth));

            var shapes = result.Level.Game.Objects.Values.OfType<ShapeObject>().ToArray();
            var parent = shapes.Single(o => o.ParentObjectId == ObjectId.Null);
            var child = shapes.Single(o => o.ParentObjectId == parent.ObjectId);

            Assert.AreEqual(ValueRules.LastLayerBehindPlayer - VgdObject.DefaultDepth, parent.Layer,
                "the whole Default band draws behind the player, so depth 0 is 0 and this is deeper");
            Assert.AreEqual(5, child.Layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_HitAndNoHit_DecideWhetherThereIsACollider()
        {
            var level = new VgdLevel();
            var hit = ABMockData.CreateObject("hit");
            var noHit = ABMockData.CreateObject("nohit");
            noHit.ObjectType = (int)ABObjectType.NoHit;
            level.Objects.Add(hit);
            level.Objects.Add(noHit);

            var result = ABLevelImporter.Import(level, null, Options());
            var shapes = result.Level.Game.Objects.Values.OfType<ShapeObject>().ToArray();

            Assert.AreEqual(1, shapes.Count(s => s.ColliderId.IsEnabled()));
            Assert.AreEqual(1, shapes.Count(s => !s.ColliderId.IsEnabled()));
            Assert.IsTrue(shapes.All(s => s.ShapeId.IsEnabled()), "both are still drawn");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_EmptyObject_HasNoShapeAtAll()
        {
            var level = new VgdLevel();
            var empty = ABMockData.CreateObject("empty");
            empty.ObjectType = (int)ABObjectType.Empty;
            level.Objects.Add(empty);

            var result = ABLevelImporter.Import(level, null, Options());
            var imported = result.Level.Game.Objects.Values.First();

            Assert.AreEqual(ObjectType.RectObject, imported.GetModelType());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ParentedToCamera_ResolvesToTheCameraId()
        {
            var level = new VgdLevel();
            var child = ABMockData.CreateObject("child");
            child.ParentId = VgdObject.CameraParentId;
            level.Objects.Add(child);

            var result = ABLevelImporter.Import(level, null, Options());
            Assert.AreEqual(ObjectId.Camera, result.Level.Game.Objects.Values.First().ParentObjectId);
        }

        // The respawn position is the field Checkpoint grew for this; before it, this was a loss.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Import_CheckpointPosition_Crosses()
        {
            var result = ABLevelImporter.Import(ABMockData.CreateLevel(), null, Options());
            var checkpoint = result.Level.Game.Events.Checkpoints[0];

            Assert.AreEqual(CheckpointSpace.World, checkpoint.Space);
            var position = (Vector2Value)checkpoint.Position;
            Assert.AreEqual(3f, position.X, 1e-4f);
            Assert.AreEqual(-4f, position.Y, 1e-4f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_Bpm_BecomesOneBeatSegmentOverTheWholeLevel()
        {
            var result = ABLevelImporter.Import(ABMockData.CreateLevel(), null, Options());
            var beats = result.Level.Game.Events.Beats;

            Assert.AreEqual(1, beats.Count);
            Assert.AreEqual(128f, beats[0].BPM, 1e-4f);
            Assert.AreEqual(result.Level.Settings.FrameDuration, beats[0].Span.FrameDuration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_Events_ReachTheirOwnTracks()
        {
            var result = ABLevelImporter.Import(ABMockData.CreateFullLevel(), null, Options());
            var game = result.Level.Game;

            Assert.AreEqual(1, game.CameraEvents.Positions.Count);
            Assert.AreEqual(1, game.CameraEvents.Zooms.Count);
            Assert.AreEqual(1, game.CameraEvents.Rotations.Count);
            Assert.AreEqual(1, game.CameraEvents.Shakes.Count);
            Assert.AreEqual(1, game.PostProcessingEvents.Blooms.Count);
            Assert.AreEqual(1, game.PostProcessingEvents.Vignettes.Count);
            Assert.AreEqual(1, game.PostProcessingEvents.Chromatics.Count);
            Assert.AreEqual(1, game.Events.Themes.Count);
        }

        // The theme track names a theme by the same string the theme itself carries, so both sides
        // must derive the same Guid or every theme change in the level points at nothing.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ThemeKeyframe_ResolvesToAThemeTheLevelActuallyHas()
        {
            var result = ABLevelImporter.Import(ABMockData.CreateFullLevel(), null, Options());
            var themeKey = result.Level.Game.Events.Themes[0];

            Assert.IsTrue(result.Level.Resources.Themes.ContainsKey(themeKey.ThemeId));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_Prefabs_ProduceTemplatesAndPlacements()
        {
            var result = ABLevelImporter.Import(ABMockData.CreateFullLevel(), null, Options());

            Assert.AreEqual(1, result.Level.Resources.Prefabs.Count);
            var template = result.Level.Resources.Prefabs.Values.First();
            Assert.AreEqual(1, template.Objects.Count, "the template keeps its own object scope");

            var placement = result.Level.Game.Objects.Values.OfType<PrefabObject>().Single();
            Assert.IsTrue(placement.PrefabId.IsEnabled());
            Assert.AreEqual(template.PrefabId, placement.PrefabId);

            // ONE ENTRY PER TEMPLATE OBJECT, and the copies themselves are still nobody's job here.
            // An empty table used to mean "not materialized yet, the host will see to it"; it now
            // means a placement that rebuilds into nothing, so the importer mints the ids and every
            // reader builds the same content out of them.
            Assert.AreEqual(template.Objects.Count, placement.ObjectIds.Count,
                "an imported placement carries the id table its content is rebuilt from");
            CollectionAssert.AreEquivalent(template.Objects.Keys, placement.ObjectIds.Keys);
            CollectionAssert.AllItemsAreUnique(placement.ObjectIds.Values);
        }

        // The regression the whole level-wide layer plan exists for. A prefab-heavy level used to
        // cost a layer per placement on top of independently ranked templates - a real 423-object
        // level arrived spread over 894 layers reaching -520, which is not a number an author can
        // work with. What it costs now is one layer per DEPTH, however many placements share them.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ManyPlacements_CostNoDrawOrderOfTheirOwn()
        {
            const int count = 50;
            var source = ABMockData.CreateLevel();

            for (var i = 0; i < count; i++)
            {
                var prefab = new VgpPrefab { Id = $"p{i}", Name = $"p{i}" };
                var inner = ABMockData.CreateObject($"inner-{i}");
                inner.Depth = VgdObject.DefaultDepth;
                prefab.Objects.Add(inner);
                source.Prefabs.Add(prefab);

                source.PrefabPlacements.Add(new VgdPrefabPlacement { Id = $"pl{i}", PrefabId = $"p{i}" });
            }

            var result = ABLevelImporter.Import(source, null, Options(ABLayerImport.Auto));

            var placements = result.Level.Game.Objects.Values.OfType<PrefabObject>().ToArray();
            Assert.AreEqual(count, placements.Length);
            Assert.IsTrue(placements.All(p => p.Layer == 0),
                "a placement is a group of ordinary objects over there, not a render band");

            var levelObject = result.Level.Game.Objects.Values.OfType<ShapeObject>().Single();
            var templateObjects = result.Level.Resources.Prefabs.Values
                .SelectMany(prefab => prefab.Objects.Values)
                .ToArray();

            Assert.IsTrue(templateObjects.All(o => o.Layer == levelObject.Layer),
                "one depth is one layer, inside a template as much as outside it");
            Assert.AreEqual(ValueRules.LastLayerBehindPlayer, levelObject.Layer,
                "the level uses one depth, so it costs exactly the one layer behind the player");
        }

        /// <summary> A prefab holding one object that dies at song time <paramref name="autokill"/>,
        /// placed once per entry of <paramref name="placementTimes"/>. </summary>
        private static VgdLevel SongTimePrefabLevel(float autokill, params float[] placementTimes)
        {
            var level = ABMockData.CreateLevel();

            var inner = ABMockData.CreateObject("inner-1");
            inner.StartTime = 0f;
            inner.AutokillType = (int)ABAutokillType.SongTime;
            inner.AutokillOffset = autokill;

            var prefab = new VgpPrefab { Id = "p1", Name = "Burst" };
            prefab.Objects.Add(inner);
            level.Prefabs.Add(prefab);

            for (var i = 0; i < placementTimes.Length; i++)
                level.PrefabPlacements.Add(new VgdPrefabPlacement
                {
                    Id = $"pl{i}",
                    PrefabId = "p1",
                    StartTime = placementTimes[i],
                });

            return level;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_SongTimeInsideAPrefab_IsResolvedAgainstItsPlacement()
        {
            var result = ABLevelImporter.Import(SongTimePrefabLevel(30f, 20f), null, Options());

            var inner = result.Level.Resources.Prefabs.Values.Single().Objects.Values.Single();

            Assert.AreEqual(10 * Framerate, inner.Span.FrameDuration,
                "song time 30 reached from a placement at 20 is ten seconds of life, not thirty");
            Assert.IsEmpty(result.Report.Issues.Where(i => i.Code == "autokill_songtime_in_prefab"),
                "one placement resolves it exactly, so there is nothing to approximate");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_SongTimeInsideAPrefabPlacedSeveralTimes_TakesTheEarliestAndSaysSo()
        {
            var result = ABLevelImporter.Import(SongTimePrefabLevel(30f, 25f, 20f), null, Options());

            var inner = result.Level.Resources.Prefabs.Values.Single().Objects.Values.Single();

            Assert.AreEqual(10 * Framerate, inner.Span.FrameDuration,
                "the earliest placement, which is the longest of the two lifetimes");
            Assert.AreEqual(1,
                result.Report.Issues.Count(i => i.Code == "autokill_songtime_in_prefab"),
                "the later placement outlives what it did over there, and the report says so");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_PrefabPlacement_IsNamedAfterWhatItPlaces()
        {
            var result = ABLevelImporter.Import(ABMockData.CreateFullLevel(), null, Options());

            var placement = result.Level.Game.Objects.Values.OfType<PrefabObject>().Single();
            Assert.AreEqual("Burst", placement.Name, "not the placement's own Guid");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ObjectWithNoAutokillRule_OutlivesTheLevel()
        {
            var level = ABMockData.CreateLevel();
            level.Objects.Single().AutokillType = (int)ABAutokillType.OldStyleNoAutokill;

            var result = ABLevelImporter.Import(level, null, Options());
            var imported = result.Level.Game.Objects.Values.Single();

            Assert.Greater(imported.Span.EndFrame, result.Level.Settings.FrameDuration,
                "Afterbeat never kills these, so neither does the import");
            Assert.AreEqual(1, result.Report.Issues.Count(i => i.Code == "autokill_none"),
                "reported, because it is also what a misread autokill rule looks like");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_WithoutPrefabs_SkipsBothHalves()
        {
            var options = Options();
            options.ImportPrefabs = false;

            var result = ABLevelImporter.Import(ABMockData.CreateFullLevel(), null, options);

            Assert.IsEmpty(result.Level.Resources.Prefabs);
            Assert.IsEmpty(result.Level.Game.Objects.Values.OfType<PrefabObject>());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_UnsupportedTracks_AreReportedNotThrown()
        {
            var level = ABMockData.CreateLevel();
            level.SetEvents(ABEventTrack.Gradient,
                new System.Collections.Generic.List<VgdEventKeyframe> { new() });
            level.SetEvents(ABEventTrack.Hue,
                new System.Collections.Generic.List<VgdEventKeyframe> { new() });
            level.SetEvents(ABEventTrack.PlayerForce,
                new System.Collections.Generic.List<VgdEventKeyframe> { new() });

            var result = ABLevelImporter.Import(level, null, Options());
            var codes = result.Report.Issues.Select(i => i.Code).ToArray();

            CollectionAssert.Contains(codes, "event_gradient");
            CollectionAssert.Contains(codes, "event_player_force");

            // Hue crosses whole - one rotation per keyframe is exactly a flat Hue vs Hue curve - so
            // it reports nothing at all. See ABFidelityTests.Import_TheHueTrack_LandsOnColorCurves.
            CollectionAssert.DoesNotContain(codes, "event_hue_curves");
            CollectionAssert.DoesNotContain(codes, "event_hue");

            // Player force is waiting on work, the other two are not - an author has to be able to
            // tell those apart.
            var playerForce = result.Report.Issues.Single(i => i.Code == "event_player_force");
            Assert.AreEqual(InteropSeverity.Deferred, playerForce.Severity);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_Parallax_BecomesCollidedlessObjectsBehindTheContent()
        {
            var level = ABMockData.CreateLevel();
            level.Parallax.Layers.Add(new VgdParallaxLayer
            {
                Depth = 1,
                Objects =
                {
                    new VgdParallaxObject
                    {
                        Id = "bg1",
                        Transform = { Position = new VgdVector2(1f, 2f), Scale = new VgdVector2(3f, 3f) },
                    },
                },
            });

            var result = ABLevelImporter.Import(level, null, Options());
            var shapes = result.Level.Game.Objects.Values.OfType<ShapeObject>().ToArray();
            var background = shapes.OrderBy(o => o.Layer).First();

            Assert.IsFalse(background.ColliderId.IsEnabled(), "a background can never hit the player");
            Assert.AreEqual(1, background.Positions.Count);
            Assert.IsTrue(shapes.Where(o => o != background).All(o => o.Layer > background.Layer),
                "the background is below every object of the level itself");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_Parallax_ArrivesInactiveByDefault()
        {
            var level = ABMockData.CreateLevel();
            level.Parallax.Layers.Add(new VgdParallaxLayer
            {
                Objects = { new VgdParallaxObject { Id = "bg1" } },
            });

            var options = Options();
            Assert.IsFalse(options.ParallaxActive, "the background is imported switched off");

            var result = ABLevelImporter.Import(level, null, options);
            var shapes = result.Level.Game.Objects.Values.OfType<ShapeObject>().ToArray();
            var background = shapes.OrderBy(o => o.Layer).First();

            Assert.IsFalse(background.Active, "imported, kept, and drawing nothing");
            Assert.IsTrue(shapes.Where(o => o != background).All(o => o.Active),
                "the level's own content is untouched by it");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ParallaxActive_TurnsTheBackgroundOn()
        {
            var level = ABMockData.CreateLevel();
            level.Parallax.Layers.Add(new VgdParallaxLayer
            {
                Objects = { new VgdParallaxObject { Id = "bg1" } },
            });

            var options = Options();
            options.ParallaxActive = true;

            var result = ABLevelImporter.Import(level, null, options);
            var background = result.Level.Game.Objects.Values.OfType<ShapeObject>()
                .OrderBy(o => o.Layer).First();

            Assert.IsTrue(background.Active);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_WithoutParallax_LeavesTheBackgroundOut()
        {
            var level = ABMockData.CreateLevel();
            level.Parallax.Layers.Add(new VgdParallaxLayer
            {
                Objects = { new VgdParallaxObject { Id = "bg1" } },
            });

            var options = Options();
            options.ImportParallax = false;

            var result = ABLevelImporter.Import(level, null, options);
            Assert.AreEqual(1, result.Level.Game.Objects.Count, "only the level's own object");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ShapeWithNoPreset_IsSynthesizedIntoLevelResources()
        {
            var level = new VgdLevel();
            var arrow = ABMockData.CreateObject("arrow");
            arrow.Shape = (int)ABShape.Misc;
            arrow.ShapeOption = 0;
            level.Objects.Add(arrow);

            var result = ABLevelImporter.Import(level, null, Options());
            var imported = result.Level.Game.Objects.Values.OfType<ShapeObject>().Single();

            Assert.AreEqual(1, result.Level.Resources.CompositeShapes.Count);
            Assert.IsTrue(result.Level.Resources.CompositeShapes.ContainsKey(imported.ShapeId));

            var shape = result.Level.Resources.CompositeShapes[imported.ShapeId];
            Assert.GreaterOrEqual(shape.TriangleCount, ValueRules.MinShapeTriangles);
        }

        // Triangle has SIX presets, and the last two were missing until the game's own shape list
        // was read out of its scene data - so every "Triangle Bottom" in a level silently became a
        // Square. They are the same triangle as options 0 and 1, pivoted at the base rather than at
        // the centroid, which is a pivot here and not a second mesh.
        //
        // BOTH options carry a pivot now, and that is the AABB-centring showing through: Afterbeat
        // centres its triangle mesh on the CENTROID while every shape in this library is centred on
        // its bounding box, so even the plain Triangle has to move its reference point to land where
        // the source object's transform was. The two offsets differ, which is the whole point of the
        // pair, and the geometry they share is still one shape.
        [TestCase(4, 0)]
        [TestCase(5, 1)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_TriangleBottom_IsTheSameTrianglePivotedAtItsBase(int option, int centred)
        {
            var level = new VgdLevel();
            var bottom = ABMockData.CreateObject("bottom");
            bottom.Shape = (int)ABShape.Triangle;
            bottom.ShapeOption = option;
            level.Objects.Add(bottom);

            var reference = ABMockData.CreateObject("centred");
            reference.Shape = (int)ABShape.Triangle;
            reference.ShapeOption = centred;
            level.Objects.Add(reference);

            var result = ABLevelImporter.Import(level, null, Options());
            var shapes = result.Level.Game.Objects.Values.OfType<ShapeObject>().ToList();

            Assert.AreEqual(2, shapes.Count);
            Assert.AreEqual(shapes[1].ShapeId, shapes[0].ShapeId, "the geometry is the same one");
            Assert.IsEmpty(result.Level.Resources.CompositeShapes, "nothing had to be synthesized");

            var basePivot = (Vector2Value)shapes[0].Pivots.Single().Value;
            Assert.AreEqual(ABObjectImporter.DefaultPivot, basePivot.X, 1e-4f);
            Assert.AreEqual(ABObjectImporter.DefaultPivot - ABShapeMap.TriangleBaseOffset,
                basePivot.Y, 1e-4f, "the reference point sits at the triangle's base");

            var centredPivot = (Vector2Value)shapes[1].Pivots.Single().Value;
            Assert.AreEqual(ABObjectImporter.DefaultPivot, centredPivot.X, 1e-4f);
            Assert.AreEqual(ABObjectImporter.DefaultPivot - ABShapeMap.TriangleCentroidOffset,
                centredPivot.Y, 1e-4f, "the centred option sits on the triangle's centroid");

            Assert.Less(basePivot.Y, centredPivot.Y, "the base is below the centroid");
        }

        // A quarter turn under a non-uniformly scaled parent is the one non-commuting composition
        // that still lands on a plain rotation and scale - S(x,y)·R(90) == R(90)·S(y,x) - so the
        // parent's two scale components simply trade places. Composing in this format's own order
        // reaches the wrong one, and on an anisotropic parent that is the difference between a
        // shape and a streak, so the child's own scale is multiplied by the ratio that undoes it.
        [TestCase(90f)]
        [TestCase(270f)]
        [TestCase(-90f, TestName = "Import_QuarterTurnUnderASquashedParent_TradesTheParentsAxes(-90)")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_QuarterTurnUnderASquashedParent_TradesTheParentsAxes(float degrees)
        {
            const float parentX = 8f;
            const float parentY = 2f;
            const float childX = 3f;
            const float childY = 5f;

            var level = new VgdLevel();
            level.Objects.Add(Squashed("parent", null, parentX, parentY, 0f));
            level.Objects.Add(Squashed("child", "parent", childX, childY, degrees));

            var imported = ABLevelImporter.Import(level, null, Options()).Level;
            var child = imported.Game.Objects.Values
                .Single(o => o.ParentObjectId.value != 0);

            // Which FIELD the child's own scale lands in is decided by the child's OWN children,
            // and it has none - so it goes to Sizes, where it reaches the renderer and nothing
            // else. The parent's went to Scales, since this child inherits it. Asserting the field
            // is half the point: the correction has to land wherever the scale did.
            Assert.IsEmpty(child.Scales, "a childless object's scale does not need to propagate");
            var scale = (Vector2Value)child.Sizes.Single().Scale;

            Assert.AreEqual(childX * (parentY / parentX), scale.X, 1e-4f);
            Assert.AreEqual(childY * (parentX / parentY), scale.Y, 1e-4f);
        }

        // R(180) == -I commutes with a diagonal scale just as R(0) does, so a straight angle is
        // already exact and must be left alone. It used to be REPORTED as shear, which is the same
        // misreading as correcting it would be.
        [TestCase(0f)]
        [TestCase(180f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_AStraightAngleUnderASquashedParent_IsLeftAlone(float degrees)
        {
            var level = new VgdLevel();
            level.Objects.Add(Squashed("parent", null, 8f, 2f, 0f));
            level.Objects.Add(Squashed("child", "parent", 3f, 5f, degrees));

            var result = ABLevelImporter.Import(level, null, Options());
            var child = result.Level.Game.Objects.Values
                .Single(o => o.ParentObjectId.value != 0);

            var scale = (Vector2Value)child.Sizes.Single().Scale;
            Assert.AreEqual(3f, scale.X, 1e-4f);
            Assert.AreEqual(5f, scale.Y, 1e-4f);
            Assert.AreEqual(Radians(degrees), ((FloatValue)child.Rotations.Single().Angle).Value, 1e-4f);
            CollectionAssert.DoesNotContain(result.Report.Issues.Select(i => i.Code),
                "parent_scale_shear", "nothing was skewed, so nothing may be reported");
        }

        // Every other angle IS genuinely skewed over there and cannot be here - but "cannot be
        // held exactly" is not "must be left alone". A childless object with a centred pivot can
        // take the whole fit, angle included, because its rotation reaches nothing but itself.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ASkewedLeafUnderASquashedParent_TakesTheWholeFit()
        {
            const float parentX = 8f, parentY = 2f, childX = 3f, childY = 5f;
            var rotation = Radians(45f);
            var fit = ABLinearFit.Free(parentX, parentY, rotation, childX, childY);

            var level = new VgdLevel();
            level.Objects.Add(Squashed("parent", null, parentX, parentY, 0f));
            level.Objects.Add(Squashed("child", "parent", childX, childY, 45f));

            var result = ABLevelImporter.Import(level, null, Options());
            var child = result.Level.Game.Objects.Values
                .Single(o => o.ParentObjectId.value != 0);

            var scale = (Vector2Value)child.Sizes.Single().Scale;
            Assert.AreEqual(childX * fit.ScaleX, scale.X, 1e-4f);
            Assert.AreEqual(childY * fit.ScaleY, scale.Y, 1e-4f);
            Assert.AreEqual(fit.Rotation, ((FloatValue)child.Rotations.Single().Angle).Value, 1e-4f);
            Assert.AreNotEqual(rotation, fit.Rotation, "the free fit has to have moved the angle at all");
            CollectionAssert.Contains(result.Report.Issues.Select(i => i.Code), "parent_scale_shear",
                "a fitted object is still an approximated one");
        }

        // An object with children may NOT have its angle moved, however much closer that would be:
        // this format rotates a child's offset by its parent's rotation, so the whole subtree would
        // swing. It takes the scale half of the fit, which reaches only its own extent.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ASkewedParentUnderASquashedParent_KeepsItsOwnRotation()
        {
            const float parentX = 8f, parentY = 2f, middleX = 3f, middleY = 5f;
            var rotation = Radians(45f);
            var fit = ABLinearFit.KeepingRotation(parentX, parentY, rotation);

            var level = new VgdLevel();
            level.Objects.Add(Squashed("parent", null, parentX, parentY, 0f));
            level.Objects.Add(Squashed("middle", "parent", middleX, middleY, 45f));
            level.Objects.Add(Squashed("leaf", "middle", 1f, 1f, 0f));

            var imported = ABLevelImporter.Import(level, null, Options()).Level;
            var middle = imported.Game.Objects.Values.Single(o => o.Name == "middle");

            var scale = (Vector2Value)middle.Scales.Single().Scale;
            Assert.AreEqual(middleX * fit.ScaleX, scale.X, 1e-4f);
            Assert.AreEqual(middleY * fit.ScaleY, scale.Y, 1e-4f);
            Assert.AreEqual(rotation, ((FloatValue)middle.Rotations.Single().Angle).Value, 1e-4f);
        }

        // The correction is a CONSTANT, and it can only be one where both of its inputs are. An
        // animated rotation makes it vary within a single object, so nothing is applied and the
        // object is reported instead.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ASkewedChildWithAnAnimatedRotation_IsLeftAloneAndReported()
        {
            var level = new VgdLevel();
            level.Objects.Add(Squashed("parent", null, 8f, 2f, 0f));

            var child = Squashed("child", "parent", 3f, 5f, 45f);
            child.Rotate.Keyframes.Add(new VgdKeyframe
            {
                Time = 1f,
                Values = new System.Collections.Generic.List<float> { 20f },
            });
            level.Objects.Add(child);

            var result = ABLevelImporter.Import(level, null, Options());
            var imported = result.Level.Game.Objects.Values
                .Single(o => o.ParentObjectId.value != 0);

            var scale = (Vector2Value)imported.Sizes.Single().Scale;
            Assert.AreEqual(3f, scale.X, 1e-4f);
            Assert.AreEqual(5f, scale.Y, 1e-4f);
            CollectionAssert.Contains(result.Report.Issues.Select(i => i.Code), "parent_scale_shear");
        }

        private static float Radians(float degrees) => degrees * (float)System.Math.PI / 180f;

        private static VgdObject Squashed(string id, string parentId, float x, float y, float degrees)
        {
            var target = new VgdObject
            {
                Id = id,
                ParentId = parentId ?? string.Empty,
                ParentType = "111",
                ObjectType = (int)ABObjectType.Hit,
                AutokillType = (int)ABAutokillType.FixedTime,
                AutokillOffset = 4f,
                Shape = (int)ABShape.Square,
            };

            target.Move.Keyframes.Add(new VgdKeyframe
                { Time = 0f, Values = new System.Collections.Generic.List<float> { 0f, 0f } });
            target.Scale.Keyframes.Add(new VgdKeyframe
                { Time = 0f, Values = new System.Collections.Generic.List<float> { x, y } });
            target.Rotate.Keyframes.Add(new VgdKeyframe
                { Time = 0f, Values = new System.Collections.Generic.List<float> { degrees } });
            target.Color.Keyframes.Add(new VgdKeyframe
                { Time = 0f, Values = new System.Collections.Generic.List<float> { 0f, 100f } });
            return target;
        }

        // The whole preset table, pinned against the game's own - every pair below was read out of
        // DataManager.GameObjectShapes, and the outline widths out of the meshes it points at. A
        // pair that starts resolving to something else is either a real correction or a regression,
        // and either way it should not happen quietly.
        // THE FORM AND THE MEMBER ARE TWO ARGUMENTS, and that is not a style choice: a built-in
        // shape's constants live in a nested class per form now (ShapeId.Circle.S2_T4), and `nameof`
        // yields only the LAST identifier - so one argument pins every case to "S2_T4" and none of
        // them to a form. Both halves stay compile-checked, which is the property worth keeping:
        // renaming a shape has to break the build rather than the run.
        [TestCase(0, 0, nameof(ShapeId.Square), nameof(ShapeId.Square.Fill))]
        [TestCase(0, 1, nameof(ShapeId.Square), nameof(ShapeId.Square.T4))]
        [TestCase(0, 2, nameof(ShapeId.Square), nameof(ShapeId.Square.T8))]
        [TestCase(1, 0, nameof(ShapeId.Circle), nameof(ShapeId.Circle.Fill))]
        [TestCase(1, 1, nameof(ShapeId.Circle), nameof(ShapeId.Circle.T4))]
        [TestCase(1, 2, nameof(ShapeId.Circle), nameof(ShapeId.Circle.S2))]
        [TestCase(1, 4, nameof(ShapeId.Circle), nameof(ShapeId.Circle.T8))]
        [TestCase(1, 5, nameof(ShapeId.Circle), nameof(ShapeId.Circle.S4))]
        [TestCase(1, 7, nameof(ShapeId.Circle), nameof(ShapeId.Circle.S8))]
        [TestCase(2, 0, nameof(ShapeId.Triangle), nameof(ShapeId.Triangle.Fill))]
        [TestCase(2, 1, nameof(ShapeId.Triangle), nameof(ShapeId.Triangle.T2))]
        [TestCase(2, 2, nameof(ShapeId.RightTriangle), nameof(ShapeId.RightTriangle.Fill))]
        [TestCase(2, 3, nameof(ShapeId.RightTriangle), nameof(ShapeId.RightTriangle.T4))]
        [TestCase(2, 4, nameof(ShapeId.Triangle), nameof(ShapeId.Triangle.Fill))]
        [TestCase(2, 5, nameof(ShapeId.Triangle), nameof(ShapeId.Triangle.T2))]
        [TestCase(5, 0, nameof(ShapeId.Hexagon), nameof(ShapeId.Hexagon.Fill))]
        [TestCase(5, 1, nameof(ShapeId.Hexagon), nameof(ShapeId.Hexagon.T4))]
        [TestCase(5, 2, nameof(ShapeId.Hexagon), nameof(ShapeId.Hexagon.T16))]
        [TestCase(5, 3, nameof(ShapeId.Hexagon), nameof(ShapeId.Hexagon.S2))]
        // The five that used to be BUILT into the level's own resources at import time, because the
        // library had no "outlined sector" in it at all. They are ordinary built-in shapes now, and
        // pinning them here is what stops that quietly regressing back into synthesis.
        [TestCase(1, 3, nameof(ShapeId.Circle), nameof(ShapeId.Circle.S2_T4))]
        [TestCase(1, 6, nameof(ShapeId.Circle), nameof(ShapeId.Circle.S4_T4))]
        [TestCase(1, 8, nameof(ShapeId.Circle), nameof(ShapeId.Circle.S8_T4))]
        [TestCase(5, 4, nameof(ShapeId.Hexagon), nameof(ShapeId.Hexagon.S2_T4))]
        [TestCase(5, 5, nameof(ShapeId.Hexagon), nameof(ShapeId.Hexagon.S2_T16))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ShapeMap_PresetPairs_ResolveToTheMeasuredPreset(int shape, int option,
            string form, string member)
        {
            var resolved = ABShapeMap.Import(shape, option, null);

            var group = typeof(ShapeId).GetNestedType(form);
            Assert.IsNotNull(group, $"ShapeId has no {form} group");

            var field = group.GetField(member);
            Assert.IsNotNull(field, $"ShapeId.{form} has no {member}");

            Assert.AreEqual((ShapeId)field.GetValue(null), resolved);
        }

        // Misc's third entry is a PA Logo the game's own custom-polygon index makes unreachable:
        // IsCustom says option 2 is custom, so an object carrying csp there is a polygon and not a
        // logo. Deferring to the index is what reproduces the game.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_MiscOptionTwo_IsACustomPolygonNotAPreset()
        {
            var level = new VgdLevel();
            var custom = ABMockData.CreateObject("custom");
            custom.Shape = (int)ABShape.Misc;
            custom.ShapeOption = ABShapeOptions.MiscCustom;
            custom.CustomShape = new System.Collections.Generic.List<float> { 5f, 0f, 1f, 5f, 0f };
            level.Objects.Add(custom);

            var result = ABLevelImporter.Import(level, null, Options());
            var imported = result.Level.Game.Objects.Values.OfType<ShapeObject>().Single();

            // A five-sided, sharp, filled, whole polygon is one of the built-in shapes, so the
            // custom-polygon branch resolves it rather than building it - which is what the shape
            // library exists to do and is how this reads as "not the logo preset" now.
            Assert.IsTrue(BH.SDK.Services.Shapes.ShapeCatalogService.TryDecode(imported.ShapeId, out var decoded),
                "a custom polygon on the rungs must land on a built-in shape");
            Assert.AreEqual(5, decoded.Form.Sides);
            Assert.IsFalse(decoded.IsRing);
            Assert.IsTrue(decoded.IsFullTurn);
            Assert.IsEmpty(result.Level.Resources.CompositeShapes,
                "nothing had to be written into the level");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_TheSameSynthesizedShapeTwice_ProducesOneResource()
        {
            var level = new VgdLevel();
            for (var i = 0; i < 3; i++)
            {
                var arrow = ABMockData.CreateObject($"arrow{i}");
                arrow.Shape = (int)ABShape.Misc;
                level.Objects.Add(arrow);
            }

            var result = ABLevelImporter.Import(level, null, Options());
            Assert.AreEqual(1, result.Level.Resources.CompositeShapes.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ImportJson_MalformedDocument_FailsWithoutThrowing()
        {
            var result = ABLevelImporter.ImportJson("{ this is not json", null, Options());

            Assert.IsNull(result.Level);
            Assert.IsTrue(result.Report.HasFailure);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ImportJson_EmptyDocument_StillProducesALevel()
        {
            var result = ABLevelImporter.ImportJson("{}", null, Options());

            Assert.IsNotNull(result.Level);
            Assert.IsEmpty(result.Level.Game.Objects);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ImportMeta_ReadsTitleAuthorsAndArtistLink()
        {
            var report = new InteropReport();
            var meta = ABMetaImporter.Import(ABMockData.CreateMeta(), report);

            Assert.AreEqual("Test Song", ((StringValue)meta.LevelName).Value);
            Assert.AreEqual(2, meta.LevelAuthors.Count);
            Assert.AreEqual("https://someband.bandcamp.com", meta.LevelAuthors[1].Url);
        }

        // The creator/artist split is the only thing the source says about a person beyond their
        // name, and a bare list of names is where it used to go to die.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ImportMeta_CreditsTheCreatorAndTheArtistSeparately()
        {
            var meta = ABMetaImporter.Import(ABMockData.CreateMeta());

            Assert.AreEqual("Creator", ((StringValue)meta.LevelAuthors[0].Name).Value);
            Assert.AreEqual(ABMetaImporter.CreatorCredit, ((StringValue)meta.LevelAuthors[0].Credit).Value);
            Assert.AreEqual("Artist", ((StringValue)meta.LevelAuthors[1].Name).Value);
            Assert.AreEqual(ABMetaImporter.ArtistCredit, ((StringValue)meta.LevelAuthors[1].Credit).Value);
        }
    }
}
