namespace BH.SDK.Models.Enums.Values
{
    /// <summary> Which ends of a curve key use their weight rather than the default tangent length. </summary>
    public enum CurveWeightedMode : byte
    {
        None = 0,
        In = 1 << 0,
        Out = 1 << 1,
        Both = In | Out
    }
}