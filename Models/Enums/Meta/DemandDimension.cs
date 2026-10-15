namespace BH.SDK.Models.Enums.Meta
{
    // The index into every demand window and every device coefficient array: reordering or inserting a
    // member changes what stored demands mean, so it raises PerformanceRules.AnalyzerVersion.

    /// <summary> One thing a level asks the device to do, measured as a mean over a demand window. </summary>
    public enum DemandDimension : byte
    {
        /// <summary> Alive objects. </summary>
        Instances = 0,

        /// <summary> Keyframes in the tracks of alive objects. </summary>
        Keyframes = 1,

        /// <summary> Sum of parent chain depth over alive objects. </summary>
        ParentLinks = 2,

        /// <summary> Alive collider shapes, while player collision is on. </summary>
        Colliders = 3,

        /// <summary> Alive text objects. </summary>
        Texts = 4,

        /// <summary> Visible glyphs of alive texts. </summary>
        Glyphs = 5,

        /// <summary> Alive effect instances. </summary>
        Effects = 6,

        /// <summary> Alive transparent shapes, one draw call each. </summary>
        TransparentShapes = 7,

        /// <summary> Distinct (material, mesh) pairs among alive opaque shapes. </summary>
        OpaqueBatches = 8,

        /// <summary> On-screen area of transparent shapes, one full screen being 1. </summary>
        TransparentArea = 9,

        /// <summary> On-screen area of opaque shapes, one full screen being 1. </summary>
        OpaqueArea = 10,

        /// <summary> Active full-screen post-processing passes. </summary>
        PostPasses = 11,
    }

    /// <summary> Facts about <see cref="DemandDimension"/> as a whole. </summary>
    public static class DemandDimensions
    {
        /// <summary> How many dimensions a demand window carries. </summary>
        public const int Count = 12;

        /// <summary> Whether a dimension is measured in screen pixels, so render scale and anti-aliasing
        /// act on it. </summary>
        public static bool IsArea(DemandDimension dimension)
            => dimension == DemandDimension.TransparentArea || dimension == DemandDimension.OpaqueArea;
    }
}
