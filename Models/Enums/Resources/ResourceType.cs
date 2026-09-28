namespace BH.SDK.Models.Enums.Resources
{
    // THREE FAMILIES SHARE THIS ENUM since generation 2. The first five are FILE resources: bytes on
    // disk, a level-local negative int id, and what the loader turns them into. The next four are DATA
    // resources: a model inside level.json, addressed by a guid, never loaded as a file - they are here
    // only so a ResourceMeta record can credit one. The last is the level's COVER, which is no resource
    // at all (LevelMeta.LevelLogo is a bare location, so the browser can draw it from metadata.json
    // alone) and so has neither id: a level has exactly one cover, and its type is its whole address.
    // Append-only: the value is what the file stores.

    /// <summary> What a level resource becomes once loaded, or which data resource a record credits. </summary>
    public enum ResourceType : byte
    {
        Bytes = 0, // byte[]
        Text = 1, // string
        Texture = 2, // Texture
        Audio = 3, // AudioClip
        Font = 4, // UniTextFont

        /// <summary> A ThemeData, addressed by ThemeId. </summary>
        Theme = 5,

        /// <summary> An EffectData, addressed by EffectId. </summary>
        Effect = 6,

        /// <summary> A CompositeShape, addressed by ShapeId. </summary>
        Shape = 7,

        /// <summary> A Prefab template, addressed by PrefabId. </summary>
        Prefab = 8,

        /// <summary> The level's cover image, addressed by its type alone - a level has exactly one. </summary>
        LevelLogo = 9,
    }

    /// <summary> Which id slot a resource family is addressed by. </summary>
    public static class ResourceTypeExtensions
    {
        /// <summary> True for the data resources, whose id is a guid rather than a level-local int. </summary>
        public static bool IsGuidAddressed(this ResourceType type)
            => type >= ResourceType.Theme && type <= ResourceType.Prefab;

        /// <summary> True for a family addressed by its type alone, filling neither id slot - the
        /// level's cover. </summary>
        public static bool IsTypeAddressed(this ResourceType type) => type == ResourceType.LevelLogo;
    }
}