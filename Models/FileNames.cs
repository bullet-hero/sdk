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

        /// <summary> Videos rendered from a level, one folder per level id. Another SIBLING of the levels
        /// folder, like <see cref="BackupsDirectory"/>: nobody shares a level to send its videos along,
        /// and a video outlives the level it was rendered from. </summary>
        public const string RecordingsDirectory = "recordings";

        /// <summary> A running export's scratch folder inside <c>recordings/&lt;level id&gt;/</c>: raw
        /// passes and the audio mix. Never part of a profile - a leftover exists only after a crash. </summary>
        public const string RecordingsTempDirectory = ".tmp";

        // TWO NAMES, AND NOTHING EVER PROBES FOR EITHER - unlike the level and metadata documents
        // above, whose extension is resolved by what is on disk. A cover is reached through
        // LevelMeta.LevelLogo, an ordinary resource uri, so the name a writer chooses is the name it
        // has to record in the metadata in the same step. Which of the two it chooses is decided by
        // the file's own bytes (the game's LevelLogoFile), never by what the source was called.

        /// <summary> The cover image, without its extension. </summary>
        public const string LogoName = "logo";

        /// <summary> The cover as a PNG. </summary>
        public const string LogoFileNamePng = "logo.png";

        /// <summary> The cover as a JPEG. </summary>
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

        // A COLLECTION IS A FOLDER, the way a level is: resources/collections/<CollectionId>/ holding a
        // collection.* manifest, the four data folders above with the device library's own envelope
        // files byte for byte, and media/ for the file resources. Reusing the library layout is what
        // lets one reader serve both and a library entry be copied into a collection as a file.

        /// <summary> Where resource collections live, under ResourcesDirectory. </summary>
        public const string CollectionsDirectory = "collections";

        /// <summary> A collection's manifest - the base name, extension chosen like the level's. </summary>
        public const string CollectionFileBaseName = "collection";

        /// <summary> Where a collection keeps its textures, fonts and audio files. </summary>
        public const string MediaDirectory = "media";

        /// <summary> A collection's cover image, optional. </summary>
        public const string CoverFileName = "cover.png";

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

        // THREE CONTAINERS, ONE FEATURE. A level archive is named by whichever of them wrote it,
        // and none of the three is the "real" one: tar.gz is what a shell opens, zip is what
        // Windows Explorer opens on a double click, and 7z is what this build can so far only
        // recognise well enough to refuse by name. What a file IS is still sniffed from its bytes -
        // these constants name what an export WRITES, never what an import trusts.

        /// <summary> What a level archive is called outside the game. </summary>
        public const string TarGzExtension = ".tar.gz";

        /// <summary> The same archive as a zip, the shape a desktop opens without a tool. </summary>
        public const string ZipExtension = ".zip";

        /// <summary> Named so a refusal can say which format it refused. Nothing writes it. </summary>
        public const string SevenZipExtension = ".7z";

        // A PROFILE ARCHIVE MIRRORS THE FOLDERS ABOVE under their own names, so it needs only its own
        // manifest. profile-backups/ is a sibling that owns EVERYTHING the profile transfer writes on
        // the device - the one kept backup and, in staging/, every temporary of an import - and it is
        // never itself packed: a backup that carried the previous backup would grow without end.

        /// <summary> A profile archive's manifest, always its first entry. </summary>
        public const string ProfileManifestFileName = "profile.json";

        /// <summary> The one kept profile backup and every temporary of a profile import. </summary>
        public const string ProfileBackupsDirectory = "profile-backups";

        /// <summary> Under <see cref="ProfileBackupsDirectory"/>: an import's working folder. </summary>
        public const string ProfileStagingDirectory = "staging";

        /// <summary> Under <see cref="ProfileBackupsDirectory"/>: the one kept backup. </summary>
        public const string ProfileBackupFileName = "profile-backup.zip";
    }
}