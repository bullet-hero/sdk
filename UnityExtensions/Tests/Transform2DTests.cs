using BH.SDK.Transforms;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    public static class Transform2DTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void Apply_NoRotation_SimpleChain()
        {
            var root = new Transform2D(new float2(0, 0), 0, 0, new float2(1, 1));
            var child = new Transform2D(new float2(2, 3), 0, 0, new float2(2, 2));
            var grandchild = new Transform2D(new float2(1, -1), 0, 0, new float2(0.5f, 0.5f));

            child.Apply(root);
            // child pos: 0 + (2,3)*1 = (2,3)
            Assert.AreEqual(new float2(2, 3), child.position);
            Assert.AreEqual(new float2(2, 2), child.scale);
            Assert.AreEqual(0f, child.rotation);

            grandchild.Apply(child);
            // grandchild pos: (2,3) + (1,-1)*(2,2) = (2+2, 3-2) = (4,1)
            Assert.AreEqual(new float2(4, 1), grandchild.position);
            Assert.AreEqual(new float2(1, 1), grandchild.scale); // 0.5*2=1
            Assert.AreEqual(0f, grandchild.rotation);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void Apply_WithRotation90_ParentOnly()
        {
            var parent = new Transform2D(new float2(5, 5), 0, math.radians(90), new float2(1, 1));
            var child = new Transform2D(new float2(2, 0), 0, 0, new float2(1, 1));

            child.Apply(parent);
            // child.scale = 1*1 = 1
            // child.rotation = 90°
            // child.position = parent.position + Rotate90( (2,0) * parent.scale ) = (5,5) + Rot90(2,0) = (5,5) + (0,2) = (5,7)
            Assert.AreEqual(new float2(5, 7), child.position);
            Assert.AreEqual(math.radians(90), child.rotation);
            Assert.AreEqual(new float2(1, 1), child.scale);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void Apply_Rotation90_ChildAndParent()
        {
            var parent = new Transform2D(new float2(0, 0), 0, math.radians(90), new float2(2, 2));
            var child = new Transform2D(new float2(1, 0), 0, math.radians(-90), new float2(0.5f, 0.5f));

            child.Apply(parent);
            // scale: 0.5*2 = 1
            // rotation: -90+90 = 0
            // position: parent.pos + Rot90( (1,0) * parent.scale(2,2) ) = (0,0) + Rot90(2,0) = (0,0)+(0,2) = (0,2)
            Assert.IsTrue(Approx.Equal(new float2(0, 2), child.position));
            Assert.AreEqual(0f, child.rotation);
            Assert.AreEqual(new float2(1, 1), child.scale);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void Apply_DeepChain_ArbitraryRotations()
        {
            var t1 = new Transform2D(new float2(1, 2), 0, math.radians(30), new float2(2, 2));
            var t2 = new Transform2D(new float2(3, 4), 0, math.radians(-45), new float2(0.5f, 0.5f));
            var t3 = new Transform2D(new float2(-1, 1), 0, math.radians(90), new float2(3, 3));
            var t4 = new Transform2D(new float2(0, 0), 0, 0, new float2(1, 1));

            t2.Apply(t1);
            t3.Apply(t2);
            t4.Apply(t3);

            // t4: parent t3
            // scale: 1*3 = 3
            // rot: 0+75=75°
            // pos: t3.pos + Rot75( (0,0)*3 ) = t3.pos
            Assert.AreEqual(t3.position, t4.position);
            Assert.AreEqual(math.radians(75), t4.rotation);
            Assert.AreEqual(new float2(3, 3), t4.scale);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void Apply_NegativeScales()
        {
            var parent = new Transform2D(new float2(0, 0), 0, math.radians(180), new float2(-2, 2));
            var child = new Transform2D(new float2(1, 1), 0, 0, new float2(-1, -1));

            child.Apply(parent);
            // scale: -1 * (-2,2) = (2, -2)
            // rot: 0+180=180°
            // pos: parent.pos + Rot180( (1,1) * (-2,2) ) = (0,0) + Rot180(-2,2) = (2, -2)
            Assert.IsTrue(Approx.Equal(new float2(2, -2), child.position));
            Assert.AreEqual(math.radians(180), child.rotation);
            Assert.AreEqual(new float2(2, -2), child.scale);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void Apply_ScalePropagation_ThreeLevels()
        {
            var root = new Transform2D(new float2(10, 10), 0, 0, new float2(0.5f, 2f));
            var mid = new Transform2D(new float2(2, 2), 0, math.radians(90), new float2(3, 0.5f));
            var leaf = new Transform2D(new float2(1, 0), 0, 0, new float2(1, 1));

            mid.Apply(root);
            leaf.Apply(mid);

            // mid: scale = (3*0.5, 0.5*2) = (1.5, 1), rot=90°, pos=root.pos + Rot0( (2,2)*(0.5,2) ) = (10,10) + (1,4) = (11,14) (root rotation=0)
            Assert.AreEqual(new float2(1.5f, 1f), mid.scale);
            Assert.AreEqual(math.radians(90), mid.rotation);
            Assert.AreEqual(new float2(11, 14), mid.position);

            // leaf: scale = 1 * mid.scale = (1.5,1), rot = 0+90=90°
            // pos: mid.pos + Rot90( (1,0)*mid.scale ) = (11,14) + Rot90(1.5,0) = (11,14) + (0,1.5) = (11, 15.5)
            Assert.AreEqual(new float2(1.5f, 1f), leaf.scale);
            Assert.AreEqual(math.radians(90), leaf.rotation);
            Assert.AreEqual(new float2(11, 15.5f), leaf.position);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void Apply_ZeroScaleChild()
        {
            var parent = new Transform2D(new float2(1, 1), 0, math.radians(45), new float2(3, 3));
            var child = new Transform2D(new float2(2, 2), 0, 0, new float2(0, 0));

            child.Apply(parent);
            // scale: 0 * 3 = 0
            // rot: 0+45 = 45°
            Assert.AreEqual(new float2(0, 0), child.scale);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void Apply_RotationAccumulation_FourLevels()
        {
            const float deg30 = math.PI / 6f;
            const float deg45 = math.PI / 4f;
            const float deg90 = math.PI / 2f;

            var t1 = new Transform2D(float2.zero, 0, deg30, new float2(1, 1));
            var t2 = new Transform2D(float2.zero, 0, deg45, new float2(1, 1));
            var t3 = new Transform2D(float2.zero, 0, deg90, new float2(1, 1));
            var t4 = new Transform2D(float2.zero, 0, -deg30, new float2(1, 1));

            t2.Apply(t1);
            t3.Apply(t2);
            t4.Apply(t3);

            // rotations accumulate: t2: 30+45=75°, t3: 75+90=165°, t4: 165-30=135°
            Assert.AreEqual(math.radians(75), t2.rotation);
            Assert.AreEqual(math.radians(165), t3.rotation);
            Assert.AreEqual(math.radians(135), t4.rotation);
            // positions all zero because input positions zero
            Assert.AreEqual(float2.zero, t4.position);
        }
        
        private const float Tolerance = 1e-5f;

        // ---------- GetAlignmentPoint ----------

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_Center_NoRotation_ReturnsPosition()
        {
            var t = new Transform2D(new float2(5, -3), 0, 0, new float2(2, 6));
            float2 p = t.GetAlignmentPoint(new float2(0.5f, 0.5f));
            Assert.AreEqual(t.position, p);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_BottomLeft_NoRotation()
        {
            var t = new Transform2D(new float2(10, 20), 0, 0, new float2(4, 8));
            float2 p = t.GetAlignmentPoint(float2.zero);
            // halfScale = (2,4), lerp(-half, half, 0) = (-2,-4), no rotation, result = position + (-2,-4)
            Assert.AreEqual(new float2(8, 16), p);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_TopRight_NoRotation()
        {
            var t = new Transform2D(new float2(0, 0), 0, 0, new float2(6, 4));
            float2 p = t.GetAlignmentPoint(new float2(1, 1));
            // half = (3,2), lerp(-3,3,1) = (3,2), result = (3,2)
            Assert.AreEqual(new float2(3, 2), p);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_Rotated90_BottomLeftBecomesBottomRight()
        {
            var t = new Transform2D(new float2(0, 0), 0, math.radians(90), new float2(2, 4));
            // halfScale = (1,2)
            // alignment (0,0): lerp(-1,1,0) = (-1,-2) -> rotate 90°: (-1,-2) becomes (2, -1) (sin=1, cos=0: x' = -1*0 - (-2)*1 = 2, y' = -1*1 + (-2)*0 = -1)
            // result = (0,0) + (2,-1) = (2,-1)
            float2 p = t.GetAlignmentPoint(float2.zero);
            Assert.IsTrue(Approx.Equal(new float2(2, -1), p));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_Rotated180_TopRightBecomesBottomLeft()
        {
            var t = new Transform2D(new float2(1, 2), 0, math.radians(180), new float2(4, 6));
            // half = (2,3), alignment (1,1) => lerp(-2,2,1) = (2,3) -> rotate 180°: (-2,-3) -> result = (1,2)+(-2,-3)=(-1,-1)
            float2 p = t.GetAlignmentPoint(new float2(1, 1));
            Assert.IsTrue(Approx.Equal(new float2(-1, -1), p));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_CustomAlignment_ArbitraryRotation()
        {
            // scale = (3,3), rotation = 45°, position = (10, 10)
            var t = new Transform2D(new float2(10, 10), 0, math.radians(45), new float2(3, 3));
            // alignment = (0.25, 0.75)
            // half = (1.5,1.5), lerp = (-1.5+0.25*3, -1.5+0.75*3) = (-0.75, 0.75)
            // rotate by 45°: cos45=sin45≈0.7071068
            float2 localPt = new float2(-0.75f, 0.75f);
            float2 rotPt = math.mul(quaternion.RotateZ(math.radians(45)), new float3(localPt, 0)).xy;
            float2 expected = t.position + rotPt;
            float2 p = t.GetAlignmentPoint(new float2(0.25f, 0.75f));
            Assert.AreEqual(expected.x, p.x, Tolerance);
            Assert.AreEqual(expected.y, p.y, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_NonUniformScale_NegativeScaleX()
        {
            var t = new Transform2D(new float2(-1, 0), 0, 0, new float2(-2, 4));
            // halfScale = (-1,2), alignment (0,0) => lerp(-(-1), (-1)? Wait: lerp(-half, half, 0) = -half = (1, -2) (since -(-1)=1, -2 remains)
            // Actually halfScale = (scale*0.5) = (-1,2). Then -halfScale = (1, -2). halfScale = (-1, 2). lerp(-half, half, 0) = -half = (1, -2). No rotation, result = position + (1,-2) = (0,-2)
            float2 p = t.GetAlignmentPoint(float2.zero);
            Assert.AreEqual(new float2(0, -2), p);
        }

        // ---------- TRS() ----------

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TRS_Identity_ReturnsIdentityMatrix()
        {
            var t = new Transform2D(float2.zero, 0, 0, new float2(1, 1));
            float4x4 m = t.TRS();
            // Expect identity
            Assert.AreEqual(float4x4.identity, m);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TRS_TranslationOnly()
        {
            var t = new Transform2D(new float2(2, 3), 5, 0, new float2(1, 1));
            float4x4 m = t.TRS();
            // Check columns
            Assert.AreEqual(new float4(1, 0, 0, 0), m.c0);
            Assert.AreEqual(new float4(0, 1, 0, 0), m.c1);
            Assert.AreEqual(new float4(0, 0, 1, 0), m.c2);
            Assert.AreEqual(new float4(2, 3, 5, 1), m.c3);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TRS_ScaleOnly()
        {
            var t = new Transform2D(float2.zero, 0, 0, new float2(2, 3));
            float4x4 m = t.TRS();
            Assert.AreEqual(new float4(2, 0, 0, 0), m.c0);
            Assert.AreEqual(new float4(0, 3, 0, 0), m.c1);
            Assert.AreEqual(new float4(0, 0, 1, 0), m.c2);
            Assert.AreEqual(new float4(0, 0, 0, 1), m.c3);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TRS_RotationOnly_90Degrees()
        {
            var t = new Transform2D(float2.zero, 0, math.radians(90), new float2(1, 1));
            float4x4 m = t.TRS();
            // rotation 90°: cos=0, sin=1 => first column (0,1,0,0), second column (-1,0,0,0)
            Assert.IsTrue(Approx.Equal(new float4(0, 1, 0, 0), m.c0));
            Assert.IsTrue(Approx.Equal(new float4(-1, 0, 0, 0), m.c1));
            Assert.IsTrue(Approx.Equal(new float4(0, 0, 1, 0), m.c2));
            Assert.IsTrue(Approx.Equal(new float4(0, 0, 0, 1), m.c3));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TRS_RotationOnly_180Degrees()
        {
            var t = new Transform2D(float2.zero, 0, math.radians(180), new float2(1, 1));
            float4x4 m = t.TRS();
            // cos=-1, sin=0 => c0 = (-1,0,0,0), c1 = (0,-1,0,0)
            Assert.IsTrue(Approx.Equal(new float4(-1, 0, 0, 0), m.c0));
            Assert.IsTrue(Approx.Equal(new float4(0, -1, 0, 0), m.c1));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TRS_FullCombination()
        {
            // position (3,4,2), rotation 45°, scale (2, 0.5)
            var t = new Transform2D(new float2(3, 4), 2, math.radians(45), new float2(2, 0.5f));
            float4x4 m = t.TRS();
            float cos = math.cos(math.radians(45));
            float sin = math.sin(math.radians(45));
            float sx = 2f, sy = 0.5f;
            Assert.AreEqual(new float4(cos * sx, sin * sx, 0, 0), m.c0);
            Assert.AreEqual(new float4(-sin * sy, cos * sy, 0, 0), m.c1);
            Assert.AreEqual(new float4(0, 0, 1, 0), m.c2);
            Assert.AreEqual(new float4(3, 4, 2, 1), m.c3);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TRS_TransformPoint_CenterRemainsPosition()
        {
            // Center (0,0 in local space) should map exactly to position
            var t = new Transform2D(new float2(-7, 11), 3, math.radians(123), new float2(4, 2));
            float4 centerLocal = new float4(0, 0, 0, 1);
            float4 world = math.mul(t.TRS(), centerLocal);
            Assert.AreEqual(t.position.x, world.x, Tolerance);
            Assert.AreEqual(t.position.y, world.y, Tolerance);
            Assert.AreEqual(t.layer, world.z, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TRS_TransformPoint_UnitVectors()
        {
            var t = new Transform2D(float2.zero, 0, 0, new float2(2, 3));
            float4x4 m = t.TRS();
            // X-axis unit vector (1,0) -> (2,0)
            float4 xVec = math.mul(m, new float4(1, 0, 0, 1));
            Assert.AreEqual(new float4(2, 0, 0, 1), xVec);
            // Y-axis unit vector (0,1) -> (0,3)
            float4 yVec = math.mul(m, new float4(0, 1, 0, 1));
            Assert.AreEqual(new float4(0, 3, 0, 1), yVec);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TRS_NegativeScale_ReflectsCorrectly()
        {
            var t = new Transform2D(new float2(1, 2), 0, 0, new float2(-2, 3));
            float4x4 m = t.TRS();
            // (1,0) local -> (-2,0) world
            float4 p = math.mul(m, new float4(1, 0, 0, 1));
            Assert.AreEqual(new float4(-1, 2, 0, 1), p); // because position + scale*point = (1,2)+(-2,0)=(-1,2)
        }
    }
}