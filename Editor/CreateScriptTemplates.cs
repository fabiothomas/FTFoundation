#if UNITY_EDITOR
using UnityEditor;

namespace FTFoundation.Editor
{
    internal static class CreateScriptTemplates
    {
        [MenuItem("Assets/Create/Foundation/MonoBehaviour", priority = 0)]
        private static void CreateServiceMonoBehaviourMenuItem()
        {
            string templatePath = "Assets/FTFoundation/Editor/ServiceMonobehaviour.cs.txt";

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewBehaviour.cs");
        }

        [MenuItem("Assets/Create/Foundation/Service", priority = 1)]
        private static void CreateServiceMenuItem()
        {
            string templatePath = "Assets/FTFoundation/Editor/Service.cs.txt";

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewService.cs");
        }

        [MenuItem("Assets/Create/Foundation/ServiceInterface", priority = 2)]
        private static void CreateServiceInterfaceMenuItem()
        {
            string templatePath = "Assets/FTFoundation/Editor/ServiceInterface.cs.txt";

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "INewService.cs");
        }
    }
}
#endif