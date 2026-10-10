using Microsoft.CodeAnalysis;

namespace BH.SDK.Roslyn.Validation
{
    // Every refusal is loud: a different walk order or property set reports the same findings under wrong
    // paths. BHS1101 and BHS1107 are RETIRED, never reissued - they described legal states
    // (Docs/ROSLYN_GENERATORS.md).

    /// <summary> Diagnostics the validation generator reports. </summary>
    internal static class ValidationDiagnostics
    {
        private const string Category = "BH.SDK.Validation";

        /// <summary> BHS1102 - a property hiding a base one, where the walk cannot tell which was meant. </summary>
        public static readonly DiagnosticDescriptor HiddenProperty = new(
            "BHS1102",
            "A walked property hides an inherited one",
            "'{0}.{1}' hides an inherited property of the same name. The walk's property list is "
            + "Type.GetProperties', which is derived-first and dedupes a hidden pair differently on "
            + "different runtimes - so the generated order and the reflective order disagree and "
            + "the report changes silently. Rename one of them",
            Category, DiagnosticSeverity.Error, true);

        /// <summary> BHS1103 - only one accessor overridden, so the pair does not agree about what it is. </summary>
        public static readonly DiagnosticDescriptor SplitAccessorOverride = new(
            "BHS1103",
            "A walked property is overridden with a different accessor set",
            "'{0}.{1}' overrides a property that declares a {2} it does not. Reflection then reports "
            + "CanRead/CanWrite differently for the two, and the property enters or leaves the walk "
            + "depending on which one wins - declare both accessors",
            Category, DiagnosticSeverity.Error, true);

        /// <summary> BHS1104 - an indexer, which the reflective walk never visited either. </summary>
        public static readonly DiagnosticDescriptor Indexer = new(
            "BHS1104",
            "A [RuleContainer] type must not declare a public indexer",
            "'{0}' declares a public instance indexer. GetProperties returns it and CanRead/CanWrite "
            + "are both true, so the walk reaches it and PropertyInfo.GetValue(target) throws "
            + "TargetParameterCountException halfway through a level - make it a method",
            Category, DiagnosticSeverity.Error, true);

        /// <summary> BHS1105 - accessors of differing accessibility, where what the walk may touch is ambiguous. </summary>
        public static readonly DiagnosticDescriptor MixedAccessorAccessibility = new(
            "BHS1105",
            "A walked property's accessors disagree about being public",
            "'{0}.{1}' is public but its {2} is not. BindingFlags.Public admits a property when ANY "
            + "accessor is public, so whether this is walked depends on which accessor is read - "
            + "give both accessors the property's own accessibility",
            Category, DiagnosticSeverity.Error, true);

        /// <summary> BHS1108 - a rule on a property the walk never reaches, so it would never run. </summary>
        public static readonly DiagnosticDescriptor RuleOnUnwalkedProperty = new(
            "BHS1108",
            "A rule on a property the walk never reads",
            "'{0}.{1}' carries a rule attribute but is not part of the walk ({2}), so that rule has "
            + "never run once and never will. Give the property a setter, or drop the rule",
            Category, DiagnosticSeverity.Error, true);
    }
}
