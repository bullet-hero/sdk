// ReSharper disable InconsistentNaming

using BH.SDK.Rules;
using Unity.Mathematics;

namespace BH.SDK
{
    // Fallback values for when an object has ZERO keyframes for a given field (an empty
    // Positions/Sizes/Rotations/etc. list is valid, sanctioned data - not a bug or missing data).
    // FrameMath.GetGlobalXxx(..., ref slice, ..., defaults.xxx) already returns this fallback
    // whenever slice.Length == 0, so any OTHER code reading a per-object keyframe slice (e.g.
    // TryGetSlice + slice[0]) must apply the same fallback explicitly instead of assuming at
    // least one keyframe exists - see BuildInstancesJob for the canonical read pattern.
    public struct defaults
    {
        // The transform fields DELEGATE to TransformDefaults rather than restating it: that type is the
        // zero state of a transform, this one is the name every consumer reads, and a number has one
        // owner. `size` is the exception and is NOT delegated - see its own note below.
        
        public static readonly float2 position = TransformDefaults.Position;
        public static readonly float layer = TransformDefaults.Layer;
        public static readonly int layer_int = 0;
        public static readonly float rotation = TransformDefaults.Rotation;
        public static readonly float2 scale = TransformDefaults.Scale;
        public static readonly float scaleUniform = 1f;
        
        // THE FALLBACK FOR AN EMPTY SIZE TRACK, AND NOTHING ELSE - which is why it is zero while
        // TransformDefaults.Size is one. The two were the same number and read as the same question,
        // and they are not: a TRANSFORM with nothing authored on it is a unit square (what the gizmo
        // handles, the collider overlays and RectTransform2D.Default ask for), while an OBJECT with no
        // size keyframes is a group node with no extent of its own. A type that wants the unit square
        // gets an explicit keyframe written at creation instead (GameEditor's SizeSeed), because a
        // seed can be deleted by the author and a fallback never can.
        public static readonly float2 size = new(0f, 0f);
        
        public static readonly float cameraZoom = ValueRules.DefaultZoom;
        public static readonly float2 anchor_min = TransformDefaults.AnchorMin;
        public static readonly float2 anchor_max = TransformDefaults.AnchorMax;
        public static readonly float2 pivot = TransformDefaults.Pivot;
        
        public static readonly float4 color = SDK.color.white;
        public static readonly (float4, float4) color2 = (color, color);
        public static readonly float4x4 color4x4 = new(color, color, color, color);
        public static readonly float2 shake = new(0f, 0f);
        
        // (x, y) - tilling, (z, w) - offset
        public static readonly float4 uv = new(1f, 1f, 0f, 0f);
    }
}