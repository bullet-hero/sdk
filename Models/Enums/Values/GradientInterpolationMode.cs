namespace BH.SDK.Models.Enums.Values
{
    /// <summary> How a gradient gets from one stop to the next. </summary>
    public enum GradientInterpolationMode : byte
    {
        /// <summary> Straight interpolation between the two stops. </summary>
        Blend = 0,

        /// <summary> No interpolation at all - the colour snaps at each stop, giving hard bands. </summary>
        Fixed = 1,

        /// <summary> Blended so the ramp looks even to the eye rather than being even numerically. </summary>
        PerceptualBlend = 2,
    }
}