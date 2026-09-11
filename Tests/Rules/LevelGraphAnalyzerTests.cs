using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Events;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Validations;
using BH.SDK.Validations.Graph;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // Every case here is data that passes the declarative rules completely - each individual value is
    // in range, each id is well-formed - and is still a broken level. That gap is the reason this
    // layer exists.

    /// <summary>
    /// LevelGraphAnalyzer: the cross-object invariants no property or object rule can express.
    /// </summary>
    public class LevelGraphAnalyzerTests
    {
        private static readonly LevelGraphAnalyzer Analyzer = new();

        private static Level LevelWith(params RectObject[] objects)
        {
            var level = new Level();
            foreach (var obj in objects) level.Game.Objects.Add(obj.ObjectId, obj);

            level.Settings.ObjectIdCounter = objects.Length == 0
                ? ObjectId.MinLevelValue
                : objects.Max(obj => obj.ObjectId.value) + 1;

            return level;
        }

        private static RectObject Obj(int id, int parent = ObjectId.NullValue)
            => new() { ObjectId = new ObjectId(id), ParentObjectId = new ObjectId(parent) };

        // A child reaching outside its parent is legal authored data, not a finding: the overhang is
        // resolved away on read and simply never plays. This used to be reported as advice, which
        // meant a level behaving exactly as designed logged an issue on every single load - fitting
        // the lifetimes is now an edit the author asks for (mod_span_fit).
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestChildSpanOutsideParentIsNotReported()
        {
            var parent = Obj(1);
            parent.Span = FrameSpan.FromBounds(10, 40);
            var child = Obj(2, 1);
            child.Span = FrameSpan.FromBounds(20, 60);

            AssertClean(LevelWith(parent, child));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestChildSpanInsideParentIsClean()
        {
            var parent = Obj(1);
            parent.Span = FrameSpan.FromBounds(10, 40);
            var child = Obj(2, 1);
            child.Span = FrameSpan.FromBounds(20, 30);

            CollectionAssert.IsEmpty(Analyzer.Analyze(LevelWith(parent, child)));
        }

        private static Level LevelWithBeats(params BeatSegment[] segments)
        {
            var level = new Level();
            foreach (var segment in segments) level.Game.Events.Beats.Add(segment);
            return level;
        }

        private static BeatSegment Beat(int start, int end) =>
            new(FrameSpan.FromBounds(start, end), LevelRules.DefaultBpm, 0f,
                LevelRules.DefaultBeatsPerBar, "beat", new Color4Value());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestBeatSegmentsOverlap()
        {
            AssertReports(LevelWithBeats(Beat(0, 100), Beat(50, 150)), GraphRule.BeatSegmentsOverlap);
        }

        // Half-open spans, so segments that meet exactly share no frame - the shape FrameSpan exists
        // for, and the ordinary way two tempo sections sit next to each other.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestAdjacentBeatSegmentsAreClean()
        {
            AssertClean(LevelWithBeats(Beat(0, 100), Beat(100, 200)));
        }

        // The list is authored and nothing sorts it, so an overlap must be found regardless of which
        // of the two comes first in it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestBeatSegmentsOverlapFoundOutOfOrder()
        {
            AssertReports(LevelWithBeats(Beat(50, 150), Beat(0, 100)), GraphRule.BeatSegmentsOverlap);
        }

        private static void AssertReports(Level level, GraphRule expected)
        {
            var issues = Analyzer.Analyze(level);

            Assert.IsTrue(issues.Any(issue => issue.Rule == expected),
                $"Expected {expected}, got: {string.Join("; ", issues.Select(i => i.ToString()))}");
        }

        private static void AssertClean(Level level)
        {
            var issues = Analyzer.Analyze(level);
            CollectionAssert.IsEmpty(issues, string.Join("\n", issues.Select(i => i.ToString())));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestHealthyLevel()
        {
            AssertClean(LevelWith(Obj(1), Obj(2, 1), Obj(3, 2)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestEmptyLevel()
        {
            AssertClean(new Level());
        }

        // The dictionary key is unique by construction, so a duplicate can only live in the value -
        // and the value's id is what every parent and remap resolves against.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestDuplicateObjectId()
        {
            var level = LevelWith(Obj(1), Obj(2));
            level.Game.Objects[new ObjectId(2)].ObjectId = new ObjectId(1);

            AssertReports(level, GraphRule.DuplicateObjectId);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestMissingParent()
        {
            AssertReports(LevelWith(Obj(1, 99)), GraphRule.MissingParent);
        }

        // Null and the reserved negatives are chain terminators, not missing objects.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestReservedParentsAreNotMissing()
        {
            AssertClean(LevelWith(Obj(1, ObjectId.NullValue), Obj(2, -1), Obj(3, -2)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestParentCycle()
        {
            var level = LevelWith(Obj(1, 2), Obj(2, 1));
            AssertReports(level, GraphRule.ParentCycle);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestSelfParent()
        {
            AssertReports(LevelWith(Obj(1, 1)), GraphRule.ParentCycle);
        }

        // A counter that is not past every id in use hands the next created object a colliding id -
        // silent while authoring, corrupt afterwards.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestIdCounterBehind()
        {
            var level = LevelWith(Obj(1), Obj(2));
            level.Settings.ObjectIdCounter = 2;

            AssertReports(level, GraphRule.IdCounterBehind);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestPlacementWithMissingTemplate()
        {
            var placement = new PrefabObject
            {
                ObjectId = new ObjectId(1),
                PrefabId = PrefabId.NewGuid(),
            };
            var level = LevelWith(placement);

            AssertReports(level, GraphRule.UnresolvedReference);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestEmptyPlacementIsNotDangling()
        {
            var placement = new PrefabObject { ObjectId = new ObjectId(1), PrefabId = PrefabId.Null };
            AssertClean(LevelWith(placement));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestBrokenRemap()
        {
            var template = new Prefab { PrefabId = PrefabId.NewGuid() };
            var inner = Obj(1);
            template.Objects.Add(inner.ObjectId, inner);
            template.ObjectIdCounter = 2;

            var placement = new PrefabObject { ObjectId = new ObjectId(1), PrefabId = template.PrefabId };
            placement.ObjectIds.Add(new ObjectId(77), new ObjectId(1));

            var level = LevelWith(placement);
            level.Resources.Prefabs.Add(template.PrefabId, template);

            AssertReports(level, GraphRule.PrefabRemapBroken);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestOverrideTargetingRemovedTemplateObject()
        {
            var template = new Prefab { PrefabId = PrefabId.NewGuid() };
            var inner = Obj(1);
            template.Objects.Add(inner.ObjectId, inner);
            template.ObjectIdCounter = 2;

            var placement = new PrefabObject { ObjectId = new ObjectId(1), PrefabId = template.PrefabId };
            var key = new ModificationKey(new ObjectId(42), ModificationFields.Positions);
            placement.Modifications.Add(key, new Modification(key, 1L));

            var level = LevelWith(placement);
            level.Resources.Prefabs.Add(template.PrefabId, template);

            AssertReports(level, GraphRule.ModificationTargetMissing);
        }

        // The failure that makes materialization non-terminating rather than merely wrong.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestPrefabSelfReference()
        {
            var template = new Prefab { PrefabId = PrefabId.NewGuid() };
            var inner = new PrefabObject { ObjectId = new ObjectId(1), PrefabId = template.PrefabId };
            template.Objects.Add(inner.ObjectId, inner);
            template.ObjectIdCounter = 2;

            var level = new Level();
            level.Resources.Prefabs.Add(template.PrefabId, template);

            AssertReports(level, GraphRule.PrefabCycle);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestPrefabMutualReference()
        {
            var first = new Prefab { PrefabId = PrefabId.NewGuid(), ObjectIdCounter = 2 };
            var second = new Prefab { PrefabId = PrefabId.NewGuid(), ObjectIdCounter = 2 };

            var intoSecond = new PrefabObject { ObjectId = new ObjectId(1), PrefabId = second.PrefabId };
            var intoFirst = new PrefabObject { ObjectId = new ObjectId(1), PrefabId = first.PrefabId };
            first.Objects.Add(intoSecond.ObjectId, intoSecond);
            second.Objects.Add(intoFirst.ObjectId, intoFirst);

            var level = new Level();
            level.Resources.Prefabs.Add(first.PrefabId, first);
            level.Resources.Prefabs.Add(second.PrefabId, second);

            AssertReports(level, GraphRule.PrefabCycle);
        }

        // Nesting several templates in a row is ordinary content, not a cycle - the walk must
        // distinguish "deep" from "looping".
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestLegitimateNestingIsClean()
        {
            var level = new Level();
            var leaf = new Prefab { PrefabId = PrefabId.NewGuid(), ObjectIdCounter = 2 };
            var leafObject = Obj(1);
            leaf.Objects.Add(leafObject.ObjectId, leafObject);

            var host = new Prefab { PrefabId = PrefabId.NewGuid(), ObjectIdCounter = 2 };
            var placement = new PrefabObject { ObjectId = new ObjectId(1), PrefabId = leaf.PrefabId };
            host.Objects.Add(placement.ObjectId, placement);

            level.Resources.Prefabs.Add(leaf.PrefabId, leaf);
            level.Resources.Prefabs.Add(host.PrefabId, host);

            var issues = Analyzer.Analyze(level);
            Assert.IsFalse(issues.Any(issue => issue.Rule == GraphRule.PrefabCycle));
            Assert.IsFalse(issues.Any(issue => issue.Rule == GraphRule.PrefabTooDeep));
        }

        // Everything above is invisible to the declarative layer: a cycling level breaks no single
        // property rule, which is precisely why this analyzer exists.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestGraphFailuresPassDeclarativeRules()
        {
            var level = LevelWith(Obj(1, 2), Obj(2, 1));

            CollectionAssert.IsEmpty(new RuleAnalyzer().Analyze(level, new RuleAnalyzerSettings(true, true)));

            AssertReports(level, GraphRule.ParentCycle);
        }
    }
}
