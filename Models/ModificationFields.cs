namespace BH.SDK.Models
{
    // These numbers are part of what a level FILE says, not an implementation detail: a prefab
    // override is addressed by (template-inner ObjectId, FIELD, index), so renumbering a field
    // silently repoints every override written against the old number in every level already on
    // disk. The constants below are therefore append-only, and a number is never handed out again
    // after its field is removed - a retired number must decode to nothing rather than to whatever
    // took its place.
    //
    // The number belongs to the DECLARATION, not to the runtime type, which is what makes
    // inheritance fall out for free: RectObject.Layer is one number, and a ShapeObject, a TextObject
    // and an EffectObject all override it through that same one. It also means two members sharing
    // a wire key are still two numbers - ShapeObject.Colors and TextObject.Colors are both "c" in
    // JSON, and they are 0x0205 and 0x0303 here, because the key alone could never say which field
    // it meant.
    //
    // The bands, written down because a new declaring type must take the next FREE one rather than
    // the one that reads as its neighbour: 0x01 RectObject, 0x02 ShapeObject, 0x03 TextObject,
    // 0x04 EffectObject, 0x05 PrefabObject. The last of those is reserved and empty - a placement's
    // own PrefabId, ObjectIds and Modifications are what an override is expressed IN, so none of
    // them is a thing an override may address. ObjectId is absent from band 0x01 for the same kind
    // of reason: it is the identity the address is built from.
    //
    // The generator is what makes a hand-written number safe. BHS1201 refuses two members claiming
    // one number, BHS1202 a member it cannot encode or that carries no [JsonProperty], and BHS1203
    // a zero or a band already spoken for by a different declaring type. ModificationFieldsTests
    // pins uniqueness a second time, from the other side.

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

        // PrefabObject - band 0x05, reserved and deliberately empty; see the band map above.
    }
}
