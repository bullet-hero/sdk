using BH.SDK.Models.Primitives;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // The three IPrimitiveInt rules share one file: they operate on the same struct family through
    // the same one-line comparison, and each one's Fix rebuilds the whole struct through
    // Activator.CreateInstance(type, value) - which is the part actually worth proving, since it is
    // what forces every id wrapper to keep a public single-arg constructor.

    /// <summary>
    /// RuleIPrimitiveIntNotNull / RuleIPrimitiveIntMin / RuleIPrimitiveIntMax over the int-backed id
    /// structs (ObjectId here; TypedResourceId and friends share the convention).
    /// </summary>
    public class RuleIPrimitiveIntTests : BaseRuleTests
    {
        /// <summary> An id wrapper that must be set at all. </summary>
        [RuleContainer]
        private class NotNullModel
        {
            [RuleIPrimitiveIntNotNull]
            public ObjectId Value { get; set; } = new(1);
        }

        /// <summary> An id wrapper with a floor, which is how one id range is told from another. </summary>
        [RuleContainer]
        private class MinModel
        {
            [RuleIPrimitiveIntMin(1)]
            public ObjectId Value { get; set; } = new(1);
        }

        /// <summary> An id wrapper with a ceiling. </summary>
        [RuleContainer]
        private class MaxModel
        {
            [RuleIPrimitiveIntMax(-1)]
            public ObjectId Value { get; set; } = new(-1);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIPrimitiveIntNotNull]
            public int Value { get; set; } = 1;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNotNullValid()
        {
            AssertValid(new NotNullModel { Value = new ObjectId(5) });
        }

        // "Not null" means "not the reserved 0", nothing more - a negative id passes, because the
        // rule deliberately does not pick a side of the game-defined/user-defined split.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNotNullAcceptsNegative()
        {
            AssertValid(new NotNullModel { Value = new ObjectId(-5) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNotNullRejectsZero()
        {
            AssertInvalid<RuleIPrimitiveIntNotNullAttribute>(new NotNullModel { Value = ObjectId.Null });
        }

        // Fix invents id 1 - a valid-looking reference that may well point at nothing. Acceptable
        // for "unset" fields, and exactly why the two properties where Null is a real authored state
        // (ShapeObject.ColliderId, PrefabObject.PrefabId) must never carry a not-null rule.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNotNullFixAssignsOne()
        {
            var model = new NotNullModel { Value = ObjectId.Null };
            AssertFixedTo(model, () => model.Value.value, 1);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMin()
        {
            AssertValid(new MinModel { Value = new ObjectId(1) });
            AssertValid(new MinModel { Value = new ObjectId(1000) });
            AssertInvalid<RuleIPrimitiveIntMinAttribute>(new MinModel { Value = new ObjectId(0) });
            AssertInvalid<RuleIPrimitiveIntMinAttribute>(new MinModel { Value = new ObjectId(-1) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMax()
        {
            AssertValid(new MaxModel { Value = new ObjectId(-1) });
            AssertValid(new MaxModel { Value = new ObjectId(-1000) });
            AssertInvalid<RuleIPrimitiveIntMaxAttribute>(new MaxModel { Value = new ObjectId(0) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinFixRaisesToMin()
        {
            var model = new MinModel { Value = new ObjectId(-50) };
            AssertFixedTo(model, () => model.Value.value, 1);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMaxFixLowersToMax()
        {
            var model = new MaxModel { Value = new ObjectId(50) };
            AssertFixedTo(model, () => model.Value.value, -1);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestWrongType()
        {
            AssertWrongType(new WrongTypeModel());
        }
    }
}
