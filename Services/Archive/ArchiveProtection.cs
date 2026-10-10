namespace BH.SDK.Services.Archive
{
    // Two schemes: OpenPgp (default; one scheme for a document, folder and archive, opened by gpg) and
    // ZipAes256 (opened by 7-Zip; costs and why it is not a duplicate: Docs/ARCHIVE.md). Only Zip can
    // carry ZipAes256; asking tar.gz for it is a caller mistake, not a state to degrade.

    /// <summary> How a written archive is protected, if at all. </summary>
    public enum ArchiveProtection
    {
        /// <summary> Written in the clear. </summary>
        None = 0,

        /// <summary> Wrapped in an OpenPGP symmetric message - the default, and the only scheme a
        /// tar.gz can take. </summary>
        OpenPgp = 1,

        /// <summary> Encrypted entry by entry inside the zip, WinZip AE-2 with AES-256. </summary>
        ZipAes256 = 2,
    }
}
