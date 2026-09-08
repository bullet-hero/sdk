using BH.SDK.Models.Primitives;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleModificationKeyValid: the only reach the property-level rules have into ModificationKey,
    /// which is otherwise doubly invisible - a struct, and used as a dictionary key.
    /// </summary>
    public class RuleModificationKeyValidTests : BaseRuleTests
    {
        private const int MaxPath = 16;

        /// <summary> A prefab-override key, whose object id and field path are both checked. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleModificationKeyValid(MaxPath)]
            public ModificationKey Key { get; set; } = new(new ObjectId(1), "pos[0].v");
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleModificationKeyValid(MaxPath)]
            public string Key { get; set; } = string.Empty;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValid()
        {
            AssertValid(new Model { Key = new ModificationKey(new ObjectId(1), "pos[0].v") });
        }

        // The id addresses an object inside the template, so it must be a user-space id - the
        // reserved negatives mean nothing there.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNullObjectId()
        {
            AssertInvalid<RuleModificationKeyValidAttribute>(
                new Model { Key = new ModificationKey(ObjectId.Null, "pos[0].v") });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestReservedObjectId()
        {
            AssertInvalid<RuleModificationKeyValidAttribute>(
                new Model { Key = new ModificationKey(ObjectId.PrefabRoot, "pos[0].v") });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmptyPath()
        {
            AssertInvalid<RuleModificationKeyValidAttribute>(
                new Model { Key = new ModificationKey(new ObjectId(1), string.Empty) });
            AssertInvalid<RuleModificationKeyValidAttribute>(
                new Model { Key = new ModificationKey(new ObjectId(1), null) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestPathBoundary()
        {
            AssertValid(new Model { Key = new ModificationKey(new ObjectId(1), new string('a', MaxPath)) });
            AssertInvalid<RuleModificationKeyValidAttribute>(
                new Model { Key = new ModificationKey(new ObjectId(1), new string('a', MaxPath + 1)) });
        }

        // Truncation moves the problem from "malformed" to "dangling", which is where the graph pass
        // can see it - the path almost certainly no longer resolves.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixTruncatesPath()
        {
            var model = new Model { Key = new ModificationKey(new ObjectId(1), new string('a', 100)) };
            AssertFixed(model);

            Assert.AreEqual(MaxPath, model.Key.Path.Length);
            Assert.AreEqual(1, model.Key.ObjectId.value);
        }

        // A broken id is deliberately left alone: repointing it would apply the author's override to
        // a different object, silently and plausibly. Dropping the entry belongs to whoever owns the
        // dictionary, so the issue stays reported.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixLeavesBrokenObjectIdAlone()
        {
            var model = new Model { Key = new ModificationKey(ObjectId.Null, "pos[0].v") };
            Fix(model);

            Assert.AreEqual(ObjectId.NullValue, model.Key.ObjectId.value);
            AssertInvalid<RuleModificationKeyValidAttribute>(model);
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
