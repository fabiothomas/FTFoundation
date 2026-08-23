namespace FTFoundation.BuildInReferences
{
    /// <summary>
    /// Service for saving and loading serialized data associated with string ids.
    /// </summary>
    public interface IFileSaveService
    {
        /// <summary>
        /// Stores a serialized value associated with the given id.
        /// Marks the service as dirty; the data will be persisted on the next flush.
        /// </summary>
        void Set(string id, string serializedValue);

        /// <summary>
        /// Returns the serialized value for the given id, or null if not found.
        /// </summary>
        string Get(string id);

        /// <summary>
        /// Immediately persists all dirty data to disk.
        /// </summary>
        void Flush();
    }
}
