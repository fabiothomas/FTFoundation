using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using System.IO;
using UnityEngine;

namespace FTFoundation.BuildInServices
{
    [Service(typeof(IFileService), ServiceType.SINGLETON)]
    public class FileService : IFileService
    {
        private string BasePath => Application.persistentDataPath;

        public string Read(string relativePath)
        {
            return File.ReadAllText(Path.Combine(BasePath, relativePath));
        }

        public bool TryRead(string relativePath, out string content)
        {
            string fullPath = Path.Combine(BasePath, relativePath);

            if (!File.Exists(fullPath))
            {
                content = "";
                return false;
            }

            content = File.ReadAllText(fullPath);
            return true;
        }

        public void Write(string relativePath, string content)
        {
            string fullPath = Path.Combine(BasePath, relativePath);
            string directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, content);
        }
    }
}