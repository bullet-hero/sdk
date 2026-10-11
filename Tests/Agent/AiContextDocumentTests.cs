using BH.SDK.Agent;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The context file's skeleton, and appending under a heading. </summary>
    [TestFixture]
    public class AiContextDocumentTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Skeleton_HoldsTheSixSections_InOrder()
        {
            Assert.AreEqual("# Brief\n\n# Music\n\n# Sections\n\n# Decisions\n\n# Author remarks\n\n# Open questions\n",
                AiContextDocument.Skeleton);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AppendSection_UnderAnExistingHeading_LandsAtTheEndOfThatSection()
        {
            var document = "# Brief\n\nA short level.\n\n# Decisions\n\n- keep it short\n\n# Open questions\n";

            var result = AiContextDocument.AppendSection(document, "decisions", "- no lasers\n");

            Assert.AreEqual("# Brief\n\nA short level.\n\n# Decisions\n\n- keep it short\n\n- no lasers\n\n" +
                            "# Open questions\n", result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AppendSection_IntoTheSkeleton_FillsAnEmptySection()
        {
            var result = AiContextDocument.AppendSection(AiContextDocument.Skeleton, "## Music", "120 BPM");

            StringAssert.Contains("# Music\n\n120 BPM\n\n# Sections\n", result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AppendSection_KeepsSubsectionsInside_AndStopsAtTheSameLevel()
        {
            var document = "# Sections\n\n## Intro\n\nbars 1-8\n\n# Decisions\n";

            var result = AiContextDocument.AppendSection(document, "Sections", "## Drop\n\nbars 9-16");

            Assert.AreEqual("# Sections\n\n## Intro\n\nbars 1-8\n\n## Drop\n\nbars 9-16\n\n# Decisions\n", result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AppendSection_WithNoSuchHeading_AddsATopLevelSectionAtTheEnd()
        {
            Assert.AreEqual("# Brief\n\nx\n\n# Palette\n\nteal on black\n",
                AiContextDocument.AppendSection("# Brief\n\nx\n\n\n", "Palette", "teal on black"));
            Assert.AreEqual("# Palette\n\nteal\n", AiContextDocument.AppendSection(string.Empty, "Palette", "teal"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AppendSection_IgnoresHeadingsInsideCodeFences_AndReadsCrLf()
        {
            var document = "# Notes\r\n\r\n```\r\n# Decisions\r\n```\r\n";

            var result = AiContextDocument.AppendSection(document, "Decisions", "real");

            Assert.AreEqual("# Notes\n\n```\n# Decisions\n```\n\n# Decisions\n\nreal\n", result);
        }
    }
}
