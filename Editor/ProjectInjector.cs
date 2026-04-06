using UnityEditor;

namespace FTFoundation.Editor
{
    public class ProjectInjector : AssetPostprocessor
    {
        public static string OnGeneratedCSProject(string path, string content)
        {
            // only modify certain .csproj files if you want:
            if (!path.EndsWith(".csproj")) return content;

            // check if your <Nullable> is already there
            if (content.Contains("<Nullable>"))
                return content;

            // Insert the `<Nullable>enable</Nullable>` property inside a PropertyGroup
            const string propertyGroupEnd = "  </PropertyGroup>";
            int insertIndex = content.IndexOf(propertyGroupEnd);
            if (insertIndex >= 0)
            {
                string before = content[..insertIndex];
                string after = content[insertIndex..];
                string toInsert = "    <Nullable>enable</Nullable>\n";
                content = before + toInsert + after;
            }

            return content;
        }
    }
}
