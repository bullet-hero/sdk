namespace BH.SDK.Versions
{
    // Domain name constants used by [DataVersion] on aggregate roots and by anything in
    // Serialization that needs to key per-domain behaviour (e.g. excluded converters) without
    // Models referencing Serialization types directly. See VERSION-UPDATE.md.
    public static class DataDomains
    {
        // Core
        public const string Level = nameof(Models.Level);
        public const string LevelMeta = nameof(Models.LevelMeta);
        public const string UserSettings = nameof(Models.UserSettings);
        public const string Prefab = nameof(Models.Objects.Prefab);
        public const string EffectData = nameof(Models.Data.EffectData);
        public const string ThemeData = nameof(Models.Data.ThemeData);
        public const string CompositeShape = nameof(Models.Data.CompositeShape);
        public const string ClipboardData = nameof(Models.Clipboard.ClipboardData);
        
        // Level
        public const string LevelSettings = nameof(Models.SettingGroups.LevelSettings);
        public const string GameLevel = nameof(Models.Game.GameLevel);
        public const string AudioLevel = nameof(Models.Audio.AudioLevel);
        public const string LevelResources = nameof(Models.Resources.LevelResources);
        public const string LevelHints = nameof(Models.Hints.LevelHints);

        // Services
        public const string PublishProfile = nameof(Publishing.PublishProfile);

        // Statistics
        public const string GameStatistics = nameof(Models.Statistics.GameStatistics);
        public const string LevelStatistics = nameof(Models.Statistics.LevelStatistics);

        // GameLevel
        public const string GameEvents = nameof(Models.Game.GameEvents);
        public const string CameraEvents = nameof(Models.Game.CameraEvents);
        public const string PostProcessingEvents = nameof(Models.Game.PostProcessingEvents);
        public const string PlayerEvents = nameof(Models.Game.PlayerEvents);
    }
}
