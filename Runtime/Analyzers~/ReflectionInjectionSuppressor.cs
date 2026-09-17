using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FTFoundation.Analyzers
{
  // Suppresses false-positive nullable/unused-member diagnostics caused by FTFoundation's
  // reflection-based injection: the compiler can't see a call to "Inject" (ServiceCompiler invokes
  // it via a compiled Expression, never from hand-written code), an assignment into an [Inject]/
  // [Config] property (ConfigLoader/ServiceResolver assign them via PropertyInfo.SetValue after
  // construction), or a field assigned from inside that same Inject() method — so all three look
  // like real problems even though the framework always populates them before any other code runs.
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public sealed class ReflectionInjectionSuppressor : DiagnosticSuppressor
  {
    private const string UnusedMemberId = "IDE0051";
    private const string UninitializedMemberId = "CS8618";
    private const string AttributeNamespace = "FTFoundation.Core";

    private static readonly SuppressionDescriptor UnusedInjectMethodRule = new(
      id: "FTFSUPP001",
      suppressedDiagnosticId: UnusedMemberId,
      justification: "FTFoundation invokes a non-public instance method named 'Inject' via reflection at startup; " +
                     "it is never called from hand-written code, so this is not actually unused.");

    private static readonly SuppressionDescriptor UninitializedInjectedMemberRule = new(
      id: "FTFSUPP002",
      suppressedDiagnosticId: UninitializedMemberId,
      justification: "FTFoundation populates [Inject]/[Config] properties via reflection, and plain fields via the " +
                     "Inject() method, both after construction; the compiler can't see either path, so this is not " +
                     "actually left uninitialized.");

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
      ImmutableArray.Create(UnusedInjectMethodRule, UninitializedInjectedMemberRule);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
      foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
      {
        if (diagnostic.Id == UnusedMemberId && IsFTFoundationInjectMethod(diagnostic, context))
          context.ReportSuppression(Suppression.Create(UnusedInjectMethodRule, diagnostic));
        else if (diagnostic.Id == UninitializedMemberId && IsFTFoundationManagedMember(diagnostic, context))
          context.ReportSuppression(Suppression.Create(UninitializedInjectedMemberRule, diagnostic));
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

      return IsInjectMethod(methodSymbol);
    }

    private static bool IsFTFoundationManagedMember(Diagnostic diagnostic, SuppressionAnalysisContext context)
    {
      SyntaxTree? tree = diagnostic.Location.SourceTree;
      if (tree == null) return false;

      SemanticModel semanticModel = context.GetSemanticModel(tree);
      SyntaxNode root = tree.GetRoot(context.CancellationToken);
      SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

      // Case 1: an [Inject]/[Config] auto-property, the attribute alone is proof it's reflection-populated.
      if (node.FirstAncestorOrSelf<PropertyDeclarationSyntax>() is { } propertyDeclaration
        && semanticModel.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken) is IPropertySymbol propertySymbol)
      {
        return HasInjectOrConfigAttribute(propertySymbol.GetAttributes());
      }

      // Case 2: a plain field. No attribute marks these, so instead check whether the class's own
      // Inject() method unconditionally assigns this exact field. If it does, the framework
      // guarantees it's non-null by the time anything else in the class can run.
      if (node.FirstAncestorOrSelf<VariableDeclaratorSyntax>() is { } declarator
        && semanticModel.GetDeclaredSymbol(declarator, context.CancellationToken) is IFieldSymbol fieldSymbol)
      {
        return IsUnconditionallyAssignedByInjectMethod(fieldSymbol, context);
      }

      return false;
    }

    private static bool HasInjectOrConfigAttribute(ImmutableArray<AttributeData> attributes) =>
      attributes.Any(a =>
        a.AttributeClass?.ContainingNamespace?.ToDisplayString() == AttributeNamespace
        && a.AttributeClass.Name is "InjectAttribute" or "ConfigAttribute");

    private static bool IsInjectMethod(IMethodSymbol methodSymbol) =>
      methodSymbol.MethodKind == MethodKind.Ordinary
      && !methodSymbol.IsStatic
      && methodSymbol.DeclaredAccessibility != Accessibility.Public;

    // Only looks at top-level statements of Inject()'s own body. Deliberately not recursing into
    // if/for/try/nested blocks, so a match here really does mean "assigned unconditionally", not
    // just "assigned somewhere on some path".
    private static bool IsUnconditionallyAssignedByInjectMethod(IFieldSymbol fieldSymbol, SuppressionAnalysisContext context)
    {
      var injectMethod = fieldSymbol.ContainingType.GetMembers("Inject")
        .OfType<IMethodSymbol>()
        .FirstOrDefault(IsInjectMethod);

      if (injectMethod == null) return false;

      foreach (var syntaxRef in injectMethod.DeclaringSyntaxReferences)
      {
        if (syntaxRef.GetSyntax(context.CancellationToken) is not MethodDeclarationSyntax { Body: { } body }) continue;

        SemanticModel methodSemanticModel = context.GetSemanticModel(body.SyntaxTree);

        foreach (var statement in body.Statements)
        {
          if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }) continue;
          if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)) continue;

          var targetSymbol = methodSemanticModel.GetSymbolInfo(assignment.Left, context.CancellationToken).Symbol;
          if (SymbolEqualityComparer.Default.Equals(targetSymbol, fieldSymbol)) return true;
        }
      }

      return false;
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
