using System.Collections.Generic;
using System.Linq;
using BH.SDK.Generators;
using BH.SDK.Generators.External;
using BH.SDK.Generators.Modifiers;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Models.Resources;
using BH.SDK.Rules;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // One sweep over EVERY registered scope generator, run with its own defaults. These are the
    // properties no individual generator should have to re-prove and every future one inherits by
    // being registered: the estimate is honest, the output stays inside the window it was given,
    // undo is exact, and the result is a level the format itself accepts.
    //
    // Adding a generator adds coverage here automatically - which is the point. A generator that
    // breaks one of these fails the suite the day it is written, not the day someone tries it.
    //
    // Each property is one test looping over the registry rather than a [TestCaseSource] case per
    // generator, so the whole sweep also runs under a plain reflective runner (no NUnit engine) -
    // which is what makes it verifiable outside a live Unity Editor.

    /// <summary> One sweep over every registered scope generator at its own defaults: honest estimate, output
    /// inside the given window, exact undo, and a level the format accepts. Registering a generator adds coverage. </summary>
    public class GeneratorSweepTests
    {
        private const int WindowStart = 30;
        private const int WindowEnd = 210;

        private static IEnumerable<IScopeGenerator> ScopeGenerators =>
            GeneratorRegistry.All.OfType<IScopeGenerator>();

        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = 600;
            return level;
        }

        private static GeneratorContext CreateContext(Level level, uint seed = 12345u) =>
            new(level, FrameSpan.FromBounds(WindowStart, WindowEnd), seed: seed);

        // A Modifier edits what is already there, so every sweep runs against a level that already
        // has content - otherwise mod_* would be swept over an empty scope and prove nothing.
        private static Level CreateSeededLevel()
        {
            var level = CreateLevel();
            for (var i = 0; i < 3; i++)
            {
                var obj = new ShapeObject
                {
                    ObjectId = level.Settings.GetNextObjectId(),
                    Name = $"seed_{i}",
                    Layer = i,
                    Span = FrameSpan.FromBounds(WindowStart + i, WindowEnd - i),
                };
                obj.Positions.Add(new PosKey(new Vector2Value(i, i), WindowStart + i));
                obj.Positions.Add(new PosKey(new Vector2Value(i + 1, i), WindowStart + i + 7));
                obj.Sizes.Add(new ScaKey(new Vector2Value(1f, 1f), WindowStart + i));
                level.Game.Objects.Add(obj.ObjectId, obj);
            }
            return level;
        }

        private static int CountKeys(RectObject obj)
        {
            var keys = obj.Positions.Count + obj.Rotations.Count + obj.Scales.Count + obj.Sizes.Count
                       + obj.AnchorsMin.Count + obj.AnchorsMax.Count + obj.Pivots.Count;
            if (obj is ShapeObject texture) keys += texture.Colors.Count + texture.UVs.Count;
            return keys;
        }

        /// <summary> The level-global camera tracks, counted apart from object keys because a
        /// camera-only generator (gen_beat_flash) creates no objects at all. </summary>
        private static int CountCameraKeys(Level level)
        {
            var camera = level.Game.CameraEvents;
            return camera.Positions.Count + camera.Rotations.Count + camera.Zooms.Count
                   + camera.Pivots.Count + camera.Shakes.Count;
        }

        // ExternalAnalysis generators are handed the data a host would have measured for them.
        // Without it they legitimately produce nothing, and the whole sweep would skip exactly the
        // generators whose inputs are hardest to get right.
        private static object CreateFilledParameters(IGenerator generator)
        {
            var parameters = generator.CreateDefaultParameters();

            if (parameters is IAudioFileInput audio)
            {
                audio.AudioPath = "music/song.ogg";
                audio.UriType = ResourceUriType.LevelPath;
                audio.DurationSeconds = 30f;
            }
            if (parameters is IWaveformInput waveform)
            {
                var peaks = new float[64];
                for (var i = 0; i < peaks.Length; i++) peaks[i] = (i % 8) / 8f + 0.1f;
                waveform.Peaks = peaks;
            }
            if (parameters is IBeatFramesInput beats)
            {
                var frames = new List<int>();
                for (var frame = WindowStart; frame <= WindowEnd; frame += 24) frames.Add(frame);
                beats.BeatFrames = frames.ToArray();
            }
            if (parameters is IPixelTextureInput image) image.Texture = CreateTestTexture();

            return parameters;
        }

        /// <summary> A tiny checkerboard with a transparent corner - enough to exercise run merging,
        /// alpha skipping and downsampling without making the test slow. </summary>
        private static PixelTexture CreateTestTexture()
        {
            const int side = 8;
            var texture = new PixelTexture(side, side);
            for (var y = 0; y < side; y++)
            for (var x = 0; x < side; x++)
            {
                var visible = !(x < 2 && y < 2);
                var shade = (byte)((x / 2 + y / 2) % 2 == 0 ? 255 : 64);
                texture.Pixels[y * side + x] = new Pixel(shade, shade, shade, visible ? (byte)255 : (byte)0);
            }
            return texture;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RegistryHasScopeGenerators()
        {
            Assert.IsNotEmpty(ScopeGenerators.ToList());
        }

        // GeneratorCost describes what a run ADDS - a Modifier legitimately adds nothing while
        // changing plenty, and mod_content_remover/mod_framerate_remap actively remove. So additions
        // are measured directly rather than as a net delta, which a removing generator would drive
        // negative: the ids the run reports creating, and the keys those new objects plus the
        // level-global camera tracks came out holding. The one thing this cannot see is a generator
        // adding keys to an object that already existed - none does, and a Modifier that starts to
        // would need this measurement rethought rather than the assertion relaxed.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Estimate_MatchesTheActualRun()
        {
            foreach (var generator in ScopeGenerators)
            {
                var level = CreateSeededLevel();
                var context = CreateContext(level);
                var parameters = CreateFilledParameters(generator);

                var cameraBefore = CountCameraKeys(level);

                var estimate = generator.Estimate(context, parameters);
                var result = generator.Run(context, parameters);

                Assert.AreEqual(result.CreatedIds.Length, estimate.Objects,
                    $"{generator.NameKey}: object count");

                var addedKeys = CountCameraKeys(level) - cameraBefore;
                foreach (var id in result.CreatedIds)
                    if (level.Game.Objects.TryGetValue(id, out var obj))
                        addedKeys += CountKeys(obj);

                Assert.AreEqual(addedKeys, estimate.Keyframes,
                    $"{generator.NameKey}: keyframe count");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        // The window bounds what a run may CREATE, not what it may touch. A Modifier shifting an
        // object that already lived outside the window must not drag it inside - the author scoped
        // where new content goes, not which existing objects are allowed to exist where. Existing
        // objects are still bounded, by the level's own timeline, which GeneratedLevel_PassesValidation
        // checks through RuleLevelFrame.
        public void EveryCreatedObject_StaysInsideTheContextWindow()
        {
            foreach (var generator in ScopeGenerators)
            {
                var level = CreateSeededLevel();
                var result = generator.Run(CreateContext(level), CreateFilledParameters(generator));

                foreach (var id in result.CreatedIds)
                {
                    var obj = level.Game.Objects[id];
                    Assert.GreaterOrEqual(obj.Span.StartFrame, WindowStart, $"{generator.NameKey}: {obj.Name} start");
                    Assert.LessOrEqual(obj.Span.EndFrame, WindowEnd, $"{generator.NameKey}: {obj.Name} end");
                    Assert.GreaterOrEqual(obj.Span.FrameDuration, FrameRules.MinFrameDuration,
                        $"{generator.NameKey}: {obj.Name} duration");
                    Assert.GreaterOrEqual(obj.Layer, ValueRules.MinLayer, $"{generator.NameKey}: {obj.Name} layer");
                    Assert.LessOrEqual(obj.Layer, ValueRules.MaxLayer, $"{generator.NameKey}: {obj.Name} layer");
                }
            }
        }

        // A keyframe's Frame is LOCAL to its object (the runtime reads it back as obj.Span.StartFrame +
        // Frame), and writing an absolute one is invisible in every other check here: the object is
        // created, framed and validated correctly, it just never reaches its own keys and therefore
        // never moves. Every animated generator shipped with an absolute frame once; this is what
        // stops the next one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryCreatedObject_KeepsItsKeyframesLocalToItsOwnLifetime()
        {
            foreach (var generator in ScopeGenerators)
            {
                var level = CreateSeededLevel();
                var result = generator.Run(CreateContext(level), CreateFilledParameters(generator));

                foreach (var id in result.CreatedIds)
                {
                    var obj = level.Game.Objects[id];
                    var span = obj.Span.EndFrame - obj.Span.StartFrame;

                    foreach (var track in ObjectTracks.Of(obj, ObjectTrackMask.All))
                    {
                        for (var i = 0; i < track.Count; i++)
                        {
                            var frame = track.FrameAt(i);
                            Assert.GreaterOrEqual(frame, FrameRules.MinFrame,
                                $"{generator.NameKey}: {obj.Name} key before its start");
                            Assert.LessOrEqual(frame, FrameRules.LastFrameOf(span),
                                $"{generator.NameKey}: {obj.Name} key at {frame} is past its own {span}-frame " +
                                "lifetime - an absolute frame was stored where a local one belongs");
                        }
                    }
                }
            }
        }

        // Frame uniqueness within a track is a format rule ([RuleCollectionUnique]) the context
        // deliberately does not enforce - a clamped lifetime is exactly where a second key would
        // land on top of the first.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryTrack_HasUniqueFrames()
        {
            foreach (var generator in ScopeGenerators)
            {
                var level = CreateSeededLevel();
                generator.Run(CreateContext(level), CreateFilledParameters(generator));

                foreach (var obj in level.Game.Objects.Values)
                {
                    AssertUnique(generator.NameKey, obj.Name, "positions", obj.Positions.Select(k => k.Frame));
                    AssertUnique(generator.NameKey, obj.Name, "rotations", obj.Rotations.Select(k => k.Frame));
                    AssertUnique(generator.NameKey, obj.Name, "sizes", obj.Sizes.Select(k => k.Frame));
                    AssertUnique(generator.NameKey, obj.Name, "scales", obj.Scales.Select(k => k.Frame));
                    if (obj is ShapeObject texture)
                        AssertUnique(generator.NameKey, obj.Name, "colors", texture.Colors.Select(k => k.Frame));
                }
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void NoTrack_ExceedsTheKeyframeCap()
        {
            foreach (var generator in ScopeGenerators)
            {
                var level = CreateLevel();
                generator.Run(CreateContext(level), CreateFilledParameters(generator));

                foreach (var obj in level.Game.Objects.Values)
                {
                    Assert.LessOrEqual(obj.Positions.Count, LevelRules.MaxObjectKeys,
                        $"{generator.NameKey}: {obj.Name} positions");
                    Assert.LessOrEqual(obj.Rotations.Count, LevelRules.MaxObjectKeys,
                        $"{generator.NameKey}: {obj.Name} rotations");
                    Assert.LessOrEqual(obj.Sizes.Count, LevelRules.MaxObjectKeys,
                        $"{generator.NameKey}: {obj.Name} sizes");
                    if (obj is ShapeObject texture)
                        Assert.LessOrEqual(texture.Colors.Count, LevelRules.MaxObjectKeys,
                            $"{generator.NameKey}: {obj.Name} colors");
                }
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Undo_RestoresTheLevelExactly()
        {
            foreach (var generator in ScopeGenerators)
            {
                var level = CreateSeededLevel();
                var before = level.Game.Copy();

                var result = generator.Run(CreateContext(level), CreateFilledParameters(generator));
                var after = level.Game.Copy();

                result.Log.Revert();
                Assert.IsTrue(before.Equals(level.Game),
                    $"{generator.NameKey}: revert must restore GameLevel exactly");

                result.Log.Reapply();
                Assert.IsTrue(after.Equals(level.Game),
                    $"{generator.NameKey}: redo must reproduce the run exactly");

                // Against what the run actually left behind, not seeded + created: a generator that
                // removes objects (mod_content_remover) ends with fewer than it started with.
                Assert.AreEqual(after.Objects.Count, level.Game.Objects.Count,
                    $"{generator.NameKey}: redo object count");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void SameSeed_ProducesIdenticalOutput()
        {
            foreach (var generator in ScopeGenerators)
            {
                var first = CreateSeededLevel();
                generator.Run(CreateContext(first, 777u), CreateFilledParameters(generator));
                var second = CreateSeededLevel();
                generator.Run(CreateContext(second, 777u), CreateFilledParameters(generator));

                Assert.IsTrue(first.Game.Equals(second.Game), $"{generator.NameKey} is not deterministic");
            }
        }

        // The format's own validator is the final word on whether generated content is legal: a
        // generator writing an out-of-range position or a null value passes every assertion above
        // and still produces a level the game refuses to load.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void GeneratedLevel_PassesValidation()
        {
            var facade = new ValidationFacade();
            foreach (var generator in ScopeGenerators)
            {
                var level = CreateSeededLevel();
                generator.Run(CreateContext(level), CreateFilledParameters(generator));

                var report = facade.Validate(level);
                Assert.IsFalse(report.HasErrors, $"{generator.NameKey}: {report}");
            }
        }

        private static void AssertUnique(string nameKey, string objectName, string track, IEnumerable<int> frames)
            => CollectionAssert.AllItemsAreUnique(frames.ToList(), $"{nameKey}: {objectName}.{track}");
    }
}
