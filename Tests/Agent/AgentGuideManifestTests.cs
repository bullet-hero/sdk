using System.Linq;
using BH.SDK.Agent;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The guide's topics: the planned set, unique, each a safe file name. </summary>
    [TestFixture]
    public class AgentGuideManifestTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Topics_AreThePlannedSet_Unique_AndLowercase()
        {
            var names = AgentGuideManifest.Topics.Select(t => t.Name).ToArray();

            CollectionAssert.AreEquivalent(new[]
            {
                "index", "coordinates", "time", "randomness", "prefabs", "shapes", "avatar", "generators", "design",
                "context", "mcp",
            }, names);
            Assert.That(names, Has.All.Matches("^[a-z]+$"));
            Assert.AreEqual(AgentGuideManifest.Index, names[0]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PageTopics_NamePagesUnderDocs_AndGeneratedOnesNone()
        {
            foreach (var topic in AgentGuideManifest.Topics)
            {
                var generated = topic.Name is AgentGuideManifest.Index or AgentGuideManifest.Context;
                Assert.AreEqual(generated, topic.Pages.Count == 0, topic.Name);
                Assert.That(topic.Pages, Has.All.Matches("^docs/[0-9a-z_/-]+\\.md(#[A-Za-z0-9 ,'-]+)?$"), topic.Name);
                Assert.IsFalse(string.IsNullOrWhiteSpace(topic.Summary), topic.Name);
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TryGet_IsExact_AndFilePath_IsUnderTheFolder()
        {
            Assert.IsTrue(AgentGuideManifest.TryGet("time", out var topic));
            Assert.AreEqual("time", topic.Name);
            Assert.IsFalse(AgentGuideManifest.TryGet("Time", out _));
            Assert.IsFalse(AgentGuideManifest.TryGet(null, out _));
            Assert.AreEqual("mcp-guide/time.md", AgentGuideManifest.FilePath("time"));
        }
    }
}