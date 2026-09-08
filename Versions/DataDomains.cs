namespace BH.SDK.Versions
{
    // Domain name constants used by [DataVersion] on aggregate roots and by anything in
    // Serialization that needs to key per-domain behaviour (e.g. excluded converters) without
    // Models referencing Serialization types directly. See VERSION-UPDATE.md.

    /// <summary> The name of every independently-versioned domain, as <c>[DataVersion]</c> spells it. Constants
    /// rather than literals so a domain has one spelling, and so Models never has to see Serialization. </summary>
    public static class DataDomains
    {
        // Core

        /// <summary> The level domain. </summary>
        public const string Level = nameof(Models.Level);
        /// <summary> The level meta domain. </summary>
        public const string LevelMeta = nameof(Models.LevelMeta);
        /// <summary> The user settings domain. </summary>
        public const string UserSettings = nameof(Models.UserSettings);
        /// <summary> The prefab domain. </summary>
        public const string Prefab = nameof(Models.Objects.Prefab);
        /// <summary> The effect data domain. </summary>
        public const string EffectData = nameof(Models.Data.EffectData);
        /// <summary> The theme data domain. </summary>
        public const string ThemeData = nameof(Models.Data.ThemeData);
        /// <summary> The composite shape domain. </summary>
        public const string CompositeShape = nameof(Models.Data.CompositeShape);
        /// <summary> The clipboard data domain. </summary>
        public const string ClipboardData = nameof(Models.Clipboard.ClipboardData);
        
        // Level

        /// <summary> The level settings domain. </summary>
        public const string LevelSettings = nameof(Models.SettingGroups.LevelSettings);
        /// <summary> The game level domain. </summary>
        public const string GameLevel = nameof(Models.Game.GameLevel);
        /// <summary> The audio level domain. </summary>
        public const string AudioLevel = nameof(Models.Audio.AudioLevel);
        /// <summary> The level resources domain. </summary>
        public const string LevelResources = nameof(Models.Resources.LevelResources);
        /// <summary> The level hints domain. </summary>
        public const string LevelHints = nameof(Models.Hints.LevelHints);

        // Services

        /// <summary> The publish profile domain. </summary>
        public const string PublishProfile = nameof(Publishing.PublishProfile);

        // Statistics

        /// <summary> The game statistics domain. </summary>
        public const string GameStatistics = nameof(Models.Statistics.GameStatistics);
        /// <summary> The level statistics domain. </summary>
        public const string LevelStatistics = nameof(Models.Statistics.LevelStatistics);

        // GameLevel

        /// <summary> The game events domain. </summary>
        public const string GameEvents = nameof(Models.Game.GameEvents);
        /// <summary> The camera events domain. </summary>
        public const string CameraEvents = nameof(Models.Game.CameraEvents);
        /// <summary> The post processing events domain. </summary>
        public const string PostProcessingEvents = nameof(Models.Game.PostProcessingEvents);
        /// <summary> The player events domain. </summary>
        public const string PlayerEvents = nameof(Models.Game.PlayerEvents);
    }
}
