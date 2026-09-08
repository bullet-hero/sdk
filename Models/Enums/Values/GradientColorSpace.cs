namespace BH.SDK.Models.Enums.Values
{
    /// <summary> Which space a gradient's stops are blended in. </summary>
    public enum GradientColorSpace : sbyte
    {
        Uninitialized = -1, // 0xFFFFFFFF
        Gamma = 0,
        Linear = 1,
    }
}