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
    // 0x04 EffectObject, 0x05 PrefabObject. A placement's own PrefabId, ObjectIds and Modifications
    // are what an override is expressed IN, so none of them is a thing an override may address -
    // which is why that band held nothing until PlacementOffset, an ordinary authored field, opened
    // it. ObjectId is absent from band 0x01 for the same kind of reason as the first three: it is
    // the identity the address is built from.
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

        // PrefabObject - band 0x05. PrefabId, ObjectIds and Modifications are still absent from it
        // and always will be: they are what an override is expressed IN. PlacementOffset is not -
        // it is an ordinary authored field that a NESTED placement (one materialized as a child of
        // an outer placement) has to be able to diverge in, like any other inner object's field.

        public const int PlacementOffset = 0x0501;

        // PlacementDuration is not a stored member and is the only field here that is not: it is a
        // view onto Span's duration half (PrefabObject.PlacementDuration), which exists BECAUSE the
        // template owns that number and ApplyRoot rewrites it. Overriding Span whole would restate
        // the start as well and beat every ordinary move, which is why Span stays excluded below
        // and this narrower field exists instead.
        public const int PlacementDuration = 0x0502;

        // THE ROOT IS ADDRESSABLE, BUT NOT FOR EVERYTHING, and this is the list. A Modification
        // keyed to ObjectId.PrefabRoot lands on the PLACEMENT itself (PrefabRootUtils), so it may
        // only address what the template owns and the placement therefore has no other say in:
        // Name, the seven positional tracks, and the span's DURATION through PlacementDuration.
        //
        // Span, Active, Layer, ParentObjectId and PlacementOffset are excluded because they are the
        // PLACEMENT's own authored fields - nothing copies them off the root, the author edits them directly, and an
        // override on one of them would be a second way to say the same thing that additionally
        // overwrote the first, since ApplyModifications runs after everything else. The remaining
        // bands are excluded by construction: a placement is a PrefabObject and has no shape,
        // text or effect member to override.
        //
        // A NESTED placement is the case that makes the exclusions read oddly and is still right: an
        // outer placement overrides its inner placement's Span or PlacementOffset by that inner
        // object's OWN id, which is case 1 in ModificationRecorder and never consults this list.

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
