using System;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleParentObjectIdValid: a parent reference may additionally be Null (level space) or one of
    /// the three reserved targets - but only the ones that mean something in the current scope.
    /// </summary>
    public class RuleParentObjectIdValidTests : BaseRuleTests
    {
        /// <summary> A parent reference, which must name an object that exists in the same scope. </summary>
        [RuleContainer]
        private class ParentModel
        {
            [RuleParentObjectIdValid]
            public ObjectId Value { get; set; } = ObjectId.Null;
        }

        private static readonly RuleParentObjectIdValidAttribute Rule = new();

        private static PropertyInfo ValueProperty => typeof(ParentModel).GetProperty(nameof(ParentModel.Value));

        private static RuleContext LevelContext => RuleContext.ForRoot(new Level());
        private static RuleContext PrefabContext => RuleContext.ForRoot(new Prefab());
        private static RuleContext NoScopeContext => RuleContext.ForRoot(new object());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUserSpaceParent()
        {
            Assert.IsTrue(Rule.IsValid(new ObjectId(1), LevelContext));
            Assert.IsTrue(Rule.IsValid(new ObjectId(1), PrefabContext));
        }

        // Null is a real state here, not an error: it means "parented to level space".
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNullParentIsValid()
        {
            Assert.IsTrue(Rule.IsValid(ObjectId.Null, LevelContext));
            Assert.IsTrue(Rule.IsValid(ObjectId.Null, PrefabContext));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestBelowReservedRangeIsInvalid()
        {
            Assert.IsFalse(Rule.IsValid(new ObjectId(-4), LevelContext));
            Assert.IsFalse(Rule.IsValid(new ObjectId(-1000), LevelContext));
        }

        // Camera and LocalPlayer are level-runtime objects: reachable from a level's own objects,
        // not from inside a template, which has no level around it to attach to.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRuntimeTargetsAreLevelScopeOnly()
        {
            Assert.IsTrue(Rule.IsValid(ObjectId.Camera, LevelContext));
            Assert.IsTrue(Rule.IsValid(ObjectId.LocalPlayer, LevelContext));

            Assert.IsFalse(Rule.IsValid(ObjectId.Camera, PrefabContext));
            Assert.IsFalse(Rule.IsValid(ObjectId.LocalPlayer, PrefabContext));
        }

        // The mirror image: PrefabRoot addresses the template's own root, which a level does not
        // have. This used to be accepted everywhere - the leniency the context abstraction closes.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestPrefabRootIsTemplateScopeOnly()
        {
            Assert.IsTrue(Rule.IsValid(ObjectId.PrefabRoot, PrefabContext));
            Assert.IsFalse(Rule.IsValid(ObjectId.PrefabRoot, LevelContext));
        }

        // With no scope resolved there is nothing to judge "meaningful here" against, so the rule
        // falls back to the plain range check rather than guessing.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNoScopeAcceptsEveryReservedTarget()
        {
            Assert.IsTrue(Rule.IsValid(ObjectId.Camera, NoScopeContext));
            Assert.IsTrue(Rule.IsValid(ObjectId.LocalPlayer, NoScopeContext));
            Assert.IsTrue(Rule.IsValid(ObjectId.PrefabRoot, NoScopeContext));
            Assert.IsFalse(Rule.IsValid(new ObjectId(-4), NoScopeContext));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixDetachesToLevelSpace()
        {
            var model = new ParentModel { Value = new ObjectId(-50) };
            Rule.Fix(model, ValueProperty, LevelContext);

            Assert.AreEqual(ObjectId.NullValue, model.Value.value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixDetachesWrongScopeTarget()
        {
            var model = new ParentModel { Value = ObjectId.PrefabRoot };
            Rule.Fix(model, ValueProperty, LevelContext);

            Assert.AreEqual(ObjectId.NullValue, model.Value.value);
        }

        // A parent id pointing at an object that does not exist passes: existence, like uniqueness,
        // is a graph property, invisible to a single-property rule.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestDanglingParentIsNotDetected()
        {
            Assert.IsTrue(Rule.IsValid(new ObjectId(12345), LevelContext));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestThroughAnalyzerInsideTemplate()
        {
            var prefab = new Prefab { PrefabId = new PrefabId(Guid.NewGuid()) };
            var inner = new RectObject { ObjectId = new ObjectId(1), ParentObjectId = ObjectId.Camera };
            prefab.Objects.Add(inner.ObjectId, inner);

            AssertInvalid<RuleParentObjectIdValidAttribute>(prefab);

            Fix(prefab);
            Assert.AreEqual(ObjectId.NullValue, inner.ParentObjectId.value);
        }
    }
}
