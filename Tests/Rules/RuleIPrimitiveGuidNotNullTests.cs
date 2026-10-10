using System;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleIPrimitiveGuidNotNull over Guid-backed ids: "is it set" is the only context-free check; the
    /// rest is graph-level.
    /// </summary>
    public class RuleIPrimitiveGuidNotNullTests : BaseRuleTests
    {
        /// <summary> The model the rule under test sits on. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleIPrimitiveGuidNotNull]
            public ThemeId Value { get; set; } = new(Guid.NewGuid());
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIPrimitiveGuidNotNull]
            public Guid Value { get; set; } = Guid.NewGuid();
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValid()
        {
            AssertValid(new Model { Value = new ThemeId(Guid.NewGuid()) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmptyGuidIsInvalid()
        {
            AssertInvalid<RuleIPrimitiveGuidNotNullAttribute>(
                new Model { Value = new ThemeId(Guid.Empty) });
        }

        // Fix mints a new Guid: wrong where Null is real (ShapeObject.ColliderId, PrefabObject.PrefabId),
        // which must never carry this rule.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixInventsNewGuid()
        {
            var model = new Model { Value = new ThemeId(Guid.Empty) };
            AssertFixed(model);

            Assert.AreNotEqual(Guid.Empty, model.Value.value);
        }

        // The rule is typed against the IPrimitiveGuid wrapper, not against a raw Guid - a bare Guid
        // property is rejected rather than silently validated.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestWrongTypeRawGuid()
        {
            AssertWrongType(new WrongTypeModel());
        }
    }
}
