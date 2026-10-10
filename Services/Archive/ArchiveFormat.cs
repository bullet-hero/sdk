namespace BH.SDK.Services.Archive
{
    // Which container, answered by the BYTES (ArchiveFormatSniffer), never by an extension. OpenPgp is
    // a member because a protected export sniffs to it; SevenZip, although unread, so a refusal can
    // say "7z" rather than "not an archive".

    /// <summary> Which container a stream turned out to be. </summary>
    public enum ArchiveFormat
    {
        /// <summary> None of the shapes below - or too few bytes to tell. </summary>
        Unknown = 0,

        /// <summary> POSIX tar inside gzip, the format this feature started as. </summary>
        TarGz = 1,

        /// <summary> A zip, which is also the only format here with a central directory. </summary>
        Zip = 2,

        /// <summary> Recognised so it can be refused by name. Never written. </summary>
        SevenZip = 3,

        /// <summary> An OpenPGP message: one of the others behind a passphrase. </summary>
        OpenPgp = 4,
    }
}
