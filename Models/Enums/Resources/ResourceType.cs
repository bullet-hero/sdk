namespace BH.SDK.Models.Enums.Resources
{
    // Three families since generation 2: five FILE kinds (negative level-local id), four DATA kinds (a guid,
    // creditable only) and the level cover (no id). Append-only: the value is what the file stores.

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