namespace BH.SDK.Serialization.Serializers
{
    // Identifies which IDataSerializer wire format a byte[] envelope was written with - see
    // VERSION-UPDATE.md, "Format-agnosticism". Byte-backed so it's cheap to persist alongside the
    // envelope bytes themselves (e.g. a leading tag byte) when the format needs to be recovered at
    // read time instead of being known ahead of time by the caller.
    //
    // TWO MEMBERS ARE RETIRED AND NEITHER NUMBER IS EVER REISSUED, because a member's NUMBER is
    // what a settings file on somebody's disk holds - renumbering silently reinterprets it. An old
    // file holding 1 or 2 lands on an undefined value that RuleEnumValid repairs to Json.
    //
    // JsonPretty was 2, and it was a WRITE-ONLY distinction: the same document as Json with
    // indentation added, sharing its ".json" extension, read back by the very same reader. Nothing
    // could recover the choice from a file, so it was never a property of the level - only of
    // whoever happened to save it. What it was FOR is reading a level file by eye, and that is what
    // an editor's own formatter does, on demand, without a second shape of the format existing.
    //
    // BSON WAS MEMBER 1 AND IS GONE. Its role here was speed, and it never delivered any: on a
    // 4.7k-object level it read about 5% faster than JSON while writing a file 30% LARGER, because
    // what dominates is Newtonsoft binding members by reflection and both formats pay that
    // identically. Blob is what actually answers that question - generated code, no reflection at
    // all. The number 1 is retired and never reissued, so an old settings file holding it lands on
    // an undefined value that RuleEnumValid repairs to Json, rather than silently meaning something
    // new.
    public enum SerializationType : byte
    {
        Json = 0,
        // 1 was Bson, 2 was JsonPretty.

        // Appended, never squeezed in, and a retired member's number is never reissued - the rule
        // TextureSizeLimit's rungs already keep and for the same reason: a member's NUMBER is what
        // a settings file on somebody's disk holds, so renumbering silently reinterprets it.
        Blob = 3,
    }
}
