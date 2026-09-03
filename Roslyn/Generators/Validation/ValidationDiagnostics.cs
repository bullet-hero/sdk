using Microsoft.CodeAnalysis;

namespace BH.SDK.Roslyn.Validation
{
    // EVERY REFUSAL IS LOUD, for the same reason the model generator's are: the defect this replaces
    // is silent. A walk that visits a different set of properties, or the same set in a different
    // order, reports the same findings under the wrong paths and hands RuleFixer a different repair
    // order - and repairs are not commutative. Nothing about that shows up as a compile error, so
    // the constructs that could cause it are compile errors instead.
    //
    // NONE OF THESE IS VIOLATED TODAY: measured across all 200 [RuleContainer] types there are zero
    // new-hidden properties, zero non-public accessors, zero indexers and zero rules on properties
    // the walk cannot reach. They exist to keep it that way, not to clean anything up.
    //
    // TWO REFUSALS WERE TRIED AND WITHDRAWN, and what they ran into is worth stating so nobody adds
    // them back. "A [RuleContainer] must be partial" (BHS1101) and "IValidatable is the generator's
    // to implement" (BHS1107) both describe LEGAL states rather than defects: a non-partial
    // container is exactly what RuleWalk.Node's reflective branch is for, and the two of them
    // together errored on dozens of the private nested fixtures in Tests/Rules - which are the only
    // coverage that fallback has. Making a test fixture partial to satisfy a generator would delete
    // the very path it was written to exercise.
    //
    // What those two were protecting is real, so it moved to where it can be said precisely:
    // BH.SDK.Tests' RuleContainerCoverageTests asserts that every [RuleContainer] IN THE SDK
    // ASSEMBLY has a generated walk. That is scoped to the format's own models, which is what the
    // claim was always about, and a test assembly's fixtures are none of its business.
    //
    // The numbering keeps the gaps: 1101 and 1107 are retired, never reissued.

    /// <summary> Diagnostics the validation generator reports. </summary>
    internal static class ValidationDiagnostics
    {
        private const string Category = "BH.SDK.Validation";

        public static readonly DiagnosticDescriptor HiddenProperty = new(
            "BHS1102",
            "A walked property hides an inherited one",
            "'{0}.{1}' hides an inherited property of the same name. The walk's property list is "
            + "Type.GetProperties', which is derived-first and dedupes a hidden pair differently on "
            + "different runtimes - so the generated order and the reflective order disagree and "
            + "the report changes silently. Rename one of them",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor SplitAccessorOverride = new(
            "BHS1103",
            "A walked property is overridden with a different accessor set",
            "'{0}.{1}' overrides a property that declares a {2} it does not. Reflection then reports "
            + "CanRead/CanWrite differently for the two, and the property enters or leaves the walk "
            + "depending on which one wins - declare both accessors",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor Indexer = new(
            "BHS1104",
            "A [RuleContainer] type must not declare a public indexer",
            "'{0}' declares a public instance indexer. GetProperties returns it and CanRead/CanWrite "
            + "are both true, so the walk reaches it and PropertyInfo.GetValue(target) throws "
            + "TargetParameterCountException halfway through a level - make it a method",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor MixedAccessorAccessibility = new(
            "BHS1105",
            "A walked property's accessors disagree about being public",
            "'{0}.{1}' is public but its {2} is not. BindingFlags.Public admits a property when ANY "
            + "accessor is public, so whether this is walked depends on which accessor is read - "
            + "give both accessors the property's own accessibility",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor RuleOnUnwalkedProperty = new(
            "BHS1108",
            "A rule on a property the walk never reads",
            "'{0}.{1}' carries a rule attribute but is not part of the walk ({2}), so that rule has "
            + "never run once and never will. Give the property a setter, or drop the rule",
            Category, DiagnosticSeverity.Error, true);
    }
}
