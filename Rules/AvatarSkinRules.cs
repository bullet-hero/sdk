namespace BH.SDK.Rules
{
    // Cosmetic limits: nothing here moves a position, a collider or a random channel.
    // The file format they bound: Docs/FILE_LAYOUT.md, "skins/".

    /// <summary> The limits of an avatar skin file. </summary>
    public static class AvatarSkinRules
    {
        /// <summary> Fewest cells along either side of the body grid. </summary>
        public const int MinGridSide = 3;

        /// <summary> Most cells along either side of the body grid - also the mask's row stride. </summary>
        public const int MaxGridSide = 8;

        /// <summary> Most cells a body grid can hold: one bit each in the 64-bit mask. </summary>
        public const int MaxCellCount = MaxGridSide * MaxGridSide;

        /// <summary> Fewest filled cells a body may have. </summary>
        public const int MinFilledCells = 5;

        /// <summary> Most parts a skin may define. </summary>
        public const int MaxParts = MaxCellCount;

        /// <summary> Most meshes a skin may define for itself. </summary>
        public const int MaxMeshes = 32;

        /// <summary> Most body colours a skin may define. </summary>
        public const int MaxColors = 8;

        /// <summary> Smallest drawn body size, as a fraction of the classic one. </summary>
        public const float MinBodySize = 0.6f;

        /// <summary> Largest drawn body size, as a fraction of the classic one. </summary>
        public const float MaxBodySize = 1.25f;

        /// <summary> Most transparent a body or shadow colour may be drawn. </summary>
        public const float MinBodyAlpha = 0.5f;

        /// <summary> Smallest a part may be drawn inside its cell. </summary>
        public const float MinPartScale = 0.2f;

        /// <summary> Largest a part may be drawn inside its cell - the whole cell. </summary>
        public const float MaxPartScale = 1f;
    }
}