using BH.SDK.Transforms;
using NUnit.Framework;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    public static class RectTransform2DTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TestHierarchy1()
        {
            var tr1 = new RectTransform2D(new float2(2, -4), 10, 0, new float2(2, 1), new float2(1, 1),
                new float2(-999, -999), new float2(-999, -999), new float2(0, 0.5f));
            var tr2 = new RectTransform2D(new float2(-0.5f, 1), 20, 0, new float2(1, 1), new float2(2, 1),
                new float2(0, 0), new float2(0.5f, 1), new float2(0f, 0.5f));
            var tr3 = new RectTransform2D(new float2(1.5f, 1), 30, 0, new float2(1, 1), new float2(1, 0.5f),
                new float2(0, 0.5f), new float2(0.5f, 1), new float2(1f, 0.5f));

            tr2.Apply(tr1);
            Assert.AreEqual(new float2(1.5f, -3f), tr2.position);
            Assert.AreEqual(new float2(2f, 2f), tr2.size);
            Assert.AreEqual(new float2(2f, 1f), tr2.scale);
            Assert.AreEqual(30, tr2.layer);

            tr3.Apply(tr2);
            Assert.AreEqual(new float2(6.5f, -1.5f), tr3.position);
            Assert.AreEqual(new float2(2f, 2f), tr3.size);
            Assert.AreEqual(new float2(2f, 0.5f), tr3.scale);
            Assert.AreEqual(60, tr3.layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TestHierarchy2()
        {
            var tr1 = new RectTransform2D(new float2(4, -4), 10, 0, new float2(1, 1), new float2(1, 1),
                new float2(-999, -999), new float2(-999, -999), new float2(0, 0.5f));
            var tr2 = new RectTransform2D(new float2(1, 1), 20, 0, new float2(0.5f, 0.5f), new float2(2, 1),
                new float2(0, 0.5f), new float2(0.5f, 1), new float2(0.5f, 0.5f));
            var tr3 = new RectTransform2D(new float2(0.5f, 1), 30, 0, new float2(1, 1), new float2(1, 0.5f),
                new float2(0, 0f), new float2(1, 1), new float2(0, 0));

            tr2.Apply(tr1);
            Assert.AreEqual(new float2(5.25f, -2.75f), tr2.position);
            Assert.AreEqual(new float2(1f, 1f), tr2.size);
            Assert.AreEqual(new float2(2f, 1f), tr2.scale);
            Assert.AreEqual(30, tr2.layer);

            tr3.Apply(tr2);
            Assert.AreEqual(new float2(5.25f, -2.25f), tr3.position);
            Assert.AreEqual(new float2(2f, 2f), tr3.size);
            Assert.AreEqual(new float2(2f, 0.5f), tr3.scale);
            Assert.AreEqual(60, tr3.layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TestHierarchy3()
        {
            var tr1 = new RectTransform2D(new float2(1, 1), 10, 0, new float2(1, 1), new float2(1, 1),
                new float2(-999, -999), new float2(-999, -999), new float2(0.5f, 0.5f));
            var tr2 = new RectTransform2D(new float2(1, 1), 20, 0, new float2(1, 1), new float2(1, 1),
                new float2(0.5f, 0.5f), new float2(0.5f, 0.5f), new float2(0.5f, 0.5f));
            var tr3 = new RectTransform2D(new float2(1, 1), 30, 0, new float2(1, 1), new float2(1, 1),
                new float2(0f, 0.5f), new float2(1f, 0.5f), new float2(0.5f, 0.5f));

            tr2.Apply(tr1);
            tr3.Apply(tr2);
            Assert.AreEqual(new float2(3f, 3f), tr3.position);
            Assert.AreEqual(new float2(2f, 1f), tr3.size);
            Assert.AreEqual(new float2(1f, 1f), tr3.scale);
            Assert.AreEqual(60, tr3.layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TestHierarchy4()
        {
            var tr1 = new RectTransform2D(new float2(0, 0), 10, 0, new float2(1, 1), new float2(1, 1),
                new float2(-999, -999), new float2(-999, -999), new float2(0.5f, 0.5f));
            var tr2 = new RectTransform2D(new float2(1.5f, 1.5f), 20, 0, new float2(1, 1), new float2(1, 1),
                new float2(0f, 0f), new float2(1f, 1f), new float2(0.5f, 0.5f));
            var tr3 = new RectTransform2D(new float2(2.5f, 2.5f), 30, 0, new float2(1, 1), new float2(1, 1),
                new float2(0f, 0f), new float2(1f, 1f), new float2(0.5f, 0.5f));

            tr2.Apply(tr1);
            tr3.Apply(tr2);
            Assert.AreEqual(new float2(4f, 4f), tr3.position);
            Assert.AreEqual(new float2(3f, 3f), tr3.size);
            Assert.AreEqual(new float2(1f, 1f), tr3.scale);
            Assert.AreEqual(60, tr3.layer);
        }
        
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TestHierarchy5()
        {
            var tr1 = new RectTransform2D(new float2(0, 0), 10, 0, new float2(1, 1), new float2(1, 1),
                new float2(-999, -999), new float2(-999, -999), new float2(0, 0));
            var tr2 = new RectTransform2D(new float2(0, 0), 20, 0, new float2(0, 0), new float2(0.5f, 0.5f),
                new float2(0.25f, 0.25f), new float2(0.75f, 0.75f), new float2(0.5f, 0.5f));

            tr2.Apply(tr1);
            Assert.AreEqual(new float2(0.5f, 0.5f), tr2.position);
            Assert.AreEqual(new float2(0.5f, 0.5f), tr2.size);
            Assert.AreEqual(new float2(0.5f, 0.5f), tr2.scale);
            Assert.AreEqual(30, tr2.layer);
        }
        
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TestHierarchy6_Rotation90_Parent()
        {
            var tr1 = new RectTransform2D(new float2(0, 0), 10, math.radians(90), new float2(2, 2), new float2(1, 1),
                new float2(-999, -999), new float2(-999, -999), new float2(0.5f, 0.5f));
            var tr2 = new RectTransform2D(new float2(1, 1), 20, 0, new float2(1, 1), new float2(1, 1),
                new float2(0, 0), new float2(0, 0), new float2(0.5f, 0.5f));

            tr2.Apply(tr1);

            Assert.AreEqual(new float2(0, 0), tr2.position);
            Assert.AreEqual(new float2(1, 1), tr2.size);
            Assert.AreEqual(new float2(1, 1), tr2.scale);
            Assert.AreEqual(math.radians(90), tr2.rotation);
            Assert.AreEqual(30, tr2.layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TestHierarchy7_GrandchildWithRotations()
        {
            var tr1 = new RectTransform2D(new float2(0, 0), 10, math.radians(90), new float2(2, 2), new float2(1, 1),
                new float2(-999, -999), new float2(-999, -999), new float2(0.5f, 0.5f));
            var tr2 = new RectTransform2D(new float2(2, 0), 20, 0, new float2(1, 1), new float2(1, 1),
                new float2(0, 0), new float2(1, 1), new float2(0.5f, 0.5f));
            var tr3 = new RectTransform2D(new float2(0, 1), 30, math.radians(-90), new float2(1, 1), new float2(2, 2),
                new float2(0, 0), new float2(0, 0), new float2(0.5f, 0.5f));

            tr2.Apply(tr1);
            tr3.Apply(tr2);

            Assert.IsTrue(Approx.Equal(new float2(0, 2), tr2.position));
            Assert.AreEqual(new float2(3, 3), tr2.size); // 1 + 2*(1) = 3
            Assert.AreEqual(new float2(1, 1), tr2.scale);
            Assert.AreEqual(math.radians(90), tr2.rotation);
            Assert.AreEqual(30, tr2.layer);

            // tr3 by tr2: parent has rotation=90°, fullSize=(3,3), half=(1.5,1.5)
            // pivotParent=0.5 -> parentCenterPoint = tr2.position - 0 = (0,2)
            // anchorNorm = (0,0) (since anchorMin=anchorMax=0)
            // anchorLocal = lerp(-1.5,1.5, 0) = (-1.5, -1.5) -> rotate 90°: (1.5, -1.5)
            // selfOffset = (0*parent.scale(1,1)=(0,1)) -> rotate 90°: (-1,0)
            // child.position = (-1,0)+(1.5,-1.5)+(0,2) = (0.5, 0.5)
            // rotation child = -90 + 90 = 0
            // scale child = (2,2)
            // size child = 1 + 3*(0) = 1
            Assert.IsTrue(Approx.Equal(new float2(0.5f, 0.5f), tr3.position));
            Assert.AreEqual(new float2(1, 1), tr3.size);
            Assert.AreEqual(new float2(2, 2), tr3.scale);
            Assert.AreEqual(0f, tr3.rotation);
            Assert.AreEqual(60, tr3.layer);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public static void TestHierarchy8_DeepChainWithMixedAnchors()
        {
            var root = new RectTransform2D(float2.zero, 10, 0, new float2(10, 10), new float2(1f, 1f),
                new float2(-999, -999), new float2(-999, -999), new float2(0.5f, 0.5f));
            var child1 = new RectTransform2D(new float2(0, 0), 20, math.radians(90), new float2(1, 1), new float2(1, 1),
                new float2(0, 0), new float2(1, 1), new float2(0.5f, 0.5f));
            var child2 = new RectTransform2D(new float2(0, 0), 30, 0, new float2(1, 1), new float2(2, 2),
                new float2(0.25f, 0.25f), new float2(0.75f, 0.75f), new float2(0.5f, 0.5f));
            var child3 = new RectTransform2D(new float2(5, 5), 40, math.radians(-90), new float2(2, 2), new float2(1, 1),
                new float2(0, 0), new float2(0, 0), new float2(0, 0));

            child1.Apply(root);
            child2.Apply(child1);
            child3.Apply(child2);

            // Check child1: size = 1 + 10*(1) = 11, position = (0,0)
            Assert.AreEqual(new float2(11, 11), child1.size);
            Assert.AreEqual(new float2(0, 0), child1.position);
            Assert.AreEqual(math.radians(90), child1.rotation);
            Assert.AreEqual(30, child1.layer); // 20 + 10

            // child2: parent=child1 (rotation=90°, fullSize=(11,11), half=5.5)
            // parentCenterPoint = (0,0) (pivot 0.5)
            // anchorNorm = (0.5,0.5) => anchorLocal = (0,0) -> rotate doesn't change
            // selfOffset = (0,0) * parent.scale (1,1) = (0,0)
            // child2.position = (0,0)
            // size = 1 + 11*(0.5,0.5) = (6.5,6.5)
            // scale = (2,2)*1 = (2,2)
            // rotation = 0+90=90
            Assert.AreEqual(new float2(6.5f, 6.5f), child2.size);
            Assert.AreEqual(new float2(0, 0), child2.position);
            Assert.AreEqual(new float2(2, 2), child2.scale);
            Assert.AreEqual(math.radians(90), child2.rotation);
            Assert.AreEqual(60, child2.layer); // 30 + 30

            // child3: parent=child2 (rotation=90°, fullSize=(13,13), half=6.5)
            // pivotParent=0.5 => parentCenterPoint = (0,0)
            // anchorNorm = (0,0) => anchorLocal = (-6.5,-6.5) -> rotate 90°: (6.5, -6.5)
            // selfOffset = (5,5)*parent.scale(2,2) = (10,10) -> rotate 90°: (-10,10)
            // child3.position = (-10,10)+(6.5,-6.5)+(0,0) = (-3.5, 3.5)
            // rotation = -90+90=0
            // scale = (1,1)*(2,2) = (2,2)
            // size = 2 + 13*(0,0) = 2
            Assert.IsTrue(Approx.Equal(new float2(-3.5f, 3.5f), child3.position));
            Assert.AreEqual(new float2(2, 2), child3.size);
            Assert.AreEqual(new float2(2, 2), child3.scale);
            Assert.AreEqual(0f, child3.rotation);
            Assert.AreEqual(100, child3.layer); // 40 + 60
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void TestHierarchy9_NegativeScaleAndRotation180()
        {
            var root = new RectTransform2D(new float2(5, 5), 10, math.radians(180), new float2(3, 3), new float2(1, 1),
                new float2(-999, -999), new float2(-999, -999), new float2(0.5f, 0.5f));
            var child = new RectTransform2D(new float2(1, 2), 20, 0, new float2(2, 2), new float2(-1, 1),
                new float2(0, 0), new float2(0.5f, 0.5f), new float2(0.5f, 0.5f));

            child.Apply(root);

            // parent.fullSize = (3,3), half=1.5, sin180=0, cos180=-1
            // parentCenterPoint = root.position - lerp(-1.5,1.5,0.5) = (5,5) - (0,0) = (5,5)
            // anchorNorm = lerp(0,0.5, 0.5) = (0.25,0.25)
            // anchorLocal = lerp(-1.5,1.5, 0.25) = (-0.75, -0.75) -> rotate 180°: (0.75, 0.75) (since (-0.75)*(-1) - (-0.75)*0 = 0.75; x*0+y*(-1)=0.75)
            // selfOffset = (1,2) * parent.scale (1,1) = (1,2) -> rotate 180°: (-1,-2)
            // position = (-1,-2)+(0.75,0.75)+(5,5) = (4.75, 3.75)
            // rotation = 0+180=180
            // scale = (-1,1)*(1,1)=(-1,1)
            // size = 2 + 3*(0.5,0.5) = (3.5,3.5)
            Assert.AreEqual(new float2(4.75f, 3.75f), child.position);
            Assert.AreEqual(new float2(3.5f, 3.5f), child.size);
            Assert.AreEqual(new float2(-1, 1), child.scale);
            Assert.AreEqual(math.radians(180), child.rotation);
            Assert.AreEqual(30, child.layer); // 20 + 10
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public static void TestHierarchy10_FourLevelsWithZeroSizeChild()
        {
            var l1 = new RectTransform2D(new float2(0, 0), 10, 0, new float2(4, 4), new float2(1, 1),
                new float2(-999, -999), new float2(-999, -999), new float2(0.5f, 0.5f));
            var l2 = new RectTransform2D(new float2(0, 0), 20, math.radians(90), new float2(0, 0), new float2(1, 1),
                new float2(0, 0), new float2(1, 1), new float2(0.5f, 0.5f));
            var l3 = new RectTransform2D(new float2(0, 0), 30, math.radians(-90), new float2(0, 0), new float2(1, 1),
                new float2(0, 0), new float2(1, 1), new float2(0.5f, 0.5f));
            var l4 = new RectTransform2D(new float2(0, 0), 40, 0, new float2(2, 2), new float2(1, 1),
                new float2(0, 0), new float2(0, 0), new float2(0.5f, 0.5f));

            l2.Apply(l1);
            l3.Apply(l2);
            l4.Apply(l3);

            // l2: size = 0 + 4*(1) = 4, rotation=90, position=(0,0)
            Assert.AreEqual(new float2(4, 4), l2.size);
            Assert.AreEqual(new float2(0, 0), l2.position);
            Assert.AreEqual(30, l2.layer); // 20 + 10
            // l3: parent=l2 (fullSize=4, half=2, rotation=90°)
            // parentCenterPoint = (0,0)
            // anchorNorm=(0.5,0.5) -> anchorLocal=(0,0)
            // selfOffset=(0,0) -> position=(0,0)
            // size = 0 + 4*1 = 4, rotation = -90+90=0
            Assert.AreEqual(new float2(4, 4), l3.size);
            Assert.AreEqual(0f, l3.rotation);
            Assert.AreEqual(60, l3.layer); // 30 + 30
            // l4: parent=l3 (fullSize=4, rotation=0)
            // anchorNorm = 0, anchorLocal = lerp(-2,2,0)=(-2,-2), with rotation
            // selfOffset = (0,0)
            // position = (0,0)+(-2,-2)+(0,0)=(-2,-2)
            // size = 2 + 4*0 = 2
            Assert.AreEqual(new float2(-2, -2), l4.position);
            Assert.AreEqual(new float2(2, 2), l4.size);
            Assert.AreEqual(100, l4.layer); // 40 + 60
        }
        
        private const float Tolerance = 0.0001f;

        // ---------- GetCenterPoint ----------
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetCenterPoint_NoRotation_ReturnsPosition()
        {
            var rt = new RectTransform2D(new float2(5, 5), 0, 0, new float2(2, 2), new float2(1, 1),
                anchorMin: float2.zero, anchorMax: float2.zero, pivot: new float2(0.5f, 0.5f));
            float2 center = rt.GetCenterPoint();
            Assert.AreEqual(new float2(5, 5), center);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetCenterPoint_WithPivot_Shifted()
        {
            var rt = new RectTransform2D(new float2(10, 10), 0, 0, new float2(4, 4), new float2(1, 1),
                anchorMin: float2.zero, anchorMax: float2.zero, pivot: new float2(0, 0));
            // full size = size*scale = 4. halfFullSize=2.
            // pivotPoint = lerp(-2,2, (0,0)) = (-2,-2) (without rotation)
            // center = position - pivotPoint = (10,10) - (-2,-2) = (12,12)
            float2 center = rt.GetCenterPoint();
            Assert.AreEqual(new float2(12, 12), center);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetCenterPoint_Rotated90_PivotBottomLeft()
        {
            var rt = new RectTransform2D(new float2(0, 0), 0, math.radians(90), new float2(2, 2), new float2(1, 1),
                anchorMin: float2.zero, anchorMax: float2.zero, pivot: new float2(0, 0));
            // halfFullSize = (2,2)*1*0.5 = (1,1)
            // pivotPoint local: lerp(-1,1, (0,0)) = (-1,-1)
            // rotate 90°: (-1,-1).RotateVectorFast(90°) -> (1, -1) (sin=1,cos=0: x' = -1*0 - (-1)*1 = 1; y' = -1*1 + (-1)*0 = -1)
            // position = (0,0) - (1,-1) = (-1, 1)
            float2 center = rt.GetCenterPoint();
            Assert.IsTrue(Approx.Equal(new float2(-1, 1), center));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetCenterPoint_Rotated180_WithScaleAndSize()
        {
            var rt = new RectTransform2D(new float2(3, 4), 0, math.radians(180), new float2(3, 3), new float2(2, 2),
                anchorMin: float2.zero, anchorMax: float2.zero, pivot: new float2(0.5f, 0.5f));
            // fullSize = (6,6), half = (3,3)
            // pivotPoint = lerp(-3,3, 0.5) = (0,0)
            // center = position - (0,0) = (3,4)
            float2 center = rt.GetCenterPoint();
            Assert.AreEqual(new float2(3, 4), center);
        }

        // ---------- GetAlignmentPoint ----------
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_Center_ReturnsCenter()
        {
            var rt = new RectTransform2D(new float2(2, 2), 0, 0, new float2(4, 4), new float2(1, 1),
                anchorMin: float2.zero, anchorMax: float2.zero, pivot: new float2(0.5f, 0.5f));
            float2 centerAlign = rt.GetAlignmentPoint(new float2(0.5f, 0.5f));
            Assert.AreEqual(new float2(2, 2), centerAlign);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_BottomLeft_NoRotation()
        {
            var rt = new RectTransform2D(new float2(5, 5), 0, 0, new float2(4, 4), new float2(1, 1));
            // halfFullSize = (2,2)
            // pivotPoint = (0,0) -> center = (5,5)
            // alignment = (0,0) -> lerp(-2,2, 0) = (-2,-2)
            // rotate (0) -> (-2,-2)
            // result = position - pivotPoint + alignmentPoint = 5 - 0 + (-2) = 3 on x, identical y=3
            float2 p = rt.GetAlignmentPoint(float2.zero);
            Assert.AreEqual(new float2(3, 3), p);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_TopRight_Rotated90()
        {
            var rt = new RectTransform2D(new float2(0, 0), 0, math.radians(90), new float2(2, 2), new float2(1, 1));
            // halfFullSize = (1,1)
            // pivotPoint = (0,0) -> center = (0,0)
            // alignment = (1,1) -> lerp(-1,1,1) = (1,1)
            // rotate 90° -> (1,1) -> (-1,1) (since x'=1*0 - 1*1 = -1, y'=1*1 + 1*0 = 1)
            // result = (0,0) - (0,0) + (-1,1) = (-1,1)
            float2 p = rt.GetAlignmentPoint(new float2(1, 1));
            Assert.IsTrue(Approx.Equal(new float2(-1, 1), p));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetAlignmentPoint_CustomPivot_LeftCenter()
        {
            var rt = new RectTransform2D(new float2(10, 10), 0, 0, new float2(6, 4), new float2(2, 2));
            rt.pivot = new float2(0f, 0.5f);
            
            // fullSize = (12,8), half = (6,4)
            // pivotPoint = lerp(-6,6, 0) on x = -6; lerp(-4,4, 0.5) on y = 0 -> (-6,0)
            // center = (10,10) - (-6,0) = (16,10)
            // alignment = (0,0.5) -> lerp(-6,6,0) = -6 on x; lerp(-4,4,0.5)=0 -> (-6,0)
            // rotate 0 -> (-6,0)
            // result = (16,10) + (-6,0) = (10,10)
            float2 p = rt.GetAlignmentPoint(new float2(0, 0.5f));
            Assert.AreEqual(new float2(10, 10), p);
        }

        // GetBoundsSize is what frames a camera on an object, so the assertion that matters is that it
        // never comes back smaller than the rect it describes - an under-report crops the object.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public static void GetBoundsSize_Unrotated_IsFullSize()
        {
            var rt = new RectTransform2D(float2.zero, 0, 0, new float2(6, 4), new float2(2, 3));

            Assert.IsTrue(Approx.Equal(new float2(12, 12), rt.GetBoundsSize()));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public static void GetBoundsSize_QuarterTurn_SwapsTheAxes()
        {
            var rt = new RectTransform2D(float2.zero, 0, math.radians(90), new float2(6, 4), new float2(1, 1));

            Assert.IsTrue(Approx.Equal(new float2(4, 6), rt.GetBoundsSize()));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public static void GetBoundsSize_Diagonal_IsTheCircumscribedBox()
        {
            var rt = new RectTransform2D(float2.zero, 0, math.radians(45), new float2(1, 1), new float2(1, 1));
            var expected = math.sqrt(2f);

            var bounds = rt.GetBoundsSize();
            Assert.AreEqual(expected, bounds.x, 0.0001f);
            Assert.AreEqual(expected, bounds.y, 0.0001f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public static void GetBoundsSize_MirroredScale_CoversAsMuchAsAnUnmirroredOne()
        {
            var mirrored = new RectTransform2D(float2.zero, 0, math.radians(30), new float2(6, 4), new float2(-2, 1));
            var plain = new RectTransform2D(float2.zero, 0, math.radians(30), new float2(6, 4), new float2(2, 1));

            Assert.IsTrue(Approx.Equal(plain.GetBoundsSize(), mirrored.GetBoundsSize()));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public static void GetBoundsSize_NeverSmallerThanTheRotatedCorners()
        {
            for (var degrees = 0; degrees < 360; degrees += 7)
            {
                var rt = new RectTransform2D(float2.zero, 0, math.radians(degrees), new float2(6, 4), new float2(1.5f, 0.5f));
                var half = rt.FullSize * 0.5f;
                math.sincos(rt.rotation, out var sin, out var cos);

                var reach = float2.zero;
                for (var i = 0; i < 4; i++)
                {
                    var corner = new float2(i is 0 or 1 ? -half.x : half.x, i is 0 or 3 ? -half.y : half.y);
                    reach = math.max(reach, math.abs(Math2D.RotateVectorFast(corner, sin, cos)));
                }

                var bounds = rt.GetBoundsSize();
                Assert.GreaterOrEqual(bounds.x, reach.x * 2f - 0.0001f, $"x at {degrees} deg");
                Assert.GreaterOrEqual(bounds.y, reach.y * 2f - 0.0001f, $"y at {degrees} deg");
            }
        }
    }
}