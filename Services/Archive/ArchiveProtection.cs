namespace BH.SDK.Services.Archive
{
    // TWO SCHEMES, AND THE SECOND ONE IS NOT A DUPLICATE OF THE FIRST. OpenPgp wraps the whole
    // archive in a message `gpg -d` opens, and it is the default because ONE scheme then covers a
    // single document, a folder and an archive alike - which is what made it worth choosing over
    // ZIP's own AES in the first place. ZipAes256 encrypts each entry inside the zip instead, and
    // it exists because the person on the other end of a level very often has 7-Zip and nothing
    // else: they type the password into the tool they already have rather than installing gpg.
    //
    // WHAT IT COSTS is written down so choosing it stays a choice: SharpZipLib takes the password
    // as a `string`, which cannot be zeroed the way the char[] every other path here carries can;
    // Windows Explorer refuses an AES zip outright; AE-2 is WinZip's extension rather than a
    // formal standard; and the salt is random, so such an archive is never byte-reproducible.
    //
    // Only Zip can carry ZipAes256 - tar.gz has no encryption of its own, and asking for the
    // combination is a caller mistake rather than a state to degrade.

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
