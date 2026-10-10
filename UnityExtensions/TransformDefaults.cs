using Unity.Mathematics;

namespace BH.SDK
{
    // The zero state of a TRANSFORM, and only that. Size stays (1, 1) here while defaults.size is zero (an
    // object with no size keyframes): two questions, not one. Not clamps, so not Rules/
    // (UnityExtensions/CLAUDE.md).

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

        /// <summary> Logical size of the rect, one world unit square - the zero state of a TRANSFORM,
        /// and never the fallback for an empty size track (see the note above). </summary>
        public static readonly float2 Size = new(1f, 1f);

        /// <summary> Lower anchor - centred, so an unanchored child does not stretch with its parent. </summary>
        public static readonly float2 AnchorMin = new(0.5f, 0.5f);

        /// <summary> Upper anchor, equal to <see cref="AnchorMin"/> for the same reason. </summary>
        public static readonly float2 AnchorMax = new(0.5f, 0.5f);

        /// <summary> Pivot - the centre, which is what makes a rect rotate about itself. </summary>
        public static readonly float2 Pivot = new(0.5f, 0.5f);
    }
}
