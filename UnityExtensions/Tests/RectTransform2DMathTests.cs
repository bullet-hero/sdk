using BH.SDK.Transforms;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    /// <summary> <see cref="RectTransform2DMath.WorldToLocal"/> as the exact inverse of
    /// <see cref="RectTransform2D.Apply"/>'s position. </summary>
    public static class RectTransform2DMathTests
    {
        private const float Tolerance = 1e-4f;

        [TestCase(0f, 1f, 1f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f)]
        [TestCase(30f, 2f, 0.5f, 0f, 1f, 0.2f, 0.8f, 0.25f, 0.75f)]
        [TestCase(-135f, -1.5f, 3f, 1f, 0f, 0f, 1f, 1f, 0f)]
        [TestCase(90f, 0.25f, 4f, 0.3f, 0.6f, 0.5f, 0.5f, 0.1f, 0.9f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public static void WorldToLocal_RoundTripsApply(float parentDegrees, float scaleX, float scaleY,
            float parentPivotX, float parentPivotY, float anchorMinX, float anchorMaxX, float pivotX, float pivotY)
        {
            var parent = new RectTransform2D(new float2(3f, -2f), 5f, math.radians(parentDegrees),
                new float2(4f, 2.5f), new float2(scaleX, scaleY), new float2(0.5f, 0.5f), new float2(0.5f, 0.5f),
                new float2(parentPivotX, parentPivotY));
            var anchorMin = new float2(anchorMinX, 1f - anchorMaxX);
            var anchorMax = new float2(anchorMaxX, 1f - anchorMinX);
            var pivot = new float2(pivotX, pivotY);
            var local = new float2(-1.25f, 0.75f);

            var child = new RectTransform2D(local, 1f, math.radians(15f), new float2(1f, 2f), new float2(1f, 1f),
                anchorMin, anchorMax, pivot);
            child.Apply(parent);

            Assert.IsTrue(RectTransform2DMath.WorldToLocal(parent, anchorMin, anchorMax, pivot, child.position,
                out var back));
            Assert.AreEqual(local.x, back.x, Tolerance);
            Assert.AreEqual(local.y, back.y, Tolerance);
        }

        // The parent handed in is GLOBAL: a composed grandparent chain must invert through it alone.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public static void WorldToLocal_UnderAComposedParent_RoundTripsApply()
        {
            var root = new RectTransform2D(new float2(-4f, 1f), 0f, math.radians(40f), new float2(6f, 3f),
                new float2(1.5f, 0.75f), new float2(0.5f, 0.5f), new float2(0.5f, 0.5f), new float2(0.2f, 0.7f));
            var parent = new RectTransform2D(new float2(1f, 2f), 0f, math.radians(-70f), new float2(2f, 2f),
                new float2(2f, -1f), new float2(0f, 0.25f), new float2(1f, 0.75f), new float2(0.5f, 0f));
            parent.Apply(root);

            var anchorMin = new float2(0.1f, 0.3f);
            var anchorMax = new float2(0.6f, 0.9f);
            var pivot = new float2(0.8f, 0.2f);
            var local = new float2(0.5f, -2f);
            var child = new RectTransform2D(local, 0f, 0f, new float2(1f, 1f), new float2(1f, 1f),
                anchorMin, anchorMax, pivot);
            child.Apply(parent);

            Assert.IsTrue(RectTransform2DMath.WorldToLocal(parent, anchorMin, anchorMax, pivot, child.position,
                out var back));
            Assert.AreEqual(local.x, back.x, Tolerance);
            Assert.AreEqual(local.y, back.y, Tolerance);
        }

        [TestCase(0f, 1f)]
        [TestCase(2f, 0f)]
        [TestCase(0f, 0f)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public static void WorldToLocal_ZeroParentScale_Fails(float scaleX, float scaleY)
        {
            var parent = new RectTransform2D(new float2(1f, 1f), 0f, math.radians(20f), new float2(2f, 2f),
                new float2(scaleX, scaleY));

            Assert.IsFalse(RectTransform2DMath.WorldToLocal(parent, new float2(0.5f), new float2(0.5f),
                new float2(0.5f), new float2(3f, 4f), out var local));
            Assert.AreEqual(float2.zero, local);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public static void WorldToLocal_AgainstAnIdentityParent_IsIdentity()
        {
            var world = new float2(7.5f, -3.25f);

            Assert.IsTrue(RectTransform2DMath.WorldToLocal(RectTransform2D.Zero, new float2(0f, 0.2f),
                new float2(1f, 0.9f), new float2(0.1f, 0.6f), world, out var local));
            Assert.AreEqual(world.x, local.x, Tolerance);
            Assert.AreEqual(world.y, local.y, Tolerance);
        }
    }
}
