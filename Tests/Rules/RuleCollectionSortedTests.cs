using System.Collections.Generic;
using System.Linq;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleCollectionSorted: the format's first Advice-level rule. Unsorted tracks are valid data -
    /// this only says they would read better sorted, and stays out of an Error-only report.
    /// </summary>
    public class RuleCollectionSortedTests : BaseRuleTests
    {
        /// <summary> The property the collection has to be ordered by. </summary>
        private class Key
        {
            public int Frame { get; set; }

            public Key() { }
            public Key(int frame)
            {
                Frame = frame;
            }
        }

        /// <summary> A list required to be in ascending order of that key. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleCollectionSorted(nameof(Key.Frame))]
            public List<Key> Value { get; set; } = new();
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleCollectionSorted(nameof(Key.Frame))]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestSorted()
        {
            AssertValid(new Model { Value = new List<Key> { new(0), new(5), new(9) } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEqualKeysCountAsSorted()
        {
            AssertValid(new Model { Value = new List<Key> { new(5), new(5) } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUnsorted()
        {
            AssertInvalid<RuleCollectionSortedAttribute>(
                new Model { Value = new List<Key> { new(5), new(1) } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixSorts()
        {
            var model = new Model { Value = new List<Key> { new(9), new(1), new(5) } };
            AssertFixed(model);

            CollectionAssert.AreEqual(new[] { 1, 5, 9 }, model.Value.Select(key => key.Frame));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestGroupIsAdvice()
        {
            AssertGroup<RuleCollectionSortedAttribute>(
                new Model { Value = new List<Key> { new(5), new(1) } }, RuleGroup.Advice);
        }

        // A consumer asking only "can this level be played" must not be told about tidiness.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestSuppressedInErrorOnlyReport()
        {
            var model = new Model { Value = new List<Key> { new(5), new(1) } };

            CollectionAssert.IsEmpty(Analyze(model, new RuleAnalyzerSettings(true, true, RuleGroup.Error)));
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
