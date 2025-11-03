#if UNITY_EDITOR
using UnityEditor;

namespace Scripts.Foundation.Templates
{
    internal static class CreateScriptTemplates
    {
        [MenuItem("Assets/Create/Foundation/MonoBehaviour", priority = 0)]
        private static void CreateServiceMonoBehaviourMenuItem()
        {
            string templatePath = "Assets/Scripts/Foundation/Templates/ServiceMonobehaviour.cs.txt";

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewBehaviour.cs");
        }

        [MenuItem("Assets/Create/Foundation/Service", priority = 1)]
        private static void CreateServiceMenuItem()
        {
            string templatePath = "Assets/Scripts/Foundation/Templates/Service.cs.txt";

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewService.cs");
        }

        [MenuItem("Assets/Create/Foundation/ServiceInterface", priority = 2)]
        private static void CreateServiceInterfaceMenuItem()
        {
            string templatePath = "Assets/Scripts/Foundation/Templates/ServiceInterface.cs.txt";

            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "INewService.cs");
        }
    }
}
#endif