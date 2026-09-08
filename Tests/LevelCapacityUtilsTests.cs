using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> How many objects are alive at once: the peak over a set of spans, and the frame it happens on. </summary>
    [TestFixture]
    public class LevelCapacityUtilsTests
    {
        #region GetPeak

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GetPeak_NoIntervals_ReturnsZero()
        {
            var peak = LevelCapacityUtils.GetPeak(new List<FrameSpan>());
            Assert.AreEqual(0, peak);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GetPeak_DisjointIntervals_ReturnsOne()
        {
            // [0,11) then [11,21) - never alive at the same frame
            var peak = LevelCapacityUtils.GetPeak(new[]
            {
                FrameSpan.FromBounds(0, 11), FrameSpan.FromBounds(11, 21),
            });
            Assert.AreEqual(1, peak);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GetPeak_AdjacentSpans_ShareNoFrame_ReturnsOne()
        {
            // The regression FrameSpan exists for: [0,10) and [10,20) meet without overlapping, so
            // two objects authored back to back never need two slots at once.
            var peak = LevelCapacityUtils.GetPeak(new[]
            {
                FrameSpan.FromBounds(0, 10), FrameSpan.FromBounds(10, 20),
            });
            Assert.AreEqual(1, peak);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GetPeak_NestedIntervals_ReturnsDepth()
        {
            var peak = LevelCapacityUtils.GetPeak(new[]
            {
                FrameSpan.FromBounds(0, 100), FrameSpan.FromBounds(1, 50), FrameSpan.FromBounds(2, 10),
            });
            Assert.AreEqual(3, peak);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GetPeak_SingleFrameIntervals_OnSameFrame_ReturnsCount()
        {
            var peak = LevelCapacityUtils.GetPeak(new[]
            {
                new FrameSpan(5, 1), new FrameSpan(5, 1), new FrameSpan(5, 1),
            });
            Assert.AreEqual(3, peak);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GetPeak_PeakIsNotTotalCount()
        {
            // three objects, but never more than two at once: [0,10) [5,15) [20,30)
            var peak = LevelCapacityUtils.GetPeak(new[]
            {
                FrameSpan.FromBounds(0, 10), FrameSpan.FromBounds(5, 15), FrameSpan.FromBounds(20, 30),
            });
            Assert.AreEqual(2, peak);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GetPeak_SingleFrameSpans_AreCountedWhereTheyLand()
        {
            // A degenerate interval is no longer representable - FrameSpan clamps Duration to at
            // least one - so the case this used to guard against cannot reach the sweep at all.
            // What is worth pinning instead is that a one-frame object still occupies its frame.
            var peak = LevelCapacityUtils.GetPeak(new[]
            {
                new FrameSpan(0, 10), new FrameSpan(9, 1),
            });
            Assert.AreEqual(2, peak);
        }

        #endregion

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetPeakUsage_EmptyLevel_ReturnsZeroes()
        {
            var hint = LevelCapacityUtils.GetPeakUsage(new Level());

            Assert.AreEqual(0, hint.Instances);
            Assert.AreEqual(0, hint.ShapesTransparent);
            Assert.AreEqual(0, hint.Effects);
            Assert.AreEqual(0, hint.Texts);
            Assert.AreEqual(0, hint.Tracks);
            Assert.IsFalse(hint.HasValue);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetPeakUsage_NullLevel_ReturnsZeroes()
        {
            var hint = LevelCapacityUtils.GetPeakUsage(null);
            Assert.IsFalse(hint.HasValue);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetPeakUsage_CountsEveryTypeAsInstance_AndItsOwnFamilySeparately()
        {
            var level = new Level();
            AddObject(level, new RectObject(), 1, 0, 100);
            AddObject(level, new ShapeObject(), 2, 0, 100);
            AddObject(level, new ShapeObject(), 3, 0, 100);
            AddObject(level, new EffectObject(), 4, 0, 100);
            AddObject(level, new TextObject(), 5, 0, 100);
            AddObject(level, new PrefabObject(), 6, 0, 100);

            var hint = LevelCapacityUtils.GetPeakUsage(level);

            Assert.AreEqual(6, hint.Instances);
            Assert.AreEqual(2, hint.ShapesTransparent);
            Assert.AreEqual(1, hint.Effects);
            Assert.AreEqual(1, hint.Texts);
            Assert.IsTrue(hint.HasValue);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetPeakUsage_FamiliesPeakIndependently()
        {
            var level = new Level();
            // textures live early, texts live late - each family peaks at 2, but only 2 instances
            // are ever alive at the same time despite there being 4 objects in total
            AddObject(level, new ShapeObject(), 1, 0, 10);
            AddObject(level, new ShapeObject(), 2, 0, 10);
            AddObject(level, new TextObject(), 3, 50, 60);
            AddObject(level, new TextObject(), 4, 50, 60);

            var hint = LevelCapacityUtils.GetPeakUsage(level);

            Assert.AreEqual(2, hint.Instances);
            Assert.AreEqual(2, hint.ShapesTransparent);
            Assert.AreEqual(2, hint.Texts);
            Assert.AreEqual(0, hint.Effects);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetPeakUsage_CountsAudioTracks()
        {
            var level = new Level();
            AddTrack(level, 1, 0, 100);
            AddTrack(level, 2, 50, 150);
            AddTrack(level, 3, 200, 300);

            var hint = LevelCapacityUtils.GetPeakUsage(level);

            Assert.AreEqual(2, hint.Tracks);
            Assert.AreEqual(0, hint.Instances);
        }

        #region ShaderType split

        // The two shape pools cannot borrow from each other at runtime - an entity's archetype is
        // fixed when its prototype is created - so a hint that lumps them together under-sizes one
        // and over-sizes the other. These pin that the split follows the authored ShaderType.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetPeakUsage_ExplicitShaderTypes_LandInTheirOwnPools()
        {
            var level = new Level();
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Opaque }, 1, 0, 100);
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Opaque }, 2, 0, 100);
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Transparent }, 3, 0, 100);

            var hint = LevelCapacityUtils.GetPeakUsage(level);

            Assert.AreEqual(2, hint.ShapesOpaque);
            Assert.AreEqual(1, hint.ShapesTransparent);
            Assert.AreEqual(3, hint.Instances);
        }

        // Auto is not decidable without the texture's own opacity, which this assembly cannot see.
        // With no resolver it therefore counts as transparent - the pool that always works.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetPeakUsage_AutoWithNoResolver_CountsAsTransparent()
        {
            var level = new Level();
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Auto }, 1, 0, 100);

            var hint = LevelCapacityUtils.GetPeakUsage(level);

            Assert.AreEqual(0, hint.ShapesOpaque);
            Assert.AreEqual(1, hint.ShapesTransparent);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetPeakUsage_AutoWithResolver_FollowsTheResolver()
        {
            var level = new Level();
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Auto }, 1, 0, 100);
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Auto }, 2, 0, 100);

            var hint = LevelCapacityUtils.GetPeakUsage(level,
                shape => shape.ObjectId.value == 1);

            Assert.AreEqual(1, hint.ShapesOpaque);
            Assert.AreEqual(1, hint.ShapesTransparent);
        }

        // A resolver never overrides an authored choice - it only ever answers for Auto.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetPeakUsage_ResolverDoesNotOverrideExplicitTransparent()
        {
            var level = new Level();
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Transparent }, 1, 0, 100);

            var hint = LevelCapacityUtils.GetPeakUsage(level, _ => true);

            Assert.AreEqual(0, hint.ShapesOpaque);
            Assert.AreEqual(1, hint.ShapesTransparent);
        }

        // Each pool peaks on its own timeline, same as the families above.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetPeakUsage_PoolsPeakIndependently()
        {
            var level = new Level();
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Opaque }, 1, 0, 10);
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Opaque }, 2, 0, 10);
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Transparent }, 3, 50, 60);
            AddObject(level, new ShapeObject { ShaderType = ShaderType.Transparent }, 4, 50, 60);

            var hint = LevelCapacityUtils.GetPeakUsage(level);

            Assert.AreEqual(2, hint.ShapesOpaque);
            Assert.AreEqual(2, hint.ShapesTransparent);
            Assert.AreEqual(2, hint.Instances);
        }

        #endregion

        private static void AddObject(Level level, RectObject levelObject, int id, int startFrame, int endFrame)
        {
            levelObject.ObjectId = new ObjectId(id);
            levelObject.Span = FrameSpan.FromBounds(startFrame, endFrame);
            level.Game.Objects.Add(levelObject.ObjectId, levelObject);
        }
        private static void AddTrack(Level level, int id, int startFrame, int endFrame)
        {
            var track = new LevelTrack
            {
                AudioId = new AudioId(id),
                AudioResourceId = AudioResourceId.Null,
                Span = FrameSpan.FromBounds(startFrame, endFrame),
            };
            level.Audio.Tracks.Add(track.AudioId, track);
        }
    }
}
