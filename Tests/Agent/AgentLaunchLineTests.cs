using BH.SDK.Agent;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> AgentLaunchLine: one typed line becomes the tokens the game reads. </summary>
    [TestFixture]
    public class AgentLaunchLineTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Split_KeepsAQuotedValueWhole()
        {
            var tokens = AgentLaunchLine.Split("--game --level \"C:\\My Levels\\Volcano\" --lives 1");

            Assert.AreEqual(new[] { "--game", "--level", @"C:\My Levels\Volcano", "--lives", "1" }, tokens);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("    ")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Split_WithNothingTyped_ProducesNoTokens(string line)
        {
            Assert.IsEmpty(AgentLaunchLine.Split(line));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Split_CollapsesRepeatedWhitespace()
        {
            Assert.AreEqual(new[] { "--menu" }, AgentLaunchLine.Split("   --menu\t "));
        }

        // An empty pair of quotes is how a value is deliberately blanked.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Split_KeepsAnEmptyQuotedToken()
        {
            Assert.AreEqual(new[] { "--level", "" }, AgentLaunchLine.Split("--level \"\""));
        }
    }
}
