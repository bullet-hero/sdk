using BH.SDK.Services.Archive;

namespace BH.SDK.Services.Profile
{
    // A PROFILE IS NOT A LEVEL, so neither of the level archive's defaults fits it. Its names are the
    // player's own paths and cannot be renamed (a level's media are referenced by name), so the policy
    // lifts the shared 100-byte cap - zip only, which is the only container a profile is written as.
    // And its size is the player's whole history, so the unpack limits are not a fixed number at all:
    // they are what the archive's own directory declared, which the import has already shown to the
    // player and checked against the disk, capped by a ceiling nothing real reaches.

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
