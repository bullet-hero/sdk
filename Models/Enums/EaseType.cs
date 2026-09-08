namespace BH.SDK.Models.Enums
{
    /// <summary> The curve a keyframe is blended into from the one before it. Grouped by family below, and
    /// <c>Constant</c> is the one that blends nothing at all - the value snaps on arrival. </summary>
    public enum EaseType : byte
    {
        Linear = 0, Constant = 1, // core
        InSine = 2, OutSine = 3, InOutSine = 4, // sin() functions
        InQuad = 5, OutQuad = 6, InOutQuad = 7, // 2-degree functions (square)
        InCubic = 8, OutCubic = 9, InOutCubic = 10, // 3-degree functions (cube)
        InQuart = 11, OutQuart = 12, InOutQuart = 13, // 4-degree functions (tesseract)
        InQuint = 14, OutQuint = 15, InOutQuint = 16, // 5-degree functions
        InExpo = 17, OutExpo = 18, InOutExpo = 19, // exponential functions
        InCirc = 20, OutCirc = 21, InOutCirc = 22, // circular functions
        InBack = 23, OutBack = 24, InOutBack = 25, // inertial functions
        InElastic = 26, OutElastic = 27, InOutElastic = 28 // elastic functions
    }
}