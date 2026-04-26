#nullable enable
using TMPro;
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

        public void ConstructCanvas();

        public GameObject ConstructEmptyChild(string name, GameObject? parent = null);

        public GameObject ConstructEmptyCanvasChild(UIPosition pos, string name, GameObject? parent = null);

        public GameObject ConstructPanel(UIPosition pos, Color color, GameObject? parent = null);

        public GameObject ConstructScrollView(UIPosition pos, Color? color, GameObject? parent = null);

        public GameObject ConstructText(UIPosition pos, string text, Color color, int fontSize, TextAlignmentOptions alignment, GameObject? parent = null);

        public GameObject ConstructButton(UIPosition pos, string label, Color color, GameObject? parent = null);
    }
}