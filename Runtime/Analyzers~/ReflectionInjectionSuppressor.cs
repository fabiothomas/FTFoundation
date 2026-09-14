using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FTFoundation.Analyzers
{
    // Suppresses false-positive nullable/unused-member diagnostics caused by FTFoundation's
    // reflection-based injection: the compiler can't see a call to "Inject" (ServiceCompiler invokes
    // it via a compiled Expression, never from hand-written code) or an assignment into an [Inject]/
    // [Config] property (ConfigLoader/ServiceResolver assign them via PropertyInfo.SetValue after
    // construction), so both look like real problems even though the framework always populates them
    // before any other code runs.
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ReflectionInjectionSuppressor : DiagnosticSuppressor
    {
        private const string UnusedMemberId = "IDE0051";
        private const string UninitializedPropertyId = "CS8618";
        private const string AttributeNamespace = "FTFoundation.Core";

        private static readonly SuppressionDescriptor UnusedInjectMethodRule = new(
            id: "FTFSUPP001",
            suppressedDiagnosticId: UnusedMemberId,
            justification: "FTFoundation invokes a non-public instance method named 'Inject' via reflection at startup; " +
                           "it is never called from hand-written code, so this is not actually unused.");

        private static readonly SuppressionDescriptor UninitializedInjectedPropertyRule = new(
            id: "FTFSUPP002",
            suppressedDiagnosticId: UninitializedPropertyId,
            justification: "FTFoundation assigns [Inject]/[Config] properties via reflection after construction; " +
                           "the compiler can't see that assignment, so this is not actually left uninitialized.");

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
            ImmutableArray.Create(UnusedInjectMethodRule, UninitializedInjectedPropertyRule);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
            {
                if (diagnostic.Id == UnusedMemberId && IsFTFoundationInjectMethod(diagnostic, context))
                    context.ReportSuppression(Suppression.Create(UnusedInjectMethodRule, diagnostic));
                else if (diagnostic.Id == UninitializedPropertyId && IsFTFoundationInjectedProperty(diagnostic, context))
                    context.ReportSuppression(Suppression.Create(UninitializedInjectedPropertyRule, diagnostic));
            }
        }

        private static bool IsFTFoundationInjectMethod(Diagnostic diagnostic, SuppressionAnalysisContext context)
        {
            if (FindDeclaration<MethodDeclarationSyntax>(diagnostic, context) is not { } methodDeclaration)
                return false;
            if (methodDeclaration.Identifier.ValueText != "Inject") return false;

            SemanticModel semanticModel = context.GetSemanticModel(diagnostic.Location.SourceTree!);
            if (semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken) is not IMethodSymbol methodSymbol)
                return false;

            return methodSymbol.MethodKind == MethodKind.Ordinary
                && !methodSymbol.IsStatic
                && methodSymbol.DeclaredAccessibility != Accessibility.Public;
        }

        private static bool IsFTFoundationInjectedProperty(Diagnostic diagnostic, SuppressionAnalysisContext context)
        {
            if (FindDeclaration<PropertyDeclarationSyntax>(diagnostic, context) is not { } propertyDeclaration)
                return false;

            SemanticModel semanticModel = context.GetSemanticModel(diagnostic.Location.SourceTree!);
            if (semanticModel.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken) is not IPropertySymbol propertySymbol)
                return false;

            return propertySymbol.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() == AttributeNamespace
                && attribute.AttributeClass.Name is "InjectAttribute" or "ConfigAttribute");
        }

        private static T? FindDeclaration<T>(Diagnostic diagnostic, SuppressionAnalysisContext context) where T : SyntaxNode
        {
            SyntaxTree? tree = diagnostic.Location.SourceTree;
            if (tree == null) return null;

            SyntaxNode root = tree.GetRoot(context.CancellationToken);
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            return node.FirstAncestorOrSelf<T>();
        }
    }
}
