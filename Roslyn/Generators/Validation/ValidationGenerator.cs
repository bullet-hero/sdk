using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BH.SDK.Roslyn.Validation
{
    // WHY A GENERATOR. RuleAnalyzer walks a level by reflection: per node it asks three dictionaries
    // (is this a container, what are its object rules, what are its properties) and then calls
    // PropertyInfo.GetValue once per property, boxing every value type it reads. On the real corpus
    // that is 1.6 s of the 3.8 s it costs to open volcano in the editor - measured, three passes,
    // 2026-09-03 - and none of it is the rule evaluation itself.
    //
    // A COMPILED-ACCESSOR CACHE IS NOT AN OPTION, which is why generation rather than convenience:
    // System.Reflection.Emit is unavailable on IL2CPP and Expression.Compile degrades to
    // interpretation under AOT, so any "cache a Func<object, object> per property" scheme is fast in
    // the Editor and worthless on the two platforms where a slow validation is actually felt.
    //
    // WHAT IT DELIBERATELY DOES NOT GENERATE is the rules themselves. RuleTable builds the
    // PropertyInfo array and the attribute arrays by the same reflection the analyzer uses, once per
    // type; this only supplies the values and the shape. That keeps RulePath.Property the same
    // object, RuleIssue.Rule the same instance, and both rule arrays in reflection's own order -
    // which decides WHICH finding is reported when a property carries several rules.
    //
    // EXPECT 3-6x, NOT 50x. The .blob codec's 49x came from reflection being the whole cost of
    // reading; here it is part of it. What survives is the virtual rule.IsValid(object, RuleContext)
    // and the boxing at that boundary, and removing those is a different piece of work.

    /// <summary> Writes the walk for every [RuleContainer] type. </summary>
    [Generator]
    public sealed class ValidationGenerator : IIncrementalGenerator
    {
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

        // [RuleContainer] IS INHERITED, and ForAttributeWithMetadataName matches DECLARED attributes
        // only - so a type that is a container purely by inheritance is invisible here and keeps the
        // reflective walk. That is correct rather than a gap, and it is bounded: measured, exactly
        // two types in the SDK were in that state, and BH.SDK.Tests' RuleContainerCoverageTests is
        // what keeps the number at zero now that both declare it.

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
            public ValidationResult(ValidationSpec spec, ImmutableArray<Diagnostic> diagnostics)
            {
                Spec = spec;
                Diagnostics = diagnostics;
            }

            public ValidationSpec Spec { get; }
            public ImmutableArray<Diagnostic> Diagnostics { get; }

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

            public override bool Equals(object obj) => obj is ValidationResult other && Equals(other);

            public override int GetHashCode() => Spec?.GetHashCode() ?? 0;
        }
    }
}
