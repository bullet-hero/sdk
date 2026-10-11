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

        // The editor's autosaves live OUTSIDE levels/: a level folder is portable. Docs/FILE_LAYOUT.md

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

        // Two names, nothing probes for either: the writer records the one it chose in LevelMeta.LevelLogo,
        // decided by the file's own bytes. Docs/FILE_LAYOUT.md

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

        // A collection is a folder like a level, reusing the device library's layout. Docs/FILE_LAYOUT.md

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

        // A sibling of levels/, like backups/: it describes the PLAYER and is named by LevelId.
        // Docs/FILE_LAYOUT.md

        /// <summary> Another SIBLING of the levels folder: progress must survive a level being deleted, and must
        /// never travel with one - it would arrive already won. </summary>
        public const string StatisticsDirectory = "stats";

        /// <summary> The player's device-wide statistics, beside the per-level files. </summary>
        public const string StatisticsFileName = "statistics.json";

        // level.json.gpg: the inner extension stays, as gpg itself writes it, and names the
        // SerializationType. Appended, never replacing. Docs/FILE_LAYOUT.md

        /// <summary> Appended to a document's own name when it is encrypted. </summary>
        public const string EncryptedExtension = ".gpg";

        // Three containers, one feature: these name what an export WRITES, never what an import trusts
        // (it sniffs the bytes). Docs/FILE_LAYOUT.md

        /// <summary> What a level archive is called outside the game. </summary>
        public const string TarGzExtension = ".tar.gz";

        /// <summary> The same archive as a zip, the shape a desktop opens without a tool. </summary>
        public const string ZipExtension = ".zip";

        /// <summary> Named so a refusal can say which format it refused. Nothing writes it. </summary>
        public const string SevenZipExtension = ".7z";

        // A profile archive mirrors the folders above; profile-backups/ is never itself packed.
        // Docs/FILE_LAYOUT.md

        /// <summary> A profile archive's manifest, always its first entry. </summary>
        public const string ProfileManifestFileName = "profile.json";

        /// <summary> The one kept profile backup and every temporary of a profile import. </summary>
        public const string ProfileBackupsDirectory = "profile-backups";

        /// <summary> Under <see cref="ProfileBackupsDirectory"/>: an import's working folder. </summary>
        public const string ProfileStagingDirectory = "staging";

        /// <summary> Under <see cref="ProfileBackupsDirectory"/>: the one kept backup. </summary>
        public const string ProfileBackupFileName = "profile-backup.zip";

        // Machine-local like the folders above, never part of a level or a profile, and not a model.
        // The bridge's own files: BH.SDK.Agent.AgentEndpoint.

        /// <summary> Under the persistent data path: where the level-editing bridge keeps its files. </summary>
        public const string AgentDirectory = "mcp";

        /// <summary> Under <see cref="AgentDirectory"/>: the running bridge's port, token and pid. </summary>
        public const string AgentEndpointFile = "endpoint.json";

        // Inside a level folder, beside resources/: notes an AI keeps about THIS level. Not a model, never
        // read by the loader. BH.SDK.Agent.AiFolderRules says what may live there.

        /// <summary> A level's AI context folder. </summary>
        public const string AiDirectory = "ai";

        /// <summary> Under <see cref="AiDirectory"/>: the main context document. </summary>
        public const string AiContextFile = "CONTEXT.md";

        /// <summary> Under <see cref="AiDirectory"/>: whether the folder travels with the level. </summary>
        public const string AiManifestFile = "manifest.json";
    }
}