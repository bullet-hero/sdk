using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.PostProcessing;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;
using BH.SDK.Utils.Performance;
using NUnit.Framework;

namespace BH.SDK.Tests.Performance
{
    /// <summary> The count dimensions, against hand-built levels with known answers, plus spec checks 3
    /// (adding never lowers) and 4 (a placement counts as its materialized objects). </summary>
    public class LevelDemandCounterTests
    {
        private const int Fps = 60;
        private const int OneSecond = 60;
        private const float Tolerance = 1e-4f;

        private static Level CreateLevel(int frames = OneSecond)
        {
            var level = new Level();
            level.Settings.Fps = Fps;
            level.Settings.FrameDuration = frames;
            return level;
        }

        private static FrameSpan Whole(int frames = OneSecond) => new(FrameRules.MinFrame, frames);

        private static T Add<T>(Level level, T obj) where T : RectObject
        {
            obj.ObjectId = level.Settings.GetNextObjectId();
            if (obj.Span.FrameDuration == FrameRules.MinFrameDuration && obj.Span.StartFrame == FrameRules.MinFrame)
                obj.Span = Whole(level.Settings.FrameDuration);
            level.Game.Objects.Add(obj.ObjectId, obj);
            return obj;
        }

        private static float Get(List<float[]> windows, int window, DemandDimension dimension)
            => windows[window][(int)dimension];

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Count_AnEmptyLevel_MeasuresNoWindows()
        {
            Assert.AreEqual(0, LevelDemandCounter.Count(null).Count);
            Assert.AreEqual(0, LevelDemandCounter.Count(CreateLevel(0)).Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Count_EveryFamilyAliveTheWholeLevel_ReadsItsCountInEveryWindow()
        {
            var level = CreateLevel();
            var parent = Add(level, new RectObject());
            var child = Add(level, new RectObject { ParentObjectId = parent.ObjectId });
            Add(level, new RectObject { ParentObjectId = child.ObjectId });

            var shape = Add(level, new ShapeObject { ShapeId = ShapeId.Square.Fill, ShaderType = ShaderType.Transparent });
            shape.Positions.Add(new PosKey(new Vector2Value(), 1));
            shape.Positions.Add(new PosKey(new Vector2Value(), 10));
            Add(level, new ShapeObject { ShapeId = ShapeId.Null, ColliderId = ShapeId.Square.Fill });
            Add(level, new ShapeObject { ShapeId = ShapeId.Square.Fill, ShaderType = ShaderType.Opaque });
            Add(level, new TextObject { Text = new StringValue("Hi there") });
            Add(level, new EffectObject());

            var windows = LevelDemandCounter.Count(level);

            Assert.AreEqual(3, windows.Count, "a one-second level has three windows");
            foreach (var window in windows) Assert.AreEqual(DemandDimensions.Count, window.Length);

            for (var w = 0; w < windows.Count; w++)
            {
                Assert.AreEqual(8f, Get(windows, w, DemandDimension.Instances), Tolerance);
                Assert.AreEqual(2f, Get(windows, w, DemandDimension.Keyframes), Tolerance);
                Assert.AreEqual(3f, Get(windows, w, DemandDimension.ParentLinks), Tolerance, "depth 1 + depth 2");
                Assert.AreEqual(1f, Get(windows, w, DemandDimension.Colliders), Tolerance);
                Assert.AreEqual(1f, Get(windows, w, DemandDimension.Texts), Tolerance);
                Assert.AreEqual(7f, Get(windows, w, DemandDimension.Glyphs), Tolerance, "whitespace draws nothing");
                Assert.AreEqual(1f, Get(windows, w, DemandDimension.Effects), Tolerance);
                Assert.AreEqual(1f, Get(windows, w, DemandDimension.TransparentShapes), Tolerance,
                    "a shape with no mesh draws nothing");
                Assert.AreEqual(1f, Get(windows, w, DemandDimension.OpaqueBatches), Tolerance);
                Assert.AreEqual(0f, Get(windows, w, DemandDimension.TransparentArea), Tolerance, "the client's");
                Assert.AreEqual(0f, Get(windows, w, DemandDimension.OpaqueArea), Tolerance, "the client's");
                Assert.AreEqual(0f, Get(windows, w, DemandDimension.PostPasses), Tolerance);
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Count_Colliders_OnlyWhilePlayerCollisionIsOn()
        {
            var level = CreateLevel();
            Add(level, new ShapeObject { ColliderId = ShapeId.Square.Fill });
            level.Game.PlayerEvents.Collisions.Add(new BoolKey(true, 1));
            level.Game.PlayerEvents.Collisions.Add(new BoolKey(false, 31));

            var windows = LevelDemandCounter.Count(level);

            Assert.AreEqual(1f, Get(windows, 0, DemandDimension.Colliders), Tolerance, "[0, 0.5) all on");
            Assert.AreEqual(0.5f, Get(windows, 1, DemandDimension.Colliders), Tolerance, "[0.25, 0.75) half on");
            Assert.AreEqual(0f, Get(windows, 2, DemandDimension.Colliders), Tolerance, "[0.5, 1) off");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Count_ACollisionTrackStartingOff_IsOffBeforeItsFirstKey()
        {
            var level = CreateLevel();
            Add(level, new ShapeObject { ColliderId = ShapeId.Square.Fill });
            level.Game.PlayerEvents.Collisions.Add(new BoolKey(false, 31));

            var windows = LevelDemandCounter.Count(level);

            foreach (var window in windows) Assert.AreEqual(0f, window[(int)DemandDimension.Colliders], Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Count_OpaqueBatches_AreDistinctMaterialAndMeshPairsAlive()
        {
            var level = CreateLevel();
            Add(level, new ShapeObject { ShapeId = ShapeId.Square.Fill, ShaderType = ShaderType.Opaque });
            Add(level, new ShapeObject { ShapeId = ShapeId.Square.Fill, ShaderType = ShaderType.Opaque });
            Add(level, new ShapeObject { ShapeId = ShapeId.Circle.Fill, ShaderType = ShaderType.Opaque });
            Add(level, new ShapeObject
            {
                ShapeId = ShapeId.Square.Fill, ShaderType = ShaderType.Opaque, TextureResourceId = new TextureResourceId(-1),
            });

            var windows = LevelDemandCounter.Count(level);

            Assert.AreEqual(3f, Get(windows, 0, DemandDimension.OpaqueBatches), Tolerance,
                "two equal pairs batch; another mesh or another texture does not");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Count_OneBatch_IsAliveWhileAnyOfItsShapesIs()
        {
            var level = CreateLevel();
            Add(level, new ShapeObject
            {
                ShapeId = ShapeId.Square.Fill, ShaderType = ShaderType.Opaque, Span = FrameSpan.FromBounds(1, 21),
            });
            Add(level, new ShapeObject
            {
                ShapeId = ShapeId.Square.Fill, ShaderType = ShaderType.Opaque, Span = FrameSpan.FromBounds(11, 31),
            });

            var windows = LevelDemandCounter.Count(level);

            Assert.AreEqual(1f, Get(windows, 0, DemandDimension.OpaqueBatches), Tolerance,
                "the union [0, 0.5) covers the first window once, not twice");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Count_AutoShader_IsTransparentUnlessTheResolverSaysOpaque()
        {
            var level = CreateLevel();
            Add(level, new ShapeObject { ShapeId = ShapeId.Square.Fill, ShaderType = ShaderType.Auto });

            var unresolved = LevelDemandCounter.Count(level);
            var resolved = LevelDemandCounter.Count(level, _ => true);

            Assert.AreEqual(1f, Get(unresolved, 0, DemandDimension.TransparentShapes), Tolerance);
            Assert.AreEqual(0f, Get(resolved, 0, DemandDimension.TransparentShapes), Tolerance);
            Assert.AreEqual(1f, Get(resolved, 0, DemandDimension.OpaqueBatches), Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Count_PostPasses_FollowEachTracksStartKey_AndTheMasterSwitch()
        {
            var level = CreateLevel();
            var events = level.Game.PostProcessingEvents;
            events.Active = true;
            events.Blooms.Add(new BloomKey { Active = true, Frame = 1 });
            events.Vignettes.Add(new VignetteKey { Active = true, Frame = 1 });
            events.Vignettes.Add(new VignetteKey { Active = false, Frame = 31 });

            var windows = LevelDemandCounter.Count(level);

            Assert.AreEqual(2f, Get(windows, 0, DemandDimension.PostPasses), Tolerance);
            Assert.AreEqual(1.5f, Get(windows, 1, DemandDimension.PostPasses), Tolerance);
            Assert.AreEqual(1f, Get(windows, 2, DemandDimension.PostPasses), Tolerance);

            events.Active = false;
            Assert.AreEqual(0f, Get(LevelDemandCounter.Count(level), 0, DemandDimension.PostPasses), Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Count_AShortLivedObject_IsCountedForTheFramesItLives()
        {
            var level = CreateLevel();
            Add(level, new RectObject { Span = new FrameSpan(40, 2) });

            var windows = LevelDemandCounter.Count(level);

            var expected = 2f / Fps / PerformanceRules.WindowSeconds;
            Assert.AreEqual(expected, Get(windows, 2, DemandDimension.Instances), Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Count_APlacement_CountsAsItsMaterializedObjects()
        {
            var level = CreateLevel(600);
            var template = new Prefab { PrefabId = PrefabId.NewId() };
            level.Resources.Prefabs.Add(template.PrefabId, template);

            var innerShape = new ShapeObject
            {
                ObjectId = template.GetNextObjectId(), ShapeId = ShapeId.Square.Fill,
                ColliderId = ShapeId.Square.Fill, Span = FrameSpan.FromBounds(1, 61),
            };
            innerShape.Positions.Add(new PosKey(new Vector2Value(), 1));
            template.Objects.Add(innerShape.ObjectId, innerShape);
            var innerText = new TextObject
            {
                ObjectId = template.GetNextObjectId(), ParentObjectId = innerShape.ObjectId,
                Text = new StringValue("abc"), Span = FrameSpan.FromBounds(1, 31),
            };
            template.Objects.Add(innerText.ObjectId, innerText);

            var placement = new PrefabObject
            {
                ObjectId = level.Settings.GetNextObjectId(), PrefabId = template.PrefabId,
                Span = FrameSpan.FromBounds(100, 160),
            };
            level.Game.Objects.Add(placement.ObjectId, placement);
            placement.ObjectIds[innerShape.ObjectId] = level.Settings.GetNextObjectId();
            placement.ObjectIds[innerText.ObjectId] = level.Settings.GetNextObjectId();
            PrefabVirtualizationUtils.Expand(level);

            var placed = LevelDemandCounter.Count(level);

            level.Game.Objects[placement.ObjectId] = PrefabFlattenUtils.Flatten(placement);
            var flattened = LevelDemandCounter.Count(level);

            var glyphs = 0f;
            foreach (var window in placed) glyphs += window[(int)DemandDimension.Glyphs];
            Assert.Greater(glyphs, 0f, "the copies were counted at all");
            for (var w = 0; w < placed.Count; w++) CollectionAssert.AreEqual(placed[w], flattened[w]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Count_AddingAnyObject_NeverLowersAnyWindow()
        {
            var random = new Random(42);
            var level = CreateLevel(300);
            var before = LevelDemandCounter.Count(level);

            for (var step = 0; step < 300; step++)
            {
                Add(level, RandomObject(random, level));
                var after = LevelDemandCounter.Count(level);

                for (var w = 0; w < before.Count; w++)
                for (var k = 0; k < DemandDimensions.Count; k++)
                    Assert.GreaterOrEqual(after[w][k], before[w][k] - Tolerance, $"window {w}, {(DemandDimension)k}");

                before = after;
            }
        }

        private static RectObject RandomObject(Random random, Level level)
        {
            var start = random.Next(1, level.Settings.FrameDuration);
            var duration = random.Next(1, level.Settings.FrameDuration - start + 2);

            RectObject obj = random.Next(5) switch
            {
                0 => new ShapeObject
                {
                    ShapeId = random.Next(2) == 0 ? ShapeId.Square.Fill : ShapeId.Circle.Fill,
                    ColliderId = random.Next(2) == 0 ? ShapeId.Square.Fill : ShapeId.Null,
                    ShaderType = (ShaderType)random.Next(3),
                },
                1 => new TextObject { Text = new StringValue(new string('x', random.Next(0, 20))) },
                2 => new EffectObject(),
                _ => new RectObject(),
            };

            obj.Span = new FrameSpan(start, duration);
            if (random.Next(3) == 0 && level.Game.Objects.Count > 0)
            {
                var ids = new List<ObjectId>(level.Game.Objects.Keys);
                obj.ParentObjectId = ids[random.Next(ids.Count)];
            }

            for (var i = random.Next(0, 4); i > 0; i--) obj.Positions.Add(new PosKey(new Vector2Value(), i));
            return obj;
        }
    }
}
