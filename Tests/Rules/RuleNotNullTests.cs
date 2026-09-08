using System;
using System.Collections.Generic;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleNotNull: the most-used rule in the format (228 properties). Its Fix constructs a
    /// replacement, which is why the Roslyn analyzer insists every [RuleContainer] has a public
    /// parameterless constructor - these tests cover each construction path it can take.
    /// </summary>
    public class RuleNotNullTests : BaseRuleTests
    {
        /// <summary> An interface a property can be typed as, which a repair cannot construct directly. </summary>
        private interface IPayload
        {
            int Number { get; }
        }

        /// <summary> The one implementation of it. </summary>
        private class Payload : IPayload
        {
            public int Number { get; }

            public Payload()
            {
                Number = 0;
            }
            public Payload(int number)
            {
                Number = number;
            }
        }

        /// <summary> A plain reference property. </summary>
        [RuleContainer]
        private class ReferenceModel
        {
            [RuleNotNull]
            public Payload Value { get; set; } = new();
        }

        /// <summary> A string, repaired to empty rather than left null. </summary>
        [RuleContainer]
        private class StringModel
        {
            [RuleNotNull]
            public string Value { get; set; } = "text";
        }

        /// <summary> A list, repaired to an empty one. </summary>
        [RuleContainer]
        private class ListModel
        {
            [RuleNotNull]
            public List<int> Value { get; set; } = new();
        }

        /// <summary> An array, repaired to an empty one. </summary>
        [RuleContainer]
        private class ArrayModel
        {
            [RuleNotNull]
            public int[] Value { get; set; } = Array.Empty<int>();
        }

        /// <summary> A property typed as the interface, where the repair has to pick an implementation. </summary>
        [RuleContainer]
        private class InterfaceModel
        {
            [RuleNotNull(typeof(Payload))]
            public IPayload Value { get; set; } = new Payload();
        }

        /// <summary> A property whose type has no parameterless constructor. </summary>
        [RuleContainer]
        private class ConstructArgsModel
        {
            [RuleNotNull(typeof(Payload), 42)]
            public IPayload Value { get; set; } = new Payload(42);
        }

        /// <summary> A nullable value type, where null is a real state rather than a missing reference. </summary>
        [RuleContainer]
        private class NullableModel
        {
            [RuleNotNull]
            public int? Value { get; set; } = 0;
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleNotNull]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValid()
        {
            AssertValid(new ReferenceModel());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInvalid()
        {
            AssertInvalid<RuleNotNullAttribute>(new ReferenceModel { Value = null });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixReference()
        {
            var model = new ReferenceModel { Value = null };
            AssertFixed(model);
            Assert.IsNotNull(model.Value);
        }

        // string has no parameterless-ctor path worth taking, so Fix special-cases it to Empty -
        // an empty name is valid, a null one is not.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixStringToEmpty()
        {
            var model = new StringModel { Value = null };
            AssertFixedTo(model, () => model.Value, string.Empty);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixListToEmpty()
        {
            var model = new ListModel { Value = null };
            AssertFixed(model);
            Assert.IsNotNull(model.Value);
            Assert.IsEmpty(model.Value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixArrayToEmpty()
        {
            var model = new ArrayModel { Value = null };
            AssertFixed(model);
            Assert.IsNotNull(model.Value);
            Assert.AreEqual(0, model.Value.Length);
        }

        // An interface property cannot be constructed from its own type - the attribute has to be
        // told which concrete variant to build. This is how every polymorphic IFloat/IColor4/...
        // property in the model tree gets repaired.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixInterfaceUsesDefaultConstructType()
        {
            var model = new InterfaceModel { Value = null };
            AssertFixed(model);
            Assert.IsInstanceOf<Payload>(model.Value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixUsesConstructArgs()
        {
            var model = new ConstructArgsModel { Value = null };
            AssertFixed(model);
            Assert.AreEqual(42, model.Value.Number);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNullable()
        {
            AssertValid(new NullableModel { Value = 5 });
            AssertInvalid<RuleNotNullAttribute>(new NullableModel { Value = null });
        }

        // Nullable<T> is detected as invalid but cannot be repaired: Fix builds the replacement with
        // Activator.CreateInstance(typeof(T?)), which yields null again, and a null replacement is
        // skipped. So a nullable property with this rule reports forever. No live model has one
        // today - pinned here so adding the first one is a deliberate choice, not a surprise.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixNullableIsNoOp()
        {
            var model = new NullableModel { Value = null };
            Fix(model);

            Assert.IsNull(model.Value);
            AssertInvalid<RuleNotNullAttribute>(model);
        }

        // A non-nullable value type can never be null, so the rule is meaningless there and the
        // analyzer refuses it outright instead of quietly always passing.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestWrongTypeValueType()
        {
            AssertWrongType(new WrongTypeModel());
        }
    }
}
