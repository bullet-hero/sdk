using BH.SDK.Generators;
using BH.SDK.Generators.Modifiers;
using BH.SDK.Models;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // The generator that took over what GraphRule.ChildSpanOutsideParent used to repair, so the
    // properties worth pinning are the ones that repair never had to answer: that a fit holds all the
    // way DOWN a chain and not one level deep, that a child which plays nowhere is dealt with
    // deliberately rather than turned into a one-frame ghost by accident, that a root object stays
    // untouched because nothing bounds it, and that a prefab placement's materialized children are
    // never orphaned from the remap table pointing at them.

    /// <summary> What the removed repair rule never had to answer: that a fit holds all the way down a chain,
    /// that a child playing nowhere is dealt with deliberately, and that a placement's children stay in the
    /// remap table. </summary>
    public class SpanFitGeneratorTests
    {
        private const int FrameDuration = 600;

        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = FrameDuration;
            return level;
        }

        private static ShapeObject AddObject(Level level, int startFrame, int endFrame,
            ObjectId parent = default, FrameAnchor anchors = FrameAnchor.None, string name = "obj")
        {
            var obj = new ShapeObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                ParentObjectId = parent,
                Name = name,
                Span = FrameSpan.FromBounds(F(startFrame), F(endFrame), anchors),
            };
            level.Game.Objects.Add(obj.ObjectId, obj);
            return obj;
        }

        private static PrefabObject AddPlacement(Level level, int startFrame, int endFrame,
            ObjectId parent = default)
        {
            var placement = new PrefabObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                ParentObjectId = parent,
                Name = "placement",
                Span = FrameSpan.FromBounds(F(startFrame), F(endFrame)),
            };
            level.Game.Objects.Add(placement.ObjectId, placement);
            return placement;
        }

        private static GeneratorContext Context(Level level, int windowStart = 0,
            int windowEnd = FrameDuration)
            => new(level, FrameSpan.FromBounds(F(windowStart), F(windowEnd)));

        private static void Run(Level level, SpanFitGenerator.Parameters parameters,
            int windowStart = 0, int windowEnd = FrameDuration)
            => new SpanFitGenerator().Run(Context(level, windowStart, windowEnd), parameters);

        private static SpanFitGenerator.Parameters Clamping(
            SpanFitOutside outside = SpanFitOutside.Delete, bool invert = false)
            => new()
            {
                Mode = SpanFitMode.ClampChildren,
                Outside = outside,
                Invert = invert,
            };

        // Bounds are given and read back as OFFSETS from the timeline's first frame - F puts one on
        // the timeline - so what these tests pin is the fit's own arithmetic rather than where the
        // timeline starts.
        private static int F(int offset) => FrameRules.MinFrame + offset;

        private static void AssertSpan(RectObject obj, int startFrame, int endFrame, string message)
        {
            Assert.AreEqual(F(startFrame), obj.Span.StartFrame, $"{message}: start");
            Assert.AreEqual(F(endFrame), obj.Span.EndFrame, $"{message}: end");
        }

        #region Clamping

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Clamp_CutsAChildIntoItsParent_KeepingItsAnchors()
        {
            var level = CreateLevel();
            var parent = AddObject(level, 10, 40);
            var child = AddObject(level, 20, 60, parent.ObjectId, FrameAnchor.Both);

            Run(level, Clamping());

            AssertSpan(child, 20, 40, "child");
            Assert.AreEqual(FrameAnchor.Both, child.Span.Anchors, "existing anchors are the author's");
            AssertSpan(parent, 10, 40, "the parent is not touched");
        }

        // The whole reason the walk is parent-first: the grandchild has to be measured against what
        // its own parent became, not against what it was authored as.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Clamp_CascadesDownTheWholeChain()
        {
            var level = CreateLevel();
            var root = AddObject(level, 10, 40);
            var middle = AddObject(level, 10, 60, root.ObjectId);
            var leaf = AddObject(level, 10, 60, middle.ObjectId);

            Run(level, Clamping());

            AssertSpan(middle, 10, 40, "middle");
            AssertSpan(leaf, 10, 40, "leaf measured against the already-clamped middle");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Clamp_LeavesAContainedChildAlone()
        {
            var level = CreateLevel();
            var parent = AddObject(level, 0, 100);
            AddObject(level, 10, 50, parent.ObjectId);

            var before = level.Game.Copy();
            Run(level, Clamping());

            Assert.IsTrue(before.Equals(level.Game), "a level that already fits must not be rewritten");
        }

        // Nothing bounds a root object: running past the end of the level is legal, it simply never
        // plays. Fitting it against the timeline would be this generator inventing a rule.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Clamp_LeavesARootObjectAlone()
        {
            var level = CreateLevel();
            var root = AddObject(level, 0, FrameDuration + 100);

            Run(level, Clamping(), 0, FrameDuration + 100);

            AssertSpan(root, 0, FrameDuration + 100, "root");
        }

        #endregion

        #region A child that plays nowhere

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Delete_TakesTheWholeSubtreeOfAChildThatSharesNoFrame()
        {
            var level = CreateLevel();
            var parent = AddObject(level, 0, 50);
            var child = AddObject(level, 100, 150, parent.ObjectId);
            var grandchild = AddObject(level, 100, 120, child.ObjectId);

            Run(level, Clamping());

            Assert.IsFalse(level.Game.Objects.ContainsKey(child.ObjectId), "the child plays nowhere");
            Assert.IsFalse(level.Game.Objects.ContainsKey(grandchild.ObjectId),
                "a child of something that plays nowhere plays nowhere either");
            Assert.IsTrue(level.Game.Objects.ContainsKey(parent.ObjectId), "the parent survives");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ClampOutside_LeavesOneFrameAtTheNearestEdge()
        {
            var level = CreateLevel();
            var parent = AddObject(level, 0, 50);
            var child = AddObject(level, 100, 150, parent.ObjectId);

            Run(level, Clamping(SpanFitOutside.Clamp));

            AssertSpan(child, 49, 50, "child cut into the nearest edge");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void SkipOutside_LeavesTheChildExactlyAsAuthored()
        {
            var level = CreateLevel();
            var parent = AddObject(level, 0, 50);
            var child = AddObject(level, 100, 150, parent.ObjectId);

            Run(level, Clamping(SpanFitOutside.Skip));

            AssertSpan(child, 100, 150, "child");
        }

        #endregion

        #region The window

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Window_NamesWhichChildrenAreTalkedAbout()
        {
            var level = CreateLevel();
            var parent = AddObject(level, 200, 250);
            var child = AddObject(level, 210, 300, parent.ObjectId);

            Run(level, Clamping(), 0, 100);

            AssertSpan(child, 210, 300, "a child outside the window is not this run's business");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Invert_TakesExactlyWhatTheWindowDoesNotCover()
        {
            var level = CreateLevel();
            var parent = AddObject(level, 200, 250);
            var child = AddObject(level, 210, 300, parent.ObjectId);

            Run(level, Clamping(invert: true), 0, 100);

            AssertSpan(child, 210, 250, "child");
        }

        #endregion

        #region Expanding

        // The mirror of the clamping cascade, and the reason that walk runs child-first: the
        // grandparent has to cover the parent as it became, not as it was authored.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Expand_StretchesEveryParentUpTheChain()
        {
            var level = CreateLevel();
            var root = AddObject(level, 0, 50);
            var middle = AddObject(level, 0, 60, root.ObjectId);
            AddObject(level, 0, 120, middle.ObjectId);

            Run(level, new SpanFitGenerator.Parameters { Mode = SpanFitMode.ExpandParents });

            AssertSpan(middle, 0, 120, "middle covers its child");
            AssertSpan(root, 0, 120, "root covers the stretched middle");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Expand_StopsAtTheEndOfTheTimeline()
        {
            var level = CreateLevel();
            var parent = AddObject(level, 0, 100);
            var child = AddObject(level, 0, FrameDuration + 100, parent.ObjectId);

            Run(level, new SpanFitGenerator.Parameters { Mode = SpanFitMode.ExpandParents },
                0, FrameDuration + 100);

            AssertSpan(parent, 0, FrameDuration, "a parent is content and stays on the timeline");
            AssertSpan(child, 0, FrameDuration + 100, "the child is left exactly as authored");
        }

        #endregion

        #region Prefab placements

        // Deleting one of these would break the placement's own ObjectIds table - a cleanup pass that
        // breaks the level it cleaned. It is fitted instead of removed.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Delete_ClampsAMaterializedChildInsteadOfRemovingIt()
        {
            var level = CreateLevel();
            var placement = AddPlacement(level, 0, 50);
            var materialized = AddObject(level, 100, 150, placement.ObjectId);
            placement.ObjectIds.Add(new ObjectId(1), materialized.ObjectId);

            Run(level, Clamping());

            Assert.IsTrue(level.Game.Objects.ContainsKey(materialized.ObjectId),
                "the remap table still points at it");
            AssertSpan(materialized, 49, 50, "materialized child");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Delete_TakesADoomedPlacementsMaterializedChildrenAlong()
        {
            var level = CreateLevel();
            var parent = AddObject(level, 0, 50);
            var placement = AddPlacement(level, 100, 150, parent.ObjectId);
            var materialized = AddObject(level, 100, 150, placement.ObjectId);
            placement.ObjectIds.Add(new ObjectId(1), materialized.ObjectId);

            Run(level, Clamping());

            Assert.IsFalse(level.Game.Objects.ContainsKey(placement.ObjectId), "placement");
            Assert.IsFalse(level.Game.Objects.ContainsKey(materialized.ObjectId),
                "its materialized children go with it, or the remap table is left dangling");
        }

        #endregion
    }
}
