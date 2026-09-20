using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace FTFoundation.Analyzers
{
  // Translates a [ServiceBuildProfile]/[ServiceBuildPlatform] flags value into the Unity scripting
  // define expression that would compile the decorated class in for exactly the same builds.
  public static class BuildGateSymbols
  {
    private readonly struct GateExpression
    {
      public readonly string Text;
      public readonly bool IsCompoundOr;

      public GateExpression(string text, bool isCompoundOr)
      {
        Text = text;
        IsCompoundOr = isCompoundOr;
      }

      public string Parenthesized() => IsCompoundOr ? $"({Text})" : Text;
    }

    private static readonly Dictionary<string, string> ProfileSymbols = new()
    {
      ["Editor"] = "UNITY_EDITOR",
      ["Development"] = "DEVELOPMENT_BUILD",
      ["Staging"] = "STAGING_BUILD",
    };

    // Console/Xbox/PlayStation symbol names have shifted across Unity versions (GameCore vs. the
    // older per-console defines). Ensure these are correct for the Unity version in use.
    private static readonly Dictionary<string, string> PlatformSymbols = new()
    {
      ["Desktop"] = "UNITY_STANDALONE",
      ["Mobile"] = "UNITY_ANDROID || UNITY_IOS",
      ["Console"] = "UNITY_PS4 || UNITY_PS5 || UNITY_GAMECORE || UNITY_XBOXONE || UNITY_SWITCH",
      ["Web"] = "UNITY_WEBGL",
      ["Windows"] = "UNITY_STANDALONE_WIN",
      ["macOS"] = "UNITY_STANDALONE_OSX",
      ["Linux"] = "UNITY_STANDALONE_LINUX",
      ["Android"] = "UNITY_ANDROID",
      ["iOS"] = "UNITY_IOS",
      ["Switch"] = "UNITY_SWITCH",
      ["PlayStation"] = "UNITY_PS4 || UNITY_PS5",
      ["Xbox"] = "UNITY_GAMECORE || UNITY_XBOXONE",
    };

    public static string? ComputeGateExpression(INamedTypeSymbol type, string attributeNamespace)
    {
      var profileAttr = type.GetAttributes().FirstOrDefault(a => IsAttribute(a, attributeNamespace, "ServiceBuildProfileAttribute"));
      var platformAttr = type.GetAttributes().FirstOrDefault(a => IsAttribute(a, attributeNamespace, "ServiceBuildPlatformAttribute"));
      if (profileAttr == null && platformAttr == null) return null;

      GateExpression? profileExpr = profileAttr is { ConstructorArguments.Length: > 0 }
        ? BuildProfileExpression(profileAttr.ConstructorArguments[0]) : null;
      GateExpression? platformExpr = platformAttr is { ConstructorArguments.Length: > 0 }
        ? BuildGenericExpression(platformAttr.ConstructorArguments[0], PlatformSymbols) : null;

      if (profileExpr == null && platformExpr == null) return null;
      if (profileExpr == null) return platformExpr!.Value.Text;
      if (platformExpr == null) return profileExpr!.Value.Text;

      return $"{profileExpr.Value.Parenthesized()} && {platformExpr.Value.Parenthesized()}";
    }

    private static bool IsAttribute(AttributeData a, string ns, string className) =>
      a.AttributeClass?.ContainingNamespace?.ToDisplayString() == ns && a.AttributeClass.Name == className;

    private static GateExpression? BuildProfileExpression(TypedConstant value)
    {
      if (!TryGetFlagMembers(value, out ulong rawValue, out var members)) return null;
      if (IsUnrestricted(members, rawValue)) return null;

      bool Includes(string name)
      {
        var member = members.FirstOrDefault(m => m.Name == name);
        if (member == null) return false;
        ulong memberValue = unchecked((ulong)System.Convert.ToInt64(member.ConstantValue!));
        return memberValue != 0 && (rawValue & memberValue) == memberValue;
      }

      string[] realCases = { "Editor", "Development", "Staging" };

      if (Includes("Production"))
      {
        var missing = realCases.Where(name => !Includes(name)).ToList();
        if (missing.Count == 0) return null; // every case included -> unrestricted

        return new GateExpression(string.Join(" && ", missing.Select(m => $"!{ProfileSymbols[m]}")), isCompoundOr: false);
      }

      var included = realCases.Where(Includes).Select(name => ProfileSymbols[name]).ToList();
      if (included.Count == 0) return null;

      return new GateExpression(string.Join(" || ", included), isCompoundOr: included.Count > 1);
    }

    private static GateExpression? BuildGenericExpression(TypedConstant value, Dictionary<string, string> symbolsByName)
    {
      if (!TryGetFlagMembers(value, out ulong rawValue, out var members)) return null;
      if (IsUnrestricted(members, rawValue)) return null;

      var included = new List<string>();
      foreach (var member in members)
      {
        if (member.Name == "All") continue;
        ulong memberValue = unchecked((ulong)System.Convert.ToInt64(member.ConstantValue!));
        if (memberValue != 0 && (rawValue & memberValue) == memberValue && symbolsByName.TryGetValue(member.Name, out var expr))
          included.Add(expr);
      }

      included = included.Distinct().ToList();
      return included.Count == 0 ? null : new GateExpression(string.Join(" || ", included), isCompoundOr: included.Count > 1);
    }

    private static bool TryGetFlagMembers(TypedConstant value, out ulong rawValue, out List<IFieldSymbol> members)
    {
      rawValue = 0;
      members = new List<IFieldSymbol>();
      if (value.Kind != TypedConstantKind.Enum || value.Type is not INamedTypeSymbol enumType) return false;
      if (value.Value is not { } boxed) return false;

      rawValue = unchecked((ulong)System.Convert.ToInt64(boxed));
      members = enumType.GetMembers().OfType<IFieldSymbol>().Where(f => f.HasConstantValue).ToList();
      return true;
    }

    private static bool IsUnrestricted(List<IFieldSymbol> members, ulong rawValue)
    {
      var allMember = members.FirstOrDefault(m => m.Name == "All");
      return allMember != null && unchecked((ulong)System.Convert.ToInt64(allMember.ConstantValue!)) == rawValue;
    }
  }
}
