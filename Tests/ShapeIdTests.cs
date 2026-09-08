using System;
using BH.SDK.Models.Interfaces.Primitives;
using BH.SDK.Models.Primitives;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // ShapeId is a Guid-backed id struct, deliberately structured like ThemeId/EffectId (see
    // those types' own doc comments) - this test suite exercises the shared shape (Null/IsEnabled/
    // NewGuid/string constructor/equality) so a future change to one is easy to cross-check against
    // the others.

    /// <summary> The id shape ShapeId shares with ThemeId and EffectId - null, enabled, minting, parsing and
    /// equality - so a change to one is easy to cross-check against the others. </summary>
    public class ShapeIdTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Null_IsNotEnabled()
        {
            Assert.IsFalse(ShapeId.Null.IsEnabled());
            Assert.AreEqual(Guid.Empty, ShapeId.Null.value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NewGuid_IsEnabled_AndDistinct()
        {
            var a = ShapeId.NewGuid();
            var b = ShapeId.NewGuid();

            Assert.IsTrue(a.IsEnabled());
            Assert.IsTrue(b.IsEnabled());
            Assert.AreNotEqual(a, b);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void StringConstructor_RoundTripsGuidConstructor()
        {
            var guid = Guid.NewGuid();
            var fromGuid = new ShapeId(guid);
            var fromString = new ShapeId(guid.ToString());

            Assert.AreEqual(fromGuid, fromString);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Equality_OperatorsAndEquatable_AgreeWithValue()
        {
            var guid = Guid.NewGuid();
            var a = new ShapeId(guid);
            var b = new ShapeId(guid);
            var c = ShapeId.NewGuid();

            Assert.IsTrue(a == b);
            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());

            Assert.IsTrue(a != c);
            Assert.IsFalse(a.Equals(c));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void IPrimitiveGuid_ExposesSameValue()
        {
            var shapeId = ShapeId.NewGuid();
            IPrimitiveGuid primitive = shapeId;

            Assert.AreEqual(shapeId.value, primitive.Value);
        }
    }
}
