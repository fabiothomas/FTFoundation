using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
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

    public static readonly DiagnosticDescriptor DuplicateInjectMethodRule = new(
      id: "FTF0001",
      title: "Multiple Inject methods on one class",
      messageFormat: "Type '{0}' declares {1} non-public instance methods named 'Inject'; FTFoundation resolves " +
                     "'Inject' with Type.GetMethod(\"Inject\", ...) by name alone, so this throws an " +
                     "AmbiguousMatchException at startup regardless of differing parameter lists",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "A class may declare at most one non-public instance method named 'Inject'.");

    public static readonly DiagnosticDescriptor InjectPropertyMissingSetterRule = new(
      id: "FTF0002",
      title: "[Inject]/[Config] property has no setter",
      messageFormat: "Property '{0}' is decorated with '[{1}]' but has no setter; FTFoundation assigns it via " +
                     "PropertyInfo.SetValue after construction, which throws at startup for a get-only property",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Properties marked [Inject] or [Config] must have a setter, since the framework populates them via reflection.");

    public static readonly DiagnosticDescriptor ServiceInterfaceMismatchRule = new(
      id: "FTF0003",
      title: "[Service] interface argument doesn't match the implementing type",
      messageFormat: "Type '{0}' is registered with [Service(typeof({1}), ...)] but does not implement '{1}'; " +
                     "resolving this service will fail an InvalidCastException-style failure at runtime",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "The type passed to [Service] must be an interface the decorated class actually implements.");

    public static readonly DiagnosticDescriptor MissingParameterlessConstructorRule = new(
      id: "FTF0004",
      title: "[Service] type has no public parameterless constructor",
      messageFormat: "Type '{0}' is decorated with [Service] but {1}; ServiceCompiler.PrecompileFactory " +
                     "builds instances with Expression.New(...), which requires a public parameterless constructor",
      category: "FTFoundation",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "A [Service]-decorated class must be a concrete type with an accessible public parameterless constructor.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
      ImmutableArray.Create(DuplicateInjectMethodRule, InjectPropertyMissingSetterRule, ServiceInterfaceMismatchRule, MissingParameterlessConstructorRule);

    public override void Initialize(AnalysisContext context)
    {
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
      context.EnableConcurrentExecution();
      context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
      var type = (INamedTypeSymbol)context.Symbol;
      if (type.TypeKind != TypeKind.Class) return;

      CheckDuplicateInjectMethods(context, type);
      CheckInjectConfigPropertiesHaveSetters(context, type);
      CheckServiceAttributeUsage(context, type);
    }

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
