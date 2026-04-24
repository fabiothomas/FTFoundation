using System.IO;
using System;
using UnityEditor;
using UnityEngine;

public class AddNameSpaceInTemplate : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] _, string[] __, string[] ___)
    {
        foreach (string path in importedAssets)
        {
            if (!path.EndsWith(".cs")) continue;
            if (path.EndsWith("/AddNameSpaceInTemplate.cs")) continue;
            if (path.EndsWith("\\AddNameSpaceInTemplate.cs")) continue;

            EditorApplication.delayCall += () => TryAddNamespace(path);
        }
    }

    private static void TryAddNamespace(string assetPath)
    {
        string fullPath = Path.GetFullPath(assetPath);

        string content = File.ReadAllText(fullPath);
        if (!content.Contains("#NAMESPACE#")) return;

        string[] segmentedPath = assetPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        int asmRoot = FindAssemblyRootSegment(segmentedPath);
        int startIndex = asmRoot >= 0 ? asmRoot : 1; // fallback: skip "Assets"

        // from assembly root folder up to (not including) the filename
        string[] parts = segmentedPath[startIndex..^1];
        string globalRoot = EditorSettings.projectGenerationRootNamespace;

        if (globalRoot != "" && parts.Length > 0 && parts[0] == globalRoot)
        {
            globalRoot = "";
        }

        string finalNamespace = parts.Length > 0
            ? (globalRoot != "" ? globalRoot + "." : "") + string.Join(".", parts)
            : globalRoot;

        string newContent = content.Replace("#NAMESPACE#", finalNamespace);
        File.WriteAllText(fullPath, newContent);
        AssetDatabase.ImportAsset(assetPath);
    }

    private static int FindAssemblyRootSegment(string[] segments)
    {
        string currentPath = "";
        int found = -1;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            currentPath = i == 0 ? segments[i] : currentPath + "/" + segments[i];
            if (Directory.GetFiles(currentPath, "*.asmdef", SearchOption.TopDirectoryOnly).Length > 0)
                found = i;
        }
        return found;
    }
}
