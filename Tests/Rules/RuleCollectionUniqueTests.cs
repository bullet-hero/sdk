using System.Collections.Generic;
using System.Linq;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleCollectionUnique: what guarantees one keyframe per frame on every track. It checks
    /// uniqueness only - ordering is explicitly not part of the format and stays the consumer's job.
    /// </summary>
    public class RuleCollectionUniqueTests : BaseRuleTests
    {
        /// <summary> The property uniqueness is judged by. </summary>
        private class Key
        {
            public int Frame { get; set; }
            public string Tag { get; set; } = string.Empty;

            public Key() { }
            public Key(int frame, string tag = "")
            {
                Frame = frame;
                Tag = tag;
            }
        }

        /// <summary> Uniqueness judged by one named property of each item. </summary>
        [RuleContainer]
        private class ByPropertyModel
        {
            [RuleCollectionUnique(nameof(Key.Frame))]
            public List<Key> Value { get; set; } = new();
        }

        /// <summary> Uniqueness judged by the items themselves. </summary>
        [RuleContainer]
        private class ByItselfModel
        {
            [RuleCollectionUnique]
            public List<int> Value { get; set; } = new();
        }

        /// <summary> Names a property the item does not have, so the DECLARATION is what must be refused. </summary>
        [RuleContainer]
        private class MissingPropertyModel
        {
            [RuleCollectionUnique("NoSuchProperty")]
            public List<Key> Value { get; set; } = new();
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleCollectionUnique]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValid()
        {
            AssertValid(new ByPropertyModel { Value = new List<Key> { new(0), new(1), new(2) } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmpty()
        {
            AssertValid(new ByPropertyModel());
        }

        // Uniqueness, not sortedness - a descending track is perfectly valid data.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUnsortedIsValid()
        {
            AssertValid(new ByPropertyModel { Value = new List<Key> { new(5), new(1), new(3) } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestDuplicateByProperty()
        {
            AssertInvalid<RuleCollectionUniqueAttribute>(
                new ByPropertyModel { Value = new List<Key> { new(1), new(1) } });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestDuplicateByItself()
        {
            AssertValid(new ByItselfModel { Value = new List<int> { 1, 2, 3 } });
            AssertInvalid<RuleCollectionUniqueAttribute>(
                new ByItselfModel { Value = new List<int> { 1, 2, 1 } });
        }

        // Fix walks backwards, so of two entries sharing a key the LAST one survives - the later edit
        // wins over the earlier. Not obvious from the rule name, and it decides which keyframe an
        // auto-fix keeps.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixKeepsLastDuplicate()
        {
            var model = new ByPropertyModel
            {
                Value = new List<Key> { new(1, "first"), new(2), new(1, "second") },
            };
            AssertFixed(model);

            Assert.AreEqual(2, model.Value.Count);
            Assert.AreEqual("second", model.Value.Single(key => key.Frame == 1).Tag);
        }

        // A null entry is skipped while validating but removed while fixing - so a list can be
        // reported valid and still get shorter when some other rule triggers a fix pass over it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNullEntryIsIgnoredButRemovedOnFix()
        {
            var model = new ByPropertyModel { Value = new List<Key> { new(1), null, new(1) } };
            AssertInvalid<RuleCollectionUniqueAttribute>(model);

            AssertFixed(model);
            Assert.AreEqual(1, model.Value.Count);
            Assert.IsNotNull(model.Value[0]);
        }

        // Naming a property that does not exist fails every non-empty collection and cannot be
        // repaired - the rule has no way to invent the key. A typo in nameof-less usage is therefore
        // a permanent validation failure rather than a silent no-op.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMissingPropertyNameStaysBroken()
        {
            var model = new MissingPropertyModel { Value = new List<Key> { new(1) } };
            AssertInvalid<RuleCollectionUniqueAttribute>(model);

            Fix(model);
            AssertInvalid<RuleCollectionUniqueAttribute>(model);
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
