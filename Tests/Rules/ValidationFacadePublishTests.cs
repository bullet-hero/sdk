using System;
using BH.SDK.Publishing;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // THE THIRD PASS, AND THE ONE MISTAKE IT EXISTS TO PREVENT. Publishing is the only caller that
    // needs all three analyzers, and the only one that would get them wrong by hand: LevelMeta is
    // its own aggregate root, so `Validate(level)` never touches a single rule on it - and the
    // metadata is exactly the half a publish check is about. Every test here is ultimately about
    // that: the metadata is validated, with or without a level beside it.

    [TestFixture]
    public class ValidationFacadePublishTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ValidateForPublish_OfValidContent_ReportsNoContentFindings()
        {
            var report = new ValidationFacade().ValidateForPublish(
                MockData.CreateTestLevelMeta(), PublishProfile.CreateOpen(),
                MockData.CreateTestLevel());

            Assert.That(report.Content.IsValid, Is.True, report.Content.ToString());
            Assert.That(report.Publish.LevelInspected, Is.True);
        }

        // THE POINT OF THE METHOD, in one assertion: a rule broken in metadata.json is reported even
        // though the level is fine. Validate(level) cannot see it, and a caller reaching for the
        // facade before publishing would never learn that.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ValidateForPublish_ReportsMetadataFindings_ThatAValidLevelWouldHide()
        {
            var report = new ValidationFacade().ValidateForPublish(
                MockData.CreateInvalidTestLevelMeta(), PublishProfile.CreateOpen(),
                MockData.CreateTestLevel());

            Assert.That(report.Content.RuleIssues, Is.Not.Empty);
            Assert.That(report.Content.GraphIssues, Is.Empty, "a valid level has no graph findings");
            Assert.That(report.IsReady, Is.False);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ValidateForPublish_ReportsLevelFindingsToo()
        {
            var withLevel = new ValidationFacade().ValidateForPublish(
                MockData.CreateTestLevelMeta(), PublishProfile.CreateOpen(),
                MockData.CreateInvalidTestLevel());

            Assert.That(withLevel.Content.RuleIssues, Is.Not.Empty,
                "the level's own rules were not walked");
        }

        // A METADATA-ONLY PASS IS A FIRST-CLASS ANSWER, not a degraded one: metadata.json is a
        // separate file precisely so a catalogue can grade thousands of levels without opening one.
        // What it cannot know is flagged rather than guessed.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ValidateForPublish_WithoutALevel_StillValidatesTheMetadata()
        {
            var report = new ValidationFacade().ValidateForPublish(
                MockData.CreateInvalidTestLevelMeta(), PublishProfile.CreateOpen());

            Assert.That(report.Content.RuleIssues, Is.Not.Empty);
            Assert.That(report.Publish.LevelInspected, Is.False);
            Assert.That(report.IsReady, Is.False,
                "a pass that never opened the level cannot conclude it is ready");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ValidateForPublish_RefusesMissingInputs()
        {
            var facade = new ValidationFacade();

            Assert.Throws<ArgumentNullException>(
                () => facade.ValidateForPublish(null, PublishProfile.CreateOpen()));
            Assert.Throws<ArgumentNullException>(
                () => facade.ValidateForPublish(MockData.CreateTestLevelMeta(), null));
        }

        // Nothing is repaired on this path, and that is deliberate rather than unimplemented:
        // ValidateAndFix is for content an author owns, while a publish check looks at content on
        // its way out, where a silent repair is the last thing anyone wants.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ValidateForPublish_RepairsNothing()
        {
            var meta = MockData.CreateInvalidTestLevelMeta();
            var facade = new ValidationFacade();

            var first = facade.ValidateForPublish(meta, PublishProfile.CreateOpen());
            var second = facade.ValidateForPublish(meta, PublishProfile.CreateOpen());

            Assert.That(second.Content.RuleIssues.Count, Is.EqualTo(first.Content.RuleIssues.Count),
                "the first pass changed the metadata it was asked about");
        }
    }
}
