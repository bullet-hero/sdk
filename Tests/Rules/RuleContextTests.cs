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
    /// RuleContext: what every rule is allowed to know about its surroundings. The scope it resolves
    /// is what decides which timeline a frame is measured against and which reserved parent targets
    /// mean anything, so getting it wrong misvalidates whole subtrees silently.
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

        // The analyzer still rebases as it walks - WithScope swaps the timeline, as the case above
        // asserts - but no rule measures a frame against it any more, so the same key is legal at
        // level scope and legal one level down. Kept as the end-to-end proof of exactly that: a
        // template shorter than the key inside it is not a finding.
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
