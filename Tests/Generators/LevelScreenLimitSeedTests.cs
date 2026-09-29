using System.Linq;
using BH.SDK.Generators;
using BH.SDK.Generators.Audio;
using BH.SDK.Models;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    /// <summary> A level built from nothing starts pinned to 16:9 on its first frame while
    /// <c>PinScreenAspect</c> is ticked (the default), and carries no limit at all when it is not. </summary>
    public class LevelScreenLimitSeedTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Empty_ByDefault_PinsSixteenByNineOnTheFirstFrame()
        {
            var (level, _) = new EmptyLevelGenerator().Create(new EmptyLevelGenerator.Parameters());
            AssertPinned(level);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Empty_Unticked_HasNoScreenLimit()
        {
            var (level, _) = new EmptyLevelGenerator().Create(
                new EmptyLevelGenerator.Parameters { PinScreenAspect = false });
            Assert.IsEmpty(level.Game.Events.ScreenLimits);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AudioFile_ByDefault_PinsSixteenByNineOnTheFirstFrame()
        {
            var (level, _) = new AudioFileLevelGenerator().Create(new AudioFileLevelGenerator.Parameters
            {
                AudioPath = "song.ogg",
                DurationSeconds = 10f,
            });
            AssertPinned(level);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AudioFile_Unticked_HasNoScreenLimit()
        {
            var (level, _) = new AudioFileLevelGenerator().Create(new AudioFileLevelGenerator.Parameters
            {
                AudioPath = "song.ogg",
                DurationSeconds = 10f,
                PinScreenAspect = false,
            });
            Assert.IsEmpty(level.Game.Events.ScreenLimits);
        }

        private static void AssertPinned(Level level)
        {
            var key = level.Game.Events.ScreenLimits.Single();
            Assert.AreEqual(FrameRules.MinFrame, key.Frame, "the timeline counts from one");

            var limit = key.ScreenLimit as ScreenLimitFixed;
            Assert.IsNotNull(limit, "a fixed limit, not bounds");
            Assert.AreEqual(16, limit.Aspect.Width);
            Assert.AreEqual(9, limit.Aspect.Height);
        }
    }
}
