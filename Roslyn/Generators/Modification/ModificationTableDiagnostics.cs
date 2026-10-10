using Microsoft.CodeAnalysis;

namespace BH.SDK.Roslyn.Modification
{
    // The id number is written by hand (nothing can change it behind the author's back), so every wrong way
    // of writing it is an ERROR, never a warning: a duplicate gives one override two fields.

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
