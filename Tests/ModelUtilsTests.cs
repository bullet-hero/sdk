using System.Collections.Generic;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary>
    /// ModelUtils' collection hash/equality helpers. The dictionary hash is the one worth pinning:
    /// almost every key type in this format is a non-comparable id struct, so anything that sorts
    /// the keys throws instead of hashing.
    /// </summary>
    [TestFixture]
    public class ModelUtilsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetDictionaryHashCode_NonComparableKey_DoesNotThrow()
        {
            var dictionary = new Dictionary<ObjectId, ObjectId>
            {
                { new ObjectId(1), new ObjectId(10) },
                { new ObjectId(2), new ObjectId(20) },
            };

            Assert.DoesNotThrow(() => dictionary.GetDictionaryHashCode());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetDictionaryHashCode_SameEntriesDifferentInsertionOrder_AreEqual()
        {
            var first = new Dictionary<ObjectId, ObjectId>
            {
                { new ObjectId(1), new ObjectId(10) },
                { new ObjectId(2), new ObjectId(20) },
                { new ObjectId(3), new ObjectId(30) },
            };
            var second = new Dictionary<ObjectId, ObjectId>
            {
                { new ObjectId(3), new ObjectId(30) },
                { new ObjectId(1), new ObjectId(10) },
                { new ObjectId(2), new ObjectId(20) },
            };

            Assert.AreEqual(first.GetDictionaryHashCode(), second.GetDictionaryHashCode());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetDictionaryHashCode_DifferentValue_Differs()
        {
            var first = new Dictionary<ObjectId, ObjectId> { { new ObjectId(1), new ObjectId(10) } };
            var second = new Dictionary<ObjectId, ObjectId> { { new ObjectId(1), new ObjectId(11) } };

            Assert.AreNotEqual(first.GetDictionaryHashCode(), second.GetDictionaryHashCode());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetDictionaryHashCode_Null_IsZero()
        {
            Dictionary<ObjectId, ObjectId> dictionary = null;
            Assert.AreEqual(0, dictionary.GetDictionaryHashCode());
        }

        /// <summary> The regression this all came from: reverting an override put a placement in a
        /// HashSet, which hashes its ObjectIds/Modifications tables. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PrefabObjectGetHashCode_WithModifications_DoesNotThrow()
        {
            var placement = new PrefabObject { ObjectId = new ObjectId(1) };
            placement.ObjectIds.Add(new ObjectId(5), new ObjectId(50));
            placement.ObjectIds.Add(new ObjectId(6), new ObjectId(60));

            var key = new ModificationKey(new ObjectId(5), "lay");
            placement.Modifications.Add(key, new Modification(key, 3L));

            var other = new PrefabObject { ObjectId = new ObjectId(2) };

            Assert.DoesNotThrow(() => placement.GetHashCode());
            Assert.DoesNotThrow(() => new HashSet<PrefabObject> { placement, other }.Add(placement));
        }
    }
}
