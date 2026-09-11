using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // WHAT THIS FIXTURE ASKS CHANGED WITH THE ADDRESS, and the change is the point rather than a
    // side effect. A dotted path could only be asked whether it was non-empty and under a length
    // ceiling - questions about a string, answerable without the model. A field id is asked whether
    // it is a field of anything at all, which is the real question, because ModificationTable
    // answers it at compile time. The length cases below are gone with the string, and the fix
    // cases with them: an unregistered id cannot be repaired into a registered one.

    /// <summary>
    /// RuleModificationKeyValid: the only reach the property-level rules have into ModificationKey,
    /// which is otherwise doubly invisible - a struct, and used as a dictionary key.
    /// </summary>
    public class RuleModificationKeyValidTests : BaseRuleTests
    {
        /// <summary> A prefab-override key, whose object id and field are both checked. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleModificationKeyValid]
            public ModificationKey Key { get; set; } = new(new ObjectId(1), ModificationFields.Layer);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleModificationKeyValid] public string Key { get; set; } = string.Empty;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValid()
        {
            AssertValid(new Model { Key = new ModificationKey(new ObjectId(1), ModificationFields.Layer) });
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
                new Model { Key = new ModificationKey(ObjectId.Null, ModificationFields.Layer) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestReservedObjectId()
        {
            AssertInvalid<RuleModificationKeyValidAttribute>(
                new Model { Key = new ModificationKey(ObjectId.PrefabRoot, ModificationFields.Layer) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNoField()
        {
            AssertInvalid<RuleModificationKeyValidAttribute>(
                new Model { Key = new ModificationKey(new ObjectId(1), ModificationFields.None) });
        }

        // A number in no band at all. This is the case the length ceiling used to stand in for, and
        // it is a stronger one: the old rule would happily pass "no_such_field".
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUnregisteredField()
        {
            AssertInvalid<RuleModificationKeyValidAttribute>(
                new Model { Key = new ModificationKey(new ObjectId(1), 0x7F01) });
        }

        // An index is legal on a collection field and on nothing else - addressing element 0 of a
        // scalar is an address the table could never answer.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestIndexBelongsToCollectionsOnly()
        {
            AssertValid(new Model { Key = new ModificationKey(new ObjectId(1), ModificationFields.Positions, 0) });
            AssertInvalid<RuleModificationKeyValidAttribute>(
                new Model { Key = new ModificationKey(new ObjectId(1), ModificationFields.Layer, 0) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNegativeIndexBelowWholeField()
        {
            AssertInvalid<RuleModificationKeyValidAttribute>(
                new Model { Key = new ModificationKey(new ObjectId(1), ModificationFields.Positions, -2) });
        }

        // Nothing here is repairable and the rule says so, which is what keeps a broken override
        // reported rather than silently repointed at a different field.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixLeavesEverythingAlone()
        {
            var model = new Model { Key = new ModificationKey(ObjectId.Null, ModificationFields.Layer) };
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
