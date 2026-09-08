using System;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // Contextual rules are the one family that cannot be exercised through a throwaway fixture root:
    // they read the analysis context. So these tests drive the attribute directly with a context of
    // our choosing, plus end-to-end passes through the analyzer over a real Level and a real Prefab.

    /// <summary>
    /// RuleLevelFrame: a frame must sit inside the timeline of the scope it lives in - the level's
    /// or, inside a prefab template, that template's own.
    /// </summary>
    public class RuleLevelFrameTests : BaseRuleTests
    {
        /// <summary> A frame number, judged against the timeline its context carries rather than against a constant. </summary>
        [RuleContainer]
        private class FrameModel
        {
            [RuleLevelFrame]
            public int Frame { get; set; }
        }

        private static readonly RuleLevelFrameAttribute Rule = new();

        private static PropertyInfo FrameProperty => typeof(FrameModel).GetProperty(nameof(FrameModel.Frame));

        private static Level LevelOfLength(int frameDuration)
        {
            var level = new Level();
            level.Settings.FrameDuration = frameDuration;
            return level;
        }

        private static RuleContext LevelContext(int frameDuration)
            => RuleContext.ForRoot(LevelOfLength(frameDuration));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInsideBounds()
        {
            var context = LevelContext(100);

            Assert.IsTrue(Rule.IsValid(0, context));
            Assert.IsTrue(Rule.IsValid(50, context));
        }

        // The upper bound is exclusive: FrameDuration is a count, so the last playable frame is
        // FrameDuration - 1.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUpperBoundIsExclusive()
        {
            var context = LevelContext(100);

            Assert.IsTrue(Rule.IsValid(99, context));
            Assert.IsFalse(Rule.IsValid(100, context));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNegativeFrame()
        {
            Assert.IsFalse(Rule.IsValid(-1, LevelContext(100)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixClampsIntoBounds()
        {
            var context = LevelContext(100);

            var overrun = new FrameModel { Frame = 500 };
            Rule.Fix(overrun, FrameProperty, context);
            Assert.AreEqual(99, overrun.Frame);

            var underrun = new FrameModel { Frame = -5 };
            Rule.Fix(underrun, FrameProperty, context);
            Assert.AreEqual(0, underrun.Frame);
        }

        // A prefab template validates against its own timeline, standalone, with no level anywhere.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestStandalonePrefabUsesOwnLength()
        {
            var context = RuleContext.ForRoot(new Prefab { FrameDuration = 10 });

            Assert.IsTrue(Rule.IsValid(9, context));
            Assert.IsFalse(Rule.IsValid(10, context));
        }

        // Descending into a template rebases the bound even when the walk started at a level: 50 is
        // legal for the level's 100-frame timeline and illegal for the template's 10-frame one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestPrefabInsideLevelUsesTemplateLength()
        {
            var level = LevelOfLength(100);
            var prefab = new Prefab { PrefabId = new PrefabId(Guid.NewGuid()), FrameDuration = 10 };
            var inner = new RectObject { ObjectId = new ObjectId(1) };
            inner.Positions.Add(new PosKey { Frame = 50 });
            prefab.Objects.Add(inner.ObjectId, inner);
            level.Resources.Prefabs.Add(prefab.PrefabId, prefab);

            var issues = Analyze(level);
            CollectionAssert.IsNotEmpty(issues);

            Fix(level);
            Assert.AreEqual(9, inner.Positions[0].Frame);
        }

        // With no scope to measure against - a LevelMeta, a UserSettings, a bare value model - the
        // rule keeps the half of itself that still means something (frames are never negative) and
        // drops the half that does not. Reporting every frame as broken, as it used to, produced
        // issues no Fix could clear.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNoScopeChecksLowerBoundOnly()
        {
            var context = RuleContext.ForRoot(new object());

            Assert.IsTrue(Rule.IsValid(0, context));
            Assert.IsTrue(Rule.IsValid(999_999, context));
            Assert.IsFalse(Rule.IsValid(-1, context));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNoScopeFixRaisesNegativeToZero()
        {
            var model = new FrameModel { Frame = -5 };
            Rule.Fix(model, FrameProperty, RuleContext.ForRoot(new object()));

            Assert.AreEqual(0, model.Frame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestThroughAnalyzerOnRealLevel()
        {
            var level = LevelOfLength(100);
            var invalid = new RectObject { ObjectId = new ObjectId(1) };
            invalid.Positions.Add(new PosKey { Frame = 500 });
            level.Game.Objects.Add(invalid.ObjectId, invalid);

            var issues = Analyze(level);
            CollectionAssert.IsNotEmpty(issues);

            Fix(level);
            Assert.AreEqual(99, invalid.Positions[0].Frame);
        }

        // Validating a template on its own no longer reports false failures - the whole reason the
        // context abstraction exists.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestThroughAnalyzerOnStandalonePrefab()
        {
            var prefab = new Prefab { PrefabId = new PrefabId(Guid.NewGuid()), FrameDuration = 10 };
            var inner = new RectObject { ObjectId = new ObjectId(1), Span = FrameSpan.FromBounds(0, 6) };
            prefab.Objects.Add(inner.ObjectId, inner);

            AssertValid(prefab);
        }
    }
}
