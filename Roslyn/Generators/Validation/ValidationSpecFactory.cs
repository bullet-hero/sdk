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
        /// <summary> The marker that opts a type in - INHERITED, which is why declared attributes alone are not enough. </summary>
        public const string ContainerAttribute = "BH.SDK.Rules.Attributes.RuleContainerAttribute";

        private const string PropertyRuleBase = "BH.SDK.Rules.Attributes.BasePropertyRuleAttribute";
        private const string ObjectRuleBase = "BH.SDK.Rules.Attributes.BaseObjectRuleAttribute";
        // The one model type named, Prefab: RuleContext.WithScope takes that concrete type, so a marker
        // interface would not compile for anything else.
        private const string PrefabScope = "BH.SDK.Models.Objects.Prefab";
        private const string Validatable = "BH.SDK.Validations.IValidatable";

        private const string ListType = "System.Collections.Generic.List<T>";
        private const string DictionaryType = "System.Collections.Generic.Dictionary<TKey, TValue>";

        /// <summary> Reads one container into a spec, or refuses it with the diagnostic naming what stopped it. </summary>
        public static ValidationSpec Create(INamedTypeSymbol type, TypeDeclarationSyntax declaration,
            List<Diagnostic> diagnostics)
        {
            // An abstract type is never a runtime type, so the walk never reaches one and there is
            // nothing to write. Its members are not lost: every concrete type below it flattens them
            // into its own list.
            if (type.IsAbstract) return null;

            // Both are LEGAL states, skipped silently: a non-partial container keeps the reflective walk and
            // a hand-written IValidatable already answers the question. Each was a refusal once
            // (Docs/ROSLYN_GENERATORS.md).
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
                type.ToDisplayString() == PrefabScope,
                HasObjectRules(type),
                EquatableArray.From(properties),
                HintName(type));
        }

        #region Properties

        // Must equal GetProperties(Public | Instance).Where(CanRead && CanWrite) in order, DERIVED-FIRST;
        // RuleTable throws if the two disagree.

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

            // A private SETTER does not change membership; a non-public GETTER does, so it is refused.
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

            // Only a sealed non-collection pins the shape; anything else keeps the runtime dispatch (N
            // indexed paths).
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
