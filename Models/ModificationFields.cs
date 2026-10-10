namespace BH.SDK.Models
{
    // Part of what a level FILE says: append-only, a retired number is never handed out again. The number
    // belongs to the DECLARATION (inheritance falls out), bands per declaring type; BHS1201-1203 enforce it.
    // Rules and the root-addressing list: Models/Objects/CLAUDE.md

    /// <summary>
    /// Stable field ids a prefab override is addressed by - the "which field" half of a
    /// <see cref="Primitives.ModificationKey"/>.
    /// </summary>
    public static class ModificationFields
    {
        /// <summary> What an operation taking part in no override says. Never a field. </summary>
        public const int None = 0x0000;

        // RectObject - band 0x01, and every object type inherits all of it.

        public const int Name = 0x0101;
        public const int Active = 0x0102;
        public const int Span = 0x0103;
        public const int Layer = 0x0104;
        public const int ParentObjectId = 0x0105;
        public const int Positions = 0x0106;
        public const int Rotations = 0x0107;
        public const int Scales = 0x0108;
        public const int Sizes = 0x0109;
        public const int AnchorsMin = 0x010A;
        public const int AnchorsMax = 0x010B;
        public const int Pivots = 0x010C;

        // ShapeObject - band 0x02.

        public const int ShapeId = 0x0201;
        public const int ColliderId = 0x0202;

        /// <summary> ShapeObject.ShaderType - the member is named for the enum, the field for what it selects. </summary>
        public const int Shader = 0x0203;

        public const int TextureResourceId = 0x0204;

        /// <summary> ShapeObject.Colors, which shares the wire key "c" with <see cref="TextColors"/> and nothing else. </summary>
        public const int ShapeColors = 0x0205;

        public const int UVs = 0x0206;

        // TextObject - band 0x03.

        public const int Text = 0x0301;
        public const int FontResourceId = 0x0302;

        /// <summary> TextObject.Colors - see <see cref="ShapeColors"/>. </summary>
        public const int TextColors = 0x0303;

        public const int FontSizes = 0x0304;
        public const int Fillments = 0x0305;
        public const int Appearings = 0x0306;
        public const int AppearingMask = 0x0307;
        public const int WordWrap = 0x0308;
        public const int HorizontalAlignment = 0x0309;
        public const int VerticalAlignment = 0x030A;

        // EffectObject - band 0x04.

        public const int EffectId = 0x0401;

        // PrefabObject, band 0x05: PlacementOffset is addressable (a nested placement diverges in it).

        public const int PlacementOffset = 0x0501;

        // PlacementDuration is a view onto Span's duration half, not a member. Models/Objects/CLAUDE.md
        public const int PlacementDuration = 0x0502;

        // The root is addressable only for what the template owns; Span, Active, Layer, ParentObjectId and
        // PlacementOffset are the placement's own and excluded. Models/Objects/CLAUDE.md

        /// <summary> Whether a <see cref="Primitives.ModificationKey"/> addressed at
        /// <see cref="Primitives.ObjectId.PrefabRoot"/> may name this field. </summary>
        public static bool IsPrefabRootField(int field) => field switch
        {
            Name => true,
            Positions => true,
            Rotations => true,
            Scales => true,
            Sizes => true,
            AnchorsMin => true,
            AnchorsMax => true,
            Pivots => true,
            PlacementDuration => true,
            _ => false,
        };
    }
}
