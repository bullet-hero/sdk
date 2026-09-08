using System.Collections.Generic;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleCollectionNoNullItems: closes the analyzer's blind spot on null elements, which are
    /// skipped during the walk and therefore never reported by anything else.
    /// </summary>
    public class RuleCollectionNoNullItemsTests : BaseRuleTests
    {
        /// <summary> A collection element. </summary>
        private class Item
        {
            public int Number { get; set; }

            public Item() { }
            public Item(int number)
            {
                Number = number;
            }
        }

        /// <summary> A list that may not hold a null entry. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleCollectionNoNullItems]
            public List<Item> Value { get; set; } = new();
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleCollectionNoNullItems]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValid()
        {
            AssertValid(new Model { Value = new List<Item> { new(1), new(2) } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmptyIsValid()
        {
            AssertValid(new Model());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNullItem()
        {
            AssertInvalid<RuleCollectionNoNullItemsAttribute>(
                new Model { Value = new List<Item> { new(1), null } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixRemovesNulls()
        {
            var model = new Model { Value = new List<Item> { null, new(1), null, new(2), null } };
            AssertFixed(model);

            Assert.AreEqual(2, model.Value.Count);
            Assert.AreEqual(1, model.Value[0].Number);
            Assert.AreEqual(2, model.Value[1].Number);
        }

        // The exact state RuleCollectionCount's own padding produces: a collection that satisfies its
        // count rule and is full of holes. Pairing the two rules is what makes the count meaningful.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCatchesWhatCountPaddingLeavesBehind()
        {
            var padded = new List<Item> { new(1), null, null };

            AssertInvalid<RuleCollectionNoNullItemsAttribute>(new Model { Value = padded });
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
