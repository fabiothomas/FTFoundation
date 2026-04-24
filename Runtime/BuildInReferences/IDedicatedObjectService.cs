using UnityEngine;

namespace FTFoundation.BuildInReferences
{
    /// <summary>
    /// <para>A service used for creating a dedicated game object.</para>
    /// <para>This service provides a dedicated game object for specific tasks or functionalities.</para>
    /// <para>These game objects will be living in DontDestroyOnLoad and will be automatically destroyed when the application quits.</para>
    /// </summary>
    public interface IDedicatedObjectService
    {
        /// <returns>The dedicated game object for this service.</returns>
        public GameObject Get();
    }
}