namespace BH.SDK.Models.Enums.Values
{
    /// <summary> Which form an authored colour is in. </summary>
    public enum ColorType : byte
    {
        /// <summary> The colour itself. </summary>
        Value = 0,

        /// <summary> An index into the level's theme, resolved once per frame - which is what makes a whole
        /// level recolour by editing one palette. </summary>
        ThemeRef = 1,

        /// <summary> Drawn between two colours, addressed rather than generated, so a run reproduces. </summary>
        RandomMinMax = 2,

        /// <summary> Drawn between two colours with ONE roll shared by every channel, so the result is
        /// always a colour on the line between them rather than a mix of four unrelated draws. </summary>
        RandomLerp = 3,
    }
}