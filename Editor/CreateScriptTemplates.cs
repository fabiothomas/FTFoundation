#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace FTFoundation.Editor
{
  internal static class CreateScriptTemplates
  {
    [MenuItem("Assets/Create/Foundation/MonoBehaviour", priority = 0)]
    private static void CreateServiceMonoBehaviourMenuItem()
    {
      string templatePath = $"{GetTemplatesFolder()}/ServiceMonobehaviour.cs.txt";

      ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewBehaviour.cs");
    }

    [MenuItem("Assets/Create/Foundation/Service", priority = 1)]
    private static void CreateServiceMenuItem()
    {
      string templatePath = $"{GetTemplatesFolder()}/Service.cs.txt";

      ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewService.cs");
    }

    [MenuItem("Assets/Create/Foundation/ServiceInterface", priority = 2)]
    private static void CreateServiceInterfaceMenuItem()
    {
      string templatePath = $"{GetTemplatesFolder()}/ServiceInterface.cs.txt";

      ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "INewService.cs");
    }

    [MenuItem("Assets/Create/Foundation/Service-AssemblyInfo", priority = 3)]
    private static void CreateServiceAssemblyInfoMenuItem()
    {
      string templatePath = $"{GetTemplatesFolder()}/AssemblyInfo_Service.cs.txt";

      ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "AssemblyInfo.cs");
    }

    [MenuItem("Assets/Create/Foundation/InjectionTarget-AssemblyInfo", priority = 4)]
    private static void CreateInjectionTargetAssemblyInfoMenuItem()
    {
      string templatePath = $"{GetTemplatesFolder()}/AssemblyInfo_InjectionTarget.cs.txt";

      ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "AssemblyInfo.cs");
    }

    private static string GetTemplatesFolder()
    {
      string[] guids = AssetDatabase.FindAssets($"{nameof(CreateScriptTemplates)} t:MonoScript");
      if (guids.Length == 0)
        throw new FileNotFoundException($"Could not locate {nameof(CreateScriptTemplates)}.cs via AssetDatabase to resolve the Templates folder.");

      string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
      string editorFolder = Path.GetDirectoryName(scriptPath)!.Replace('\\', '/');
      return $"{editorFolder}/Templates";
    }
  }
}
#endif
