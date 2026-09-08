using System.Collections.Generic;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleCollectionMinCount: for collections that mean nothing below a size - a curve without two
    /// keys has no segment, a gradient without two stops has no blend.
    /// </summary>
    public class RuleCollectionMinCountTests : BaseRuleTests
    {
        /// <summary> A collection element. </summary>
        private class Item
        {
            public int Number { get; set; }
        }

        /// <summary> An element with no parameterless constructor, so a collection of them cannot be padded. </summary>
        private class NoDefaultCtorItem
        {
            public int Number { get; }

            public NoDefaultCtorItem(int number)
            {
                Number = number;
            }
        }

        /// <summary> The rule on a List property. </summary>
        [RuleContainer]
        private class ListModel
        {
            [RuleCollectionMinCount(2)]
            public List<int> Value { get; set; } = new() { 1, 2 };
        }

        /// <summary> The rule on a list of reference-typed items, which a repair has to construct. </summary>
        [RuleContainer]
        private class ReferenceListModel
        {
            [RuleCollectionMinCount(2)]
            public List<Item> Value { get; set; } = new() { new Item(), new Item() };
        }

        /// <summary> A short list whose element cannot be constructed - the shortfall is reported rather than repaired. </summary>
        [RuleContainer]
        private class UnconstructableModel
        {
            [RuleCollectionMinCount(2)]
            public List<NoDefaultCtorItem> Value { get; set; } = new()
            {
                new NoDefaultCtorItem(1),
                new NoDefaultCtorItem(2),
            };
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleCollectionMinCount(2)]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValid()
        {
            AssertValid(new ListModel { Value = new List<int> { 1, 2, 3 } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestBoundary()
        {
            AssertValid(new ListModel { Value = new List<int> { 1, 2 } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestJustUnder()
        {
            AssertInvalid<RuleCollectionMinCountAttribute>(new ListModel { Value = new List<int> { 1 } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmpty()
        {
            AssertInvalid<RuleCollectionMinCountAttribute>(new ListModel { Value = new List<int>() });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixPadsValueTypes()
        {
            var model = new ListModel { Value = new List<int> { 7 } };
            AssertFixed(model);

            CollectionAssert.AreEqual(new[] { 7, 0 }, model.Value);
        }

        // The whole point of this rule over RuleCollectionCount: padding produces usable entries,
        // never nulls that satisfy the count and break everything downstream.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixPadsReferenceTypesWithInstances()
        {
            var model = new ReferenceListModel { Value = new List<Item> { new() } };
            AssertFixed(model);

            Assert.AreEqual(2, model.Value.Count);
            CollectionAssert.AllItemsAreNotNull(model.Value);
        }

        // An element type that cannot be constructed leaves the issue standing rather than inventing
        // a null - reporting an unfixable truth beats silently satisfying the count.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUnconstructableElementStaysBroken()
        {
            var model = new UnconstructableModel
            {
                Value = new List<NoDefaultCtorItem> { new(1) },
            };
            AssertInvalid<RuleCollectionMinCountAttribute>(model);

            Fix(model);

            Assert.AreEqual(1, model.Value.Count);
            AssertInvalid<RuleCollectionMinCountAttribute>(model);
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
