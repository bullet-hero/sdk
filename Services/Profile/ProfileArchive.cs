using BH.SDK.Services.Archive;

namespace BH.SDK.Services.Profile
{
    // A profile is not a level: its names are the player's own and cannot be renamed (the policy lifts
    // the 100-byte cap, zip only), and its unpack limits are what the archive's own directory declared,
    // checked by the import against the disk. Docs/ARCHIVE.md.

    /// <summary> The constants a profile archive is written and read under. </summary>
    public static class ProfileArchive
    {
        /// <summary> The longest entry name a profile writes - the file systems' own order of magnitude. </summary>
        public const int MaxNameBytes = 1024;

        /// <summary> More entries than any profile holds; beyond it a directory is refused unread. </summary>
        public const int MaxEntries = 262144;

        /// <summary> The ceiling on what a profile may unpack to, whatever its directory declares. </summary>
        public const long MaxTotalBytes = 64L * 1024 * 1024 * 1024;

        /// <summary> How a profile is packed. </summary>
        public static ArchivePolicy Policy => new ArchivePolicy { MaxNameBytes = MaxNameBytes };

        /// <summary> What reading a profile's directory is bounded by, before anything is declared. </summary>
        public static ArchiveLimits DirectoryLimits => new ArchiveLimits
        {
            MaxEntries = MaxEntries,
            MaxEntryBytes = MaxTotalBytes,
            MaxTotalBytes = MaxTotalBytes,
        };
    }
}
