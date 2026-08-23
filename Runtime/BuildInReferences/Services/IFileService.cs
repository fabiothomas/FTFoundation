namespace FTFoundation.BuildInReferences
{
    /// <summary>
    /// Service for reading and writing files in the persistent data path.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Reads the entire content of a file at the given path relative to persistentDataPath.
        /// Throws if the file does not exist.
        /// </summary>
        string Read(string relativePath);

        /// <summary>
        /// Tries to read the file at the given path relative to persistentDataPath.
        /// Returns false and sets content to null if the file does not exist.
        /// </summary>
        bool TryRead(string relativePath, out string content);

        /// <summary>
        /// Writes content to a file at the given path relative to persistentDataPath.
        /// Creates any missing directories automatically.
        /// </summary>
        void Write(string relativePath, string content);
    }
}