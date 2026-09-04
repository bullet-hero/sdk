namespace BH.SDK.Models.Enums.Keyframes
{
    public enum FontSizeKeyType : byte
    {
        Value = 0, // one authored font size, used as-is
        Auto = 1, // a min/max band the renderer shrinks the text into so it fits its rect
    }
}
