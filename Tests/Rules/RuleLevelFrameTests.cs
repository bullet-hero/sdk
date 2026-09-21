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
    // The contexts are still built and still handed to the rule, and that is the point of half this
    // fixture: the answer must not depend on them any more. A level, a template with a ten-frame
    // timeline and a root with no timeline at all are asked the same question and give the same
    // answer, because the only bound left is the origin every scope shares.

    /// <summary>
    /// RuleLevelFrame: a frame must be on the timeline at all - <see cref="FrameRules.MinFrame"/> or
    /// later - and nothing bounds it from the right, since a level's length moves.
    /// </summary>
    public class RuleLevelFrameTests : BaseRuleTests
    {
        /// <summary> A frame number, judged against the origin rather than against any timeline. </summary>
        [RuleContainer]
        private class FrameModel
        {
            [RuleLevelFrame] public int Frame { get; set; }
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
        public void TestFrameOnTheTimeline()
        {
            var context = LevelContext(100);

            Assert.IsTrue(Rule.IsValid(FrameRules.MinFrame, context));
            Assert.IsTrue(Rule.IsValid(50, context));
        }

        // A level's end is a number the author drags, so content parked past it is content waiting
        // for the level to grow - not a defect, and emphatically not something to clamp away.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestPastTheEndIsLegal()
        {
            var context = LevelContext(100);

            Assert.IsTrue(Rule.IsValid(100, context));
            Assert.IsTrue(Rule.IsValid(101, context));
            Assert.IsTrue(Rule.IsValid(999_999, context));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNegativeFrame()
        {
            Assert.IsFalse(Rule.IsValid(-1, LevelContext(100)));
            Assert.IsFalse(Rule.IsValid(FrameRules.NoFrame, LevelContext(100)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixRaisesTheFloorAndNothingElse()
        {
            var context = LevelContext(100);

            var overrun = new FrameModel { Frame = 500 };
            Rule.Fix(overrun, FrameProperty, context);
            Assert.AreEqual(500, overrun.Frame);

            var underrun = new FrameModel { Frame = -5 };
            Rule.Fix(underrun, FrameProperty, context);
            Assert.AreEqual(FrameRules.MinFrame, underrun.Frame);
        }

        // A template's own length used to be the bound here. It is not one any more: a template is
        // lengthened exactly like a level is.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestStandalonePrefabIgnoresItsOwnLength()
        {
            var context = RuleContext.ForRoot(
                new Prefab { Root = { Span = new FrameSpan(FrameRules.MinFrame, 10) } });

            Assert.IsTrue(Rule.IsValid(10, context));
            Assert.IsTrue(Rule.IsValid(11, context));
            Assert.IsFalse(Rule.IsValid(FrameRules.NoFrame, context));
        }

        // The end-to-end counterpart of the case above, and the one that used to REWRITE the level:
        // walking into a template found frame 50 against its ten frames and Fix clamped the key onto
        // frame 10, losing the author's number.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestPrefabInsideLevelKeepsAKeyPastItsEnd()
        {
            var level = LevelOfLength(100);
            var prefab = new Prefab
            {
                PrefabId = new PrefabId(Guid.NewGuid()),
                Root = { Span = new FrameSpan(FrameRules.MinFrame, 10) },
            };
            var inner = new RectObject { ObjectId = new ObjectId(1) };
            inner.Positions.Add(new PosKey { Frame = 50 });
            prefab.Objects.Add(inner.ObjectId, inner);
            level.Resources.Prefabs.Add(prefab.PrefabId, prefab);

            AssertValid(level);

            Fix(level);
            Assert.AreEqual(50, inner.Positions[0].Frame);
        }

        // A root with no scope at all - a LevelMeta, a UserSettings, a bare value model - is no
        // longer a special case: there is nothing left for a scope to answer.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestWithoutScopeGivesTheSameAnswer()
        {
            var context = RuleContext.ForRoot(new object());

            Assert.IsTrue(Rule.IsValid(FrameRules.MinFrame, context));
            Assert.IsTrue(Rule.IsValid(999_999, context));
            Assert.IsFalse(Rule.IsValid(FrameRules.NoFrame, context));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNoScopeFixRaisesNegativeToTheFirstFrame()
        {
            var model = new FrameModel { Frame = -5 };
            Rule.Fix(model, FrameProperty, RuleContext.ForRoot(new object()));

            Assert.AreEqual(FrameRules.MinFrame, model.Frame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestThroughAnalyzerOnRealLevel()
        {
            var level = LevelOfLength(100);
            var invalid = new RectObject { ObjectId = new ObjectId(1) };
            invalid.Positions.Add(new PosKey { Frame = FrameRules.NoFrame });
            level.Game.Objects.Add(invalid.ObjectId, invalid);

            var issues = Analyze(level);
            CollectionAssert.IsNotEmpty(issues);

            Fix(level);
            Assert.AreEqual(FrameRules.MinFrame, invalid.Positions[0].Frame);
        }

        // Validating a template on its own no longer reports false failures - the whole reason the
        // context abstraction exists.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestThroughAnalyzerOnStandalonePrefab()
        {
            var prefab = new Prefab
            {
                PrefabId = new PrefabId(Guid.NewGuid()),
                Root = { Span = new FrameSpan(FrameRules.MinFrame, 10) },
            };
            var inner = new RectObject
                { ObjectId = new ObjectId(1), Span = FrameSpan.FromBounds(FrameRules.MinFrame, 6) };
            prefab.Objects.Add(inner.ObjectId, inner);

            AssertValid(prefab);
        }
    }
}