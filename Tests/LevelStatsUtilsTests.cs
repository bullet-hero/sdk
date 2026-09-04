using BH.SDK.Models;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    [TestFixture]
    public class LevelStatsUtilsTests
    {
        private static void Add(Level level, int id, RectObject levelObject)
        {
            levelObject.ObjectId = new ObjectId(id);
            level.Game.Objects.Add(levelObject.ObjectId, levelObject);
        }

        #region CountKeyframes

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CountKeyframes_Null_ReturnsZero()
        {
            Assert.AreEqual(0, LevelStatsUtils.CountKeyframes(null));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CountKeyframes_FreshObject_ReturnsZero()
        {
            // Empty tracks are valid data, not missing data - a static object genuinely has none.
            Assert.AreEqual(0, LevelStatsUtils.CountKeyframes(new RectObject()));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CountKeyframes_SharedTracks_SumsEveryTrack()
        {
            var levelObject = new RectObject();
            levelObject.Positions.Add(new PosKey());
            levelObject.Positions.Add(new PosKey());
            levelObject.Rotations.Add(new AngleKey());
            levelObject.Scales.Add(new ScaKey());
            levelObject.Sizes.Add(new ScaKey());
            levelObject.AnchorsMin.Add(new AlignmentKey());
            levelObject.AnchorsMax.Add(new AlignmentKey());
            levelObject.Pivots.Add(new AlignmentKey());

            Assert.AreEqual(8, LevelStatsUtils.CountKeyframes(levelObject));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CountKeyframes_ShapeObject_AddsItsOwnTracks()
        {
            var shape = new ShapeObject();
            shape.Positions.Add(new PosKey());
            shape.Colors.Add(new Color4Key());
            shape.UVs.Add(new UVKey());

            Assert.AreEqual(3, LevelStatsUtils.CountKeyframes(shape));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CountKeyframes_TextObject_AddsItsOwnTracks()
        {
            var text = new TextObject();
            text.Colors.Add(new Color4Key());
            text.FontSizes.Add(new FontSizeKey());
            text.Fillments.Add(new FillmentKey());
            text.Appearings.Add(new AppearingKey());

            Assert.AreEqual(4, LevelStatsUtils.CountKeyframes(text));
        }

        #endregion

        #region CollectObjects

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CollectObjects_NullScope_ReturnsZeroes()
        {
            var stats = LevelStatsUtils.CollectObjects(null);

            Assert.AreEqual(0, stats.Total);
            Assert.AreEqual(0, stats.Keyframes);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void CollectObjects_MixedScope_PartitionsByKind()
        {
            var level = new Level();
            Add(level, 1, new RectObject());
            Add(level, 2, new ShapeObject());
            Add(level, 3, new ShapeObject());
            Add(level, 4, new TextObject());
            Add(level, 5, new EffectObject());
            Add(level, 6, new PrefabObject());

            var stats = LevelStatsUtils.CollectObjects(level.Game);

            Assert.AreEqual(6, stats.Total);
            Assert.AreEqual(1, stats.Transforms);
            Assert.AreEqual(2, stats.Shapes);
            Assert.AreEqual(1, stats.Texts);
            Assert.AreEqual(1, stats.Effects);
            Assert.AreEqual(1, stats.Prefabs);
            Assert.AreEqual(stats.Total,
                stats.Transforms + stats.Shapes + stats.Texts + stats.Effects + stats.Prefabs);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void CollectObjects_SumsKeyframesAcrossObjects()
        {
            var level = new Level();

            var first = new RectObject();
            first.Positions.Add(new PosKey());
            first.Positions.Add(new PosKey());
            Add(level, 1, first);

            var second = new ShapeObject();
            second.Colors.Add(new Color4Key());
            Add(level, 2, second);

            Assert.AreEqual(3, LevelStatsUtils.CollectObjects(level.Game).Keyframes);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void CollectObjects_NullEntry_IsSkippedRatherThanThrowing()
        {
            var level = new Level();
            Add(level, 1, new ShapeObject());
            level.Game.Objects.Add(new ObjectId(2), null);

            var stats = LevelStatsUtils.CollectObjects(level.Game);

            Assert.AreEqual(1, stats.Total);
            Assert.AreEqual(1, stats.Shapes);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void CollectObjects_PrefabTemplate_IsMeasuredLikeAnyScope()
        {
            var prefab = new Prefab();
            var inner = new ShapeObject { ObjectId = new ObjectId(1) };
            inner.Positions.Add(new PosKey());
            prefab.Objects.Add(inner.ObjectId, inner);

            var stats = LevelStatsUtils.CollectObjects(prefab);

            Assert.AreEqual(1, stats.Total);
            Assert.AreEqual(1, stats.Shapes);
            Assert.AreEqual(1, stats.Keyframes);
        }

        #endregion

        #region Collect

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Collect_NullLevel_ReturnsZeroes()
        {
            var stats = LevelStatsUtils.Collect(null);

            Assert.AreEqual(0, stats.Objects.Total);
            Assert.AreEqual(0, stats.Resources.Total);
            Assert.AreEqual(0, stats.AudioTracks);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Collect_TestLevel_CountsEveryAggregate()
        {
            var level = MockData.CreateTestLevel();
            var stats = LevelStatsUtils.Collect(level);

            Assert.AreEqual(level.Game.Objects.Count, stats.Objects.Total);
            Assert.AreEqual(level.Audio.Tracks.Count, stats.AudioTracks);
            Assert.AreEqual(level.Resources.Themes.Count, stats.Resources.Themes);
            Assert.AreEqual(level.Resources.Prefabs.Count, stats.Resources.Prefabs);
            Assert.AreEqual(
                level.Resources.Textures.Count + level.Resources.Fonts.Count
                + level.Resources.Audios.Count + level.Resources.CompositeShapes.Count
                + level.Resources.Themes.Count + level.Resources.Effects.Count
                + level.Resources.Prefabs.Count,
                stats.Resources.Total);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Collect_LargeLevel_CountsTemplateContentsAsResourcesOnly()
        {
            // A Prefab template's own objects live in the resource, not in the level's scope, so the
            // object total must stay the level dictionary's own count.
            var level = MockData.CreateLargeTestLevel(64, 2, 4);
            var stats = LevelStatsUtils.Collect(level);

            Assert.AreEqual(level.Game.Objects.Count, stats.Objects.Total);
            Assert.AreEqual(2, stats.Resources.Prefabs);
        }

        #endregion
    }
}
