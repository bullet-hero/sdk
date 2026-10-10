using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Values;
using BH.SDK.Models.Primitives;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary>
    /// ModelUtils' collection hash/equality helpers. The dictionary hash is the one worth pinning: key
    /// types are non-comparable id structs, so anything that sorts keys throws instead of hashing.
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

            var key = new ModificationKey(new ObjectId(5), ModificationFields.Layer);
            placement.Modifications.Add(key, new Modification(key, 3L));

            var other = new PrefabObject { ObjectId = new ObjectId(2) };

            Assert.DoesNotThrow(() => placement.GetHashCode());
            Assert.DoesNotThrow(() => new HashSet<PrefabObject> { placement, other }.Add(placement));
        }

        // CopyArray must Copy() reference elements too (ThemeData.Matrix shared all 64 colours once).
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CopyArray_OfAReferenceType_CopiesTheElementsToo()
        {
            var array = new[] { new Color4Value(1f, 0f, 0f, 1f), new Color4Value(0f, 1f, 0f, 1f) };

            var copy = array.CopyArray();

            Assert.AreNotSame(array[0], copy[0], "element 0 is the same instance");
            Assert.AreNotSame(array[1], copy[1], "element 1 is the same instance");
            Assert.IsTrue(array[0].Equals(copy[0]), "element 0 lost its value");
            Assert.IsTrue(array[1].Equals(copy[1]), "element 1 lost its value");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CopyingATheme_LeavesTheOriginalAlone()
        {
            var theme = new ThemeData();
            theme.Matrix[0] = new Color4Value(1f, 0f, 0f, 1f);

            var copy = theme.Copy();
            copy.Matrix[0].R = 0f;

            Assert.AreEqual(1f, theme.Matrix[0].R, "editing the copy edited the original");
        }
    }
}
