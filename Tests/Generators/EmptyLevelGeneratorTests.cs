using BH.SDK.Generators;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // The only shipped generator so far, and the reference implementation of the contract - if
    // something here needs a special case, the contract is wrong, not this class.
    public class EmptyLevelGeneratorTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Create_ProducesAnEmptyLevelWithTheRequestedTimeline()
        {
            var generator = new EmptyLevelGenerator();
            var parameters = (EmptyLevelGenerator.Parameters)generator.CreateDefaultParameters();
            parameters.Framerate = 120;
            parameters.FrameDuration = 2400;

            var (level, meta) = generator.Create(parameters);

            Assert.IsEmpty(level.Game.Objects);
            Assert.AreEqual(120, level.Settings.Fps);
            Assert.AreEqual(2400, level.Settings.FrameDuration);
            Assert.IsNotNull(meta);
        }

        // Name and description are copied, not aliased: the host hands in the live IString its own
        // text field is still bound to, and a level that keeps mutating after creation is a bug
        // that would only show up much later.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Create_CopiesNameAndDescription_RatherThanAliasingThem()
        {
            var generator = new EmptyLevelGenerator();
            var name = new StringValue("original");
            var parameters = new EmptyLevelGenerator.Parameters { LevelName = name };

            var (_, meta) = generator.Create(parameters);
            name.Value = "changed afterwards";

            Assert.AreEqual("original", ((StringValue)meta.LevelName).Value);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Create_ProducesARuleValidLevel()
        {
            var generator = new EmptyLevelGenerator();
            var (level, meta) = generator.Create(generator.CreateDefaultParameters());

            var facade = new ValidationFacade();
            Assert.IsFalse(facade.Validate(level).HasErrors, "generated Level must be rule-valid");
            Assert.IsFalse(facade.Validate(meta).HasErrors, "generated LevelMeta must be rule-valid");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Hints_DeclareTheTimelineRanges()
        {
            var generator = new EmptyLevelGenerator();

            Assert.IsTrue(generator.Hints.TryGetRange(nameof(EmptyLevelGenerator.Parameters.Framerate),
                out var framerate));
            Assert.AreEqual(FrameRules.MinFramerate, framerate.Min);
            Assert.AreEqual(FrameRules.MaxFramerate, framerate.Max);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Create_RejectsTheWrongParametersType()
        {
            var generator = new EmptyLevelGenerator();
            Assert.Throws<System.ArgumentException>(() => generator.Create(new SpawnTestGenerator.Parameters()));
        }
    }
}
