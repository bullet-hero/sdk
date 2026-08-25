namespace BH.SDK.Models.Enums.Settings
{
    // Display only, and that is the whole contract: an angle is stored and processed in RADIANS
    // everywhere - in the model, in the jobs, in every generator - and this decides nothing except
    // what a rotation field SHOWS and what it accepts back. The conversion happens at that one
    // boundary and nowhere else, so no value that reaches a file ever depends on this.

    /// <summary>
    /// Which unit the editor's rotation fields are read and typed in.
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
