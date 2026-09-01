using Unity.Mathematics;

namespace BH.SDK
{
    // THE TRANSFORM HALF OF BH.Shared.defaults, AND ONLY THAT HALF. Transform2D and RectTransform2D
    // live here now and read their own zero state from these; the consumer's `defaults` keeps every
    // other field it has (color, uv, cameraZoom, shake, layer_int, scaleUniform) and DELEGATES these
    // eight, so there is still one source of truth and every existing `defaults.position` call site is
    // untouched.
    //
    // MOVING `defaults` WHOLE WAS THE ALTERNATIVE AND IT DOES NOT FIT. That type reaches for
    // BH.Shared.alignment and BH.Shared.color, both of which stay in the consumer, and it is what every
    // GamePlayer job falls back to when a per-object keyframe collection is empty - dragging it down
    // here would drag half of Shared with it for eight numbers.
    //
    // These are NOT clamps and do not belong in Rules/: a clamp says what a value may be, and these say
    // what a transform IS before anything is authored onto it.

    /// <summary> The zero state of a 2D transform - what an unauthored one reads as. </summary>
    public static class TransformDefaults
    {
        /// <summary> Anchored position: the rect's own origin. </summary>
        public static readonly float2 Position = new(0f, 0f);

        /// <summary> Draw order contribution, parent-relative like everything else here. </summary>
        public static readonly float Layer = 0f;

        /// <summary> Rotation, in radians. </summary>
        public static readonly float Rotation = 0f;

        /// <summary> Additional local scale, on top of <see cref="Size"/>. </summary>
        public static readonly float2 Scale = new(1f, 1f);

        /// <summary> Logical size of the rect, one world unit square. </summary>
        public static readonly float2 Size = new(1f, 1f);

        /// <summary> Lower anchor - centred, so an unanchored child does not stretch with its parent. </summary>
        public static readonly float2 AnchorMin = new(0.5f, 0.5f);

        /// <summary> Upper anchor, equal to <see cref="AnchorMin"/> for the same reason. </summary>
        public static readonly float2 AnchorMax = new(0.5f, 0.5f);

        /// <summary> Pivot - the centre, which is what makes a rect rotate about itself. </summary>
        public static readonly float2 Pivot = new(0.5f, 0.5f);
    }
}
