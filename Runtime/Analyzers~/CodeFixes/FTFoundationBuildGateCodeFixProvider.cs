using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FTFoundation.Analyzers
{
  // The IDE-only half of FTF0006/FTF0007: inserts or corrects the exact #if the diagnostic already
  // computed (via BuildGateSymbols, shared with the analyzer so they can never disagree). 
  // Kept in its own assembly because CodeFixProvider needs Microsoft.CodeAnalysis.Workspaces, 
  // which the compiler itself never loads.
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FTFoundationBuildGateCodeFixProvider))]
  [Shared]
  public sealed class FTFoundationBuildGateCodeFixProvider : CodeFixProvider
  {
    private const string AttributeNamespace = "FTFoundation.Core";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("FTF0006", "FTF0007");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
      var diagnostic = context.Diagnostics.FirstOrDefault();
      if (diagnostic == null) return;

      var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
      var classDecl = root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ClassDeclarationSyntax>();
      if (classDecl == null) return;

      var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
      if (semanticModel?.GetDeclaredSymbol(classDecl, context.CancellationToken) is not INamedTypeSymbol type) return;

      string? expression = BuildGateSymbols.ComputeGateExpression(type, AttributeNamespace);
      if (expression == null) return;

      if (diagnostic.Id == "FTF0006")
      {
        context.RegisterCodeFix(
          CodeAction.Create(
            title: $"Wrap in '#if {expression}' / '#endif'",
            createChangedDocument: ct => WrapInIfDirectiveAsync(context.Document, classDecl, expression, ct),
            equivalenceKey: nameof(FTFoundationBuildGateCodeFixProvider) + ".Wrap"),
          diagnostic);
      }
      else // FTF0007
      {
        var enclosingIf = BuildGateDirectives.FindEnclosingIfDirective(classDecl);
        if (enclosingIf == null) return;

        context.RegisterCodeFix(
          CodeAction.Create(
            title: $"Replace with '#if {expression}'",
            createChangedDocument: ct => ReplaceConditionAsync(context.Document, enclosingIf.Condition, expression, ct),
            equivalenceKey: nameof(FTFoundationBuildGateCodeFixProvider) + ".Replace"),
          diagnostic);
      }
    }

    private static async Task<Document> WrapInIfDirectiveAsync(Document document, ClassDeclarationSyntax classDecl, string expression, System.Threading.CancellationToken cancellationToken)
    {
      var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

      // Text-level insertion rather than building directive trivia through SyntaxFactory: Roslyn's
      // directive trivia (needing correctly linked #if/#endif via DirectiveStack) is fiddly to
      // construct by hand and easy to get subtly wrong, while inserting plain text at two disjoint
      // offsets and re-parsing is simple and can't produce a mismatched directive pair.
      var newText = text.WithChanges(
        new TextChange(new TextSpan(classDecl.FullSpan.Start, 0), $"#if {expression}{System.Environment.NewLine}"),
        new TextChange(new TextSpan(classDecl.FullSpan.End, 0), $"{System.Environment.NewLine}#endif{System.Environment.NewLine}"));

      return document.WithText(newText);
    }

    private static async Task<Document> ReplaceConditionAsync(Document document, ExpressionSyntax existingCondition, string expression, System.Threading.CancellationToken cancellationToken)
    {
      var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
      var newText = text.WithChanges(new TextChange(existingCondition.Span, expression));
      return document.WithText(newText);
    }
  }
}
