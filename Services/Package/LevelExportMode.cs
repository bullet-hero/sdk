namespace BH.SDK.Services.Package
{
    // Two independent choices flattened into one enum, and flattening them is deliberate: an author
    // picks ONE thing from a dropdown, not a container and then a checkbox. The two predicates below
    // are what code reads instead of switching on all four.

    /// <summary> How a level package is written out. </summary>
    public enum LevelExportMode
    {
        /// <summary> A plain folder - the same shape a level already has on disk, so it can be
        /// zipped, sent and unzipped by anyone. </summary>
        Folder = 0,

        /// <summary> A folder whose level document is encrypted. The metadata, the cover and the
        /// media stay readable, so a browser still renders the card. </summary>
        FolderProtectedLevel = 1,

        /// <summary> One .tar.gz. </summary>
        Archive = 2,

        /// <summary> One .tar.gz.gpg - the whole package behind a passphrase. </summary>
        ArchiveProtected = 3,
    }

    /// <summary> What each <see cref="LevelExportMode"/> implies. </summary>
    public static class LevelExportModeExtensions
    {
        /// <summary> Whether the mode writes one file rather than a folder. </summary>
        public static bool IsArchive(this LevelExportMode mode) =>
            mode == LevelExportMode.Archive || mode == LevelExportMode.ArchiveProtected;

        /// <summary> Whether the mode needs a passphrase. </summary>
        public static bool IsProtected(this LevelExportMode mode) =>
            mode == LevelExportMode.FolderProtectedLevel || mode == LevelExportMode.ArchiveProtected;
    }
}
