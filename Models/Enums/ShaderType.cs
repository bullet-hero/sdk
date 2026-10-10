namespace BH.SDK.Models.Enums
{
    // Auto is 0: a file from before the field keeps rendering as it did, so no bump. Opaque/Transparent are
    // the two render paths; a consumer deriving Auto must never change how a level looks. Docs/ENUMS.md

    /// <summary>
    /// Which render path a <see cref="Objects.ShapeObject"/> asks for. Opaque writes depth and
    /// ignores alpha, which is far cheaper on a tiled GPU; Transparent blends and does not.
    /// </summary>
    public enum ShaderType : byte
    {
        Auto = 0,
        Opaque = 1,
        Transparent = 2,
    }
}
