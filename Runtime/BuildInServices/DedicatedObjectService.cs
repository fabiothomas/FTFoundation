#nullable enable
using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FTFoundation.BuildInServices
{
    [Service(typeof(IDedicatedObjectService), ServiceType.TRANSIENT)]
    public class DedicatedObjectService : IDedicatedObjectService
    {
        private GameObject This { get; set; } = null!;
        void Inject(IServiceTargetData targetData)
        {
            This = new GameObject($"{targetData.Name}");
            Object.DontDestroyOnLoad(This);
        }

        public GameObject Get()
        {
            return This;
        }

        public void ConstructCanvas()
        {
            Canvas canvas = This.AddComponent<Canvas>();
            CanvasScaler scaler = This.AddComponent<CanvasScaler>();
            This.AddComponent<GraphicRaycaster>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
            canvas.vertexColorAlwaysGammaSpace = true;
            canvas.pixelPerfect = false;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
        }

        public GameObject ConstructEmptyChild(string name, GameObject? parent = null)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent ? parent.transform : This.transform, false);
            return child;
        }

        public GameObject ConstructEmptyCanvasChild(UIPosition pos, string name, GameObject? parent = null)
        {
            GameObject element = ConstructEmptyChild(name, parent);
            RectTransform transform = element.AddComponent<RectTransform>();
            pos.SetTransform(transform);

            if (parent == null) return element;

            if (parent.transform.parent.TryGetComponent<ScrollRect>(out var scrollRect))
            {
                RectTransform rectTransform = element.GetComponent<RectTransform>();
                rectTransform.pivot = new Vector2(0f, 1f);

                scrollRect.content = rectTransform;
            }

            return element;
        }

        private GameObject ConstructCanvasElement(string name, UIPosition pos, GameObject? parent = null)
        {
            GameObject element = ConstructEmptyCanvasChild(pos, name, parent);
            element.AddComponent<CanvasRenderer>();
            return element;
        }

        public GameObject ConstructPanel(UIPosition pos, Color color, GameObject? parent = null)
        {
            GameObject panel = ConstructCanvasElement("Panel", pos, parent);
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        public GameObject ConstructScrollView(UIPosition pos, Color? color = null, GameObject? parent = null)
        {
            GameObject scrollView = ConstructCanvasElement("ScrollView", pos, parent);
            if (color != null) scrollView.AddComponent<Image>().color = color.Value;

            GameObject viewport = ConstructCanvasElement("Viewport", UIPosition.FullScreen, scrollView);
            Image image = viewport.AddComponent<Image>();
            image.isMaskingGraphic = true;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            return viewport;
        }

        public GameObject ConstructText(UIPosition pos, string text, Color color, int fontSize, TextAlignmentOptions alignment, GameObject? parent = null)
        {
            GameObject textObj = ConstructCanvasElement("Text", pos, parent);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.alignment = alignment;

            if (fontSize > 0) tmp.fontSize = fontSize;
            else
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 1;
                tmp.fontSizeMax = 1000;
            }

            if (parent == null) return textObj;

            if (parent.transform.parent.GetComponent<ScrollRect>() != null)
            {
                tmp.overflowMode = TextOverflowModes.ScrollRect;

                textObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            return textObj;
        }

        public GameObject ConstructButton(UIPosition pos, string label, Color color, GameObject? parent = null)
        {
            GameObject button = ConstructCanvasElement("Button", pos, parent);
            Button btn = button.AddComponent<Button>();
            btn.targetGraphic = button.AddComponent<Image>();
            btn.targetGraphic.color = color;

            ConstructText(UIPosition.FullScreen, label, Color.black, -1, TextAlignmentOptions.Center, button).GetComponent<TextMeshProUGUI>();
            return button;
        }
    }
}