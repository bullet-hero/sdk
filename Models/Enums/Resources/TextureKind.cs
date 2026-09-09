namespace BH.SDK.Models.Enums.Resources
{
    // The first of the author's answers about an image, and the one every other answer defaults FROM.
    // Every value here says "what kind of picture is this", never "what should the device do with it":
    // a level must play the same on a phone and on a workstation, so an author who could pin the
    // memory format would be authoring the device's budget as well as their own level. What the
    // device does with each kind is the player's own setting (UserSettings.Graphics.Textures), and
    // the two meet in one place - Core's TextureLoadPlanner.
    //
    // The technical consequences each kind carries are therefore stated as INTENT, not as format:
    // "resampling this destroys it" is a property of pixel art, true on every device, while
    // "compress it to BC1" is a property of one device on one day.
    //
    // WHAT A KIND NO LONGER DOES IS VETO ON ITS OWN BEHALF, except mip-maps. It used to carry two
    // unrelated statements at once - what the picture is, and whether a device may compress or
    // point-sample it - so an author wanting one of those answers had to accept the other, and an
    // author wanting a look the vocabulary had no word for could only lie about the content.
    // TextureSampling and TextureCompressionKind are those two halves as axes of their own; a kind
    // SEEDS both (their Auto reads it), which is why nothing authored before them changed.

    /// <summary>
    /// What an image IS, as the level's author sees it. Purely artistic - it names the content, and
    /// the player's own graphics settings decide what the device does with content of that kind.
    /// </summary>
    public enum TextureKind : byte
    {
        /// <summary>
        /// Not stated. The device decides from the file itself, and treats it as conservatively as
        /// <see cref="Graphic"/> - the default for every image nobody has classified, which is most
        /// of them.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// A photograph or a painted backdrop: continuous tone, no pixel the eye can point at. The
        /// most forgiving kind - it survives resampling and block compression better than anything
        /// else, so a device under pressure turns this down first.
        /// </summary>
        Photo = 1,

        /// <summary>
        /// A drawing, a logo, an icon, a UI-like sprite: flat areas with hard edges, possibly with
        /// text in it. Smoothed when scaled, but the hard edges are what block compression damages
        /// first, so a device compresses it more carefully than a <see cref="Photo"/>.
        /// </summary>
        Graphic = 2,

        /// <summary>
        /// Pixel art: every pixel is authored, and blurring one is a defect rather than a
        /// trade-off. Seeds <see cref="TextureSampling.Sharp"/> and a compression refusal, and is
        /// never mip-mapped whatever any setting says - a lower mip level is a blurred copy. It is
        /// also the one kind that refuses to be AVERAGED while being scaled down, which no other
        /// axis can ask for or take away.
        /// </summary>
        PixelArt = 3,

        /// <summary>
        /// A smooth gradient, a glow, a soft backdrop: continuous tone whose whole content is the
        /// transition itself. The opposite trade from a <see cref="Photo"/> despite looking like one
        /// - block compression stores two endpoint colours per 4x4 block, which turns a gradual
        /// ramp into visible bands, and no encoder setting fixes that because it is the format. So
        /// it seeds a compression refusal. Everything else it takes: it is smoothed, scaled and
        /// mip-mapped like any other picture.
        /// </summary>
        Gradient = 4,
    }
}