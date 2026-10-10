namespace BH.SDK.Serialization.Serializers
{
    // Which IDataSerializer wire format a byte[] envelope was written with; byte-backed to persist
    // beside the bytes. Members 1 (Bson) and 2 (JsonPretty) are RETIRED and never reissued: a number
    // is what a settings file holds, and RuleEnumValid repairs an old one to Json (Serialization/CLAUDE.md).

    /// <summary> Which wire format a document was written with. </summary>
    public enum SerializationType : byte
    {
        /// <summary> Text, readable and diffable. The default everywhere, and what a hand-made folder carries. </summary>
        Json = 0,
        // 1 was Bson, 2 was JsonPretty.

        // Appended, never squeezed in, and a retired member's number is never reissued - the rule
        // TextureSizeLimit's rungs already keep and for the same reason: a member's NUMBER is what
        // a settings file on somebody's disk holds, so renumbering silently reinterprets it.

        /// <summary> The binary form of the same data, read by generated code - forty times faster on a large level. </summary>
        Blob = 3,
    }
}
