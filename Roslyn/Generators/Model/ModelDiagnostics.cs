using Microsoft.CodeAnalysis;

namespace BH.SDK.Roslyn.Model
{
    // EVERY REFUSAL IS LOUD, and that is the whole point of the generator existing. The defect it
    // replaces - a member present in Copy and missing from Equals, or an `is T` pasted from a
    // sibling class - is silent by nature: the code compiles and behaves almost right. So a member
    // the generator cannot express is an ERROR naming the member, never a member quietly skipped.

    /// <summary> Diagnostics the model generator reports. </summary>
    internal static class ModelDiagnostics
    {
        private const string Category = "BH.SDK.Model";

        public static readonly DiagnosticDescriptor NotPartial = new(
            "BHS1001",
            "A [GenerateModel] type must be partial",
            "'{0}' carries [GenerateModel] but is not declared partial, so nothing can be generated for it",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor NoParameterlessConstructor = new(
            "BHS1002",
            "A [GenerateModel] type needs a parameterless constructor",
            "'{0}' has no accessible parameterless constructor - the generated Copy() and Reset() " +
            "both build one, and Reset() takes its defaults from what that constructor writes",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor UnsupportedMember = new(
            "BHS1003",
            "A model member the generator cannot express",
            "'{0}.{1}' is of type '{2}', which the generator has no encoding for. Mark it " +
            "[GenerateModelIgnore] and handle it in the partial hooks, or give it a shape the " +
            "generator knows",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor BaseNotGenerated = new(
            "BHS1004",
            "A [GenerateModel] type derives from a model that is not generated",
            "'{0}' derives from the model '{1}', which does not carry [GenerateModel]. The two " +
            "halves of every generated body chain through base, so both ends have to be generated",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor ValueTypeModel = new(
            "BHS1006",
            "[GenerateModel] does not cover a struct",
            "'{0}' is a value type. A struct model is copied by assignment and has no constructor "
            + "body to read defaults from (C# 9), so the generated contract would be a different "
            + "thing wearing the same name - write it by hand, as FrameSpan and ModificationKey do",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor NoTypeTag = new(
            "BHS1008",
            "A polymorphic model needs a discriminator",
            "'{0}' is reachable as a '{1}', so a reader meets it without knowing what to construct - "
            + "but it has no `GetModelType()` returning a constant for the tag to come from. Give it "
            + "one, as every polymorphic value in this format has",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor TagOutOfRange = new(
            "BHS1007",
            "A polymorphic tag does not fit in a byte",
            "'{0}' answers GetModelType() with {1}, and the blob writes a discriminator as one byte "
            + "with 0xFF reserved for null. A family with more than 255 members needs a wider tag",
            Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor MergeOnNonDictionary = new(
            "BHS1005",
            "[GenerateModelMerge] applies to a dictionary only",
            "'{0}.{1}' is not a Dictionary<,>, so there is nothing to merge key by key",
            Category, DiagnosticSeverity.Error, true);
    }
}
