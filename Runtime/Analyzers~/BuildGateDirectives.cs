using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FTFoundation.Analyzers
{
  // Syntax-level helpers for finding and reasoning about the #if wrapping FTF0006/FTF0007 care
  // about. Public (like BuildGateSymbols) so FTFoundation.Analyzers.CodeFixes can reuse
  // FindEnclosingIfDirective when building the FTF0007 fix, without duplicating this logic.
  public static class BuildGateDirectives
  {
    // Finds an #if directive whose #if/#endif span encloses node, if any.
    public static IfDirectiveTriviaSyntax? FindEnclosingIfDirective(SyntaxNode node)
    {
      foreach (var trivia in node.SyntaxTree.GetRoot().DescendantTrivia(descendIntoTrivia: true))
      {
        if (trivia.GetStructure() is not IfDirectiveTriviaSyntax ifDirective) continue;

        var endIf = ifDirective.GetRelatedDirectives().OfType<EndIfDirectiveTriviaSyntax>().FirstOrDefault();
        if (endIf == null) continue; // unterminated/malformed -> ignore rather than misreport

        if (node.SpanStart > ifDirective.SpanStart && node.Span.End < endIf.Span.End)
          return ifDirective;
      }
      return null;
    }

    public static bool HasElifOrElse(IfDirectiveTriviaSyntax ifDirective) =>
      ifDirective.GetRelatedDirectives().Any(d => d is ElifDirectiveTriviaSyntax or ElseDirectiveTriviaSyntax);

    public static bool Covers(ExpressionSyntax existing, string expectedText)
    {
      var expected = SyntaxFactory.ParseExpression(expectedText);

      var names = new HashSet<string>();
      CollectIdentifiers(existing, names);
      CollectIdentifiers(expected, names);
      var nameList = names.ToList();

      if (nameList.Count > 20) return true;

      int combinations = 1 << nameList.Count;
      for (int mask = 0; mask < combinations; mask++)
      {
        var env = new Dictionary<string, bool>();
        for (int i = 0; i < nameList.Count; i++)
          env[nameList[i]] = (mask & (1 << i)) != 0;

        if (Evaluate(expected, env) && !Evaluate(existing, env))
          return false;
      }
      return true;
    }

    private static void CollectIdentifiers(ExpressionSyntax expr, HashSet<string> names)
    {
      switch (expr)
      {
        case IdentifierNameSyntax id:
          names.Add(id.Identifier.ValueText);
          break;
        case PrefixUnaryExpressionSyntax unary:
          CollectIdentifiers(unary.Operand, names);
          break;
        case BinaryExpressionSyntax binary:
          CollectIdentifiers(binary.Left, names);
          CollectIdentifiers(binary.Right, names);
          break;
        case ParenthesizedExpressionSyntax paren:
          CollectIdentifiers(paren.Expression, names);
          break;
      }
    }

    // Covers the grammar C# preprocessor expressions actually support: !, &&, ||, ==, !=, parens,
    // true/false literals, and bare identifiers (a defined-or-not symbol).
    private static bool Evaluate(ExpressionSyntax expr, Dictionary<string, bool> env)
    {
      switch (expr)
      {
        case IdentifierNameSyntax id:
          return env.TryGetValue(id.Identifier.ValueText, out bool v) && v;
        case PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.LogicalNotExpression):
          return !Evaluate(unary.Operand, env);
        case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression):
          return Evaluate(binary.Left, env) && Evaluate(binary.Right, env);
        case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalOrExpression):
          return Evaluate(binary.Left, env) || Evaluate(binary.Right, env);
        case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.EqualsExpression):
          return Evaluate(binary.Left, env) == Evaluate(binary.Right, env);
        case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.NotEqualsExpression):
          return Evaluate(binary.Left, env) != Evaluate(binary.Right, env);
        case ParenthesizedExpressionSyntax paren:
          return Evaluate(paren.Expression, env);
        case LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.TrueLiteralExpression):
          return true;
        case LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.FalseLiteralExpression):
          return false;
        default:
          return true;
      }
    }
  }
}
