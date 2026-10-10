using System;
using BH.SDK.Models;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleContext: what a rule may know of its surroundings. A wrong scope misvalidates whole subtrees
    /// silently (which timeline, which reserved parent targets).
    /// </summary>
    public class RuleContextTests : BaseRuleTests
    {
        // A level's scope is assembled from two different objects on purpose: GameLevel owns the
        // objects, LevelSettings owns the timeline. Nothing else in the SDK pairs them up, which is
        // why a level cannot simply carry a timeline of its own the way a template does.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestLevelRootPairsGameAndSettings()
        {
            var level = new Level();
            level.Settings.FrameDuration = 250;

            var context = RuleContext.ForRoot(level);

            Assert.IsTrue(context.HasScope);
            Assert.IsFalse(context.IsPrefabScope);
            Assert.AreSame(level, context.Root);
            Assert.AreSame(level, context.Level);
            Assert.AreSame(level.Game, context.Objects);
            Assert.AreEqual(250, context.FrameDuration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestPrefabRootIsItsOwnScope()
        {
            var prefab = new Prefab { Root = { Span = new FrameSpan(FrameRules.MinFrame, 42) } };

            var context = RuleContext.ForRoot(prefab);

            Assert.IsTrue(context.HasScope);
            Assert.IsTrue(context.IsPrefabScope);
            Assert.AreSame(prefab, context.Objects);
            Assert.AreEqual(42, context.FrameDuration);
            Assert.IsNull(context.Level, "A standalone template has no level around it");
        }

        // LevelMeta, UserSettings, a bare value model: no timeline, no objects. Scope-dependent rules
        // must degrade rather than fail here.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestScopelessRoot()
        {
            var context = RuleContext.ForRoot(new LevelMeta());

            Assert.IsFalse(context.HasScope);
            Assert.IsFalse(context.IsPrefabScope);
            Assert.IsNull(context.Level);
            Assert.IsNull(context.Objects);
            Assert.AreEqual(0, context.FrameDuration);
        }

        // Descending into a template swaps everything scope-local but keeps the level, which
        // reference-resolving rules still need - a template's objects point at the same
        // Level.Resources as everything else.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestWithScopeKeepsLevelAndRoot()
        {
            var level = new Level();
            level.Settings.FrameDuration = 250;
            var prefab = new Prefab { Root = { Span = new FrameSpan(FrameRules.MinFrame, 42) } };

            var context = RuleContext.ForRoot(level).WithScope(prefab);

            Assert.AreSame(level, context.Root);
            Assert.AreSame(level, context.Level);
            Assert.AreSame(prefab, context.Objects);
            Assert.AreEqual(42, context.FrameDuration);
            Assert.IsTrue(context.IsPrefabScope);
        }

        // Immutable: entering a nested scope must not disturb the context an already-recorded issue
        // is holding, or a fix would clamp against the wrong timeline once the walk moved on.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestWithScopeDoesNotMutateOriginal()
        {
            var level = new Level();
            level.Settings.FrameDuration = 250;
            var outer = RuleContext.ForRoot(level);

            var inner = outer.WithScope(
                new Prefab { Root = { Span = new FrameSpan(FrameRules.MinFrame, 42) } });

            Assert.AreNotSame(outer, inner);
            Assert.AreEqual(250, outer.FrameDuration);
            Assert.IsFalse(outer.IsPrefabScope);
            Assert.AreSame(level.Game, outer.Objects);
        }

        // The analyzer still rebases the timeline, but no rule measures a frame against it: a short template
        // holding a long key is not a finding.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestAnalyzerDoesNotBoundAFrameByItsTemplate()
        {
            var atLevel = new Level();
            atLevel.Settings.FrameDuration = 100;
            var levelObject = new RectObject { ObjectId = new ObjectId(1) };
            levelObject.Positions.Add(new PosKey { Frame = 50 });
            atLevel.Game.Objects.Add(levelObject.ObjectId, levelObject);

            AssertValid(atLevel);

            var withTemplate = new Level();
            withTemplate.Settings.FrameDuration = 100;
            var prefab = new Prefab
            {
                PrefabId = new PrefabId(Guid.NewGuid()),
                Root = { Span = new FrameSpan(FrameRules.MinFrame, 10) },
            };
            var innerObject = new RectObject { ObjectId = new ObjectId(1) };
            innerObject.Positions.Add(new PosKey { Frame = 50 });
            prefab.Objects.Add(innerObject.ObjectId, innerObject);
            withTemplate.Resources.Prefabs.Add(prefab.PrefabId, prefab);

            AssertValid(withTemplate);
        }
    }
}
