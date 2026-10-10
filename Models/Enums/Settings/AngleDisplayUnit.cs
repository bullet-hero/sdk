namespace BH.SDK.Models.Enums.Settings
{
    // Display only: angles are stored and processed in RADIANS everywhere; conversion is at the field.
    /// </summary>
    public enum AngleDisplayUnit : byte
    {
        /// <summary> The unit the value is actually stored in - shown unconverted. </summary>
        Radians = 0,

        /// <summary> Degrees, converted on the way in and on the way out. The default, because it
        /// is the unit an author thinks a rotation in. </summary>
        Degrees = 1,
    }
}
