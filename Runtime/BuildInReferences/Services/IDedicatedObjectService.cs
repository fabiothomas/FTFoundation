#nullable enable
using UnityEngine;
using UnityEngine.UI;

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
        public GameObject This { get; }

        public void MakeCanvas(out Canvas canvas, out CanvasScaler scaler);

        public GameObject ConstructEmpty(Position pos, string name, GameObject? parent = null);

        public GameObject ConstructObject<T>(Position pos, string name, out T compontent, GameObject? parent = null) where T : Component;

        public GameObject ConstructCanvasEmpty(UIPosition pos, string name, GameObject? parent = null);

        public GameObject ConstructCanvasObject<T>(UIPosition pos, string name, out T compontent, GameObject? parent = null) where T : Component;
    }
}