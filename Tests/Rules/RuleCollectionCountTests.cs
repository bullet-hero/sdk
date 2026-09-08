using System.Collections.Generic;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleCollectionCount: exact size. Only ThemeData.Matrix uses it today (the fixed 64-slot theme
    /// grid), which is also the case its padding behaviour has to be judged against.
    /// </summary>
    public class RuleCollectionCountTests : BaseRuleTests
    {
        /// <summary> A collection element. </summary>
        private class Item
        {
            public int Number { get; set; }
        }

        /// <summary> The rule on a List property. </summary>
        [RuleContainer]
        private class ListModel
        {
            [RuleCollectionCount(3)]
            public List<int> Value { get; set; } = new() { 1, 2, 3 };
        }

        /// <summary> The rule on a list of reference-typed items, which a repair has to construct. </summary>
        [RuleContainer]
        private class ReferenceListModel
        {
            [RuleCollectionCount(3)]
            public List<Item> Value { get; set; } = new() { new Item(), new Item(), new Item() };
        }

        /// <summary> The rule on an array, which is fixed-size and therefore repaired by replacement. </summary>
        [RuleContainer]
        private class ArrayModel
        {
            [RuleCollectionCount(3)]
            public int[] Value { get; set; } = { 1, 2, 3 };
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleCollectionCount(3)]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValid()
        {
            AssertValid(new ListModel());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestTooFew()
        {
            AssertInvalid<RuleCollectionCountAttribute>(new ListModel { Value = new List<int> { 1 } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestTooMany()
        {
            AssertInvalid<RuleCollectionCountAttribute>(
                new ListModel { Value = new List<int> { 1, 2, 3, 4 } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixTrims()
        {
            var model = new ListModel { Value = new List<int> { 1, 2, 3, 4, 5 } };
            AssertFixed(model);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixPadsValueTypes()
        {
            var model = new ListModel { Value = new List<int> { 7 } };
            AssertFixed(model);

            CollectionAssert.AreEqual(new[] { 7, 0, 0 }, model.Value);
        }

        // Padding a reference-typed collection inserts nulls: the count rule is then satisfied while
        // the collection holds unusable entries, and the analyzer walks straight past them (it skips
        // null elements). Anything relying on this rule to guarantee usable slots - ThemeData.Matrix
        // above all - needs a not-null-elements check too, which no rule provides today.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixPadsReferenceTypesWithNulls()
        {
            var model = new ReferenceListModel { Value = new List<Item> { new() } };
            AssertFixed(model);

            Assert.AreEqual(3, model.Value.Count);
            Assert.IsNotNull(model.Value[0]);
            Assert.IsNull(model.Value[1]);
            Assert.IsNull(model.Value[2]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixArrayResizes()
        {
            var model = new ArrayModel { Value = new[] { 1 } };
            AssertFixed(model);

            CollectionAssert.AreEqual(new[] { 1, 0, 0 }, model.Value);
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
