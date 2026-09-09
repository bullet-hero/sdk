namespace BH.SDK
{
    // THE SDK'S OWN VERSION, WHICH IS NONE OF THE OTHER THREE. It is semver over this library's
    // PUBLIC API - major for a breaking change, minor for an addition, patch for a fix that moves
    // no signature - and its consumers are the things compiled against the DLL rather than the
    // things reading a file: the site, the server, external tools, modders. Docs/VERSIONING.md
    // names all six axes and says which is which.
    //
    // NOT the model format's generation (ModelGenerations, per domain, in every file on disk), NOT
    // the client's own gv (Application.version, from bundleVersion), NOT the author's version of a
    // level (LevelMeta.LevelVersion). It STARTED at 0.5.5 by duplicating the client's number once,
    // because a library that has shipped inside a client since 0.0.1 has no other honest origin;
    // the two are free to diverge from here.
    //
    // READONLY, NEVER CONST, and that is the whole reason this is a field rather than a constant: a
    // const is inlined into every consumer at ITS compile time, so a tool built against 0.6.0 would
    // keep reporting 0.6.0 after being handed a newer DLL - which is precisely the question this
    // exists to answer. Two other copies must agree with it (package.json and BH.SDK.csproj's
    // <Version>), and Services.Shared.Tests' SdkVersionAgreementTests is what fails when one moves
    // alone.

    /// <summary> This library's own version, as its consumers see it. </summary>
    public static class SdkVersion
    {
        /// <summary> Semver over the public API. Read, never inlined - see this file's header. </summary>
        public static readonly string Value = "0.6.2";
    }
}
