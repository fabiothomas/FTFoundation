#nullable enable
using System;
using System.Collections.Generic;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FTFoundation.BuildInServices
{
    [InstantiateOnStartup]
    [ServiceBuildProfile(BuildTargetProfile.Editor | BuildTargetProfile.Development)]
    [Service(typeof(IDebugScreenService), ServiceType.SINGLETON)]
    public class DebugScreenService : IDebugScreenService
    {
        private IDedicatedObjectService DedicatedObjectService { get; set; } = null!;
        private GameObject DebugScreenObject { get; set; } = null!;

        private GameObject Terminal { get; set; } = null!;
        private GameObject ButtonPanel { get; set; } = null!;
        private GameObject ValueWatcherPanel { get; set; } = null!;
        private Button TerminalButton { get; set; } = null!;
        private Button ButtonPanelButton { get; set; } = null!;
        private Button ValueWatcherPanelButton { get; set; } = null!;

        private TextMeshProUGUI TerminalText { get; set; } = null!;
        private ScrollRect TerminalScrollRect { get; set; } = null!;
        private bool UpdateRequested { get; set; } = false;

        private GameObject ButtonPanelContent { get; set; } = null!;
        private RectTransform ButtonPanelContentRect { get; set; } = null!;
        private Dictionary<Key, List<Button>> HotkeyButtonMap { get; set; } = new();

        private GameObject ValueWatcherPanelContent { get; set; } = null!;
        private RectTransform ValueWatcherPanelContentRect { get; set; } = null!;

        void Inject(IDedicatedObjectService dedicatedObjectService, ILifetimeService lifetimeService)
        {
            DedicatedObjectService = dedicatedObjectService;

            ConstructObject();

            SelectTab("Terminal");
            Toggle(false);

            lifetimeService.OnUpdate(Update);
        }

        #region Construction
        private void ConstructObject()
        {
            DebugScreenObject = DedicatedObjectService.This;

            // Define object as canvas
            DedicatedObjectService.MakeCanvas(out Canvas canvas, out CanvasScaler scaler);

            // Background
            GameObject panel = DedicatedObjectService.ConstructCanvasObject<Image>(UIPosition.Get(
                new UIAnchored { anchor = 0, offset = 160f, size = 300 },
                new UIStretch { anchorMin = 0, anchorMax = 1, offsetMin = 10f, offsetMax = -10f }
            ), "Background", out var backgroundImage);
            backgroundImage.color = new Color(0, 0, 0, 0.5f);

            // Header
            DedicatedObjectService.ConstructCanvasObject<Image>(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -15f, size = 30f }
            ), "Header", out var headerImage, panel);
            headerImage.color = new Color(0, 0, 0, 0.5f);

            // Close Button
            ConstructButton(UIPosition.Get(
                new UIAnchored { anchor = 1f, offset = 15f, size = 20f },
                new UIAnchored { anchor = 1f, offset = -15f, size = 20f }
            ), "X", Color.gray, out var closeButton, panel);
            closeButton.onClick.AddListener(() => Toggle());

            // Tabs
            ConstructTerminal(panel);
            ConstructButtonPanel(panel);
            ConstructValueWatcherPanel(panel);

            // Terminal Button
            ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0.02f, anchorMax = 0.32f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -15f, size = 20f }
            ), "Terminal", Color.gray, out var terminalButton, panel);
            TerminalButton = terminalButton;
            TerminalButton.onClick.AddListener(() => SelectTab("Terminal"));

            // Button Panel Button
            ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0.34f, anchorMax = 0.65f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -15f, size = 20f }
            ), "Buttons", Color.gray, out var buttonPanelButton, panel);
            ButtonPanelButton = buttonPanelButton;
            ButtonPanelButton.onClick.AddListener(() => SelectTab("ButtonPanel"));

            // Value Watcher Panel Button
            ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0.67f, anchorMax = 0.98f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -15f, size = 20f }
            ), "Values", Color.gray, out var valueWatcherPanelButton, panel);
            ValueWatcherPanelButton = valueWatcherPanelButton;
            ValueWatcherPanelButton.onClick.AddListener(() => SelectTab("ValueWatcherPanel"));
        }

        private void ConstructTerminal(GameObject parent)
        {
            // Terminal
            Terminal = DedicatedObjectService.ConstructCanvasEmpty(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -30 }
            ), "Terminal", parent);

            // Terminal ScrollView
            GameObject scrollView = DedicatedObjectService.ConstructCanvasObject<ScrollRect>(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 5, offsetMax = -5 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 5, offsetMax = -5 }
            ), "ScrollView", out var scrollRect, Terminal);
            TerminalScrollRect = scrollRect;
            GameObject viewport = DedicatedObjectService.ConstructCanvasObject<Mask>(UIPosition.FullScreen, "Viewport", out var maskComponent, scrollView);
            maskComponent.showMaskGraphic = false;
            viewport.AddComponent<Image>().isMaskingGraphic = true;
            scrollRect.horizontal = false;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            // Terminal Text
            GameObject textObject = ConstructText(UIPosition.FullScreen, "", Color.white, 14, TextAlignmentOptions.TopLeft, out var textComponent, viewport);
            TerminalText = textComponent;
            scrollRect.content = TerminalText.GetComponent<RectTransform>();
            textComponent.overflowMode = TextOverflowModes.ScrollRect;
            textObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Clear Button
            ConstructButton(UIPosition.Get(
                new UIAnchored { anchor = 1f, offset = 30f, size = 50f },
                new UIAnchored { anchor = 1f, offset = -15f, size = 20f }
            ), "Clear", Color.gray, out var clearButton, Terminal);
            clearButton.onClick.AddListener(() => Clear());
        }

        private void ConstructButtonPanel(GameObject parent)
        {
            // Button Panel
            ButtonPanel = DedicatedObjectService.ConstructCanvasEmpty(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -30 }
            ), "ButtonPanel", parent);

            // Button Panel ScrollView
            GameObject scrollView = DedicatedObjectService.ConstructCanvasObject<ScrollRect>(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 5, offsetMax = -5 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 5, offsetMax = -5 }
            ), "ScrollView", out var scrollRect, ButtonPanel);
            GameObject viewport = DedicatedObjectService.ConstructCanvasObject<Mask>(UIPosition.FullScreen, "Viewport", out var maskComponent, scrollView);
            maskComponent.showMaskGraphic = false;
            viewport.AddComponent<Image>().isMaskingGraphic = true;
            scrollRect.horizontal = false;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            // Button Panel Content
            ButtonPanelContent = DedicatedObjectService.ConstructCanvasEmpty(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = 0, size = 0 }
            ), "ButtonPanelContent", viewport);
            ButtonPanelContentRect = ButtonPanelContent.GetComponent<RectTransform>();
        }

        private void ConstructValueWatcherPanel(GameObject parent)
        {
            // Value Watcher Panel
            ValueWatcherPanel = DedicatedObjectService.ConstructCanvasEmpty(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -30 }
            ), "ValueWatcherPanel", parent);

            // Value Watcher Panel ScrollView
            GameObject scrollView = DedicatedObjectService.ConstructCanvasObject<ScrollRect>(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 5, offsetMax = -5 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 5, offsetMax = -5 }
            ), "ScrollView", out var scrollRect, ValueWatcherPanel);
            GameObject viewport = DedicatedObjectService.ConstructCanvasObject<Mask>(UIPosition.FullScreen, "Viewport", out var maskComponent, scrollView);
            maskComponent.showMaskGraphic = false;
            viewport.AddComponent<Image>().isMaskingGraphic = true;
            scrollRect.horizontal = false;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            // Value Watcher Panel Content
            ValueWatcherPanelContent = DedicatedObjectService.ConstructCanvasEmpty(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = 0, size = 0 }
            ), "ValueWatcherPanelContent", viewport);
            ValueWatcherPanelContentRect = ValueWatcherPanelContent.GetComponent<RectTransform>();
        }
        #endregion

        #region Construction Helpers
        private GameObject ConstructText(UIPosition pos, string text, Color color, int fontSize, TextAlignmentOptions alignment, out TextMeshProUGUI textComponent, GameObject? parent = null)
        {
            GameObject textObject = DedicatedObjectService.ConstructCanvasObject(pos, "Text", out textComponent, parent);
            textComponent.text = text;
            textComponent.color = color;
            textComponent.font = TMP_Settings.defaultFontAsset;
            textComponent.alignment = alignment;
            if (fontSize > 0) textComponent.fontSize = fontSize;

            return textObject;
        }

        public GameObject ConstructButton(UIPosition pos, string label, Color color, out Button buttonComponent, GameObject? parent = null)
        {
            GameObject buttonObject = DedicatedObjectService.ConstructCanvasObject(pos, label, out buttonComponent, parent);
            buttonComponent.targetGraphic = buttonObject.AddComponent<Image>();
            buttonComponent.targetGraphic.color = color;

            ConstructText(UIPosition.FullScreen, label, Color.black, -1, TextAlignmentOptions.Center, out var textComponent, buttonObject);
            textComponent.enableAutoSizing = true;
            textComponent.fontSizeMin = 1;
            textComponent.fontSizeMax = 1000;

            return buttonObject;
        }

        public GameObject ConstructInput(UIPosition pos, string placeholder, Color color, out TMP_InputField inputFieldComponent, GameObject? parent = null)
        {
            GameObject inputObject = DedicatedObjectService.ConstructCanvasObject(pos, placeholder, out inputFieldComponent, parent);
            inputFieldComponent.targetGraphic = inputObject.AddComponent<Image>();
            inputFieldComponent.targetGraphic.color = color;

            GameObject textArea = DedicatedObjectService.ConstructCanvasObject<Mask>(UIPosition.FullScreen, "Text Area", out var maskComponent, inputObject);
            maskComponent.showMaskGraphic = false;
            inputFieldComponent.textViewport = textArea.GetComponent<RectTransform>();

            ConstructText(UIPosition.FullScreen, placeholder, Color.black, -1, TextAlignmentOptions.Left, out var textComponent, textArea);
            textComponent.enableAutoSizing = true;
            textComponent.fontSizeMin = 1;
            textComponent.fontSizeMax = 1000;
            inputFieldComponent.textComponent = textComponent;

            ConstructText(UIPosition.FullScreen, placeholder, Color.black, -1, TextAlignmentOptions.Left, out var placeholderComponent, textArea);
            placeholderComponent.enableAutoSizing = true;
            placeholderComponent.fontSizeMin = 1;
            placeholderComponent.fontSizeMax = 1000;
            inputFieldComponent.placeholder = placeholderComponent;

            return inputObject;
        }
        #endregion

        #region Runtime
        private void Update()
        {
            if (UpdateRequested)
            {
                TerminalScrollRect.verticalNormalizedPosition = 0f;
                UpdateRequested = false;
            }

            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                Toggle();
            }

            foreach (var hotkey in HotkeyButtonMap.Keys)
            {
                if (Keyboard.current != null && Keyboard.current[hotkey].wasPressedThisFrame)
                {
                    foreach (var button in HotkeyButtonMap[hotkey])
                    {
                        button.onClick.Invoke();
                    }
                }
            }
        }
        #endregion

        #region Functionality
        public void Toggle(bool? active = null)
        {
            DebugScreenObject.SetActive(active ?? !DebugScreenObject.activeSelf);
        }

        public void Print(string message)
        {
            TerminalText.text += $"{message}\n";
            UpdateRequested = true;
        }

        public void Clear()
        {
            TerminalText.text = "";
        }

        public IDisposable AddButton(string label, Action onClick, Color? color = null, Key? hotkey = null)
        {
            float offset = ButtonPanelContent.transform.childCount * 25f + 15f;
            ButtonPanelContentRect.sizeDelta = new Vector2(0, offset + 10f);

            GameObject button = ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -30 },
                new UIAnchored { anchor = 1f, offset = -offset, size = 20f }
            ), label, color ?? Color.gray, out var buttonComponent, ButtonPanelContent);
            buttonComponent.onClick.AddListener(() => onClick());

            if (hotkey != null)
            {
                if (!HotkeyButtonMap.ContainsKey(hotkey.Value))
                {
                    HotkeyButtonMap[hotkey.Value] = new List<Button>();
                }
                HotkeyButtonMap[hotkey.Value].Add(buttonComponent);

                ConstructText(UIPosition.Get(
                    new UIAnchored { anchor = 1f, offset = 15f, size = 20f },
                    new UIAnchored { anchor = 1f, offset = -10f, size = 20f }
                ), hotkey.ToString() ?? "", Color.white, 14, TextAlignmentOptions.Center, out var textComponent, button);
            }

            return new DelegateDisposable(() => RemoveButton(button, hotkey));
        }

        private void RemoveButton(GameObject button, Key? hotkey)
        {
            if (hotkey != null && HotkeyButtonMap.ContainsKey(hotkey.Value))
            {
                HotkeyButtonMap[hotkey.Value].Remove(button.GetComponent<Button>());
                if (HotkeyButtonMap[hotkey.Value].Count == 0)
                {
                    HotkeyButtonMap.Remove(hotkey.Value);
                }
            }
            UnityEngine.Object.Destroy(button);
        }

        public IDisposable AddValueWatcher(string label, Saveable<string> saveable, Color? color = null)
        {
            float offset = ValueWatcherPanelContent.transform.childCount * 25f + 15f;
            ValueWatcherPanelContentRect.sizeDelta = new Vector2(0, offset + 10f);

            GameObject inputObject = ConstructInput(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -50 },
                new UIAnchored { anchor = 1f, offset = -offset, size = 20f }
            ), label, color ?? Color.gray, out var inputFieldComponent, ValueWatcherPanelContent);
            inputFieldComponent.text = saveable.Get();
            inputFieldComponent.onValueChanged.AddListener(value => saveable.Set(value));
            IDisposable binding = saveable.Bind(value => inputFieldComponent.text = value);

            GameObject saveButtonObject = ConstructButton(UIPosition.Get(
                new UIAnchored { anchor = 1f, offset = -35f, size = 20f },
                new UIAnchored { anchor = 1f, offset = -offset, size = 20f }
            ), "S", Color.gray, out var saveButtonComponent, ValueWatcherPanelContent);
            saveButtonComponent.onClick.AddListener(() => saveable.Save());

            GameObject restoreButtonObject = ConstructButton(UIPosition.Get(
                new UIAnchored { anchor = 1f, offset = -10f, size = 20f },
                new UIAnchored { anchor = 1f, offset = -offset, size = 20f }
            ), "R", Color.gray, out var restoreButtonComponent, ValueWatcherPanelContent);
            restoreButtonComponent.onClick.AddListener(() => saveable.Restore());

            return new DelegateDisposable(() => RemoveValueWatcher(inputObject, saveButtonObject, restoreButtonObject, binding));
        }

        private void RemoveValueWatcher(GameObject inputObject, GameObject saveButtonObject, GameObject restoreButtonObject, IDisposable binding)
        {
            binding.Dispose();
            UnityEngine.Object.Destroy(inputObject);
            UnityEngine.Object.Destroy(saveButtonObject);
            UnityEngine.Object.Destroy(restoreButtonObject);
        }

        private void SelectTab(string tabName)
        {
            TerminalButton.interactable = tabName != "Terminal";
            ButtonPanelButton.interactable = tabName != "ButtonPanel";
            ValueWatcherPanelButton.interactable = tabName != "ValueWatcherPanel";

            Terminal.SetActive(tabName == "Terminal");
            ButtonPanel.SetActive(tabName == "ButtonPanel");
            ValueWatcherPanel.SetActive(tabName == "ValueWatcherPanel");
        }
        #endregion
    }
}