using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BH.SDK.Roslyn
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RuleContainerAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "BHSDK.RuleContainerAnalyzer";

        private const string RuleContainerAttributeName = "RuleContainerAttribute";
        private const string RuleContainerName = "RuleContainer";

        private const string Title = "Class with attribute [" + RuleContainerName + "] must apply dependencies";
        private const string MessageFormat = "Class {0} must be non-static, and non-abstract ones " +
                                             "must have a public parameterless constructor";
        private const string Description = "All concrete rule containers must be instantiatable";
        private const string Category = "BHSDK.Rules";

        private const string StructTitle = "[" + RuleContainerName + "] must not be applied to a struct";
        private const string StructMessageFormat = "Struct {0} cannot be a " + RuleContainerName;
        private const string StructDescription =
            "RuleAnalyzer reaches a nested model through reflection, which boxes a struct. Every Fix " +
            "then writes into that box and the repair is lost, while IsValid still reports the issue " +
            "forever. Validate a struct-typed model through its owning property instead (the owner's " +
            "setter writes the whole struct back), or make it a class.";

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Error, true, description: Description);

        private static readonly DiagnosticDescriptor StructRule = new(DiagnosticId + ".Struct", StructTitle,
            StructMessageFormat, Category, DiagnosticSeverity.Error, true, description: StructDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule, StructRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeStructDeclaration, SyntaxKind.StructDeclaration);
        }

        private static void AnalyzeStructDeclaration(SyntaxNodeAnalysisContext context)
        {
            var structDeclaration = (StructDeclarationSyntax)context.Node;
            if (structDeclaration.AttributeLists.Count == 0) return;

            var structSymbol = context.SemanticModel.GetDeclaredSymbol(structDeclaration);
            if (structSymbol == null) return;
            if (!HasRuleContainer(structSymbol)) return;

            var diagnostic = Diagnostic.Create(StructRule,
                structDeclaration.Identifier.GetLocation(), structSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool HasRuleContainer(ISymbol symbol) => symbol.GetAttributes().Any(attributeData =>
            attributeData.AttributeClass?.Name switch
            {
                RuleContainerName => true,
                RuleContainerAttributeName => true,
                _ => false,
            });

        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (classDeclaration.AttributeLists.Count == 0) return;

            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
            if (classSymbol == null) return;

            if (!HasRuleContainer(classSymbol)) return;

            if (classSymbol.IsStatic)
            {
                var diagnostic = Diagnostic.Create(Rule,
                    classDeclaration.Identifier.GetLocation(), classSymbol.Name);
                context.ReportDiagnostic(diagnostic);
                return;
            }

            // Abstract is allowed, and has to be: [RuleContainer] is inherited, so putting it on a
            // base class is how a whole family opts in at once (Resource, BaseGraphicsSettings both
            // do). Nothing ever instantiates the container itself - the Activator calls live in
            // RuleNotNull and the RuleIPrimitiveXxx family, and they construct the *property's* type,
            // which is always a concrete one. Flagging abstract here used to make two shipped classes
            // an error the moment this analyzer was switched on.
            if (classSymbol.IsAbstract) return;

            var hasParameterlessConstructor = classSymbol.Constructors
                .Any(ctor => ctor.DeclaredAccessibility == Accessibility.Public && ctor.Parameters.IsEmpty);
            if (!hasParameterlessConstructor)
            {
                var diagnostic = Diagnostic.Create(Rule,
                    classDeclaration.Identifier.GetLocation(), classSymbol.Name);
                context.ReportDiagnostic(diagnostic);
                return;
            }
        }
    }
}
