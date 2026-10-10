namespace BH.SDK.Models.Enums.Values
{
    // Authored intent only: every producer writes Free. No Broken member (an independent bit in Unity);
    // whoever adds tangent modes adds a flag. Docs/ENUMS.md

    /// <summary> How a CurveKeyframeValue's tangents were derived when it was authored. </summary>
    public enum CurveTangentMode : byte
    {
        Free = 0,
        Auto = 1,
        Linear = 2,
        Constant = 3,
        ClampedAuto = 4,
    }
}