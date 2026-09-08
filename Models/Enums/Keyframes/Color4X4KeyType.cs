namespace BH.SDK.Models.Enums.Keyframes
{
    /// <summary> How many of a quad's four corner colours are authored separately. </summary>
    public enum Color4X4KeyType : byte
    {
        Value = 0, // all colors setup with one color value
        Horizontal = 1, // setup by 2 colors: BL/TL for one and BR/TR for second
        Vertical = 2, // setup by 2 colors: BL/BR for one and TL/TR for second
        BariCentrical = 3, // all 4 colors separately, for each side
    }
}