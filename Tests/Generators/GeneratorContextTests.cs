using System.Collections.Generic;
using System.Linq;
using BH.SDK.Generators;
using BH.SDK.Models;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // The journal is the whole undo story (see GeneratorChangeLog's header), so these tests are less
    // about "does Create add an object" and more about "does Revert put the model back exactly, and
    // does Reapply put it forward exactly" - the two properties every future generator inherits for
    // free and none of them can test for itself.

    /// <summary> The journal every generator inherits and none can test for itself: that Revert puts the model
    /// back exactly, and Reapply puts it forward exactly. </summary>
    public class GeneratorContextTests
    {
        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = 600;
            return level;
        }

        private static RectObject AddObject(Level level, string name, int layer)
        {
            var obj = new RectObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                Name = name,
                Layer = layer,
                Span = FrameSpan.FromBounds(0, 60),
            };
            level.Game.Objects.Add(obj.ObjectId, obj);
            return obj;
        }

        // Grouping is context-level, so it works for every generator without one of them knowing
        // about it - which is exactly what has to be proven here rather than per generator.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Grouping_WrapsEverythingCreatedInOneContainer()
        {
            var level = CreateLevel();
            var host = AddObject(level, "host", 0);
            var context = new GeneratorContext(level, FrameSpan.FromBounds(10, 90), host.ObjectId, layer: 5, groupName: "Radial");

            var result = new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 3 });

            Assert.AreEqual(4, result.CreatedIds.Length, "3 objects + 1 container");

            var group = level.Game.Objects.Values.Single(o => o.Name == "Radial");
            Assert.AreEqual(host.ObjectId, group.ParentObjectId, "the container takes the author's parent");
            Assert.AreEqual(10, group.Span.StartFrame);
            Assert.AreEqual(90, group.Span.EndFrame);
            Assert.AreEqual(0, group.Layer, "Layer is parent-relative - a container repeating it would double it");

            foreach (var id in result.CreatedIds)
            {
                if (id == group.ObjectId) continue;
                Assert.AreEqual(group.ObjectId, level.Game.Objects[id].ParentObjectId);
                Assert.AreEqual(5, level.Game.Objects[id].Layer, "children keep the context layer");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Grouping_Off_ParentsDirectlyToTheGivenParent()
        {
            var level = CreateLevel();
            var host = AddObject(level, "host", 0);
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60), host.ObjectId);

            var result = new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 2 });

            Assert.AreEqual(2, result.CreatedIds.Length);
            foreach (var id in result.CreatedIds)
                Assert.AreEqual(host.ObjectId, level.Game.Objects[id].ParentObjectId);
        }

        // An empty container is worse than no container: it looks like the run half-worked.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Grouping_CreatesNothingWhenTheRunCreatesNothing()
        {
            var level = CreateLevel();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60), groupName: "Radial");

            var result = new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 0 });

            Assert.IsEmpty(result.CreatedIds);
            Assert.IsEmpty(level.Game.Objects);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Grouping_IsCountedByEstimate_AndOnlyWhenSomethingIsCreated()
        {
            var level = CreateLevel();
            var generator = new SpawnTestGenerator();

            var grouped = new GeneratorContext(level, FrameSpan.FromBounds(0, 60), groupName: "Radial");
            Assert.AreEqual(4, generator.Estimate(grouped, new SpawnTestGenerator.Parameters { Count = 3 }).Objects);

            var empty = new GeneratorContext(level, FrameSpan.FromBounds(0, 60), groupName: "Radial");
            Assert.AreEqual(0, generator.Estimate(empty, new SpawnTestGenerator.Parameters { Count = 0 }).Objects);

            var plain = new GeneratorContext(level, FrameSpan.FromBounds(0, 60));
            Assert.AreEqual(3, generator.Estimate(plain, new SpawnTestGenerator.Parameters { Count = 3 }).Objects);
        }

        // Splitting is a property of the RUN, not of any generator: SpawnTestGenerator writes
        // context.Layer onto every object it makes, and the split still wins because it runs after
        // Generate.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void LayerSplit_GivesEveryCreatedObjectItsOwnLayer()
        {
            var level = CreateLevel();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60), layer: 7, splitLayers: true);

            var result = new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 3 });

            var layers = result.CreatedIds.Select(id => level.Game.Objects[id].Layer).ToList();
            CollectionAssert.AreEqual(new[] { 7, 8, 9 }, layers);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void LayerSplit_Off_LeavesEveryObjectOnTheContextLayer()
        {
            var level = CreateLevel();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60), layer: 7);

            var result = new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 3 });

            foreach (var id in result.CreatedIds)
                Assert.AreEqual(7, level.Game.Objects[id].Layer);
        }

        // The container is the parent, and Layer is parent-relative - stepping it too would push its
        // whole subtree up by one on top of each child's own step.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void LayerSplit_SkipsTheGroupContainer()
        {
            var level = CreateLevel();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60), layer: 2, groupName: "Radial", splitLayers: true);

            new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 3 });

            var group = level.Game.Objects.Values.Single(o => o.Name == "Radial");
            Assert.AreEqual(0, group.Layer);

            var children = level.Game.Objects.Values.Where(o => o != group).Select(o => o.Layer).OrderBy(l => l);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, children.ToList());
        }

        // The container goes through Create like everything else, so undo has to take it with it -
        // otherwise an undone run leaves an empty object behind.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Grouping_Undo_RemovesTheContainerToo()
        {
            var level = CreateLevel();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60), groupName: "Radial");

            var result = new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 3 });
            Assert.AreEqual(4, level.Game.Objects.Count);

            result.Log.Revert();
            Assert.IsEmpty(level.Game.Objects);

            result.Log.Reapply();
            Assert.AreEqual(4, level.Game.Objects.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Create_AddsToScope_AndMintsIdFromCounter()
        {
            var level = CreateLevel();
            var before = level.Settings.ObjectIdCounter;
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60));

            var generator = new SpawnTestGenerator();
            var result = generator.Run(context, new SpawnTestGenerator.Parameters { Count = 3 });

            Assert.AreEqual(3, level.Game.Objects.Count);
            Assert.AreEqual(3, result.CreatedIds.Length);
            Assert.AreEqual(before + 3, level.Settings.ObjectIdCounter);
            foreach (var id in result.CreatedIds)
                Assert.IsTrue(level.Game.Objects.ContainsKey(id));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CreatedObjects_StayWithinContextBoundsAndLayer()
        {
            var level = CreateLevel();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(120, 240), ObjectId.Null, 7);

            new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 5 });

            foreach (var obj in level.Game.Objects.Values)
            {
                Assert.GreaterOrEqual(obj.Span.StartFrame, 120);
                Assert.LessOrEqual(obj.Span.EndFrame, 240);
                Assert.AreEqual(7, obj.Layer);
            }
        }

        // Every generator's estimate is shown to the author BEFORE the run and is what a host
        // refuses on, so a drifting estimate is a correctness bug, not a cosmetic one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Estimate_MatchesWhatRunActuallyProduces()
        {
            var level = CreateLevel();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60));
            var parameters = new SpawnTestGenerator.Parameters { Count = 17 };
            var generator = new SpawnTestGenerator();

            var estimate = generator.Estimate(context, parameters);
            generator.Run(context, parameters);

            var actualKeys = level.Game.Objects.Values.Sum(obj => obj.Positions.Count);
            Assert.AreEqual(level.Game.Objects.Count, estimate.Objects);
            Assert.AreEqual(actualKeys, estimate.Keyframes);
        }

        // Id counters deliberately stay advanced across a revert - see GeneratorChangeLog's header.
        // That is why this compares GameLevel rather than the whole Level: a Level-wide equality
        // check would fail on ObjectIdCounter alone and hide whatever else it was meant to catch.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Revert_RestoresGameLevel_ButLeavesIdCounterAdvanced()
        {
            var level = CreateLevel();
            AddObject(level, "existing", 0);
            var snapshot = level.Game.Copy();
            var counterBefore = level.Settings.ObjectIdCounter;

            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60));
            var result = new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 6 });

            Assert.AreEqual(7, level.Game.Objects.Count);

            result.Log.Revert();

            Assert.IsTrue(snapshot.Equals(level.Game), "Revert must restore GameLevel exactly");
            Assert.AreEqual(counterBefore + 6, level.Settings.ObjectIdCounter,
                "Ids of removed objects are never reused, so the counter must not roll back");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Reapply_AfterRevert_ReproducesTheRunExactly()
        {
            var level = CreateLevel();
            AddObject(level, "existing", 0);

            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60));
            var result = new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 6 });
            var afterRun = level.Game.Copy();

            result.Log.Revert();
            result.Log.Reapply();

            Assert.IsTrue(afterRun.Equals(level.Game), "Redo must reproduce the run byte for byte");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void RevertReapply_SurvivesRepeatedCycles()
        {
            var level = CreateLevel();
            var pristine = level.Game.Copy();

            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60));
            var result = new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 4 });
            var afterRun = level.Game.Copy();

            for (var i = 0; i < 3; i++)
            {
                result.Log.Revert();
                Assert.IsTrue(pristine.Equals(level.Game), $"cycle {i}: revert");
                result.Log.Reapply();
                Assert.IsTrue(afterRun.Equals(level.Game), $"cycle {i}: reapply");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Edit_SnapshotsBeforeState_AndRevertRestoresIt()
        {
            var level = CreateLevel();
            var a = AddObject(level, "a", 1);
            var b = AddObject(level, "b", 2);

            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60),
                selection: new List<ObjectId> { a.ObjectId, b.ObjectId });
            var result = new RenameTestModifier().Run(context,
                new RenameTestModifier.Parameters { Prefix = "x", Layer = 9 });

            Assert.AreEqual(9, level.Game.Objects[a.ObjectId].Layer);
            Assert.AreEqual(0, result.CreatedIds.Length, "A modifier creates nothing");

            result.Log.Revert();

            Assert.AreEqual("a", level.Game.Objects[a.ObjectId].Name);
            Assert.AreEqual(1, level.Game.Objects[a.ObjectId].Layer);
            Assert.AreEqual("b", level.Game.Objects[b.ObjectId].Name);
            Assert.AreEqual(2, level.Game.Objects[b.ObjectId].Layer);
        }

        // A second Edit of the same object must not overwrite the ORIGINAL before-copy, or undo
        // restores a half-generated state instead of the pre-run one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Edit_Twice_KeepsTheOriginalBeforeCopy()
        {
            var level = CreateLevel();
            var obj = AddObject(level, "original", 3);
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60));

            context.Edit(obj.ObjectId).Name = "first";
            context.Edit(obj.ObjectId).Name = "second";

            Assert.AreEqual("second", level.Game.Objects[obj.ObjectId].Name);
            context.Log.Revert();
            Assert.AreEqual("original", level.Game.Objects[obj.ObjectId].Name);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Delete_RemovesObject_AndRevertBringsItBack()
        {
            var level = CreateLevel();
            var obj = AddObject(level, "doomed", 4);
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60));

            context.Delete(obj.ObjectId);
            Assert.IsFalse(level.Game.Objects.ContainsKey(obj.ObjectId));

            context.Log.Revert();
            Assert.IsTrue(level.Game.Objects.ContainsKey(obj.ObjectId));
            Assert.AreEqual("doomed", level.Game.Objects[obj.ObjectId].Name);
        }

        // The destructive path: a generator that wipes a range before writing its own keys still
        // has to be one undoable step, wiped keys included.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ClearRange_RemovedLevelKeys_ComeBackOnRevert()
        {
            var level = CreateLevel();
            var zooms = level.Game.CameraEvents.Zooms;
            zooms.Add(new ZoomKey(new FloatValue(1f), 10));
            zooms.Add(new ZoomKey(new FloatValue(1.5f), 20));
            zooms.Add(new ZoomKey(new FloatValue(1f), 500));
            var snapshot = level.Game.CameraEvents.Copy();

            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 100));
            var result = new CameraFlashTestGenerator().Run(context, new CameraFlashTestGenerator.Parameters
            {
                Frames = new[] { 30, 40 },
                ClearRange = true,
            });

            Assert.AreEqual(3, zooms.Count, "two in-range keys wiped, two added, one out-of-range kept");
            Assert.IsTrue(zooms.Any(key => key.Frame == 500));
            Assert.IsFalse(zooms.Any(key => key.Frame == 10));

            result.Log.Revert();

            Assert.IsTrue(snapshot.Equals(level.Game.CameraEvents),
                "Undo must restore wiped keys at their original indices");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PrefabScopedContext_HasNoGameOrAudio()
        {
            var prefab = new Prefab { PrefabId = PrefabId.NewGuid(), Name = "template" };
            var level = CreateLevel();

            var context = new GeneratorContext(prefab, prefab, level.Settings, level.Resources, FrameSpan.FromBounds(0, 60));

            Assert.IsNull(context.Game, "A prefab template has no level-global event tracks");
            Assert.IsNull(context.Audio, "A prefab template has no audio");
            Assert.AreSame(prefab, context.Scope);
            Assert.AreSame(prefab, context.Counter);
        }

        // The scope/counter split is the one thing every consumer of this SDK gets wrong at least
        // once: a Prefab is both, a Level splits them across Game and Settings.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void PrefabScopedRun_WritesIntoTheTemplate_NotTheLevel()
        {
            var prefab = new Prefab { PrefabId = PrefabId.NewGuid(), Name = "template" };
            var level = CreateLevel();
            var context = new GeneratorContext(prefab, prefab, level.Settings, level.Resources, FrameSpan.FromBounds(0, 60));

            new SpawnTestGenerator().Run(context, new SpawnTestGenerator.Parameters { Count = 3 });

            Assert.AreEqual(3, prefab.Objects.Count);
            Assert.AreEqual(0, level.Game.Objects.Count);
            Assert.AreEqual(0, level.Settings.ObjectIdCounter - ObjectId.MinLevelValue,
                "Template ids must come from the prefab's own counter");
        }
    }
}
