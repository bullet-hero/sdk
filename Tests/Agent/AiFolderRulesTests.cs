using System.Collections.Generic;
using BH.SDK.Agent;
using BH.SDK.Models;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> What a level's <c>ai/</c> folder may hold, what of it travels, and the manifest's shape.
    /// </summary>
    [TestFixture]
    public class AiFolderRulesTests
    {
        [TestCase("CONTEXT.md")]
        [TestCase("notes.md")]
        [TestCase("plan-2_final.json")]
        [TestCase("manifest.json")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CheckName_AcceptsAPlainTextName(string name)
        {
            Assert.IsNull(AiFolderRules.CheckName(name));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("context.md")]
        [TestCase("Notes.md")]
        [TestCase("sub/notes.md")]
        [TestCase("sub\\notes.md")]
        [TestCase("../notes.md")]
        [TestCase("notes.txt")]
        [TestCase("notes")]
        [TestCase(".md")]
        [TestCase("con.md")]
        [TestCase("notes .md")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CheckName_RefusesAnythingElse_WithAReason(string name)
        {
            Assert.IsNotEmpty(AiFolderRules.CheckName(name));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CheckName_RefusesANameTooLongForAnArchive()
        {
            Assert.IsNotNull(AiFolderRules.CheckName(new string('a', 120) + ".md"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Sizes_AreLimitedPerFileAndPerFolder()
        {
            Assert.IsNull(AiFolderRules.CheckSize(AgentRules.MaxContextFileBytes));
            Assert.IsNotNull(AiFolderRules.CheckSize(AgentRules.MaxContextFileBytes + 1));
            Assert.IsNull(AiFolderRules.CheckFolder(AgentRules.MaxContextFolderBytes));
            Assert.IsNotNull(AiFolderRules.CheckFolder(AgentRules.MaxContextFolderBytes + 1));
        }

        [TestCase("ai/CONTEXT.md", "CONTEXT.md")]
        [TestCase("ai/sub/notes.md", null)]
        [TestCase("resources/notes.md", null)]
        [TestCase("aii/notes.md", null)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NameOf_AnswersOnlyForAFileDirectlyInsideAi(string storePath, string expected)
        {
            Assert.AreEqual(expected, AiFolderRules.NameOf(storePath));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void StorePath_IsUnderTheAiFolder()
        {
            Assert.AreEqual("ai/CONTEXT.md", AiFolderRules.StorePath(FileNames.AiContextFile));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SelectShareable_SortsSkipsRefusedFiles_AndStopsAtTheFolderLimit()
        {
            const int max = AgentRules.MaxContextFileBytes;
            var files = new Dictionary<string, long>
            {
                ["e.md"] = 10,
                ["c.md"] = max - 20,
                ["CONTEXT.md"] = max,
                ["bad.txt"] = 10,
                ["big.md"] = max + 1,
                ["a.md"] = max,
                ["b.md"] = max,
                ["d.md"] = 100,
            };

            var selected = AiFolderRules.SelectShareable(files);

            // Four files fill the folder to 20 bytes short of its limit: d.md does not fit, e.md does.
            Assert.AreEqual(AgentRules.MaxContextFolderBytes, 4 * max);
            CollectionAssert.AreEqual(new[] { "CONTEXT.md", "a.md", "b.md", "c.md", "e.md" }, selected);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Manifest_DefaultsToShared_AndRoundTripsWithCamelCaseKeys()
        {
            var json = new AiFolderManifest().ToJson();

            StringAssert.Contains("\"format\": 1", json);
            StringAssert.Contains("\"share\": true", json);

            Assert.IsTrue(AiFolderManifest.TryParse("{ \"format\": 1, \"share\": false }", out var manifest,
                out var error), error);
            Assert.AreEqual(AiFolderManifest.CurrentFormat, manifest.Format);
            Assert.IsFalse(manifest.Share);
        }

        [TestCase("")]
        [TestCase("{ not json")]
        [TestCase("{ \"share\": \"maybe\" }")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Manifest_TryParse_RefusesWhatIsNotOne(string json)
        {
            Assert.IsFalse(AiFolderManifest.TryParse(json, out _, out var error));
            Assert.IsNotEmpty(error);
        }
    }
}
