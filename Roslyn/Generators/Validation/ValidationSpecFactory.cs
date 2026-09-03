using System.Collections.Generic;
using System.Linq;
using BH.SDK.Roslyn.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BH.SDK.Roslyn.Validation
{
    /// <summary> Turns one [RuleContainer] symbol into a <see cref="ValidationSpec"/>, or into
    /// diagnostics saying why it cannot. </summary>
    internal static class ValidationSpecFactory
    {
        public const string ContainerAttribute = "BH.SDK.Rules.Attributes.RuleContainerAttribute";

        private const string PropertyRuleBase = "BH.SDK.Rules.Attributes.BasePropertyRuleAttribute";
        private const string ObjectRuleBase = "BH.SDK.Rules.Attributes.BaseObjectRuleAttribute";
        private const string FrameScope = "BH.SDK.Models.Interfaces.IFrameScope";
        private const string Validatable = "BH.SDK.Validations.IValidatable";

        private const string ListType = "System.Collections.Generic.List<T>";
        private const string DictionaryType = "System.Collections.Generic.Dictionary<TKey, TValue>";

        public static ValidationSpec Create(INamedTypeSymbol type, TypeDeclarationSyntax declaration,
            List<Diagnostic> diagnostics)
        {
            // An abstract type is never a runtime type, so the walk never reaches one and there is
            // nothing to write. Its members are not lost: every concrete type below it flattens them
            // into its own list.
            if (type.IsAbstract) return null;

            // BOTH OF THESE ARE LEGAL STATES, SKIPPED SILENTLY, and that is a decision rather than
            // laziness - each was a refusal once and each errored on the private nested fixtures in
            // Tests/Rules, which are the only coverage RuleWalk.Node's reflective branch has.
            //
            // A container that is not partial keeps the reflective walk, which is what that branch
            // is for. A container that implements IValidatable by hand already answers the question
            // this generator exists to answer. What used to be claimed here - that every model of
            // the FORMAT has a generated walk - is asserted where it can be scoped to the format:
            // BH.SDK.Tests' RuleContainerCoverageTests.
            if (!declaration.Modifiers.Any(modifier => modifier.ValueText == "partial")) return null;
            if (type.AllInterfaces.Any(i => i.ToDisplayString() == Validatable)) return null;

            var properties = ResolveProperties(type, declaration, diagnostics);
            // Every refusal is collected before any is acted on, so one build names every problem in
            // the type rather than one problem per rebuild.
            if (properties is null) return null;

            return new ValidationSpec(
                type.ContainingNamespace.IsGlobalNamespace
                    ? string.Empty
                    : type.ContainingNamespace.ToDisplayString(),
                type.Name,
                type.DeclaredAccessibility == Accessibility.Public ? "public" : "internal",
                type.IsSealed,
                type.AllInterfaces.Any(i => i.ToDisplayString() == FrameScope),
                HasObjectRules(type),
                EquatableArray.From(properties),
                HintName(type));
        }

        #region Properties

        // THE WHOLE CORRECTNESS OF THE GENERATED WALK IS THIS METHOD. It has to produce exactly what
        // Type.GetProperties(Public | Instance).Where(CanRead && CanWrite) produces, in exactly that
        // order - which is DERIVED-FIRST, each level in declaration order, an override appearing
        // once at the derived position. RuleTable re-derives the same list at runtime and throws if
        // the two disagree, so a mistake here is loud rather than a quietly reordered report.

        private static List<PropertySpec> ResolveProperties(INamedTypeSymbol type,
            TypeDeclarationSyntax declaration, List<Diagnostic> diagnostics)
        {
            var result = new List<PropertySpec>();
            var claimed = new HashSet<string>();
            var failed = false;

            for (var current = type; current != null; current = current.BaseType)
            {
                if (current.SpecialType == SpecialType.System_Object) break;

                foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
                {
                    if (property.IsStatic) continue;

                    if (property.IsIndexer)
                    {
                        if (property.DeclaredAccessibility != Accessibility.Public) continue;

                        diagnostics.Add(Diagnostic.Create(ValidationDiagnostics.Indexer,
                            Where(property, declaration), current.Name));
                        failed = true;
                        continue;
                    }

                    // The most derived declaration of a name wins, exactly as an override wins its
                    // base's slot in reflection. Claimed BEFORE the accessor checks below, so a
                    // base's fuller declaration cannot slip back in under a narrower override.
                    if (!claimed.Add(property.Name)) continue;

                    if (Hides(property))
                    {
                        diagnostics.Add(Diagnostic.Create(ValidationDiagnostics.HiddenProperty,
                            Where(property, declaration), current.Name, property.Name));
                        failed = true;
                        continue;
                    }

                    if (SplitOverride(property, out var missing))
                    {
                        diagnostics.Add(Diagnostic.Create(ValidationDiagnostics.SplitAccessorOverride,
                            Where(property, declaration), current.Name, property.Name, missing));
                        failed = true;
                        continue;
                    }

                    if (MixedAccessibility(property, out var narrower))
                    {
                        diagnostics.Add(Diagnostic.Create(
                            ValidationDiagnostics.MixedAccessorAccessibility,
                            Where(property, declaration), current.Name, property.Name, narrower));
                        failed = true;
                        continue;
                    }

                    // CanRead && CanWrite, and CanWrite is SetMethod != null regardless of the
                    // setter's own accessibility - which is why a `{ get; private set; }` property
                    // IS walked. The generated body only ever reads it, so that costs nothing.
                    var excluded = Excluded(property);
                    if (excluded != null)
                    {
                        if (HasPropertyRules(property))
                        {
                            diagnostics.Add(Diagnostic.Create(
                                ValidationDiagnostics.RuleOnUnwalkedProperty,
                                Where(property, declaration), current.Name, property.Name, excluded));
                            failed = true;
                        }
                        continue;
                    }

                    result.Add(new PropertySpec(property.Name, current.Name,
                        HasPropertyRules(property), Shape(property.Type)));
                }
            }

            return failed ? null : result;
        }

        /// <summary> Why the walk skips this property, or null when it does not. </summary>
        private static string Excluded(IPropertySymbol property)
        {
            if (property.DeclaredAccessibility != Accessibility.Public) return "it is not public";
            if (property.GetMethod == null) return "it has no getter";
            if (property.SetMethod == null) return "it has no setter";
            return null;
        }

        private static bool Hides(IPropertySymbol property)
        {
            if (property.IsOverride) return false;

            for (var basis = property.ContainingType.BaseType; basis != null; basis = basis.BaseType)
            {
                if (basis.GetMembers(property.Name).OfType<IPropertySymbol>().Any()) return true;
            }
            return false;
        }

        private static bool SplitOverride(IPropertySymbol property, out string missing)
        {
            missing = null;
            var overridden = property.OverriddenProperty;
            if (overridden == null) return false;

            if (overridden.GetMethod != null && property.GetMethod == null) missing = "getter";
            else if (overridden.SetMethod != null && property.SetMethod == null) missing = "setter";

            return missing != null;
        }

        private static bool MixedAccessibility(IPropertySymbol property, out string narrower)
        {
            narrower = null;
            if (property.DeclaredAccessibility != Accessibility.Public) return false;

            // A private SETTER is legal and common and does not change membership - CanWrite is true
            // either way. A non-public GETTER does: BindingFlags.Public admits the property because
            // the setter is public, and then CanRead is still true, so which accessor decides is a
            // question nothing here should have to answer.
            if (property.GetMethod != null
                && property.GetMethod.DeclaredAccessibility != Accessibility.Public
                && property.GetMethod.DeclaredAccessibility != Accessibility.NotApplicable)
            {
                narrower = "getter";
            }

            return narrower != null;
        }

        #endregion

        #region Shape

        // A MIRROR OF RuleAnalyzer.IsWalkable, and it has to be exact in its corners: List<string>
        // and Dictionary<string, string> ARE walkable (only the element's IsValueType is tested)
        // while a bare string property is not.

        private static DescentShape Shape(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol array)
                return array.ElementType.IsValueType ? DescentShape.None : DescentShape.Array;

            if (type is INamedTypeSymbol named && named.IsGenericType)
            {
                var definition = named.ConstructedFrom.ToDisplayString();

                if (definition == ListType)
                {
                    return named.TypeArguments[0].IsValueType
                        ? DescentShape.None
                        : DescentShape.List;
                }

                if (definition == DictionaryType)
                {
                    return named.TypeArguments[1].IsValueType
                        ? DescentShape.None
                        : DescentShape.Dictionary;
                }
            }

            if (type.IsValueType || type.SpecialType == SpecialType.System_String)
                return DescentShape.None;

            // A declared type that cannot be a collection at runtime pins the shape; anything else
            // keeps the runtime dispatch the reflective walk has, because a member declared object,
            // an interface or an open base may hold a list and would otherwise get one RulePath
            // where the reflective walk pushes N indexed ones.
            return type.IsSealed && !IsCollection(type) ? DescentShape.One : DescentShape.Runtime;
        }

        private static bool IsCollection(ITypeSymbol type)
        {
            foreach (var contract in type.AllInterfaces)
            {
                var name = contract.ToDisplayString();
                if (name == "System.Collections.IList" || name == "System.Collections.IDictionary")
                    return true;
            }
            return false;
        }

        #endregion

        #region Rules

        private static bool HasPropertyRules(IPropertySymbol property)
            => property.GetAttributes().Any(attribute => Derives(attribute, PropertyRuleBase));

        private static bool HasObjectRules(INamedTypeSymbol type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (current.GetAttributes().Any(attribute => Derives(attribute, ObjectRuleBase)))
                    return true;
            }
            return false;
        }

        private static bool Derives(AttributeData attribute, string baseName)
        {
            for (var current = attribute.AttributeClass; current != null; current = current.BaseType)
            {
                if (current.ToDisplayString() == baseName) return true;
            }
            return false;
        }

        #endregion

        #region Names

        private static string HintName(INamedTypeSymbol type) =>
            type.ToDisplayString().Replace('<', '_').Replace('>', '_').Replace(", ", "_")
            + ".Validation.g.cs";

        private static Location Where(IPropertySymbol property, TypeDeclarationSyntax fallback)
            => property.Locations.FirstOrDefault() ?? fallback.Identifier.GetLocation();

        #endregion
    }
}
