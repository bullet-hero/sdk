using System;

namespace BH.SDK.Models.Enums.Values
{
    // [Flags] rather than a plain enum, which it always WAS in shape - In and Out are bits and Both
    // is their union - and was not in declaration. It matters now that something tests one side at a
    // time (CurveMath's weighted branch, the curve editor's per-handle drag): a bitwise test against
    // an unflagged enum is a warning at every call site, and the whole point of the type is that the
    // two sides are answered separately.

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