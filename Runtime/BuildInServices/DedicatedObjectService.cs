#nullable enable
using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FTFoundation.BuildInServices
{
    [Service(typeof(IDedicatedObjectService), ServiceType.TRANSIENT)]
    public class DedicatedObjectService : IDedicatedObjectService, IServiceCleanup
    {
        public GameObject This { get; private set; } = null!;

        void Inject(IServiceTargetData targetData)
        {
            This = new GameObject($"{targetData.Name}");
            Object.DontDestroyOnLoad(This);
        }

        public void OnCleanup()
        {
            if (This != null) Object.Destroy(This);
        }

        public void MakeCanvas(out Canvas canvas, out CanvasScaler scaler)
        {
            canvas = This.AddComponent<Canvas>();
            scaler = This.AddComponent<CanvasScaler>();
            This.AddComponent<GraphicRaycaster>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            canvas.vertexColorAlwaysGammaSpace = true;
            canvas.pixelPerfect = false;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        public GameObject ConstructEmpty(Position pos, string name, GameObject? parent = null)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent ? parent.transform : This.transform, false);
            pos.SetTransform(child.transform);
            return child;
        }

        public GameObject ConstructObject<T>(Position pos, string name, out T compontent, GameObject? parent = null) where T : Component
        {
            GameObject child = ConstructEmpty(pos, name, parent);
            compontent = child.AddComponent<T>();
            return child;
        }

        public GameObject ConstructCanvasEmpty(UIPosition pos, string name, GameObject? parent = null)
        {
            GameObject element = ConstructObject<RectTransform>(Position.Default, name, out var rectTransform, parent);
            pos.SetTransform(rectTransform);

            element.AddComponent<CanvasRenderer>();

            if (parent == null) return element;

            if (parent.transform.parent.TryGetComponent<ScrollRect>(out var scrollRect))
            {
                rectTransform.pivot = new Vector2(0f, 1f);
                scrollRect.content = rectTransform;
            }

            return element;
        }

        public GameObject ConstructCanvasObject<T>(UIPosition pos, string name, out T compontent, GameObject? parent = null) where T : Component
        {
            GameObject element = ConstructCanvasEmpty(pos, name, parent);
            compontent = element.AddComponent<T>();
            return element;
        }

        public GameObject ConstructFromPrefab(GameObject prefab, Position pos, string? name = null, GameObject? parent = null)
        {
            GameObject instance = Object.Instantiate(prefab, parent ? parent.transform : This.transform);
            if (name != null) instance.name = name;
            pos.SetTransform(instance.transform);
            return instance;
        }

        public GameObject ConstructFromPrefab<T>(GameObject prefab, Position pos, out T compontent, string? name = null, GameObject? parent = null) where T : Component
        {
            GameObject instance = ConstructFromPrefab(prefab, pos, name, parent);
            if (!instance.TryGetComponent(out compontent))
                throw new UnityException($"Prefab '{prefab.name}' does not have a component of type '{typeof(T).Name}'.");
            return instance;
        }
    }
}