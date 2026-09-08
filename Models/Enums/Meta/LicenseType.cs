namespace BH.SDK.Models.Enums.Meta
{
    /// <summary> Which form a licence is stated in. </summary>
    public enum LicenseType : byte
    {
        /// <summary> Nothing said, which is not the same as permissive. </summary>
        NoSpecified = 0,

        /// <summary> One of the named licences, chosen from a list. </summary>
        Typical = 1,

        /// <summary> Terms the author spelled out by hand. </summary>
        Custom = 2,
    }
}