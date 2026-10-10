namespace BH.SDK
{
    // sv: semver over this library's public API, none of the other version axes (Docs/VERSIONING.md).
    // Readonly, never const - a const is inlined into consumers at their compile time.
    // Lockstep: package.json and BH.SDK.csproj <Version>, checked by SdkVersionAgreementTests.

    /// <summary> This library's own version, as its consumers see it. </summary>
    public static class SdkVersion
    {
        /// <summary> Semver over the public API. Read, never inlined - see this file's header. </summary>
        public static readonly string Value = "1.2.0";
    }
}