namespace BH.SDK.Rules
{
    /// <summary> What a level's resources may be - how many, how large, and how many places one asset may be
    /// fetched from. </summary>
    public static class ResourceRules
    {
        // Two different "sources": Resource.MaxSourcesCount (4) is fallback URIs for ONE asset, each a
        // load attempt the player waits through; MaxMetaSources (16) is attribution entries, never fetched.

        /// <summary> Upper bound of ResourceMeta.ResourceSources. </summary>
        public const int MaxMetaSources = 16;

        /// <summary> Upper bound of LevelMeta.LevelAuthors, ResourceMeta.ResourceAuthors. </summary>
        public const int MaxAuthors = 16;

        // Hashes and permissions of ONE resource, plural because it can be several files (per-platform
        // re-encodes) and need several rights holders. Past a handful the record describes nothing.

        /// <summary> Upper bound of ResourceMeta.ResourceHashes. </summary>
        public const int MaxHashes = 8;
        /// <summary> Upper bound of ResourceMeta.ResourcePermissions. </summary>
        public const int MaxPermissions = 8;

        // "sha256:" plus 64 hex characters is 71; the rest is headroom for a longer digest later.

        /// <summary> Highest hash length allowed. </summary>
        public const int MaxHashLength = 128;

        // A quoted permission is a mail or a DM, not a contract - long enough to hold the exchange,
        // short enough that nobody pastes a whole thread into every level file.

        /// <summary> Upper bound of PermissionGrant.ProofText. </summary>
        public const int MaxProofText = 8192;

        // Per-category caps on a level's own resource dictionaries: they bound what a level REFERENCES, so
        // the cost is load time, not frame time.

        /// <summary> Upper bound of LevelResources.Textures. </summary>
        public const int MaxTextures = 256;
        /// <summary> Upper bound of LevelResources.Fonts. </summary>
        public const int MaxFonts = 32;
        /// <summary> Upper bound of LevelResources.Audios. </summary>
        public const int MaxAudios = 64;
        /// <summary> Upper bound of LevelResources.CompositeShapes. </summary>
        public const int MaxCompositeShapes = 256;
        /// <summary> Upper bound of LevelResources.Themes. </summary>
        public const int MaxThemes = 128;
        /// <summary> Upper bound of LevelResources.Effects. </summary>
        public const int MaxEffects = 256;

        // LevelHints.FontCharacters is keyed by ANY FontResourceId, game-defined ones included,
        // so it is not bounded by MaxFonts (which counts only what a level ships). Sized to leave
        // room for the game's own font presets on top of the level's own.

        /// <summary> Upper bound of LevelHints.FontCharacters. </summary>
        public const int MaxFontCharacterEntries = MaxFonts * 4;

        // One source entry is a path, a URL or an addressable key.

        /// <summary> Upper bound of ResourceKey.Uri. </summary>
        public const int MaxUriLength = ValueRules.MaxUrl;
    }
}
