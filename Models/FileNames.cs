namespace BH.SDK.Models
{
    /// <summary>
    /// Fixed names of everything the game reads from disk - level folders, save files, shared
    /// libraries. Centralized so a level folder authored by one build is readable by any other.
    /// </summary>
    public static class FileNames
    {
        /// <summary> Where the player's own levels live, one folder each. </summary>
        public const string LevelDirectory = "levels";

        /// <summary> The device-wide settings document. </summary>
        public const string SettingsFileName = "settings.json";

        // No fixed extension - level.json/level.blob and metadata.json/metadata.blob are chosen
        // per-level at creation time and resolved by which extension is present on disk at load
        // time (see PathUtils.FindDataFile), not stored as a field in Level/LevelMeta itself.

        /// <summary> The level document, without its extension - which is what says which format it is in. </summary>
        public const string LevelFileBaseName = "level";

        /// <summary> The metadata document, read by a browser without opening the level. </summary>
        public const string MetadataFileBaseName = "metadata";

        // The editor's autosaves, and they live OUTSIDE levels/ on purpose: a backup that sits inside
        // the folder it protects is copied, zipped, shared and deleted along with it, and a level
        // folder is a portable document (see the project's "Level portability") rather than a place to
        // hide a history. One folder per level id, so a level deleted by mistake still has its copies.

        /// <summary> A SIBLING of the levels folder, so a level being zipped, shared or deleted does not take its
        /// backups with it. </summary>
        public const string BackupsDirectory = "backups";

        // No fixed extension either - a backup is written in whatever format the level itself is
        // written in, so its name carries only the timestamp: backup_level_2026-08-24_18-05-03.json.

        /// <summary> What a backup's file name starts with; the rest is its timestamp. </summary>
        public const string BackupLevelFilePrefix = "backup_level_";

        /// <summary> The cover image, without its extension. </summary>
        public const string LogoName = "logo";

        /// <summary> The cover as a PNG. </summary>
        public const string LogoFileNamePng = "logo.png";

        /// <summary> The cover as a JPEG - the two are tried in order. </summary>
        public const string LogoFileNameJpg = "logo.jpg";

        // Device-wide (not per-level) shared library of reusable Themes/Effects/Shapes/Prefabs -
        // see PathUtils.GetThemesDirectoryInfo/GetEffectsDirectoryInfo/GetShapesDirectoryInfo/
        // GetPrefabsDirectoryInfo.

        /// <summary> Where a level keeps the files it carries. </summary>
        public const string ResourcesDirectory = "resources";

        /// <summary> Device-wide theme library. </summary>
        public const string ThemesDirectory = "themes";

        /// <summary> Device-wide effect library. </summary>
        public const string EffectsDirectory = "effects";

        /// <summary> Device-wide shape library. </summary>
        public const string ShapesDirectory = "shapes";

        /// <summary> Device-wide prefab library - the only way a prefab is shared between levels. </summary>
        public const string PrefabsDirectory = "prefabs";

        /// <summary> Where diagnostic reports are written. </summary>
        public const string ReportsDirectory = "reports";

        // A SIBLING OF levels/, LIKE backups/, AND FOR THE SAME REASON: what is in here describes the
        // PLAYER rather than the level, so it must survive the level folder being zipped, shared,
        // re-imported or deleted. A per-level file is named by the level's own LevelId - the one
        // identifier a rename, a translation or a folder move cannot change, and the one LevelMeta
        // already declares that scores and progress attach to.

        /// <summary> Another SIBLING of the levels folder: progress must survive a level being deleted, and must
        /// never travel with one - it would arrive already won. </summary>
        public const string StatisticsDirectory = "stats";

        /// <summary> The player's device-wide statistics, beside the per-level files. </summary>
        public const string StatisticsFileName = "statistics.json";

        // A protected level is level.json.gpg, and the inner extension stays in the name on purpose:
        // it is what gpg itself does (`gpg -c level.json` writes level.json.gpg), and it answers
        // "which SerializationType is this" without a header byte or a guess. Appended, never
        // replacing - level.gpg would lose that answer.

        /// <summary> Appended to a document's own name when it is encrypted. </summary>
        public const string EncryptedExtension = ".gpg";

        /// <summary> What a level package is called outside the game. </summary>
        public const string PackageExtension = ".tar.gz";
    }
}