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
    /// RuleObjectIdValid: an object's own identity must be a user-space id (>= 1). The reserved
    /// negative ids are parent targets only - an object may attach to the camera, it may not be it.
    /// </summary>
    public class RuleObjectIdValidTests : BaseRuleTests
    {
        /// <summary> An object id, judged against the range its scope owns. </summary>
        [RuleContainer]
        private class IdModel
        {
            [RuleObjectIdValid]
            public ObjectId Value { get; set; } = new(1);
        }

        private static readonly RuleObjectIdValidAttribute Rule = new();

        private static PropertyInfo ValueProperty => typeof(IdModel).GetProperty(nameof(IdModel.Value));

        private static RuleContext LevelContext => RuleContext.ForRoot(new Level());
        private static RuleContext PrefabContext => RuleContext.ForRoot(new Prefab());
        private static RuleContext NoScopeContext => RuleContext.ForRoot(new object());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUserSpaceIdIsValid()
        {
            Assert.IsTrue(Rule.IsValid(new ObjectId(1), LevelContext));
            Assert.IsTrue(Rule.IsValid(new ObjectId(9999), LevelContext));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNullIdIsInvalid()
        {
            Assert.IsFalse(Rule.IsValid(ObjectId.Null, LevelContext));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestReservedNegativeIdsAreInvalid()
        {
            Assert.IsFalse(Rule.IsValid(ObjectId.Camera, LevelContext));
            Assert.IsFalse(Rule.IsValid(ObjectId.LocalPlayer, LevelContext));
            Assert.IsFalse(Rule.IsValid(ObjectId.PrefabRoot, LevelContext));
        }

        // Identity means the same thing in every scope, so unlike the parent rule this one reads
        // nothing off the context - it works standalone, inside a template, anywhere.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestScopeIndependent()
        {
            Assert.IsTrue(Rule.IsValid(new ObjectId(1), PrefabContext));
            Assert.IsTrue(Rule.IsValid(new ObjectId(1), NoScopeContext));

            Assert.IsFalse(Rule.IsValid(ObjectId.Null, PrefabContext));
            Assert.IsFalse(Rule.IsValid(ObjectId.Null, NoScopeContext));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixAssignsFirstUserId()
        {
            var model = new IdModel { Value = ObjectId.Null };
            Rule.Fix(model, ValueProperty, LevelContext);

            Assert.AreEqual(ObjectId.MinLevelValue, model.Value.value);
        }

        // The fix no longer depends on the root being a Level, so a standalone aggregate can be
        // repaired too.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixWorksWithoutLevel()
        {
            var model = new IdModel { Value = ObjectId.Null };
            Rule.Fix(model, ValueProperty, NoScopeContext);

            Assert.AreEqual(ObjectId.MinLevelValue, model.Value.value);
        }

        // Uniqueness is NOT checked - two objects may hold the same id and both pass. Anything about
        // how ids relate to each other needs the graph pass, since a per-property rule only ever
        // sees one value.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestDuplicateIdsAreNotDetected()
        {
            var first = new RectObject { ObjectId = new ObjectId(1) };
            var second = new RectObject { ObjectId = new ObjectId(1) };

            Assert.IsTrue(Rule.IsValid(first.ObjectId, LevelContext));
            Assert.IsTrue(Rule.IsValid(second.ObjectId, LevelContext));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TestThroughAnalyzerOnRealLevel()
        {
            var level = new Level();
            var invalid = new RectObject { ObjectId = ObjectId.Null };
            level.Game.Objects.Add(invalid.ObjectId, invalid);

            var issues = Analyze(level);
            CollectionAssert.IsNotEmpty(issues);

            Fix(level);
            Assert.AreEqual(ObjectId.MinLevelValue, invalid.ObjectId.value);
        }
    }
}
