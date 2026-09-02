using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BH.SDK.Roslyn
{
    /// <summary>
    /// Probe analyzer: reports BHS0001 on every type whose name ends with "RoslynProbe".
    /// Existence of the warning in the Unity console is the proof that Unity loaded this assembly
    /// as an analyzer at all - it says nothing about whether analyzers are useful yet.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SandboxProbeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: "BHS0001",
            title: "Roslyn analyzer probe",
            messageFormat: "Analyzer alive: saw type '{0}' in assembly '{1}'",
            category: "BH.Sandbox",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Fires on any type named *RoslynProbe, to prove the analyzer runs.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (!symbol.Name.EndsWith("RoslynProbe", System.StringComparison.Ordinal))
                return;

            var assembly = context.Compilation.AssemblyName ?? "<unknown>";
            foreach (var location in symbol.Locations)
            {
                if (!location.IsInSource)
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, location, symbol.Name, assembly));
            }
        }
    }
}
