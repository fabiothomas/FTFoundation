using UnityEditor;

namespace FTFoundation.Editor
{
    public class Config : EditorWindow
    {
        [MenuItem("Window/FTConfig")]
        public static void ShowWindow()
        {
            GetWindow<Config>("FT Foundation Configuration");
        }
        
        void OnGUI()
        {
            
        }
    }
}
