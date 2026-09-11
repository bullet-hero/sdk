using System.Linq;
using BH.SDK.Generators;
using BH.SDK.Generators.Audio;
using BH.SDK.Generators.External;
using BH.SDK.Generators.Textures;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // Stage 4's generators are the first to depend on data the SDK cannot produce itself, so these
    // tests double as the contract test for ExternalAnalysis: given the inputs a host would supply,
    // each generator has to produce exactly the content it promised - and given none, it has to
    // produce nothing rather than inventing some.

    /// <summary> The generators that need data the SDK cannot produce itself: given a host's inputs they must
    /// produce exactly what they promised, and given none they must produce nothing rather than inventing some. </summary>
    public class AudioTextureGeneratorsTests
    {
        private const int Start = 0;
        private const int End = 240;

        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = 600;
            return level;
        }

        private static GeneratorContext Context(Level level) => new(level, FrameSpan.FromBounds(Start, End));

        #region gen_level_audio_file

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AudioFile_BuildsResourceTrackAndTimelineFromTheDuration()
        {
            var generator = new AudioFileLevelGenerator();
            var parameters = new AudioFileLevelGenerator.Parameters
            {
                AudioPath = "music/theme song.ogg",
                DurationSeconds = 10f,
                TailSeconds = 2f,
                Framerate = 60,
            };

            var (level, meta) = generator.Create(parameters);

            Assert.AreEqual(60, level.Settings.Fps);
            Assert.AreEqual(720, level.Settings.FrameDuration, "(10 + 2) seconds at 60 fps");

            var resource = level.Resources.Audios.Values.Single();
            Assert.IsTrue(resource.AudioResourceId.IsUserDefined(), "a level's own clip is user-defined");
            Assert.AreEqual("music/theme song.ogg", resource.Sources.Single().Uri);
            Assert.AreEqual(ResourceUriType.LevelPath, resource.Sources.Single().UriType);

            var track = level.Audio.Tracks.Values.Single();
            Assert.AreEqual(resource.AudioResourceId, track.AudioResourceId);
            Assert.AreEqual(FrameRules.MinFrame, track.Span.StartFrame);
            Assert.AreEqual(FrameRules.EndBoundaryOf(level.Settings.FrameDuration), track.Span.EndFrame,
                "the track covers the whole timeline, so its exclusive end is the boundary past the "
                + "timeline's last frame");

            Assert.AreEqual("theme song", ((StringValue)meta.LevelName).Value,
                "an untitled level is named after its song");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AudioFile_KeepsAnAuthoredNameOverTheFileName()
        {
            var generator = new AudioFileLevelGenerator();
            var (_, meta) = generator.Create(new AudioFileLevelGenerator.Parameters
            {
                AudioPath = "song.ogg",
                LevelName = new StringValue("My Level"),
                DurationSeconds = 5f,
            });

            Assert.AreEqual("My Level", ((StringValue)meta.LevelName).Value);
        }

        // A host that cannot measure the clip still has to get a usable level - a zero-length
        // timeline would be one nothing can be authored on.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AudioFile_WithoutAMeasuredDurationFallsBackToADefaultLength()
        {
            var generator = new AudioFileLevelGenerator();
            var (level, _) = generator.Create(generator.CreateDefaultParameters());

            Assert.GreaterOrEqual(level.Settings.FrameDuration, FrameRules.MinFrameDuration);
            Assert.Greater(level.Settings.FrameDuration, 1);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void AudioFile_ProducesARuleValidLevel()
        {
            var generator = new AudioFileLevelGenerator();
            var (level, _) = generator.Create(new AudioFileLevelGenerator.Parameters
            {
                AudioPath = "song.ogg", DurationSeconds = 42f,
            });

            Assert.IsFalse(new Validations.ValidationFacade().Validate(level).HasErrors);
        }

        // Split from the level check above deliberately: validating a LevelMeta reaches code that
        // needs Unity present, so this one only runs inside the Editor - keeping it in the same test
        // as the level would make the level's own coverage unverifiable outside it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void AudioFile_ProducesRuleValidMetadata()
        {
            var generator = new AudioFileLevelGenerator();
            var (_, meta) = generator.Create(new AudioFileLevelGenerator.Parameters
            {
                AudioPath = "song.ogg", DurationSeconds = 42f,
            });

            Assert.IsFalse(new Validations.ValidationFacade().Validate(meta).HasErrors);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AudioFile_DeclaresItsExternalInput()
        {
            var generator = new AudioFileLevelGenerator();
            Assert.IsTrue(generator.Requirements.HasFlag(GeneratorRequirements.ExternalAnalysis));
            Assert.IsInstanceOf<IAudioFileInput>(generator.CreateDefaultParameters());
        }

        #endregion

        #region gen_audio_waveform

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Waveform_DrawsOneBarPerPeak_ScaledByIt()
        {
            var level = CreateLevel();
            var parameters = new AudioWaveformGenerator.Parameters
            {
                BarCount = 0, // one bar per peak
                Peaks = new[] { 0f, 0.5f, 1f },
                Height = 10f, MinHeight = 1f, BarWidth = 1f, Spacing = 0f, OriginY = 0f,
            };
            new AudioWaveformGenerator().Run(Context(level), parameters);

            var heights = level.Game.Objects.OrderBy(pair => pair.Key.value)
                .Select(pair => ((Vector2Value)pair.Value.Sizes[0].Scale).Y).ToList();
            CollectionAssert.AreEqual(new[] { 1f, 6f, 11f }, heights);
        }

        // No peaks means the host has not sampled the track yet. Inventing a shape would look like
        // success, which is worse than an empty run.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Waveform_WithoutPeaksProducesNothing()
        {
            var level = CreateLevel();
            var generator = new AudioWaveformGenerator();
            var parameters = generator.CreateDefaultParameters();

            var context = Context(level);
            var estimate = generator.Estimate(context, parameters);
            generator.Run(context, parameters);

            Assert.AreEqual(0, level.Game.Objects.Count);
            Assert.AreEqual(0, estimate.Objects);
        }

        // Asking for fewer bars than there are peaks must summarise the whole track, not truncate
        // it to the opening seconds.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Waveform_ResamplesWhenFewerBarsThanPeaksAreAsked()
        {
            var level = CreateLevel();
            new AudioWaveformGenerator().Run(Context(level), new AudioWaveformGenerator.Parameters
            {
                BarCount = 2,
                Peaks = new[] { 0.1f, 0.2f, 0.9f, 0.3f },
                Height = 10f, MinHeight = 0f, BarWidth = 1f, Spacing = 0f,
            });

            var heights = level.Game.Objects.OrderBy(pair => pair.Key.value)
                .Select(pair => ((Vector2Value)pair.Value.Sizes[0].Scale).Y).ToList();
            Assert.AreEqual(2, heights.Count);
            Assert.AreEqual(2f, heights[0], 0.001f, "max of the first half");
            Assert.AreEqual(9f, heights[1], 0.001f, "max of the second half - the loud part survives");
        }

        #endregion

        #region gen_beat_flash

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void BeatFlash_WritesAZoomPunchAndReleasePerBeat()
        {
            var level = CreateLevel();
            new BeatFlashGenerator().Run(Context(level), new BeatFlashGenerator.Parameters
            {
                BeatFrames = new[] { 30, 90 },
                BaseZoom = 10f, ZoomPunch = 2f, DecayFrames = 10, Shake = false,
            });

            Assert.AreEqual(0, level.Game.Objects.Count, "gen_beat_flash is camera-only");

            var zooms = level.Game.CameraEvents.Zooms.OrderBy(key => key.Frame).ToList();
            CollectionAssert.AreEqual(new[] { 30, 40, 90, 100 }, zooms.Select(key => key.Frame));
            Assert.AreEqual(8f, ((FloatValue)zooms[0].Zoom).Value, 0.001f, "punched in");
            Assert.AreEqual(10f, ((FloatValue)zooms[1].Zoom).Value, 0.001f, "released back");
        }

        // ClearRange is the destructive path; the whole point of routing it through the context is
        // that undo brings the wiped keys back.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void BeatFlash_ClearRangeWipesInsideTheWindowOnly_AndUndoRestoresIt()
        {
            var level = CreateLevel();
            var zooms = level.Game.CameraEvents.Zooms;
            zooms.Add(new ZoomKey(new FloatValue(3f), 10));
            zooms.Add(new ZoomKey(new FloatValue(4f), 500));
            var snapshot = level.Game.CameraEvents.Copy();

            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 240));
            var result = new BeatFlashGenerator().Run(context, new BeatFlashGenerator.Parameters
            {
                BeatFrames = new[] { 60 }, DecayFrames = 10, Shake = false, ClearRange = true,
            });

            Assert.IsTrue(zooms.Any(key => key.Frame == 500), "keys outside the window survive");
            Assert.IsFalse(zooms.Any(key => key.Frame == 10), "keys inside it are wiped");

            result.Log.Revert();
            Assert.IsTrue(snapshot.Equals(level.Game.CameraEvents));
        }

        // Beats closer together than the decay would otherwise put two keys on one frame, which the
        // format rejects.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void BeatFlash_CollidingBeatsNeverDuplicateAFrame()
        {
            var level = CreateLevel();
            var generator = new BeatFlashGenerator();
            var parameters = new BeatFlashGenerator.Parameters
            {
                BeatFrames = new[] { 30, 35, 40 }, DecayFrames = 10, Shake = true,
            };

            var context = Context(level);
            var estimate = generator.Estimate(context, parameters);
            generator.Run(context, parameters);

            var camera = level.Game.CameraEvents;
            CollectionAssert.AllItemsAreUnique(camera.Zooms.Select(key => key.Frame).ToList());
            CollectionAssert.AllItemsAreUnique(camera.Shakes.Select(key => key.Frame).ToList());
            Assert.AreEqual(camera.Zooms.Count + camera.Shakes.Count, estimate.Keyframes);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void BeatFlash_IgnoresBeatsOutsideTheWindow()
        {
            var level = CreateLevel();
            new BeatFlashGenerator().Run(new GeneratorContext(level, FrameSpan.FromBounds(100, 200)),
                new BeatFlashGenerator.Parameters
                {
                    BeatFrames = new[] { 10, 150, 500 }, DecayFrames = 5, Shake = false,
                });

            var frames = level.Game.CameraEvents.Zooms.Select(key => key.Frame).OrderBy(f => f).ToList();
            CollectionAssert.AreEqual(new[] { 150, 155 }, frames);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void BeatFlash_DeclaresLevelScope()
        {
            var generator = new BeatFlashGenerator();
            Assert.IsTrue(generator.Requirements.HasFlag(GeneratorRequirements.LevelScope));
            Assert.IsInstanceOf<IBeatFramesInput>(generator.CreateDefaultParameters());
        }

        #endregion

        #region gen_texture_objects

        private static PixelTexture SolidRow(int width, int height, Pixel color)
        {
            var texture = new PixelTexture(width, height);
            for (var i = 0; i < texture.Pixels.Length; i++) texture.Pixels[i] = color;
            return texture;
        }

        // The headline feature of the rewrite: a flat row is one wide object, not one object per
        // pixel. The original generator would have produced 16 here (and added none of them to the
        // scope).
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Texture_MergesRunsOfOneColourIntoSingleObjects()
        {
            var level = CreateLevel();
            var generator = new ShapeObjectsGenerator();
            var parameters = new ShapeObjectsGenerator.Parameters
            {
                Image = SolidRow(4, 4, new Pixel(255, 255, 255, 255)),
                TargetWidth = 4, TargetHeight = 4, PixelSize = 1f, MergeRuns = true,
            };

            var context = Context(level);
            var estimate = generator.Estimate(context, parameters);
            generator.Run(context, parameters);

            Assert.AreEqual(4, level.Game.Objects.Count, "one object per row, not per pixel");
            Assert.AreEqual(4, estimate.Objects);

            foreach (var obj in level.Game.Objects.Values)
                Assert.AreEqual(4f, ((Vector2Value)obj.Sizes[0].Scale).X, 0.001f, "a run is as wide as it is long");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Texture_WithoutMergingPlacesOneObjectPerPixel()
        {
            var level = CreateLevel();
            new ShapeObjectsGenerator().Run(Context(level), new ShapeObjectsGenerator.Parameters
            {
                Image = SolidRow(4, 4, new Pixel(255, 255, 255, 255)),
                TargetWidth = 4, TargetHeight = 4, PixelSize = 1f, MergeRuns = false,
            });

            Assert.AreEqual(16, level.Game.Objects.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Texture_SkipsFullyTransparentPixels()
        {
            var level = CreateLevel();
            var texture = SolidRow(4, 1, new Pixel(255, 255, 255, 255));
            texture.Pixels[1] = new Pixel(0, 0, 0, 0);
            texture.Pixels[2] = new Pixel(0, 0, 0, 0);

            new ShapeObjectsGenerator().Run(Context(level), new ShapeObjectsGenerator.Parameters
            {
                Image = texture, TargetWidth = 4, TargetHeight = 1, PixelSize = 1f, MergeRuns = true,
            });

            Assert.AreEqual(2, level.Game.Objects.Count, "the transparent gap splits the row in two");
            foreach (var obj in level.Game.Objects.Values)
                Assert.AreEqual(1f, ((Vector2Value)obj.Sizes[0].Scale).X, 0.001f);
        }

        // Downsampling is what makes a real image affordable at all: a 128x128 source must not
        // become 16 384 objects because nobody changed the defaults.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Texture_DownsamplesToTheRequestedSize()
        {
            var level = CreateLevel();
            new ShapeObjectsGenerator().Run(Context(level), new ShapeObjectsGenerator.Parameters
            {
                Image = SolidRow(128, 128, new Pixel(255, 0, 0, 255)),
                TargetWidth = 8, TargetHeight = 8, PixelSize = 1f, MergeRuns = true,
            });

            Assert.AreEqual(8, level.Game.Objects.Count, "8 merged rows out of a 128px source");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Texture_MapsColoursToThemeRefsWhenAsked()
        {
            var level = CreateLevel();
            var themeId = ThemeId.NewGuid();
            var matrix = new Color4Value[ValueRules.ThemeCount];
            for (var i = 0; i < matrix.Length; i++) matrix[i] = new Color4Value(0f, 0f, 0f, 1f);
            matrix[7] = new Color4Value(1f, 0f, 0f, 1f);
            level.Resources.Themes[themeId] = new ThemeData(themeId, "test", matrix);

            new ShapeObjectsGenerator().Run(Context(level), new ShapeObjectsGenerator.Parameters
            {
                Image = SolidRow(2, 1, new Pixel(255, 0, 0, 255)),
                TargetWidth = 2, TargetHeight = 1, PixelSize = 1f,
                UseThemeRef = true, Theme = themeId,
            });

            var obj = (Models.Objects.ShapeObject)level.Game.Objects.Values.Single();
            var color = ((Color4Key)obj.Colors[0]).Value;
            Assert.IsInstanceOf<Color4ThemeRef>(color);
            Assert.AreEqual(7, ((Color4ThemeRef)color).ThemeColorIndex, "nearest palette entry");
        }

        // A missing theme must degrade to literal colours rather than writing a reference into a
        // palette that is not there.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Texture_FallsBackToLiteralColoursWhenTheThemeIsMissing()
        {
            var level = CreateLevel();
            new ShapeObjectsGenerator().Run(Context(level), new ShapeObjectsGenerator.Parameters
            {
                Image = SolidRow(2, 1, new Pixel(255, 0, 0, 255)),
                TargetWidth = 2, TargetHeight = 1, PixelSize = 1f,
                UseThemeRef = true, Theme = ThemeId.NewGuid(),
            });

            var obj = (Models.Objects.ShapeObject)level.Game.Objects.Values.Single();
            Assert.IsInstanceOf<Color4Value>(((Color4Key)obj.Colors[0]).Value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Texture_WithoutAnImageProducesNothing()
        {
            var level = CreateLevel();
            var generator = new ShapeObjectsGenerator();
            var parameters = generator.CreateDefaultParameters();

            var context = Context(level);
            var estimate = generator.Estimate(context, parameters);
            generator.Run(context, parameters);

            Assert.AreEqual(0, level.Game.Objects.Count);
            Assert.AreEqual(0, estimate.Objects);
        }

        #endregion
    }
}
