namespace BH.SDK.Models.Enums.Resources
{
    // A SECOND author axis beside TextureKind, and deliberately not a fifth kind of it. A kind says
    // what the picture IS; this says whether the picture USES the alpha channel its file happens to
    // carry, and the two are independent - an opaque pixel-art tile and an opaque photo are both
    // ordinary content, and neither is expressible if the two axes share one enum.
    //
    // It exists because the question is genuinely undecidable anywhere else. ImageHeaderReader proves
    // a file CANNOT be transparent from 33 bytes (a PNG's colour type, the absence of tRNS), and
    // TextureDecoder already repacks those into an alpha-less format; what nobody can prove cheaply
    // is that an alpha channel which EXISTS is 255 everywhere, since that needs every pixel read -
    // exactly the cost Core's CLAUDE.md rejects. So the author is the only one who can answer, and
    // this is where they answer it.
    //
    // Unlike ShaderType.Auto, whose contract is that it never changes how a level looks, an Opaque
    // claim IS unchecked and a wrong one flattens an authored fade. That is why the editor's resource
    // row reports what the image actually became ("alpha dropped") rather than staying silent.
    //
    // An enum rather than a bool for the reason ShapeObject.ShaderType is one: Cutout - alpha that is
    // only ever 0 or 1, and therefore packable into a one-bit format - is the obvious next member,
    // and two booleans that can contradict each other is what this shape avoids.

    /// <summary>
    /// What the level's author states about an image's transparency - the one thing about a picture
    /// that a device can see the file for and still not know.
    /// </summary>
    public enum TextureAlpha : byte
    {
        /// <summary>
        /// Not stated. The device decides from the file's own header alone: a container that cannot
        /// hold a transparent pixel loses its alpha channel, anything else keeps one. The default,
        /// and correct for every image nobody has thought about.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Fully opaque: the alpha channel, if the file has one at all, is unused. The image is
        /// repacked without it - the same pixels, drawn faster and in less memory, on the opaque
        /// render path. Untrue of an image that really does fade, and nothing checks.
        /// </summary>
        Opaque = 1,
    }
}
