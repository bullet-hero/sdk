using System.Collections.Generic;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleCollectionMaxCount: every per-track/per-level cap in the format goes through this rule.
    /// Fix drops from the tail, so the earliest authored entries survive an over-long track.
    /// </summary>
    public class RuleCollectionMaxCountTests : BaseRuleTests
    {
        /// <summary> The rule on a List property. </summary>
        [RuleContainer]
        private class ListModel
        {
            [RuleCollectionMaxCount(3)]
            public List<int> Value { get; set; } = new();
        }

        /// <summary> The rule on an array, which is fixed-size and therefore repaired by replacement. </summary>
        [RuleContainer]
        private class ArrayModel
        {
            [RuleCollectionMaxCount(3)]
            public int[] Value { get; set; } = { 1, 2, 3 };
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleCollectionMaxCount(3)]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValid()
        {
            AssertValid(new ListModel { Value = new List<int> { 1 } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmpty()
        {
            AssertValid(new ListModel());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestBoundary()
        {
            AssertValid(new ListModel { Value = new List<int> { 1, 2, 3 } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestJustOver()
        {
            AssertInvalid<RuleCollectionMaxCountAttribute>(
                new ListModel { Value = new List<int> { 1, 2, 3, 4 } });
        }

        // Tail-drop, not head-drop: keyframes authored first are the ones kept.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixDropsFromTail()
        {
            var model = new ListModel { Value = new List<int> { 1, 2, 3, 4, 5 } };
            AssertFixed(model);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixArray()
        {
            var model = new ArrayModel { Value = new[] { 1, 2, 3, 4, 5 } };
            AssertFixed(model);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Value);
        }

        // The array Fix rebuilds at exactly MaxCount whenever the length differs, including when the
        // array is SHORTER - it would pad a 1-element array up to 3. Unreachable through the analyzer
        // (a short array raises no issue, so Fix never runs), but it makes the array path lossy in the
        // other direction if anything ever calls Fix directly.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestShortArrayRaisesNoIssue()
        {
            AssertValid(new ArrayModel { Value = new[] { 1 } });
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
