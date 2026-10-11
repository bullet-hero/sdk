using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BH.SDK.Agent.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests.Agent
{
    /// <summary> The vision DTOs on the wire: camelCase names, unset geometry left out, requests read from
    /// the documented shape. </summary>
    [TestFixture]
    public class VisionMessagesTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SchematicRequest_ReadsFromTheDocumentedShape()
        {
            const string json = "{\"frame\":\"s:2\",\"region\":{\"x\":-8,\"y\":-4.5,\"width\":16,\"height\":9}," +
                                "\"size\":512,\"labels\":false}";

            var request = JsonConvert.DeserializeObject<ViewSchematicRequest>(json);

            Assert.AreEqual("s:2", (string)request.Frame);
            Assert.AreEqual(-4.5f, request.Region.Y);
            Assert.AreEqual(16f, request.Region.Width);
            Assert.AreEqual(512, request.Size);
            Assert.AreEqual(false, request.Labels);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SchematicResult_RoundTrips_WithItsImageAndStamp()
        {
            var result = new ViewSchematicResult
            {
                Revision = 7, SessionId = "abc", Scope = "level",
                Png = "iVBORw0K", Width = 640, Height = 360, Legend = "frame 120",
                Frame = 120, Region = new AgentRegion { X = -8f, Y = -4.5f, Width = 16f, Height = 9f },
                Shapes = 3, Colliders = 2, Labels = 3,
            };

            var token = JObject.Parse(JsonConvert.SerializeObject(result));
            CollectionAssert.IsSupersetOf(token.Properties().Select(p => p.Name).ToList(),
                new[] { "revision", "sessionId", "scope", "png", "width", "height", "legend", "frame", "region",
                    "shapes", "colliders", "labels" });
            Assert.IsNull(token["hints"], "absent hints are not written");

            var back = token.ToObject<ViewSchematicResult>();
            Assert.AreEqual("iVBORw0K", back.Png);
            Assert.AreEqual(9f, back.Region.Height);
            Assert.AreEqual(2, back.Colliders);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Cursor_OutsideTheFrame_LeavesItsGeometryOut()
        {
            var cursor = new AgentCursor { Id = 4, Type = "shape", Layer = 2, Start = 10, Duration = 60 };

            var token = JObject.Parse(JsonConvert.SerializeObject(cursor));

            Assert.IsNull(token["corners"]);
            Assert.IsNull(token["center"]);
            Assert.IsNull(token["resolvedStart"]);
            Assert.AreEqual(false, (bool)token["inFrame"]);
            Assert.AreEqual(false, (bool)token["plays"]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Cursor_InTheFrame_RoundTripsItsCorners()
        {
            var cursor = new AgentCursor
            {
                Id = 4, InFrame = true, Plays = true, ResolvedStart = 10, ResolvedDuration = 30,
                Corners = new List<AgentPoint> { new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f) },
                Center = new AgentPoint(0.5f, 0.5f), RotationDegrees = 90f,
            };

            var back = JsonConvert.DeserializeObject<AgentCursor>(JsonConvert.SerializeObject(cursor));

            Assert.AreEqual(4, back.Corners.Count);
            Assert.AreEqual(1f, back.Corners[2].Y);
            Assert.AreEqual(30, back.ResolvedDuration);
            Assert.AreEqual(90f, back.RotationDegrees);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void StatsAndExplainAndAtlas_WriteTheirDocumentedNames()
        {
            var stats = JObject.Parse(JsonConvert.SerializeObject(new FrameStatsResult
                { From = 1, To = 100, Step = 5, Lines = { "f1 active:0 shapes:0 colliders:0 hazard:0%" } }));
            Assert.AreEqual(5, (int)stats["step"]);
            Assert.IsNull(stats["note"]);

            var explain = JObject.Parse(JsonConvert.SerializeObject(new ObjectExplainResult
                { Id = 3, Frame = 9, Reason = AgentExplainReason.OffCamera, Detail = "x", Drawn = false }));
            Assert.AreEqual("off_camera", (string)explain["reason"]);

            var atlas = JsonConvert.DeserializeObject<ShapesAtlasRequest>("{\"form\":\"Circle\",\"offset\":10}");
            Assert.AreEqual("Circle", atlas.Form);
            Assert.AreEqual(10, atlas.Offset);
            Assert.IsNull(atlas.Limit);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ExplainReasons_AreUnique_AndAllListed()
        {
            var constants = typeof(AgentExplainReason).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()).ToList();

            CollectionAssert.AllItemsAreUnique(AgentExplainReason.All);
            CollectionAssert.AreEquivalent(constants, AgentExplainReason.All);
        }
    }
}
