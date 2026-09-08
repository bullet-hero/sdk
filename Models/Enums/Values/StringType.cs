namespace BH.SDK.Models.Enums.Values
{
    /// <summary> Which form an authored string is in. </summary>
    public enum StringType : byte
    {
        /// <summary> One text, shown to everybody. </summary>
        Value = 0,

        /// <summary> One text per language, inline - a level's own strings carry no keys and go through no table. </summary>
        Localized = 1,
    }
}