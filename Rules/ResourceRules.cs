namespace BH.SDK.Rules
{
    /// <summary> What a level's resources may be - how many, how large, and how many places one asset may be
    /// fetched from. </summary>
    public static class ResourceRules
    {
        // Two different things used to share the name "sources", which read as one limit contradicting
        // itself (4 vs 16):
        // - Resource.MaxSourcesCount (4) - fallback URIs for ONE asset, tried in order until one
        //   loads. Small on purpose: each is a load attempt the player waits through.
        // - MaxMetaSources (16) - attribution entries in ResourceMeta, i.e. where the asset came
        //   from, for licensing. Never fetched, so a generous count costs nothing.

        /// <summary> Upper bound of ResourceMeta.ResourceSources. </summary>
        public const int MaxMetaSources = 16;

        /// <summary> Upper bound of LevelMeta.LevelAuthors, ResourceMeta.ResourceAuthors. </summary>
        public const int MaxAuthors = 16;

        // Content hashes of the actual bytes behind ONE resource, and the permissions covering it.
        // Both are plural for the same reason Resource.Sources is: one resource can be several files
        // (a track re-encoded per platform), and one file can need permission from more than one
        // rights holder (composer and performer). Small caps - past a handful of either, the record
        // has stopped describing one resource.

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

        // Per-category caps on a level's own resource dictionaries. These bound what a level may
        // REFERENCE, not what it may ship: every entry is a user-defined (negative-id) resource the
        // loader has to resolve before playback starts, so the count is load time, not frame time.
        // Sized by what authoring plausibly needs - a level with 256 distinct textures is already
        // extreme, one with 32 fonts is unheard of.

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
