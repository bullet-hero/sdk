using BH.SDK.Generators;
using BH.SDK.Generators.Modifiers;
using BH.SDK.Models;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // The mass form of a flatten, whose whole promise is "no placement survives in this scope". Two
    // of its properties are not visible from the level it produces and have to be pinned here: that
    // the run reaches the LEVEL before it reaches the templates (a template edit propagates to
    // placements, so the other order leaves the level describing content the templates no longer
    // hold until something happens to resync it), and that both switches decline in Prefab Mode
    // rather than answering half the question from inside one template.

    /// <summary> What a whole-scope flatten leaves behind, in a level and inside its templates. </summary>
    public class PrefabFlattenGeneratorTests
    {
        private const int FrameDuration = 600;

        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = FrameDuration;
            return level;
        }

        private static Prefab AddTemplate(Level level, string name)
        {
            var template = new Prefab { PrefabId = PrefabId.NewId(), Name = name, FrameDuration = FrameDuration };
            level.Resources.Prefabs[template.PrefabId] = template;
            return template;
        }

        private static PrefabObject AddPlacement(IObjectScope scope, IObjectIdCounter counter, PrefabId prefabId)
        {
            var placement = new PrefabObject
            {
                ObjectId = counter.GetNextObjectId(),
                Name = "placement",
                Span = FrameSpan.FromBounds(0, 60),
                PrefabId = prefabId,
            };
            scope.Objects.Add(placement.ObjectId, placement);
            return placement;
        }

        private static ShapeObject AddShape(IObjectScope scope, IObjectIdCounter counter)
        {
            var shape = new ShapeObject
            {
                ObjectId = counter.GetNextObjectId(),
                Name = "shape",
                Span = FrameSpan.FromBounds(0, 60),
            };
            scope.Objects.Add(shape.ObjectId, shape);
            return shape;
        }

        private static GeneratorResult RunOnLevel(Level level, bool includeTemplates = false,
            bool removeUnused = false)
            => new PrefabFlattenGenerator().Run(
                new GeneratorContext(level, FrameSpan.FromBounds(0, FrameDuration)),
                new PrefabFlattenGenerator.Parameters
                {
                    IncludeTemplates = includeTemplates,
                    RemoveUnusedTemplates = removeUnused,
                });

        private static GeneratorResult RunOnTemplate(Level level, Prefab template,
            bool includeTemplates = false, bool removeUnused = false)
            => new PrefabFlattenGenerator().Run(
                new GeneratorContext(template, template, level.Settings, level.Resources,
                    FrameSpan.FromBounds(0, FrameDuration)),
                new PrefabFlattenGenerator.Parameters
                {
                    IncludeTemplates = includeTemplates,
                    RemoveUnusedTemplates = removeUnused,
                });

        private static int CountPlacements(IObjectScope scope)
        {
            var count = 0;
            foreach (var obj in scope.Objects.Values)
                if (obj is PrefabObject)
                    count++;
            return count;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_LeavesNoPlacementInTheLevel_AndTouchesNothingElse()
        {
            var level = CreateLevel();
            var template = AddTemplate(level, "t");
            var placement = AddPlacement(level.Game, level.Settings, template.PrefabId);
            var copy = AddShape(level.Game, level.Settings);
            placement.ObjectIds[new ObjectId(1)] = copy.ObjectId;
            copy.ParentObjectId = placement.ObjectId;

            RunOnLevel(level);

            Assert.AreEqual(0, CountPlacements(level.Game));
            Assert.IsTrue(level.Game.Objects.ContainsKey(placement.ObjectId),
                "the placement stays as the transform its children hang off");
            Assert.AreEqual(placement.ObjectId, level.Game.Objects[copy.ObjectId].ParentObjectId,
                "a flatten moves nothing");
            Assert.AreEqual(1, level.Resources.Prefabs.Count, "the library is kept by default");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_TakesANestedPlacementCopyToo()
        {
            var level = CreateLevel();
            var outerTemplate = AddTemplate(level, "outer");
            var innerTemplate = AddTemplate(level, "inner");

            var outer = AddPlacement(level.Game, level.Settings, outerTemplate.PrefabId);
            var nestedCopy = AddPlacement(level.Game, level.Settings, innerTemplate.PrefabId);
            outer.ObjectIds[new ObjectId(1)] = nestedCopy.ObjectId;

            RunOnLevel(level);

            Assert.AreEqual(0, CountPlacements(level.Game),
                "a nested copy left behind starts reading as a genuine placement - see PrefabFlattenUtils");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_LeavesTemplatesAlone_Unless_IncludeTemplates()
        {
            var level = CreateLevel();
            var host = AddTemplate(level, "host");
            var inner = AddTemplate(level, "inner");
            AddPlacement(host, host, inner.PrefabId);

            RunOnLevel(level);
            Assert.AreEqual(1, CountPlacements(host), "off by default");

            RunOnLevel(level, includeTemplates: true);
            Assert.AreEqual(0, CountPlacements(host));
        }

        // Undo is the journal, so a run that reached outside its own scope has to be revertible from
        // outside it too - which is what the scope-carrying Replace overload exists for.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_IncludingTemplates_IsRevertedByItsOwnJournal()
        {
            var level = CreateLevel();
            var host = AddTemplate(level, "host");
            var inner = AddTemplate(level, "inner");
            var nested = AddPlacement(host, host, inner.PrefabId);
            var levelPlacement = AddPlacement(level.Game, level.Settings, host.PrefabId);

            var result = RunOnLevel(level, includeTemplates: true);
            result.Log.Revert();

            Assert.IsInstanceOf<PrefabObject>(host.Objects[nested.ObjectId]);
            Assert.AreEqual(inner.PrefabId, ((PrefabObject)host.Objects[nested.ObjectId]).PrefabId);
            Assert.IsInstanceOf<PrefabObject>(level.Game.Objects[levelPlacement.ObjectId]);
        }

        // A template and the level both minting id 1 is the ordinary case, not a contrived one -
        // a Prefab is its own IObjectIdCounter. The journal has to keep the two apart.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_IncludingTemplates_DoesNotConfuseTwoScopesSharingAnObjectId()
        {
            var level = CreateLevel();
            var host = AddTemplate(level, "host");
            var inner = AddTemplate(level, "inner");

            var inTemplate = AddPlacement(host, host, inner.PrefabId);
            var inLevel = AddPlacement(level.Game, level.Settings, host.PrefabId);
            Assert.AreEqual(inTemplate.ObjectId, inLevel.ObjectId, "the fixture is only interesting if they collide");

            var result = RunOnLevel(level, includeTemplates: true);
            result.Log.Revert();

            Assert.AreEqual(inner.PrefabId, ((PrefabObject)host.Objects[inTemplate.ObjectId]).PrefabId);
            Assert.AreEqual(host.PrefabId, ((PrefabObject)level.Game.Objects[inLevel.ObjectId]).PrefabId);
        }

        // The fixed point is the whole reason the sweep loops: host leaves because nothing names it
        // any more, and stillUsed was kept only BY host, so one pass would strand it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_RemoveUnusedTemplates_SweepsWhatItsOwnRemovalsOrphan()
        {
            var level = CreateLevel();
            var host = AddTemplate(level, "host");
            var stillUsed = AddTemplate(level, "still-used");
            AddTemplate(level, "orphan");
            AddPlacement(host, host, stillUsed.PrefabId);
            AddPlacement(level.Game, level.Settings, host.PrefabId);

            RunOnLevel(level, removeUnused: true);

            Assert.AreEqual(0, level.Resources.Prefabs.Count,
                "the level is flat, so host is named by nothing - and stillUsed only by host");
        }

        // The sweep answers "referenced", not "reachable from the level": a template still holding a
        // placement of another one keeps it, which is what stops the sweep breaking a survivor.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_RemoveUnusedTemplates_InPrefabMode_KeepsWhatTemplatesStillName()
        {
            var level = CreateLevel();
            var edited = AddTemplate(level, "edited");
            var host = AddTemplate(level, "host");
            var stillUsed = AddTemplate(level, "still-used");
            AddPlacement(host, host, stillUsed.PrefabId);

            // Prefab Mode declines the switch entirely, so nothing leaves at all.
            RunOnTemplate(level, edited, removeUnused: true);

            CollectionAssert.AreEquivalent(
                new System.Collections.Generic.List<PrefabId>
                    { edited.PrefabId, host.PrefabId, stillUsed.PrefabId },
                new System.Collections.Generic.List<PrefabId>(level.Resources.Prefabs.Keys));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_RemoveUnusedTemplates_WithIncludeTemplates_EmptiesTheLibrary()
        {
            var level = CreateLevel();
            var host = AddTemplate(level, "host");
            var inner = AddTemplate(level, "inner");
            AddPlacement(host, host, inner.PrefabId);
            AddPlacement(level.Game, level.Settings, host.PrefabId);

            RunOnLevel(level, includeTemplates: true, removeUnused: true);

            Assert.AreEqual(0, level.Resources.Prefabs.Count);
        }

        // Both switches need the whole level: one to reach the level's placements first, the other to
        // know what still names a template. A Prefab scope can see neither, so it declines.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_InPrefabMode_FlattensThatTemplateOnly_AndIgnoresBothSwitches()
        {
            var level = CreateLevel();
            var edited = AddTemplate(level, "edited");
            var other = AddTemplate(level, "other");
            var inner = AddTemplate(level, "inner");
            AddPlacement(edited, edited, inner.PrefabId);
            AddPlacement(other, other, inner.PrefabId);
            AddPlacement(level.Game, level.Settings, edited.PrefabId);

            RunOnTemplate(level, edited, includeTemplates: true, removeUnused: true);

            Assert.AreEqual(0, CountPlacements(edited), "the active scope is always flattened");
            Assert.AreEqual(1, CountPlacements(other), "IncludeTemplates declines here");
            Assert.AreEqual(1, CountPlacements(level.Game), "the level is not this run's scope");
            Assert.AreEqual(3, level.Resources.Prefabs.Count, "RemoveUnusedTemplates declines here");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void IsDangerous_OnlyWhenTemplatesWouldBeDeleted()
        {
            var level = CreateLevel();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, FrameDuration));
            var generator = new PrefabFlattenGenerator();

            Assert.IsFalse(generator.IsDangerous(context,
                    new PrefabFlattenGenerator.Parameters { IncludeTemplates = true }),
                "unlinking destroys nothing and is one undo away");
            Assert.IsTrue(generator.IsDangerous(context,
                new PrefabFlattenGenerator.Parameters { RemoveUnusedTemplates = true }));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Run_OnAScopeWithNoPlacements_ChangesNothing()
        {
            var level = CreateLevel();
            AddShape(level.Game, level.Settings);

            var result = RunOnLevel(level);

            Assert.AreEqual(0, result.Log.Count);
            Assert.IsEmpty(result.CreatedIds, "a modifier creates nothing");
        }
    }
}