using System;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleIPrimitiveGuidNotNull over the Guid-backed id structs. Guid ids have no
    /// game-defined/user-defined sign split, so "is it set" is the only context-free check possible
    /// on one - everything else needs the owning collection, i.e. graph-level validation.
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

        // Fix mints a brand new Guid, i.e. a reference to a resource that does not exist. Fine for a
        // field that must point somewhere and currently points nowhere; catastrophic for the two
        // properties where Null is a real authored state - which is why they must never carry this
        // rule (ShapeObject.ColliderId would gain a phantom collider, PrefabObject.PrefabId a
        // dangling template). This test is the reason that stays a deliberate omission.
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
