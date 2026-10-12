using System;
using System.IO;
using BH.SDK.Agent;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The endpoint file: written whole, read back checked, and found at Unity's data path per OS.
    /// </summary>
    [TestFixture]
    public class AgentEndpointTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "bh-agent-endpoint-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Write_ThenTryRead_ReturnsTheSameEndpoint_AndCreatesTheFolder()
        {
            var path = Path.Combine(_directory, "mcp", "endpoint.json");
            var written = AgentEndpoint.Create(51234, 1234, "1.2.0");

            written.Write(path);

            Assert.IsTrue(AgentEndpoint.TryRead(path, out var read, out var error), error);
            Assert.AreEqual(AgentProtocol.Version, read.Protocol);
            Assert.AreEqual(51234, read.Port);
            Assert.AreEqual(written.Token, read.Token);
            Assert.AreEqual(1234, read.Pid);
            Assert.AreEqual("1.2.0", read.Gv);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Write_OverAnExistingFile_ReplacesIt_AndLeavesNoTemporary()
        {
            var path = Path.Combine(_directory, "endpoint.json");
            AgentEndpoint.Create(1000, 1, "a").Write(path);

            AgentEndpoint.Create(2000, 2, "b").Write(path);

            Assert.IsTrue(AgentEndpoint.TryRead(path, out var read, out _));
            Assert.AreEqual(2000, read.Port);
            Assert.AreEqual(1, Directory.GetFiles(_directory).Length);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Write_UsesTheDocumentedKeys_AndNoByteOrderMark()
        {
            var path = Path.Combine(_directory, "endpoint.json");
            AgentEndpoint.Create(51234, 1234, "1.2.0").Write(path);

            var bytes = File.ReadAllBytes(path);
            var text = File.ReadAllText(path);

            Assert.AreEqual((byte)'{', bytes[0]);
            foreach (var key in new[] { "\"protocol\"", "\"port\"", "\"token\"", "\"pid\"", "\"gv\"" })
                StringAssert.Contains(key, text);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TryRead_MissingFile_FailsNamingThePath()
        {
            var path = Path.Combine(_directory, "nothing.json");

            Assert.IsFalse(AgentEndpoint.TryRead(path, out var read, out var error));
            Assert.IsNull(read);
            StringAssert.Contains(path, error);
        }

        [TestCase("not json")]
        [TestCase("{}")]
        [TestCase("{\"protocol\":1,\"port\":0,\"token\":\"0123456789abcdef0123456789abcdef\"}")]
        [TestCase("{\"protocol\":1,\"port\":70000,\"token\":\"0123456789abcdef0123456789abcdef\"}")]
        [TestCase("{\"protocol\":1,\"port\":5000,\"token\":\"short\"}")]
        [TestCase("{\"protocol\":1,\"port\":5000,\"token\":\"zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz\"}")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TryRead_UnusableFile_Fails(string content)
        {
            Directory.CreateDirectory(_directory);
            var path = Path.Combine(_directory, "endpoint.json");
            File.WriteAllText(path, content);

            Assert.IsFalse(AgentEndpoint.TryRead(path, out var read, out var error));
            Assert.IsNull(read);
            Assert.IsNotEmpty(error);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Delete_RemovesTheFile_AndToleratesAMissingOne()
        {
            var path = Path.Combine(_directory, "mcp", "endpoint.json");
            AgentEndpoint.Create(5000, 1, "a").Write(path);

            AgentEndpoint.Delete(path);
            AgentEndpoint.Delete(path);
            AgentEndpoint.Delete(Path.Combine(_directory, "no-such-folder", "endpoint.json"));

            Assert.IsFalse(File.Exists(path));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void InstancePath_IsBesideTheEndpointFile()
        {
            var endpoint = Path.Combine(_directory, "mcp", "endpoint.json");

            Assert.AreEqual(Path.Combine(_directory, "mcp", "instances", "42.json"),
                AgentEndpoint.InstancePath(endpoint, 42));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ListInstances_ReadsValidFilesOnly_ByPid()
        {
            var endpoint = Path.Combine(_directory, "mcp", "endpoint.json");
            AgentEndpoint.Create(5007, 7, "a").Write(AgentEndpoint.InstancePath(endpoint, 7));
            AgentEndpoint.Create(5003, 3, "a").Write(AgentEndpoint.InstancePath(endpoint, 3));
            AgentEndpoint.Create(5008, 8, "a").Write(AgentEndpoint.InstancePath(endpoint, 9));
            File.WriteAllText(Path.Combine(AgentEndpoint.InstancesDirectoryFor(endpoint), "5.json"), "not json");

            var pids = System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.Select(AgentEndpoint.ListInstances(endpoint), e => e.Pid));

            CollectionAssert.AreEqual(new[] { 3, 7 }, pids);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ListInstances_NoFolder_IsEmpty()
        {
            Assert.IsEmpty(AgentEndpoint.ListInstances(Path.Combine(_directory, "mcp", "endpoint.json")));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NewToken_IsHex_OfTheProtocolLength_AndFresh()
        {
            var first = AgentEndpoint.NewToken();

            Assert.AreEqual(AgentProtocol.TokenLength, first.Length);
            Assert.IsTrue(System.Linq.Enumerable.All(first, Uri.IsHexDigit));
            Assert.AreNotEqual(first, AgentEndpoint.NewToken());
        }

        [TestCase(AgentOs.Windows, "C:\\Users\\kostya", "AppData/LocalLow/vertoker/Bullet Hero/mcp/endpoint.json")]
        [TestCase(AgentOs.Linux, "/home/kostya", ".config/unity3d/vertoker/Bullet Hero/mcp/endpoint.json")]
        [TestCase(AgentOs.MacOs, "/Users/kostya",
            "Library/Application Support/vertoker/Bullet Hero/mcp/endpoint.json")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DefaultPath_IsUnitysPersistentDataPath_PerOs(AgentOs os, string home, string relative)
        {
            var expected = Path.Combine(home, Path.Combine(relative.Split('/')));

            Assert.AreEqual(expected, AgentEndpoint.DefaultPath(os, home));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void DefaultPath_ForThisMachine_EndsWithTheEndpointFile()
        {
            StringAssert.EndsWith(Path.Combine("mcp", "endpoint.json"), AgentEndpoint.DefaultPath());
        }
    }
}