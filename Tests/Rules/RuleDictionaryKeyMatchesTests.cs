using System.Collections.Generic;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleDictionaryKeyMatches: catches an id-keyed dictionary whose key and value's own id have
    /// drifted apart - a state serialization can never produce but code can.
    /// </summary>
    public class RuleDictionaryKeyMatchesTests : BaseRuleTests
    {
        /// <summary> A dictionary whose keys must equal the id each value already carries. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleDictionaryKeyMatches(nameof(RectObject.ObjectId))]
            public Dictionary<ObjectId, RectObject> Value { get; set; } = new();
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleDictionaryKeyMatches(nameof(RectObject.ObjectId))]
            public List<RectObject> Value { get; set; } = new();
        }

        private static Dictionary<ObjectId, RectObject> Consistent()
        {
            var first = new RectObject { ObjectId = new ObjectId(1) };
            var second = new RectObject { ObjectId = new ObjectId(2) };

            return new Dictionary<ObjectId, RectObject>
            {
                { first.ObjectId, first },
                { second.ObjectId, second },
            };
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestConsistent()
        {
            AssertValid(new Model { Value = Consistent() });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmptyIsValid()
        {
            AssertValid(new Model());
        }

        // The realistic failure: something set the object's id without re-keying the dictionary, so
        // lookup by id finds nothing while iteration finds the object.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestDriftedKey()
        {
            var objects = Consistent();
            objects[new ObjectId(1)].ObjectId = new ObjectId(42);

            AssertInvalid<RuleDictionaryKeyMatchesAttribute>(new Model { Value = objects });
        }

        // The value's own id is the authored intent; the key is bookkeeping, so the key gets rebuilt.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixRekeysToValueId()
        {
            var objects = Consistent();
            var moved = objects[new ObjectId(1)];
            moved.ObjectId = new ObjectId(42);

            var model = new Model { Value = objects };
            AssertFixed(model);

            Assert.AreEqual(2, model.Value.Count);
            Assert.IsTrue(model.Value.ContainsKey(new ObjectId(42)));
            Assert.IsFalse(model.Value.ContainsKey(new ObjectId(1)));
            Assert.AreSame(moved, model.Value[new ObjectId(42)]);
        }

        // Two objects claiming one identity cannot both survive a re-key. Collapsing them is the
        // honest outcome - and it is exactly why id uniqueness needs its own graph-level check
        // rather than being inferred from the dictionary surviving a fix.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixCollapsesCollidingIds()
        {
            var objects = Consistent();
            objects[new ObjectId(1)].ObjectId = new ObjectId(2);

            var model = new Model { Value = objects };
            AssertFixed(model);

            Assert.AreEqual(1, model.Value.Count);
            Assert.IsTrue(model.Value.ContainsKey(new ObjectId(2)));
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
