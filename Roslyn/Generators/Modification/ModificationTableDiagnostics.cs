using Microsoft.CodeAnalysis;

namespace BH.SDK.Roslyn.Modification
{
    // THE NUMBER IS WRITTEN BY HAND, so every way of writing it wrongly has to be an error here.
    // That is the whole trade: declaration order would renumber itself on a member reorder, a hash
    // of the JSON key would re-couple the id to the spelling this mechanism exists to decouple
    // from, and a hash of the C# name would make a rename the breaking change instead. A hand-
    // written number is the only one nothing can change behind the author's back - and it is safe
    // only because a collision, an unserialized member and a stray band are all refused below.
    //
    // Errors, never warnings. A duplicated id means one override addresses two fields and reaches
    // whichever the switch answers first, which is exactly the silent failure the whole item exists
    // to end.

    /// <summary> Diagnostics the modification table generator reports. </summary>
    internal static class ModificationTableDiagnostics
    {
        private const string Category = "BH.SDK.Modification";

        /// <summary> BHS1201 - two members claiming one number, so an override would address both. </summary>
        public static readonly DiagnosticDescriptor DuplicateField = new(
            "BHS1201",
            "Two members claim the same modification field id",
            "'{0}' and '{1}' both carry [ModificationField] with id 0x{2}. An override addresses a "
            + "field by that number alone, so one of them is unreachable - ids are append-only, take "
            + "the next free one",
            Category, DiagnosticSeverity.Error, true);

        /// <summary> BHS1202 - a member that cannot carry an override: not serialized, or not writable. </summary>
        public static readonly DiagnosticDescriptor UnaddressableMember = new(
            "BHS1202",
            "A [ModificationField] member an override could not reach",
            "'{0}.{1}' carries [ModificationField] but {2}. An override is written onto a "
            + "materialized object and has to survive a save, so the member needs both a "
            + "[JsonProperty] and a setter",
            Category, DiagnosticSeverity.Error, true);

        /// <summary> BHS1203 - a zero id, or a band already belonging to a different declaring type. </summary>
        public static readonly DiagnosticDescriptor BandViolation = new(
            "BHS1203",
            "A modification field id outside its declaring type's band",
            "'{0}.{1}' claims 0x{2}, and {3}. The high byte of an id is the band of the type that "
            + "DECLARES the member - one band per declaring type - and 0 is reserved for "
            + "ModificationFields.None",
            Category, DiagnosticSeverity.Error, true);
    }
}
