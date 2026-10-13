using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent.Messages
{
    /// <summary> A point in world units. </summary>
    public sealed class AgentPoint
    {
        /// <summary> Horizontal coordinate. </summary>
        [JsonProperty("x")]
        public float X { get; set; }

        /// <summary> Vertical coordinate, up positive. </summary>
        [JsonProperty("y")]
        public float Y { get; set; }

        /// <summary> The origin. </summary>
        public AgentPoint()
        {
        }

        /// <summary> Built from its coordinates. </summary>
        public AgentPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary> An axis-aligned rect in world units: its lower-left corner and its size. </summary>
    public sealed class AgentRegion
    {
        /// <summary> Left edge. </summary>
        [JsonProperty("x")]
        public float X { get; set; }

        /// <summary> Bottom edge. </summary>
        [JsonProperty("y")]
        public float Y { get; set; }

        /// <summary> Width, positive. </summary>
        [JsonProperty("width")]
        public float Width { get; set; }

        /// <summary> Height, positive. </summary>
        [JsonProperty("height")]
        public float Height { get; set; }
    }

    /// <summary> A result that carries one picture: PNG bytes as base64, its size and one legend line.
    /// </summary>
    public class AgentImageResult : AgentResultBase
    {
        /// <summary> The PNG file, base64. </summary>
        [JsonProperty("png")]
        public string Png { get; set; }

        /// <summary> Width in pixels. </summary>
        [JsonProperty("width")]
        public int Width { get; set; }

        /// <summary> Height in pixels. </summary>
        [JsonProperty("height")]
        public int Height { get; set; }

        /// <summary> What the picture shows and what its colours mean. </summary>
        [JsonProperty("legend")]
        public string Legend { get; set; }
    }

    /// <summary> The <c>view.schematic</c> request. </summary>
    public sealed class ViewSchematicRequest
    {
        /// <summary> The frame to draw, a <c>TimeAddress</c>. </summary>
        [JsonProperty("frame")]
        public JToken Frame { get; set; }

        /// <summary> The world rect to draw; absent for the camera's. </summary>
        [JsonProperty("region", NullValueHandling = NullValueHandling.Ignore)]
        public AgentRegion Region { get; set; }

        /// <summary> The longest side in pixels, capped by the game's image limit. </summary>
        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public int? Size { get; set; }

        /// <summary> Whether to write <c>#id</c> on objects; true when absent. </summary>
        [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Labels { get; set; }
    }

    /// <summary> The <c>view.schematic</c> answer. </summary>
    public sealed class ViewSchematicResult : AgentImageResult
    {
        /// <summary> The global frame drawn. </summary>
        [JsonProperty("frame")]
        public int Frame { get; set; }

        /// <summary> The world rect the picture covers. </summary>
        [JsonProperty("region")]
        public AgentRegion Region { get; set; }

        /// <summary> Shapes drawn. </summary>
        [JsonProperty("shapes")]
        public int Shapes { get; set; }

        /// <summary> Colliders outlined. </summary>
        [JsonProperty("colliders")]
        public int Colliders { get; set; }

        /// <summary> Labels written. </summary>
        [JsonProperty("labels")]
        public int Labels { get; set; }
    }

    /// <summary> The <c>frame.stats</c> request; every field is optional. </summary>
    public sealed class FrameStatsRequest
    {
        /// <summary> The first frame, a <c>TimeAddress</c>; the level's first frame when absent. </summary>
        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        public JToken From { get; set; }

        /// <summary> The last frame, a <c>TimeAddress</c>; the level's last frame when absent. </summary>
        [JsonProperty("to", NullValueHandling = NullValueHandling.Ignore)]
        public JToken To { get; set; }

        /// <summary> Frames between two samples, a <c>TimeAddress</c> duration; 1 when absent. </summary>
        [JsonProperty("step", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Step { get; set; }
    }

    /// <summary> The <c>frame.stats</c> answer: one line per sampled frame. </summary>
    public sealed class FrameStatsResult : AgentResultBase
    {
        /// <summary> The first frame sampled. </summary>
        [JsonProperty("from")]
        public int From { get; set; }

        /// <summary> The last frame of the range. </summary>
        [JsonProperty("to")]
        public int To { get; set; }

        /// <summary> The step actually used; wider than asked when the range held too many samples. </summary>
        [JsonProperty("step")]
        public int Step { get; set; }

        /// <summary> <c>f120 active:34 shapes:20 colliders:12 hazard:8.5%</c>, one per sample. </summary>
        [JsonProperty("lines")]
        public List<string> Lines { get; set; } = new List<string>();

        /// <summary> Why the range or the step differs from the request; absent when it does not. </summary>
        [JsonProperty("note", NullValueHandling = NullValueHandling.Ignore)]
        public string Note { get; set; }
    }

    /// <summary> The <c>cursor.resolve</c> request. </summary>
    public sealed class CursorResolveRequest
    {
        /// <summary> The objects to place, at most <see cref="AgentRules.MaxPageSize"/>. </summary>
        [JsonProperty("ids")]
        public List<int> Ids { get; set; } = new List<int>();

        /// <summary> The frame, a <c>TimeAddress</c>. </summary>
        [JsonProperty("frame")]
        public JToken Frame { get; set; }
    }

    /// <summary> Where one object is at one frame. </summary>
    public sealed class AgentCursor
    {
        /// <summary> The object. </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary> Its type, as the outline names it. </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary> Its effective layer: its own plus every ancestor's. </summary>
        [JsonProperty("layer")]
        public int Layer { get; set; }

        /// <summary> The authored span's first frame. </summary>
        [JsonProperty("start")]
        public int Start { get; set; }

        /// <summary> The authored span's length in frames. </summary>
        [JsonProperty("duration")]
        public int Duration { get; set; }

        /// <summary> The first frame it really plays on, clipped by its ancestors; absent when it never does.
        /// </summary>
        [JsonProperty("resolvedStart", NullValueHandling = NullValueHandling.Ignore)]
        public int? ResolvedStart { get; set; }

        /// <summary> The length of the span it really plays on; absent when it never plays. </summary>
        [JsonProperty("resolvedDuration", NullValueHandling = NullValueHandling.Ignore)]
        public int? ResolvedDuration { get; set; }

        /// <summary> Whether the resolved span holds the frame. </summary>
        [JsonProperty("plays")]
        public bool Plays { get; set; }

        /// <summary> Whether it and every ancestor are active. </summary>
        [JsonProperty("active")]
        public bool Active { get; set; }

        /// <summary> Whether the built frame holds it; the geometry below is absent when not. </summary>
        [JsonProperty("inFrame")]
        public bool InFrame { get; set; }

        /// <summary> The rect's corners in world units: lower-left, lower-right, upper-right, upper-left
        /// before rotation. </summary>
        [JsonProperty("corners", NullValueHandling = NullValueHandling.Ignore)]
        public List<AgentPoint> Corners { get; set; }

        /// <summary> The rect's centre in world units. </summary>
        [JsonProperty("center", NullValueHandling = NullValueHandling.Ignore)]
        public AgentPoint Center { get; set; }

        /// <summary> Its pivot point in world units - what a world-space position key sets. </summary>
        [JsonProperty("pivot", NullValueHandling = NullValueHandling.Ignore)]
        public AgentPoint Pivot { get; set; }

        /// <summary> Width and height after scale, in world units. </summary>
        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public AgentPoint Size { get; set; }

        /// <summary> The composed rotation in radians, counter-clockwise: the unit a rotation key takes.
        /// </summary>
        [JsonProperty("rotation", NullValueHandling = NullValueHandling.Ignore)]
        public float? Rotation { get; set; }

        /// <summary> The same rotation in degrees, for reading only. </summary>
        [JsonProperty("rotationDegrees", NullValueHandling = NullValueHandling.Ignore)]
        public float? RotationDegrees { get; set; }
    }

    /// <summary> The <c>cursor.resolve</c> answer. </summary>
    public sealed class CursorResolveResult : AgentResultBase
    {
        /// <summary> The global frame resolved. </summary>
        [JsonProperty("frame")]
        public int Frame { get; set; }

        /// <summary> One entry per found id, in request order. </summary>
        [JsonProperty("objects")]
        public List<AgentCursor> Objects { get; set; } = new List<AgentCursor>();

        /// <summary> Requested ids that do not exist. </summary>
        [JsonProperty("missing", NullValueHandling = NullValueHandling.Ignore)]
        public List<int> Missing { get; set; }
    }

    /// <summary> The <c>object.explain</c> request. </summary>
    public sealed class ObjectExplainRequest
    {
        /// <summary> The object. </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary> The frame, a <c>TimeAddress</c>. </summary>
        [JsonProperty("frame")]
        public JToken Frame { get; set; }
    }

    /// <summary> The <c>object.explain</c> answer: the first reason that applies, in a fixed order. </summary>
    public sealed class ObjectExplainResult : AgentResultBase
    {
        /// <summary> The object. </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary> The global frame asked about. </summary>
        [JsonProperty("frame")]
        public int Frame { get; set; }

        /// <summary> One of <see cref="AgentExplainReason"/>. </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }

        /// <summary> The reason as a sentence with its numbers. </summary>
        [JsonProperty("detail")]
        public string Detail { get; set; }

        /// <summary> True only for <see cref="AgentExplainReason.Drawn"/>. </summary>
        [JsonProperty("drawn")]
        public bool Drawn { get; set; }
    }

    /// <summary> The reasons <c>object.explain</c> answers with. An object missing from the frame is
    /// <see cref="CapacityOverflow"/> early; one dropped from a shape pool is, last. </summary>
    public static class AgentExplainReason
    {
        /// <summary> The frame is outside the object's own span. </summary>
        public const string OutsideSpan = "outside_span";

        /// <summary> Its own span holds the frame, an ancestor's clips it away. </summary>
        public const string ResolvedAway = "resolved_away";

        /// <summary> Its parent is missing, or not in the frame. </summary>
        public const string Orphan = "orphan";

        /// <summary> The frame outgrew this device's capacity and dropped it, from the frame or its pool.
        /// </summary>
        public const string CapacityOverflow = "capacity_overflow";

        /// <summary> It plays, yet the built frame does not hold it, with no overflow to blame. </summary>
        public const string NotInFrame = "not_in_frame";

        /// <summary> A rect object: a group that draws nothing itself. </summary>
        public const string NotDrawable = "not_drawable";

        /// <summary> Its width or height is zero. </summary>
        public const string ZeroSize = "zero_size";

        /// <summary> It or an ancestor is inactive. </summary>
        public const string Inactive = "inactive";

        /// <summary> A shape with no mesh: a hitbox only. </summary>
        public const string NoMesh = "no_mesh";

        /// <summary> An effect object whose effect id names no effect. </summary>
        public const string NoEffect = "no_effect";

        /// <summary> Every corner of its colour has alpha 0. </summary>
        public const string Transparent = "transparent";

        /// <summary> It lies wholly outside the camera rect. </summary>
        public const string OffCamera = "off_camera";

        /// <summary> It is drawn, but an opaque shape that fills its rect draws over the whole of it. </summary>
        public const string Covered = "covered";

        /// <summary> Nothing stops it being drawn. </summary>
        public const string Drawn = "drawn";

        /// <summary> Every reason. </summary>
        public static readonly string[] All =
        {
            OutsideSpan, ResolvedAway, Orphan, NotInFrame, NotDrawable, ZeroSize, Inactive, NoMesh, NoEffect,
            Transparent, OffCamera, CapacityOverflow, Covered, Drawn,
        };
    }

    /// <summary> The <c>shapes.atlas</c> request: the <c>shapes.find</c> filters, paged. </summary>
    public sealed class ShapesAtlasRequest : AgentPagedRequest
    {
        /// <summary> The geometric form, as the shape catalog names it. </summary>
        [JsonProperty("form", NullValueHandling = NullValueHandling.Ignore)]
        public string Form { get; set; }

        /// <summary> The sector, as the shape catalog names it. </summary>
        [JsonProperty("sector", NullValueHandling = NullValueHandling.Ignore)]
        public string Sector { get; set; }

        /// <summary> The thickness, as the shape catalog names it. </summary>
        [JsonProperty("thickness", NullValueHandling = NullValueHandling.Ignore)]
        public string Thickness { get; set; }

        /// <summary> A name fragment, case-insensitive. </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }

    /// <summary> The <c>shapes.atlas</c> answer: numbered cells, and a line per number naming its shape.
    /// </summary>
    public sealed class ShapesAtlasResult : AgentImageResult
    {
        /// <summary> Shapes matching, before paging. </summary>
        [JsonProperty("total")]
        public int Total { get; set; }

        /// <summary> Shapes skipped. </summary>
        [JsonProperty("offset")]
        public int Offset { get; set; }

        /// <summary> <c>n &lt;shape id&gt; Name (display)</c>, where n is the number under the cell. </summary>
        [JsonProperty("lines")]
        public List<string> Lines { get; set; } = new List<string>();
    }
}