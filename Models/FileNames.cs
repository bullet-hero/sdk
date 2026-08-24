namespace BH.SDK.Models
{
    /// <summary>
    /// Fixed names of everything the game reads from disk - level folders, save files, shared
    /// libraries. Centralized so a level folder authored by one build is readable by any other.
    /// </summary>
    public static class FileNames
    {
        public const string LevelDirectory = "levels";

        public const string SettingsFileName = "settings.json";

        // No fixed extension - level.json/level.bson and metadata.json/metadata.bson are chosen
        // per-level at creation time and resolved by which extension is present on disk at load
        // time (see PathUtils.FindDataFile), not stored as a field in Level/LevelMeta itself.
        public const string LevelFileBaseName = "level";
        public const string MetadataFileBaseName = "metadata";

        // The editor's autosaves, and they live OUTSIDE levels/ on purpose: a backup that sits inside
        // the folder it protects is copied, zipped, shared and deleted along with it, and a level
        // folder is a portable document (see the project's "Level portability") rather than a place to
        // hide a history. One folder per level id, so a level deleted by mistake still has its copies.
        public const string BackupsDirectory = "backups";

        // No fixed extension either - a backup is written in whatever format the level itself is
        // written in, so its name carries only the timestamp: backup_level_2026-08-24_18-05-03.json.
        public const string BackupLevelFilePrefix = "backup_level_";

        public const string LogoName = "logo";
        public const string LogoFileNamePng = "logo.png";
        public const string LogoFileNameJpg = "logo.jpg";

        // Device-wide (not per-level) shared library of reusable Themes/Effects/Shapes/Prefabs -
        // see PathUtils.GetThemesDirectoryInfo/GetEffectsDirectoryInfo/GetShapesDirectoryInfo/
        // GetPrefabsDirectoryInfo.
        public const string ResourcesDirectory = "resources";
        public const string ThemesDirectory = "themes";
        public const string EffectsDirectory = "effects";
        public const string ShapesDirectory = "shapes";
        public const string PrefabsDirectory = "prefabs";

        public const string ReportsDirectory = "reports";

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