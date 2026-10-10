using System.Linq;
using System.Reflection;
using BH.SDK.Agent;
using BH.SDK.Agent.Messages;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The names both ends must agree on: unique, complete, and readable on the wire. </summary>
    [TestFixture]
    public class AgentProtocolTests
    {
        private static string[] MethodNames() => typeof(AgentProtocol.Methods)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (string)f.GetRawConstantValue())
            .ToArray();

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void MethodNames_AreUnique_AndAreaDotVerb()
        {
            var names = MethodNames();

            CollectionAssert.AllItemsAreUnique(names);
            Assert.That(names.Where(n => n != AgentProtocol.Methods.Hello),
                Has.All.Matches("^[a-z]+(_[a-z]+)*\\.[a-z]+$"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ToolName_ReplacesTheDotWithAnUnderscore()
        {
            Assert.AreEqual("level_outline", AgentProtocol.ToolName(AgentProtocol.Methods.LevelOutline));
            Assert.AreEqual("hello", AgentProtocol.ToolName(AgentProtocol.Methods.Hello));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ErrorCodes_AndHintKeys_AreUnique_AndListedInAll()
        {
            CollectionAssert.AllItemsAreUnique(AgentErrorCode.All);
            CollectionAssert.AllItemsAreUnique(AgentHintKey.All);

            var codes = typeof(AgentErrorCode).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue());
            CollectionAssert.AreEquivalent(codes, AgentErrorCode.All);

            var keys = typeof(AgentHintKey).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue());
            CollectionAssert.AreEquivalent(keys, AgentHintKey.All);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Rules_HoldThePlannedValues()
        {
            Assert.AreEqual(256, AgentRules.ChangeLogCapacity);
            Assert.AreEqual(60, AgentRules.CallTimeoutSeconds);
            Assert.AreEqual(20, AgentRules.CaptureTimeoutSeconds);
            Assert.AreEqual(2, AgentRules.EffectSettleFrames);
            Assert.LessOrEqual(AgentRules.DefaultPageSize, AgentRules.MaxPageSize);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Response_WithAnError_WritesCamelCaseKeys_AndOmitsTheUnset()
        {
            var response = new AgentResponse
            {
                Id = 7,
                Error = new AgentRpcError
                {
                    Code = AgentProtocol.JsonRpcCode.Application,
                    Message = "stale",
                    Data = new AgentErrorData { Code = AgentErrorCode.Stale, Revision = 42 },
                },
            };

            var text = JsonConvert.SerializeObject(response);

            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"id\":7,\"error\":{\"code\":-32000,\"message\":\"stale\"," +
                            "\"data\":{\"code\":\"stale\",\"revision\":42}}}", text);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EditBatchRequest_ReadsFromTheDocumentedShape()
        {
            const string json = "{\"expectedRevision\":41,\"caption\":\"Ring\",\"ops\":[{\"op\":\"create\"}]}";

            var request = JsonConvert.DeserializeObject<EditBatchRequest>(json);

            Assert.AreEqual(41, request.ExpectedRevision);
            Assert.AreEqual("Ring", request.Caption);
            Assert.AreEqual("create", (string)request.Ops[0]["op"]);
        }
    }
}