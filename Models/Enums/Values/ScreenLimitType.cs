namespace BH.SDK.Models.Enums.Values
{
    /// <summary> How a level constrains the aspect ratio it is framed at. </summary>
    public enum ScreenLimitType : byte
    {
        /// <summary> Whatever the screen is; the level never letterboxes. </summary>
        None = 0,

        /// <summary> One aspect, always - a wider or narrower screen gets bars. </summary>
        Fixed = 1,

        /// <summary> A band of aspects; only a screen outside it gets bars. </summary>
        Bounds = 2,
    }
}