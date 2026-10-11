using System.Collections.Generic;
using System.Linq;
using BH.SDK.Agent;
using BH.SDK.Agent.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The music DTOs on the wire: camelCase names, optional answers left out, requests read from
    /// the documented shape. </summary>
    [TestFixture]
    public class MusicMessagesTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void BeatgridApplyRequest_ReadsFromTheDocumentedShape()
        {
            const string json = "{\"expectedRevision\":4,\"caption\":\"grid\",\"segments\":[{\"start\":\"s:0.5\"," +
                                "\"duration\":\"8bars\",\"bpm\":128.5,\"offset\":-12.25,\"beatsPerBar\":3}]}";

            var request = JsonConvert.DeserializeObject<BeatgridApplyRequest>(json);

            Assert.AreEqual(4, request.ExpectedRevision);
            Assert.AreEqual("grid", request.Caption);
            Assert.AreEqual(1, request.Segments.Count);
            Assert.AreEqual("s:0.5", (string)request.Segments[0].Start);
            Assert.AreEqual("8bars", (string)request.Segments[0].Duration);
            Assert.AreEqual(128.5f, request.Segments[0].Bpm);
            Assert.AreEqual(-12.25f, request.Segments[0].Offset);
            Assert.AreEqual(3, request.Segments[0].BeatsPerBar);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnalyzeResult_WritesCamelCase_AndRoundTrips()
        {
            var result = new MusicAnalyzeResult
            {
                Revision = 3, SessionId = "s", Scope = "level", Resource = -1, Placement = "audio#1", Bpm = 120,
                PhaseFrame = 16, BeatsPerBar = 4, DownbeatFrame = 46, Cached = true,
                Sections = new List<MusicSection> { new() { Letter = "A", Energy = "high", Start = 1, Duration = 480 } },
                Onsets = new Dictionary<string, int> { ["low"] = 4 },
                Notes = new List<string> { "note" },
            };

            var token = JObject.Parse(JsonConvert.SerializeObject(result));
            CollectionAssert.IsSupersetOf(token.Properties().Select(p => p.Name).ToList(),
                new[] { "revision", "sessionId", "scope", "resource", "placement", "durationSeconds", "bpm",
                    "phaseFrame", "confidence", "ambiguity", "driftMs", "beatsPerBar", "downbeatFrame",
                    "downbeatConfidence", "sections", "onsets", "notes", "cached" });
            Assert.IsNull(token["sections"][0]["bar"], "a section off the grid has no bar");

            var back = token.ToObject<MusicAnalyzeResult>();
            Assert.AreEqual(46, back.DownbeatFrame);
            Assert.AreEqual(480, back.Sections[0].Duration);
            Assert.AreEqual(4, back.Onsets["low"]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ResolveResult_OffTheGrid_WritesOnlyTheNote()
        {
            var token = JObject.Parse(JsonConvert.SerializeObject(
                new MusicResolveResult { Frame = 700, Seconds = 11.65, Note = "outside" }));

            Assert.IsNull(token["bar"]);
            Assert.IsNull(token["beat"]);
            Assert.AreEqual("outside", (string)token["note"]);
            Assert.AreEqual(700, (int)token["frame"]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PlayLimit_StaysUnderTheCallTimeout()
        {
            Assert.Less(AgentRules.MaxPlaySeconds, AgentRules.CallTimeoutSeconds);
            Assert.GreaterOrEqual(AgentRules.DefaultBeatTolerance, 0);
        }
    }
}
