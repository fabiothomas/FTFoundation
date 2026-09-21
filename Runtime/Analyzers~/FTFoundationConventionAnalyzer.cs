using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FTFoundation.Analyzers
{
  // Flags two FTFoundation convention violations that compile fine but are guaranteed to fail
  // at runtime, since both are only ever checked via reflection when the container resolves a
  // service. By the time you'd notice, it's a startup exception instead of a compile error.
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public sealed class FTFoundationConventionAnalyzer : DiagnosticAnalyzer
  {
    private const string AttributeNamespace = "FTFoundation.Core";
    private const string DocsBaseUrl = "https://github.com/fabiothomas/FTFoundation/blob/main/Documentation~/analyzers.md";

    public static readonly DiagnosticDescriptor DuplicateInjectMethodRule = new(
      id: "FTF0001",
      title: "Multiple Inject methods on one class",
      messageFormat: "Type '{0}' declares {1} non-public instance methods named 'Inject'",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "A class may declare at most one non-public instance method named 'Inject'. " +
                   "FTFoundation resolves 'Inject' with Type.GetMethod(\"Inject\", ...) by name alone, " +
                   "so multiple throw an AmbiguousMatchException at startup regardless of differing parameter lists.",
      helpLinkUri: DocsBaseUrl + "#ftf0001");

    public static readonly DiagnosticDescriptor InjectPropertyMissingSetterRule = new(
      id: "FTF0002",
      title: "[Inject]/[Config] property has no setter",
      messageFormat: "Property '{0}' is decorated with '[{1}]' but has no setter",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Properties marked [Inject] or [Config] must have a setter. FTFoundation assigns it via " +
                   "PropertyInfo.SetValue after construction, which throws at startup for a get-only property.",
      helpLinkUri: DocsBaseUrl + "#ftf0002");

    public static readonly DiagnosticDescriptor ServiceInterfaceMismatchRule = new(
      id: "FTF0003",
      title: "[Service] interface argument doesn't match the implementing type",
      messageFormat: "Type '{0}' is registered with [Service(typeof({1}), ...)] but does not implement '{1}'",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "The type passed to [Service] must be an interface the decorated class actually implements. " +
                   "Resolving a service without a matching interface will fail an InvalidCastException-style failure at runtime.",
      helpLinkUri: DocsBaseUrl + "#ftf0003");

    public static readonly DiagnosticDescriptor MissingParameterlessConstructorRule = new(
      id: "FTF0004",
      title: "[Service] type has no public parameterless constructor",
      messageFormat: "Type '{0}' is decorated with [Service] but {1}",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "A [Service]-decorated class must be a concrete type with an accessible public parameterless constructor. " +
                   "FTFoundation uses Expression.New(...) to create instances, which requires a public parameterless constructor.",
      helpLinkUri: DocsBaseUrl + "#ftf0004");

    public static readonly DiagnosticDescriptor DirectServiceReferenceRule = new(
      id: "FTF0005",
      title: "Direct reference to a [Service]-decorated type",
      messageFormat: "'{0}' is decorated with [Service] and should not be referenced directly outside of the container",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "FTFoundation only supports interface-based injection. A [Service]-decorated concrete type " +
                   "should never be referenced directly (new, typeof, casts, is/as, field/parameter/local types, " +
                   "base lists, generic type arguments etc.) to ensure proper container usage and code-stripping safety.",
      helpLinkUri: DocsBaseUrl + "#ftf0005");

    public static readonly DiagnosticDescriptor BuildGateNotCompiledOutRule = new(
      id: "FTF0006",
      title: "Service with specific BuildProfile/BuildPlatform isn't excluded from non-matching builds",
      messageFormat: "Type '{0}' is restricted via [ServiceBuildProfile]/[ServiceBuildPlatform], but its " +
                     "declaration isn't wrapped in a matching '#if {1}' / '#endif'",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "A [ServiceBuildProfile]/[ServiceBuildPlatform]-restricted service still compiles into every " +
                   "build regardless of whether it can ever be selected there unless its declaration is also " +
                   "gated behind a matching #if (FTF0005 guarantees a [Service] type is never referenced except through its " +
                   "interface, so this is always safe to do). Ensuring the declaration is properly gated by #if " +
                   "prevents it from being included in non-matching builds as dead code.",
      helpLinkUri: DocsBaseUrl + "#ftf0006");

    public static readonly DiagnosticDescriptor BuildGateMismatchRule = new(
      id: "FTF0007",
      title: "#if doesn't cover every build the attribute allows",
      messageFormat: "Type '{0}' is wrapped in '#if {1}', but its [ServiceBuildProfile]/[ServiceBuildPlatform] " +
                     "attributes needs at least '#if {2}'",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "#if must not be narrower than what [ServiceBuildProfile]/[ServiceBuildPlatform] actually allows. " +
                   "An #if that's broader than the attribute requires isn't flagged. " +
                   "That's wasteful, not wrong, since the container still filters it out at runtime. Skipped " +
                   "for an #if with #elif/#else branches (too ambiguous which branch is 'the' condition to " +
                   "compare), and a symbol outside FTFoundation's own set is treated as a free variable, so an " +
                   "intentionally added extra condition may still surface here.",
      helpLinkUri: DocsBaseUrl + "#ftf0007");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
      ImmutableArray.Create(DuplicateInjectMethodRule, InjectPropertyMissingSetterRule, ServiceInterfaceMismatchRule, MissingParameterlessConstructorRule, DirectServiceReferenceRule, BuildGateNotCompiledOutRule, BuildGateMismatchRule);

    public override void Initialize(AnalysisContext context)
    {
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.EnableConcurrentExecution();
      context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
      context.RegisterSyntaxNodeAction(AnalyzeTypeReference, SyntaxKind.IdentifierName, SyntaxKind.GenericName);
    }

    private static void AnalyzeTypeReference(SyntaxNodeAnalysisContext context)
    {
      var nameSyntax = (SimpleNameSyntax)context.Node;

      // 'var' is itself represented as an IdentifierNameSyntax resolving to the inferred type, which
      // would otherwise double-report alongside the initializer's own, real reference to that type
      // (e.g. "var x = new ConcreteFoo();" reports once for "ConcreteFoo" already).
      if (nameSyntax.Identifier.ValueText == "var") return;

      // A type name inside nameof(...) doesn't need the type to exist at runtime.
      if (IsInsideNameof(nameSyntax)) return;

      var symbolInfo = context.SemanticModel.GetSymbolInfo(nameSyntax, context.CancellationToken);
      if (symbolInfo.Symbol is not INamedTypeSymbol namedType) return;
      if (!HasServiceAttribute(namedType)) return;

      // A [Service] type's own declaration (including any type nested inside it, e.g. a private
      // MonoBehaviour helper that needs to call back into members the interface doesn't expose) is exempt.
      if (IsWithinOwnDeclaration(context, nameSyntax, namedType)) return;

      context.ReportDiagnostic(Diagnostic.Create(DirectServiceReferenceRule, nameSyntax.GetLocation(), namedType.Name));
    }

    private static bool IsWithinOwnDeclaration(SyntaxNodeAnalysisContext context, SyntaxNode node, INamedTypeSymbol serviceType)
    {
      var enclosingTypeDecl = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
      if (enclosingTypeDecl == null) return false;

      var enclosingType = context.SemanticModel.GetDeclaredSymbol(enclosingTypeDecl, context.CancellationToken);
      for (var current = enclosingType; current != null; current = current.ContainingType)
        if (SymbolEqualityComparer.Default.Equals(current, serviceType))
          return true;

      return false;
    }

    private static bool IsInsideNameof(SyntaxNode node)
    {
      var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
      if (invocation?.Expression is not IdentifierNameSyntax { Identifier.ValueText: "nameof" }) return false;
      return invocation.ArgumentList.Arguments.Count == 1 && invocation.ArgumentList.Span.Contains(node.Span);
    }

    private static bool HasServiceAttribute(INamedTypeSymbol type) =>
      type.GetAttributes().Any(a =>
        a.AttributeClass?.ContainingNamespace?.ToDisplayString() == AttributeNamespace
        && a.AttributeClass.Name == "ServiceAttribute");

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
      var type = (INamedTypeSymbol)context.Symbol;
      if (type.TypeKind != TypeKind.Class) return;

      CheckDuplicateInjectMethods(context, type);
      CheckInjectConfigPropertiesHaveSetters(context, type);
      CheckServiceAttributeUsage(context, type);
      CheckBuildGateWrapping(context, type);
    }

    private static void CheckBuildGateWrapping(SymbolAnalysisContext context, INamedTypeSymbol type)
    {
      bool hasBuildGateAttribute = type.GetAttributes().Any(a =>
        IsFTFoundationAttribute(a, "ServiceBuildProfileAttribute") || IsFTFoundationAttribute(a, "ServiceBuildPlatformAttribute"));
      if (!hasBuildGateAttribute) return;

      var declaration = type.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken);
      if (declaration == null) return;

      string? expected = BuildGateSymbols.ComputeGateExpression(type, AttributeNamespace);

      var enclosingIf = BuildGateDirectives.FindEnclosingIfDirective(declaration);
      Location location = type.Locations.FirstOrDefault() ?? Location.None;

      if (enclosingIf == null)
      {
        if (expected == null) return; // unrestricted (e.g. explicit .All) -> nothing to gate
        context.ReportDiagnostic(Diagnostic.Create(BuildGateNotCompiledOutRule, location, type.Name, expected));
        return;
      }

      if (expected == null) return; // attribute is unrestricted -> any existing #if is the author's own business
      if (BuildGateDirectives.HasElifOrElse(enclosingIf)) return; // ambiguous which branch to compare -> skip rather than risk a wrong verdict

      if (!BuildGateDirectives.Covers(enclosingIf.Condition, expected))
        context.ReportDiagnostic(Diagnostic.Create(BuildGateMismatchRule, location, type.Name, enclosingIf.Condition.ToString().Trim(), expected));
    }

    private static bool IsFTFoundationAttribute(AttributeData a, string className) =>
      a.AttributeClass?.ContainingNamespace?.ToDisplayString() == AttributeNamespace && a.AttributeClass.Name == className;

    private static void CheckServiceAttributeUsage(SymbolAnalysisContext context, INamedTypeSymbol type)
    {
      var serviceAttribute = type.GetAttributes().FirstOrDefault(a =>
        a.AttributeClass?.ContainingNamespace?.ToDisplayString() == AttributeNamespace
        && a.AttributeClass.Name == "ServiceAttribute");

      if (serviceAttribute == null) return;

      Location location = serviceAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
        ?? type.Locations.FirstOrDefault() ?? Location.None;

      // FTF0003: the interface argument must be one the class actually implements.
      if (serviceAttribute.ConstructorArguments.Length > 0)
      {
        var interfaceArg = serviceAttribute.ConstructorArguments[0];
        if (interfaceArg.Kind == TypedConstantKind.Type && interfaceArg.Value is ITypeSymbol interfaceType)
        {
          bool implementsInterface = type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
          if (!implementsInterface)
            context.ReportDiagnostic(Diagnostic.Create(ServiceInterfaceMismatchRule, location, type.Name, interfaceType.Name));
        }
      }

      // FTF0004: the class must be instantiable via Expression.New(type), concrete, with a public parameterless constructor.
      if (type.IsAbstract)
      {
        context.ReportDiagnostic(Diagnostic.Create(MissingParameterlessConstructorRule, location, type.Name, "is abstract and cannot be instantiated"));
        return;
      }

      bool hasPublicParameterlessConstructor = type.InstanceConstructors.Any(c =>
        c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);

      if (!hasPublicParameterlessConstructor)
        context.ReportDiagnostic(Diagnostic.Create(MissingParameterlessConstructorRule, location, type.Name, "has no public parameterless constructor"));
    }

    private static void CheckDuplicateInjectMethods(SymbolAnalysisContext context, INamedTypeSymbol type)
    {
      var injectMethods = type.GetMembers()
        .OfType<IMethodSymbol>()
        .Where(m => m.MethodKind == MethodKind.Ordinary
          && m.Name == "Inject"
          && !m.IsStatic
          && m.DeclaredAccessibility != Accessibility.Public)
        .ToList();

      if (injectMethods.Count < 2) return;

      foreach (var method in injectMethods)
        foreach (var location in method.Locations)
          context.ReportDiagnostic(Diagnostic.Create(DuplicateInjectMethodRule, location, type.Name, injectMethods.Count));
    }

    private static void CheckInjectConfigPropertiesHaveSetters(SymbolAnalysisContext context, INamedTypeSymbol type)
    {
      foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
      {
        if (property.SetMethod != null) continue;

        var attribute = property.GetAttributes().FirstOrDefault(a =>
          a.AttributeClass?.ContainingNamespace?.ToDisplayString() == AttributeNamespace
          && a.AttributeClass.Name is "InjectAttribute" or "ConfigAttribute");

        if (attribute == null) continue;

        string attributeName = attribute.AttributeClass!.Name == "InjectAttribute" ? "Inject" : "Config";

        foreach (var location in property.Locations)
          context.ReportDiagnostic(Diagnostic.Create(InjectPropertyMissingSetterRule, location, property.Name, attributeName));
      }
    }
  }
}
