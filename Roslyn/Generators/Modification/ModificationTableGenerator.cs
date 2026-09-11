using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BH.SDK.Roslyn.Modification
{
    // WHICH FIELD AN OVERRIDE MEANS, answered at compile time by a flat switch instead of at run
    // time by reflection. What this replaces walked ~200 types at static init to build a dictionary
    // keyed on ([JsonProperty] name, declaring type), parsed a dotted path like "pos[0].v" per
    // apply, and answered an unresolved lookup by returning - silently, with the warning commented
    // out. Three of the twenty-eight live paths did not resolve at all, and nothing could see it.
    //
    // IT KEYS ON THE MEMBER, not on the type, which is what makes inheritance free: the attribute
    // sits on RectObject.Layer and a ShapeObject, a TextObject and an EffectObject all reach it
    // through the same number. The generated Apply takes the base type and casts down only where
    // the declaring type is not the root - so a case is one type test at most, and most are none.
    //
    // WHY THE COLLECTED OUTPUT REPORTS EVERY DIAGNOSTIC, including the per-member ones that could
    // be reported in the transform: a collision is only visible across members, and splitting the
    // reporting would mean a member with two problems naming one of them in this build and the
    // other in the next. One pass, every problem named.

    /// <summary> Writes the field-id-to-member table the modification system applies overrides through. </summary>
    [Generator]
    public sealed class ModificationTableGenerator : IIncrementalGenerator
    {
        /// <summary> The marker that puts a member in the table. </summary>
        internal const string FieldAttribute = "BH.SDK.Models.Attributes.ModificationFieldAttribute";

        private const string JsonAttribute = "Newtonsoft.Json.JsonPropertyAttribute";
        private const string ListType = "System.Collections.Generic.List<T>";

        // The one type the table applies onto, and it is a design limit of the override system
        // rather than of this generator: Modification's own header states that only RectObject and
        // Prefab are override targets. A placement holds overrides for objects inside a template,
        // and every one of those is a RectObject.
        private const string RootType = "global::BH.SDK.Models.Objects.RectObject";

        /// <summary> Builds the pipeline: find the marked members, turn each into a spec, emit from the specs alone. </summary>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var members = context.SyntaxProvider.ForAttributeWithMetadataName(
                FieldAttribute,
                static (node, _) => node is PropertyDeclarationSyntax,
                static (ctx, _) => Transform(ctx));

            context.RegisterSourceOutput(members.Collect(), static (production, collected) =>
            {
                var specs = collected
                    .Where(entry => !entry.Spec.IsEmpty)
                    .Select(entry => entry.Spec)
                    .OrderBy(spec => spec.Field)
                    .ToImmutableArray();

                foreach (var diagnostic in Verify(collected, specs))
                    production.ReportDiagnostic(diagnostic);

                if (specs.Length == 0) return;

                production.AddSource("ModificationTable.g.cs",
                    SourceText.From(ModificationTableEmitter.Emit(specs, RootType), Encoding.UTF8));
            });
        }

        private static FieldResult Transform(GeneratorAttributeSyntaxContext context)
        {
            if (!(context.TargetSymbol is IPropertySymbol property)) return FieldResult.Empty;

            var attribute = context.Attributes.FirstOrDefault();
            if (attribute is null || attribute.ConstructorArguments.Length != 1) return FieldResult.Empty;

            if (!(attribute.ConstructorArguments[0].Value is int field)) return FieldResult.Empty;

            var declaring = property.ContainingType;
            var location = property.Locations.FirstOrDefault() ?? Location.None;

            var reason = Unaddressable(property);
            if (reason != null)
            {
                return FieldResult.Refused(Diagnostic.Create(
                    ModificationTableDiagnostics.UnaddressableMember, location,
                    declaring?.Name, property.Name, reason));
            }

            if (field == 0)
            {
                return FieldResult.Refused(Diagnostic.Create(
                    ModificationTableDiagnostics.BandViolation, location,
                    declaring?.Name, property.Name, field.ToString("X4"),
                    "an id of 0 means no field at all"));
            }

            return new FieldResult(
                new FieldSpec(field, Display(declaring), declaring?.Name, property.Name,
                    Display(property.Type), ElementOf(property.Type)),
                null, location);
        }

        // What a member has to have to carry an override, stated as the reason it does not. Note
        // this is NOT a second encodability check: the model generator already refuses a member it
        // cannot encode (BHS1003), and every type carrying these attributes is one it owns.
        private static string Unaddressable(IPropertySymbol property)
        {
            var serialized = property.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() == JsonAttribute);

            if (!serialized && property.SetMethod is null)
                return "declares no [JsonProperty] and has no setter";
            if (!serialized)
                return "declares no [JsonProperty], so an override of it could never be saved";

            return property.SetMethod is null ? "has no setter, so nothing could write the override" : null;
        }

        // A List is the only shape an index may address into. An array would work as well and no
        // model uses one for an overridable member; when one does, this is where it is added, and
        // the emitter's indexed branch is already written in terms of Count and the indexer.
        private static string ElementOf(ITypeSymbol type)
        {
            if (!(type is INamedTypeSymbol named) || !named.IsGenericType) return null;
            if (named.ConstructedFrom?.ToDisplayString() != ListType) return null;

            return Display(named.TypeArguments[0]);
        }

        private static string Display(ISymbol symbol)
            => symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Everything that can only be seen across members. A band belongs to ONE declaring type, and
        // checking it that way rather than against a hardcoded map means this generator holds no
        // second copy of ModificationFields' band list to keep in lockstep with the first.
        private static IEnumerable<Diagnostic> Verify(
            ImmutableArray<FieldResult> collected, ImmutableArray<FieldSpec> specs)
        {
            foreach (var entry in collected)
            {
                if (entry.Diagnostic != null) yield return entry.Diagnostic;
            }

            var byField = collected
                .Where(entry => !entry.Spec.IsEmpty)
                .OrderBy(entry => entry.Spec.DeclaringName, StringComparer.Ordinal)
                .ThenBy(entry => entry.Spec.Property, StringComparer.Ordinal)
                .GroupBy(entry => entry.Spec.Field);

            foreach (var group in byField)
            {
                var members = group.ToList();
                for (var i = 1; i < members.Count; i++)
                {
                    yield return Diagnostic.Create(
                        ModificationTableDiagnostics.DuplicateField, members[i].Location,
                        Member(members[0].Spec), Member(members[i].Spec), group.Key.ToString("X4"));
                }
            }

            var owners = new Dictionary<int, string>();
            foreach (var spec in specs)
            {
                var band = (spec.Field >> 8) & 0xFF;
                if (!owners.TryGetValue(band, out var owner)) owners.Add(band, spec.DeclaringType);
                else if (owner != spec.DeclaringType) owners[band] = null;
            }

            foreach (var entry in collected)
            {
                if (entry.Spec.IsEmpty) continue;

                var band = (entry.Spec.Field >> 8) & 0xFF;
                if (owners.TryGetValue(band, out var owner) && owner != null) continue;

                yield return Diagnostic.Create(
                    ModificationTableDiagnostics.BandViolation, entry.Location,
                    entry.Spec.DeclaringName, entry.Spec.Property, entry.Spec.Field.ToString("X4"),
                    "band 0x" + band.ToString("X2") + " is claimed by more than one declaring type");
            }
        }

        private static string Member(FieldSpec spec) => spec.DeclaringName + "." + spec.Property;

        /// <summary> One member's spec, or the refusal that replaced it, plus where to report either. </summary>
        private readonly struct FieldResult : IEquatable<FieldResult>
        {
            /// <summary> A property this generator has nothing to say about. </summary>
            public static readonly FieldResult Empty = new FieldResult(FieldSpec.Empty, null, Location.None);

            /// <summary> A member refused, with the diagnostic naming why. </summary>
            public static FieldResult Refused(Diagnostic diagnostic)
                => new FieldResult(FieldSpec.Empty, diagnostic, diagnostic.Location);

            /// <summary> A spec, a refusal, and the location either is reported at. </summary>
            public FieldResult(FieldSpec spec, Diagnostic diagnostic, Location location)
            {
                Spec = spec;
                Diagnostic = diagnostic;
                Location = location;
            }

            /// <summary> The member, when it is in the table. </summary>
            public FieldSpec Spec { get; }
            /// <summary> The refusal, when it is not. </summary>
            public Diagnostic Diagnostic { get; }
            /// <summary> Where to point either one. </summary>
            public Location Location { get; }

            /// <summary> Compared by VALUE, like every spec in every generator here. </summary>
            public bool Equals(FieldResult other)
                => Spec.Equals(other.Spec) && Equals(Diagnostic, other.Diagnostic);

            /// <summary> The same, boxed. </summary>
            public override bool Equals(object obj) => obj is FieldResult other && Equals(other);

            /// <summary> Compared by VALUE, see Equals. </summary>
            public override int GetHashCode() => Spec.GetHashCode();
        }
    }
}
