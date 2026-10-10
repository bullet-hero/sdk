// ReSharper disable InconsistentNaming

using BH.SDK.Rules;
using Unity.Mathematics;

namespace BH.SDK
{
    // Fallbacks for an object with ZERO keyframes in a field (valid data). FrameMath.GetGlobalXxx applies
    // them; any other direct slice reader must too - see BuildInstancesJob for the canonical pattern.
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
        
        // The fallback for an EMPTY size track only: zero, unlike TransformDefaults.Size. A type that wants a
        // visible size gets a keyframe at creation (SizeSeed), because a fallback cannot be deleted.
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