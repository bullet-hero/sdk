namespace BH.SDK.Models.Enums
{
    // Auto is 0 on purpose, so a file written before this field existed deserializes into it and
    // keeps rendering exactly as it did. That is also why the domain never bumped its generation:
    // "absent" and "Auto" have to mean the same thing for this to need no migration.
    //
    // The three values are not three shaders. Opaque and Transparent name the two render paths a
    // consumer actually has; Auto names the absence of an authored choice and leaves the consumer
    // to derive one from the object's own data. A consumer that derives it must never change how a
    // level looks - an object is only Auto-Opaque when every alpha it can resolve to is 1.

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
