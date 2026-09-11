using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Utils
{
    // The file stopped carrying a placement's materialized copies, so everything here is about the
    // two halves that replaced them agreeing: Thin drops exactly the objects Expand can rebuild, and
    // Expand rebuilds exactly what the edit-time materializer would have written. The Unity project's
    // PrefabExpandParityTests is what pins the second claim against the real materializer; this is
    // the pure half, and it is also where every refusal is pinned, because a refusal is the case
    // nobody looks at until a level opens short.
    //
    // A LEVEL IS BUILT THIN HERE AND EXPANDED, which is the direction a file travels. The one test
    // that starts from a flat level is the pre-virtualization file, and it is the only reason
    // Expand writes by indexer.

    /// <summary> Dropping a placement's copies for a write and rebuilding them after a read. </summary>
    public class PrefabVirtualizationTests
    {
        private const int PlacementStart = 100;

        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = 600;
            return level;
        }

        private static Prefab CreateTemplate(Level level)
        {
            var template = new Prefab { PrefabId = PrefabId.NewId(), Name = "template" };
            level.Resources.Prefabs.Add(template.PrefabId, template);
            return template;
        }

        private static ShapeObject AddInner(Prefab template, string name, ObjectId parentId = default)
        {
            var inner = new ShapeObject
            {
                ObjectId = template.GetNextObjectId(),
                Name = name,
                ParentObjectId = parentId,
                Span = FrameSpan.FromBounds(1, 11),
                Layer = 5,
            };
            template.Objects.Add(inner.ObjectId, inner);
            return inner;
        }

        private static PrefabObject AddPlacement(Level level, PrefabId prefabId, int startFrame = PlacementStart)
        {
            var placement = new PrefabObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                Name = "placement",
                PrefabId = prefabId,
                Span = FrameSpan.FromBounds(startFrame, startFrame + 60),
            };
            level.Game.Objects.Add(placement.ObjectId, placement);
            return placement;
        }

        // The table an author's editor would have written: one outer id per template object, minted
        // out of the host's own counter exactly as materializing does.
        private static void Remap(Level level, PrefabObject placement, params ObjectId[] innerIds)
        {
            foreach (var innerId in innerIds)
                placement.ObjectIds[innerId] = level.Settings.GetNextObjectId();
        }

        #region Expand

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Expand_RebuildsOneCopyPerTemplateObject_UnderTheIdsTheTableNames()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var first = AddInner(template, "first");
            var second = AddInner(template, "second");

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, first.ObjectId, second.ObjectId);

            var report = PrefabVirtualizationUtils.Expand(level);

            Assert.IsTrue(report.IsEmpty, "a whole placement has nothing to report");
            Assert.AreEqual(2, report.Expanded);
            Assert.AreEqual(3, level.Game.Objects.Count, "the placement plus its two copies");

            foreach (var pair in placement.ObjectIds)
            {
                Assert.IsTrue(level.Game.Objects.TryGetValue(pair.Value, out var copy),
                    $"the table names {pair.Value.value} and nothing was written there");
                Assert.AreEqual(pair.Value, copy.ObjectId, "a copy carries its OUTER id, not the template's");
                Assert.AreEqual(template.Objects[pair.Key].Name, copy.Name);
            }
        }

        // A template's spans are local to its own timeline; the placement's start is the anchor that
        // resolves them into the host's. Only the start moves - a duration means the same number in
        // either coordinate system.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Expand_ResolvesATemplateLocalSpanAgainstThePlacementStart()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var inner = AddInner(template, "inner");

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, inner.ObjectId);

            PrefabVirtualizationUtils.Expand(level);

            var copy = level.Game.Objects[placement.ObjectIds[inner.ObjectId]];

            // A LOCAL FRAME IS A FRAME, NOT AN OFFSET: the template object sits at its own
            // timeline's FIRST frame, so its copy sits on the placement's first frame - the same
            // number, not one past it. Adding the two positions outright counts the origin twice,
            // which is exactly the bug this pins (see ApplyPlacementFrameOffset's own note).
            Assert.AreEqual(FrameRules.MinFrame, inner.Span.StartFrame, "guard: the fixture is local frame 1");
            Assert.AreEqual(PlacementStart, copy.Span.StartFrame);
            Assert.AreEqual(inner.Span.FrameDuration, copy.Span.FrameDuration);
        }

        // The same statement said the only way it cannot be misread, and the case the off-by-one
        // made absurd: a placement at the very start of a level, holding a template object at the
        // very start of its own timeline, plays on frame one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Expand_OfAPlacementAtFrameOne_PutsATemplateRootOnFrameOne()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var inner = AddInner(template, "inner");

            var placement = AddPlacement(level, template.PrefabId, FrameRules.MinFrame);
            Remap(level, placement, inner.ObjectId);

            PrefabVirtualizationUtils.Expand(level);

            Assert.AreEqual(FrameRules.MinFrame,
                level.Game.Objects[placement.ObjectIds[inner.ObjectId]].Span.StartFrame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Expand_ParentsATemplateRootToThePlacementItself()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var unset = AddInner(template, "unset-parent");
            var explicitRoot = AddInner(template, "explicit-root", ObjectId.PrefabRoot);

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, unset.ObjectId, explicitRoot.ObjectId);

            PrefabVirtualizationUtils.Expand(level);

            Assert.AreEqual(placement.ObjectId,
                level.Game.Objects[placement.ObjectIds[unset.ObjectId]].ParentObjectId);
            Assert.AreEqual(placement.ObjectId,
                level.Game.Objects[placement.ObjectIds[explicitRoot.ObjectId]].ParentObjectId,
                "PrefabRoot and an unset parent mean the same thing by design");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Expand_RemapsAnInnerParentThroughThePlacementsOwnTable()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var parent = AddInner(template, "parent");
            var child = AddInner(template, "child", parent.ObjectId);

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, parent.ObjectId, child.ObjectId);

            PrefabVirtualizationUtils.Expand(level);

            Assert.AreEqual(placement.ObjectIds[parent.ObjectId],
                level.Game.Objects[placement.ObjectIds[child.ObjectId]].ParentObjectId);
        }

        // Overrides are re-applied onto a fresh template copy, always last. A path that rebuilds a
        // placement and does not end there loses every override on it silently.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Expand_ReAppliesThePlacementsOwnOverrides()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var inner = AddInner(template, "inner");

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, inner.ObjectId);

            var key = new ModificationKey(inner.ObjectId, ModificationFields.Layer);
            placement.Modifications[key] = new Modification(key, 42);

            PrefabVirtualizationUtils.Expand(level);

            Assert.AreEqual(42, level.Game.Objects[placement.ObjectIds[inner.ObjectId]].Layer);
            Assert.AreEqual(5, template.Objects[inner.ObjectId].Layer, "the template keeps its own value");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Expand_WritesNothingForAnEmptyPlacement_AndReportsNothing()
        {
            var level = CreateLevel();
            AddPlacement(level, PrefabId.Null);

            var report = PrefabVirtualizationUtils.Expand(level);

            Assert.IsTrue(report.IsEmpty, "an empty placement is a real authored state, not a finding");
            Assert.AreEqual(0, report.Placements);
            Assert.AreEqual(1, level.Game.Objects.Count);
        }

        #endregion

        #region Refusals

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Expand_ReportsAPlacementNamingATemplateThisLevelDoesNotHave()
        {
            var level = CreateLevel();
            AddPlacement(level, PrefabId.NewId());

            var report = PrefabVirtualizationUtils.Expand(level);

            Assert.AreEqual(1, report.Count);
            Assert.AreEqual(PrefabExpandProblem.TemplateMissing, report.Entries[0].Problem);
            Assert.AreEqual(1, level.Game.Objects.Count);
        }

        // What a placement the editor never materialized looks like on disk - an Afterbeat import
        // before its host resynced, or one the materializer refused on depth. One finding for the
        // whole placement rather than one per template object, or a 300-object template would file
        // 300 findings for a single authoring state.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Expand_ReportsAPlacementWithNoIdTableOnce()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            AddInner(template, "first");
            AddInner(template, "second");
            AddInner(template, "third");

            AddPlacement(level, template.PrefabId);

            var report = PrefabVirtualizationUtils.Expand(level);

            Assert.AreEqual(1, report.Count);
            Assert.AreEqual(PrefabExpandProblem.RemapMissing, report.Entries[0].Problem);
            Assert.AreEqual(0, report.Expanded);
        }

        // The one thing this may never do is invent an id. An outer id is addressable from outside
        // the placement - an ordinary object may be parented under a copy - so a minted one would be
        // an object nothing else in the file names.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Expand_ReportsATemplateObjectMissingFromTheTable_AndMintsNothing()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var mapped = AddInner(template, "mapped");
            AddInner(template, "unmapped");

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, mapped.ObjectId);

            var counterBefore = level.Settings.ObjectIdCounter;
            var report = PrefabVirtualizationUtils.Expand(level);

            Assert.AreEqual(1, report.Count);
            Assert.AreEqual(PrefabExpandProblem.RemapMissing, report.Entries[0].Problem);
            Assert.AreEqual(1, report.Expanded, "the mapped one is still rebuilt");
            Assert.AreEqual(counterBefore, level.Settings.ObjectIdCounter,
                "expansion reads ids, it never mints them");
            Assert.AreEqual(2, level.Game.Objects.Count, "the placement plus the one copy it could name");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Expand_RefusesAPlacementWhoseChainIsAlreadyTooDeep()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var inner = AddInner(template, "inner");

            // A chain long enough that the placement at its end is already past the cap.
            var parentId = ObjectId.Null;
            for (var i = 0; i <= LevelRules.MaxObjectDepth; i++)
            {
                var link = new ShapeObject
                {
                    ObjectId = level.Settings.GetNextObjectId(),
                    ParentObjectId = parentId,
                    Span = FrameSpan.FromBounds(1, 600),
                };
                level.Game.Objects.Add(link.ObjectId, link);
                parentId = link.ObjectId;
            }

            var placement = AddPlacement(level, template.PrefabId);
            placement.ParentObjectId = parentId;
            Remap(level, placement, inner.ObjectId);

            var before = level.Game.Objects.Count;
            var report = PrefabVirtualizationUtils.Expand(level);

            Assert.AreEqual(1, report.Count);
            Assert.AreEqual(PrefabExpandProblem.TooDeep, report.Entries[0].Problem);
            Assert.AreEqual(before, level.Game.Objects.Count, "a refused placement writes nothing");
        }

        #endregion

        #region Thin

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Thin_DropsEveryCopy_AndKeepsThePlacementWhole()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var inner = AddInner(template, "inner");

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, inner.ObjectId);

            var key = new ModificationKey(inner.ObjectId, ModificationFields.Layer);
            placement.Modifications[key] = new Modification(key, 42);

            PrefabVirtualizationUtils.Expand(level);
            Assert.AreEqual(2, level.Game.Objects.Count, "guard: the fixture is expanded before it is thinned");

            var thin = PrefabVirtualizationUtils.Thin(level);

            Assert.AreEqual(1, thin.Game.Objects.Count);
            var written = (PrefabObject)thin.Game.Objects[placement.ObjectId];
            Assert.AreEqual(template.PrefabId, written.PrefabId);
            Assert.AreEqual(1, written.ObjectIds.Count, "the id table is what makes the copies rebuildable");
            Assert.AreEqual(1, written.Modifications.Count);
        }

        // The live level is what the editor keeps editing through a save, so thinning it for the
        // writer must leave it exactly as it was.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Thin_LeavesTheLevelItWasGivenUntouched()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var inner = AddInner(template, "inner");

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, inner.ObjectId);
            PrefabVirtualizationUtils.Expand(level);

            var before = level.Copy();
            PrefabVirtualizationUtils.Thin(level);

            Assert.IsTrue(before.Equals(level), "Thin modified the level it was handed");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Thin_DropsCopiesInsideAPrefabTemplateToo()
        {
            var level = CreateLevel();
            var inner = CreateTemplate(level);
            var innerContent = AddInner(inner, "inner-content");

            // A template holding a placement of ANOTHER template, already materialized into it.
            var outer = CreateTemplate(level);
            var nested = new PrefabObject
            {
                ObjectId = outer.GetNextObjectId(),
                PrefabId = inner.PrefabId,
                Span = FrameSpan.FromBounds(1, 60),
            };
            outer.Objects.Add(nested.ObjectId, nested);
            nested.ObjectIds[innerContent.ObjectId] = outer.GetNextObjectId();

            PrefabVirtualizationUtils.Expand(level);

            Assert.AreEqual(2, outer.Objects.Count, "guard: the nested placement materialized into the template");

            var thin = PrefabVirtualizationUtils.Thin(level);

            Assert.AreEqual(1, thin.Resources.Prefabs[outer.PrefabId].Objects.Count,
                "a template is a scope like any other and carries no copies into the file either");
        }

        #endregion

        #region Round trips

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ExpandThenThin_GivesBackExactlyWhatWasWritten()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var parent = AddInner(template, "parent");
            var child = AddInner(template, "child", parent.ObjectId);

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, parent.ObjectId, child.ObjectId);

            var key = new ModificationKey(child.ObjectId, ModificationFields.Layer);
            placement.Modifications[key] = new Modification(key, 7);

            var written = level.Copy();
            PrefabVirtualizationUtils.Expand(level);

            Assert.IsTrue(written.Equals(PrefabVirtualizationUtils.Thin(level)),
                "the file round trips: thin -> expand -> thin is the file it started as");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Expand_IsIdempotent()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var inner = AddInner(template, "inner");

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, inner.ObjectId);

            PrefabVirtualizationUtils.Expand(level);
            var once = level.Copy();
            PrefabVirtualizationUtils.Expand(level);

            Assert.IsTrue(once.Equals(level));
        }

        // THE PRE-VIRTUALIZATION FILE, and the only reason Expand writes by indexer rather than by
        // Add. Such a level holds its flat copies AND its id table: an Add would throw or double
        // them, an indexer overwrites them where they stand. No migration, per Rule 11.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Expand_OverALevelThatStillCarriesItsCopies_DoesNotDoubleThem()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var inner = AddInner(template, "inner");

            var placement = AddPlacement(level, template.PrefabId);
            Remap(level, placement, inner.ObjectId);

            // What an older build wrote: the copies are in the file beside the placement.
            PrefabVirtualizationUtils.Expand(level);
            var flat = level.Copy();

            var report = PrefabVirtualizationUtils.Expand(flat);

            Assert.IsTrue(report.IsEmpty);
            Assert.AreEqual(2, flat.Game.Objects.Count, "the placement and ONE copy, not two");
            Assert.IsTrue(level.Equals(flat));
        }

        #endregion

        #region Capacity hints

        // THE HINT DESCRIBES WHAT PLAYS, NEVER WHAT IS WRITTEN, and the order it is computed in is
        // what enforces that: Level.Hints.Limits is swept on the LIVE, expanded level inside the
        // editor's frozen window, and the writer thins a level only afterwards. Computing it on the
        // thinned one instead would under-report by every prefab copy - and the consumer
        // (PlayerService.EnsureLevelCapacity) treats the hint as a lower bound, so the error would
        // show up as a reallocation on load rather than as anything anyone could trace back.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void PeakUsage_OfAThinnedLevel_UnderReports_WhichIsWhyTheHintIsSweptBeforeTheWrite()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var first = AddInner(template, "first");
            var second = AddInner(template, "second");

            var placement = AddPlacement(level, template.PrefabId, 1);
            Remap(level, placement, first.ObjectId, second.ObjectId);

            PrefabVirtualizationUtils.Expand(level);

            var played = LevelCapacityUtils.GetPeakUsage(level);
            var written = LevelCapacityUtils.GetPeakUsage(PrefabVirtualizationUtils.Thin(level));

            Assert.AreEqual(3, played.Instances, "the placement and both of its copies are on screen");
            Assert.AreEqual(1, written.Instances, "the file holds the placement alone");
            Assert.Greater(played.Instances, written.Instances,
                "if these ever agree, the sweep and the write are no longer in the order this rests on");
        }

        // The other half of the same claim, and the one a reader of a FILE depends on: a level that
        // travelled through the writer and back comes out with the same peak it went in with.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void PeakUsage_SurvivesAThinExpandRoundTrip()
        {
            var level = CreateLevel();
            var template = CreateTemplate(level);
            var first = AddInner(template, "first");
            var second = AddInner(template, "second");

            var placement = AddPlacement(level, template.PrefabId, 1);
            Remap(level, placement, first.ObjectId, second.ObjectId);
            PrefabVirtualizationUtils.Expand(level);

            var before = LevelCapacityUtils.GetPeakUsage(level);

            var reread = PrefabVirtualizationUtils.Thin(level);
            PrefabVirtualizationUtils.Expand(reread);

            Assert.IsTrue(before.Equals(LevelCapacityUtils.GetPeakUsage(reread)));
        }

        #endregion

        #region Nesting

        // No corpus level has a template holding a placement of its own, so this path has never been
        // exercised by real content. The ordering it pins is correctness, not tidiness: the outer
        // placement copies the template WHOLE, so the template has to be expanded first or the outer
        // copy is short by everything the nested placement owns.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Expand_BuildsANestedTemplateBeforeThePlacementThatCopiesIt()
        {
            var level = CreateLevel();

            var innerTemplate = CreateTemplate(level);
            var innerContent = AddInner(innerTemplate, "inner-content");

            var outerTemplate = CreateTemplate(level);
            var nested = new PrefabObject
            {
                ObjectId = outerTemplate.GetNextObjectId(),
                Name = "nested",
                PrefabId = innerTemplate.PrefabId,
                Span = FrameSpan.FromBounds(1, 60),
            };
            outerTemplate.Objects.Add(nested.ObjectId, nested);
            nested.ObjectIds[innerContent.ObjectId] = outerTemplate.GetNextObjectId();

            var placement = AddPlacement(level, outerTemplate.PrefabId);
            Remap(level, placement, nested.ObjectId, nested.ObjectIds[innerContent.ObjectId]);

            var report = PrefabVirtualizationUtils.Expand(level);

            Assert.IsTrue(report.IsEmpty, report.ToString());
            Assert.AreEqual(2, outerTemplate.Objects.Count,
                "the nested placement and the one object it owns, both inside the template");
            Assert.AreEqual(3, level.Game.Objects.Count,
                "the placement plus a copy of each of the template's two objects");

            var copies = new List<ObjectId>(placement.ObjectIds.Values);
            foreach (var outerId in copies)
                Assert.IsTrue(level.Game.Objects.ContainsKey(outerId),
                    $"the outer placement never wrote {outerId.value}");
        }

        #endregion
    }
}