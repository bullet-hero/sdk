namespace BH.SDK.Services.Archive
{
    // WHICH CONTAINER, ANSWERED BY THE BYTES AND NOTHING ELSE. The name of a file is the one part
    // of it anybody can write, so this enum is never parsed from an extension - ArchiveFormatSniffer
    // reads the first bytes and says which of these it saw.
    //
    // OpenPgp is a member because a sniff CAN legitimately land on it: a protected export is a PGP
    // message whose plaintext is one of the other two, and the reader that sees it decrypts and
    // sniffs again. SevenZip is a member for the opposite reason - nothing here reads or writes 7z,
    // and naming it is what lets a refusal say "7z" instead of "not an archive", which is the
    // difference between a person trying something else and a person concluding their file is
    // broken.

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
