using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BH.SDK.Roslyn.Validation
{
    // Replaces a reflective walk (boxing GetValue per property) because Reflection.Emit and
    // Expression.Compile are no use on IL2CPP. It generates the walk only, never the rules
    // (Docs/ROSLYN_GENERATORS.md).

    /// <summary> Writes the walk for every [RuleContainer] type. </summary>
    [Generator]
    public sealed class ValidationGenerator : IIncrementalGenerator
    {
        /// <summary> Builds the pipeline: find the marked types, turn each into a spec, emit from the spec alone. </summary>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var containers = context.SyntaxProvider.ForAttributeWithMetadataName(
                ValidationSpecFactory.ContainerAttribute,
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, _) => Transform(ctx));

            context.RegisterSourceOutput(containers, static (production, result) =>
            {
                foreach (var diagnostic in result.Diagnostics) production.ReportDiagnostic(diagnostic);
                if (result.Spec is null) return;

                production.AddSource(result.Spec.HintName,
                    SourceText.From(ValidationEmitter.Emit(result.Spec), Encoding.UTF8));
            });
        }

        // [RuleContainer] is inherited but this matches DECLARED attributes: a container by inheritance
        // alone keeps the reflective walk. RuleContainerCoverageTests keeps that at zero.

        private static ValidationResult Transform(GeneratorAttributeSyntaxContext context)
        {
            var diagnostics = new List<Diagnostic>();

            if (!(context.TargetSymbol is INamedTypeSymbol type)
                || !(context.TargetNode is TypeDeclarationSyntax declaration))
                return new ValidationResult(null, ImmutableArray<Diagnostic>.Empty);

            // A struct container is refused by RuleContainerAnalyzer already, and for a reason this
            // generator would inherit anyway: reflection boxes it on the way in, so every Fix writes
            // into a copy and the repair is lost while IsValid reports forever.
            if (type.IsValueType) return new ValidationResult(null, ImmutableArray<Diagnostic>.Empty);

            var spec = ValidationSpecFactory.Create(type, declaration, diagnostics);
            return new ValidationResult(spec, diagnostics.ToImmutableArray());
        }

        /// <summary> One transform's answer. Diagnostics travel with the spec so a refusal survives
        /// the incremental cache instead of appearing only on a cold build. </summary>
        private sealed class ValidationResult : System.IEquatable<ValidationResult>
        {
            /// <summary> One type's outcome: what to emit, or why nothing will be. </summary>
            public ValidationResult(ValidationSpec spec, ImmutableArray<Diagnostic> diagnostics)
            {
                Spec = spec;
                Diagnostics = diagnostics;
            }

            /// <summary> What to emit, or null when the type was refused. </summary>
            public ValidationSpec Spec { get; }
            /// <summary> Why it was refused, where it was. </summary>
            public ImmutableArray<Diagnostic> Diagnostics { get; }

            /// <summary> Compared by VALUE: an incremental generator that compares its specs by reference re-emits every model on every keystroke. </summary>
            public bool Equals(ValidationResult other)
            {
                if (other is null) return false;
                if (!(Spec is null ? other.Spec is null : Spec.Equals(other.Spec))) return false;
                if (Diagnostics.Length != other.Diagnostics.Length) return false;

                for (var i = 0; i < Diagnostics.Length; i++)
                {
                    if (!Diagnostics[i].Equals(other.Diagnostics[i])) return false;
                }
                return true;
            }

            /// <summary> The same, boxed. </summary>
            public override bool Equals(object obj) => obj is ValidationResult other && Equals(other);

            /// <summary> Compared by VALUE: an incremental generator that compares its specs by reference re-emits every model on every keystroke. </summary>
            public override int GetHashCode() => Spec?.GetHashCode() ?? 0;
        }
    }
}
