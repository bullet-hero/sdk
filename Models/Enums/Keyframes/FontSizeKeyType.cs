namespace BH.SDK.Models.Enums.Keyframes
{
    /// <summary> Whether a text's size is a number the author picked or one the renderer fits for them. </summary>
    public enum FontSizeKeyType : byte
    {
        Value = 0, // one authored font size, used as-is
        Auto = 1, // a min/max band the renderer shrinks the text into so it fits its rect
    }
}
