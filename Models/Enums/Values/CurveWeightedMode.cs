using System;

namespace BH.SDK.Models.Enums.Values
{
    // [Flags]: In and Out are bits, Both is their union, tested one side at a time (CurveMath, editor).

    /// <summary> Which ends of a curve key use their weight rather than the default tangent length. </summary>
    [Flags]
    public enum CurveWeightedMode : byte
    {
        None = 0,
        In = 1 << 0,
        Out = 1 << 1,
        Both = In | Out
    }
}