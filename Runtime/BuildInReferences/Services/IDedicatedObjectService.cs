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

        /// <summary>
        /// Turns this dedicated game object into a canvas with a canvas scaler and graphic raycaster.
        /// </summary>
        /// <param name="canvas">The created canvas component.</param>
        /// <param name="scaler">The created canvas scaler component.</param>
        public void MakeCanvas(out Canvas canvas, out CanvasScaler scaler);

        /// <summary>
        /// Construct an empty game object under this service's dedicated game object.
        /// </summary>
        /// <param name="pos">The position and rotation for the new game object.</param>
        /// <param name="name">The name of the new game object.</param>
        /// <param name="parent">The parent game object. If null, the dedicated game object will be used as the parent.</param>
        /// <returns>The newly created game object.</returns>
        public GameObject ConstructEmpty(Position pos, string name, GameObject? parent = null);

        /// <summary>
        /// Construct a game object with a specific component under this service's dedicated game object.
        /// </summary>
        /// <typeparam name="T">The type of component to add to the new game object.</typeparam>
        /// <param name="pos">The position and rotation for the new game object.</param>
        /// <param name="name">The name of the new game object.</param>
        /// <param name="compontent">The created component of type T.</param>
        /// <param name="parent">The parent game object. If null, the dedicated game object will be used as the parent.</param>
        /// <returns>The newly created game object.</returns>
        public GameObject ConstructObject<T>(Position pos, string name, out T compontent, GameObject? parent = null) where T : Component;

        /// <summary>
        /// Construct an empty game object with a RectTransform under this service's dedicated game object, which is suitable for UI elements.
        /// </summary>
        /// <param name="pos">The layout for the new UI element.</param>
        /// <param name="name">The name of the new UI element.</param>
        /// <param name="parent">The parent game object. If null, the dedicated game object will be used as the parent.</param>
        /// <returns>The newly created UI element.</returns>
        public GameObject ConstructCanvasEmpty(UIPosition pos, string name, GameObject? parent = null);

        /// <summary>
        /// Construct a UI element with a specific component under this service's dedicated game object.
        /// </summary>
        /// <typeparam name="T">The type of component to add to the new UI element.</typeparam>
        /// <param name="pos">The layout for the new UI element.</param>
        /// <param name="name">The name of the new UI element.</param>
        /// <param name="compontent">The created component of type T.</param>
        /// <param name="parent">The parent game object. If null, the dedicated game object will be used as the parent.</param>
        /// <returns>The newly created UI element.</returns>
        public GameObject ConstructCanvasObject<T>(UIPosition pos, string name, out T compontent, GameObject? parent = null) where T : Component;

        /// <summary>
        /// Instantiate a prefab under this service's dedicated game object.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate. The caller is responsible for obtaining this reference (e.g. via a serialized field, Resources.Load, or Addressables).</param>
        /// <param name="pos">The position and rotation for the instantiated prefab.</param>
        /// <param name="name">The name to give the instantiated game object. If null, the prefab's instantiated name is kept.</param>
        /// <param name="parent">The parent game object. If null, the dedicated game object will be used as the parent.</param>
        /// <returns>The instantiated game object.</returns>
        public GameObject ConstructFromPrefab(GameObject prefab, Position pos, string? name = null, GameObject? parent = null);

        /// <summary>
        /// Instantiate a prefab under this service's dedicated game object and retrieve a component from it.
        /// </summary>
        /// <typeparam name="T">The type of component to retrieve from the instantiated prefab.</typeparam>
        /// <param name="prefab">The prefab to instantiate. The caller is responsible for obtaining this reference (e.g. via a serialized field, Resources.Load, or Addressables).</param>
        /// <param name="pos">The position and rotation for the instantiated prefab.</param>
        /// <param name="compontent">The retrieved component of type T.</param>
        /// <param name="name">The name to give the instantiated game object. If null, the prefab's instantiated name is kept.</param>
        /// <param name="parent">The parent game object. If null, the dedicated game object will be used as the parent.</param>
        /// <returns>The instantiated game object.</returns>
        /// <exception cref="UnityException">Thrown if the instantiated prefab does not have a component of type T.</exception>
        public GameObject ConstructFromPrefab<T>(GameObject prefab, Position pos, out T compontent, string? name = null, GameObject? parent = null) where T : Component;
    }
}