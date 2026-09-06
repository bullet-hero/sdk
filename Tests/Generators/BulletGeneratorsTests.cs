using System.Linq;
using BH.SDK.Generators;
using BH.SDK.Generators.Bullets;
using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // Bullet generators differ from the geometry ones in one way that matters: their output lives
    // in TIME, so the interesting failures are about frames - a bullet outliving its window, two
    // keys landing on the same frame, a stagger that silently does nothing.
    public class BulletGeneratorsTests
    {
        private const int Start = 0;
        private const int End = 300;

        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = 600;
            return level;
        }

        private static GeneratorContext Context(Level level, int start = Start, int end = End, uint seed = 42u)
            => new(level, FrameSpan.FromBounds(start, end), seed: seed);

        private static Vector2Value PositionAt(RectObject obj, int index) => (Vector2Value)obj.Positions[index].Pos;

        /// <summary> Degrees -> the radians an AngleKey actually stores (see BaseSpawnGenerator). </summary>
        private static float Rad(float degrees) => (float)(degrees * (System.Math.PI / 180.0));

        #region Wave

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Wave_TravelsFromStartToEnd()
        {
            var level = CreateLevel();
            new BulletWaveGenerator().Run(Context(level), new BulletWaveGenerator.Parameters
            {
                Count = 1, FromX = -5f, FromY = 0f, ToX = 5f, ToY = 0f,
                TravelFrames = 60, StaggerFrames = 0, Spacing = 0f,
            });

            var bullet = level.Game.Objects.Values.Single();
            Assert.AreEqual(2, bullet.Positions.Count);
            Assert.AreEqual(-5f, PositionAt(bullet, 0).X, 0.001f);
            Assert.AreEqual(5f, PositionAt(bullet, 1).X, 0.001f);
            Assert.AreEqual(0, bullet.Span.StartFrame);
            Assert.AreEqual(60, bullet.Span.EndFrame);
        }

        // Spacing spreads bullets ACROSS the travel direction. Firing along X must therefore vary Y,
        // and getting the perpendicular wrong turns a wave into a queue.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Wave_SpreadsPerpendicularToTravel()
        {
            var level = CreateLevel();
            new BulletWaveGenerator().Run(Context(level), new BulletWaveGenerator.Parameters
            {
                Count = 3, FromX = 0f, FromY = 0f, ToX = 10f, ToY = 0f,
                TravelFrames = 30, StaggerFrames = 0, Spacing = 2f,
            });

            var ys = level.Game.Objects.Values.Select(obj => PositionAt(obj, 0).Y).OrderBy(y => y).ToList();
            CollectionAssert.AreEqual(new[] { -2f, 0f, 2f }, ys.Select(y => (float)System.Math.Round(y, 3)));

            foreach (var obj in level.Game.Objects.Values)
                Assert.AreEqual(0f, PositionAt(obj, 0).X, 0.001f, "spread must not move along travel");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Wave_StaggerDelaysEachBullet()
        {
            var level = CreateLevel();
            new BulletWaveGenerator().Run(Context(level), new BulletWaveGenerator.Parameters
            {
                Count = 4, TravelFrames = 30, StaggerFrames = 5, Spacing = 1f,
            });

            var starts = level.Game.Objects.Values.Select(obj => obj.Span.StartFrame).OrderBy(f => f).ToList();
            CollectionAssert.AreEqual(new[] { 0, 5, 10, 15 }, starts);
        }

        // A run whose window is shorter than the pattern must truncate, not overflow - and the
        // truncated bullets must still be legal (one position key, not two on the same frame).
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Wave_TruncatedByAShortWindow_StaysValidAndEstimated()
        {
            var level = CreateLevel();
            var generator = new BulletWaveGenerator();
            var parameters = new BulletWaveGenerator.Parameters
            {
                Count = 6, TravelFrames = 40, StaggerFrames = 10, Spacing = 1f,
            };

            var context = Context(level, 100, 120);
            var estimate = generator.Estimate(context, parameters);
            generator.Run(context, parameters);

            var actualKeys = level.Game.Objects.Values.Sum(obj =>
                obj.Positions.Count + obj.Sizes.Count + ((ShapeObject)obj).Colors.Count);
            // Only the bullets with room to travel exist: a stagger of 10 over [100, 120] fits two
            // (100 and 110), while 120 onwards used to be clamped onto the last frame as one-frame
            // ghosts flashing after the pattern was over.
            Assert.AreEqual(2, level.Game.Objects.Count);
            Assert.AreEqual(2, estimate.Objects);
            Assert.AreEqual(actualKeys, estimate.Keyframes);
            CollectionAssert.IsEmpty(
                level.Game.Objects.Values.Where(o => o.Span.FrameDuration == FrameRules.MinFrameDuration).ToList(),
                "no one-frame ghosts");

            foreach (var obj in level.Game.Objects.Values)
            {
                Assert.LessOrEqual(obj.Span.EndFrame, 120);
                CollectionAssert.AllItemsAreUnique(obj.Positions.Select(key => key.Frame).ToList());
            }
        }

        #endregion

        #region Spiral

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void BulletSpiral_RotatesEachShotByTheAngularStep()
        {
            var level = CreateLevel();
            new BulletSpiralGenerator().Run(Context(level), new BulletSpiralGenerator.Parameters
            {
                Count = 4, AngularStep = 90f, StartAngle = 0f,
                RadiusStart = 0f, RadiusEnd = 10f, TravelFrames = 30, StaggerFrames = 0,
                FaceOutward = true,
            });

            var angles = level.Game.Objects.Values
                .Select(obj => ((FloatValue)obj.Rotations[0].Angle).Value)
                .OrderBy(a => a).ToList();
            // Stored as RADIANS - the format's own unit; the generator's math stays in degrees.
            CollectionAssert.AreEqual(new[] { Rad(0f), Rad(90f), Rad(180f), Rad(270f) }, angles);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void BulletSpiral_TravelsOutwardAlongItsOwnAngle()
        {
            var level = CreateLevel();
            new BulletSpiralGenerator().Run(Context(level), new BulletSpiralGenerator.Parameters
            {
                Count = 1, AngularStep = 0f, StartAngle = 0f,
                RadiusStart = 1f, RadiusEnd = 9f, TravelFrames = 30, StaggerFrames = 0,
            });

            var bullet = level.Game.Objects.Values.Single();
            Assert.AreEqual(1f, PositionAt(bullet, 0).X, 0.001f);
            Assert.AreEqual(9f, PositionAt(bullet, 1).X, 0.001f);
        }

        #endregion

        #region Laser sweep

        // The warning beam must never collide, whatever collider the template carries - that is the
        // entire reason it is a separate object.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Laser_WarnBeamIsHarmless_FireBeamKeepsTheCollider()
        {
            var level = CreateLevel();
            var collider = ShapeId.NewGuid();
            new BulletLaserSweepGenerator().Run(Context(level), new BulletLaserSweepGenerator.Parameters
            {
                Collider = collider, WarnFrames = 30, FireFrames = 60,
            });

            var objects = level.Game.Objects.Values.Cast<ShapeObject>().ToList();
            Assert.AreEqual(2, objects.Count);

            var warn = objects.Single(obj => obj.Name.Contains("warn"));
            var fire = objects.Single(obj => obj.Name.Contains("fire"));
            Assert.AreEqual(ShapeId.Null, warn.ColliderId);
            Assert.AreEqual(collider, fire.ColliderId);
            Assert.AreEqual(warn.Span.EndFrame, fire.Span.StartFrame, "the warning must end exactly as firing starts");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Laser_SweepsFromStartAngleToEndAngle()
        {
            var level = CreateLevel();
            new BulletLaserSweepGenerator().Run(Context(level), new BulletLaserSweepGenerator.Parameters
            {
                WarnFrames = 0, FireFrames = 60, StartAngle = 0f, EndAngle = 90f, Length = 10f,
            });

            var fire = level.Game.Objects.Values.Single();
            Assert.AreEqual(2, fire.Rotations.Count);
            Assert.AreEqual(Rad(0f), ((FloatValue)fire.Rotations[0].Angle).Value, 0.001f);
            Assert.AreEqual(Rad(90f), ((FloatValue)fire.Rotations[1].Angle).Value, 0.001f);

            // The beam extends out of the origin, so its midpoint sits half a length along it.
            Assert.AreEqual(5f, PositionAt(fire, 0).X, 0.001f);
            Assert.AreEqual(5f, PositionAt(fire, 1).Y, 0.001f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Laser_WithoutAWarningPhaseSpawnsOneObject()
        {
            var level = CreateLevel();
            var generator = new BulletLaserSweepGenerator();
            var parameters = new BulletLaserSweepGenerator.Parameters { WarnFrames = 0, FireFrames = 30 };

            var context = Context(level);
            var estimate = generator.Estimate(context, parameters);
            generator.Run(context, parameters);

            Assert.AreEqual(1, level.Game.Objects.Count);
            Assert.AreEqual(1, estimate.Objects);
        }

        #endregion

        #region Rain

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Rain_StaysInsideItsArea_AndFallsDownward()
        {
            var level = CreateLevel();
            new BulletRainGenerator().Run(Context(level), new BulletRainGenerator.Parameters
            {
                Count = 30, AreaLeft = -4f, AreaRight = 4f, TopY = 6f, BottomY = -6f,
                TravelFrames = 60, SpreadFrames = 30, TravelJitter = 0f,
            });

            foreach (var obj in level.Game.Objects.Values)
            {
                var top = PositionAt(obj, 0);
                Assert.GreaterOrEqual(top.X, -4f);
                Assert.LessOrEqual(top.X, 4f);
                Assert.AreEqual(6f, top.Y, 0.001f);

                if (obj.Positions.Count < 2) continue;
                var bottom = PositionAt(obj, 1);
                Assert.AreEqual(-6f, bottom.Y, 0.001f);
                Assert.AreEqual(top.X, bottom.X, 0.001f, "rain falls straight down");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Rain_DifferentSeedsScatterDifferently()
        {
            var parameters = new BulletRainGenerator.Parameters { Count = 16 };
            var generator = new BulletRainGenerator();

            var first = CreateLevel();
            generator.Run(Context(first, seed: 1u), parameters);
            var second = CreateLevel();
            generator.Run(Context(second, seed: 2u), parameters);

            Assert.IsFalse(first.Game.Equals(second.Game));
        }

        #endregion

        #region Homing

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Homing_BakesATrajectoryThatApproachesTheTarget()
        {
            var level = CreateLevel();
            new BulletHomingGenerator().Run(Context(level), new BulletHomingGenerator.Parameters
            {
                BurstCount = 1, Spread = 0f, LaunchAngle = 90f,
                OriginX = 0f, OriginY = 0f, TargetX = 0f, TargetY = -10f,
                Speed = 1f, TurnRate = 30f, TravelFrames = 120, Steps = 12,
            });

            var bullet = level.Game.Objects.Values.Single();
            Assert.Greater(bullet.Positions.Count, 2, "a homing bullet is a baked curve, not a straight line");

            var first = PositionAt(bullet, 0);
            var last = PositionAt(bullet, bullet.Positions.Count - 1);
            var firstDistance = Distance(first, 0f, -10f);
            var lastDistance = Distance(last, 0f, -10f);
            Assert.Less(lastDistance, firstDistance, "it must end up closer to the target than it started");
        }

        // MaxObjectKeys is 32 per track; Steps is capped so a baked curve can never exceed it, no
        // matter what the author types.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Homing_StepsAreCappedByTheKeyframeLimit()
        {
            var level = CreateLevel();
            new BulletHomingGenerator().Run(Context(level), new BulletHomingGenerator.Parameters
            {
                BurstCount = 1, Steps = 999, TravelFrames = 240, FaceTravel = true,
            });

            var bullet = level.Game.Objects.Values.Single();
            Assert.LessOrEqual(bullet.Positions.Count, LevelRules.MaxObjectKeys);
            Assert.LessOrEqual(bullet.Rotations.Count, LevelRules.MaxObjectKeys);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Homing_SpreadFansTheBurstSymmetrically()
        {
            var level = CreateLevel();
            new BulletHomingGenerator().Run(Context(level), new BulletHomingGenerator.Parameters
            {
                BurstCount = 3, Spread = 90f, LaunchAngle = 90f, TurnRate = 0f,
                Speed = 1f, TravelFrames = 60, Steps = 2, FaceTravel = true, StaggerFrames = 0,
            });

            // TurnRate 0 keeps every bullet on its launch angle, so the fan is readable off the
            // first rotation key: 45 / 90 / 135 around the 90 degree centre.
            var angles = level.Game.Objects.Values
                .Select(obj => ((FloatValue)obj.Rotations[0].Angle).Value)
                .OrderBy(a => a).ToList();
            CollectionAssert.AreEqual(new[] { Rad(45f), Rad(90f), Rad(135f) }, angles);
        }

        private static float Distance(Vector2Value from, float x, float y)
        {
            var dx = from.X - x;
            var dy = from.Y - y;
            return (float)System.Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion
    }
}
