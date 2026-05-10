#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        private Sprite panelImage = null!;

        private static readonly float MenuWidth = 400f;
        private static readonly float ItemRadius = 15f;
        private static readonly float PixelPerUnitMultiplier = 10f;
        private static readonly float GapSize = 10f;
        private static readonly float FontSize = 25f;

        private static readonly float ItemSize = ItemRadius * 2;
        private static readonly float ItemSpace = ItemSize + GapSize;
        private static readonly float HeaderHeight = ItemSize + (GapSize * 2);

        void Inject(IDedicatedObjectService dedicatedObjectService, ILifetimeService lifetimeService)
        {
            DedicatedObjectService = dedicatedObjectService;

            panelImage = Resources.Load<Sprite>("square-rounded-512");
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
            // scaler.scaleFactor = 0.5f;

            // Background
            GameObject panel = DedicatedObjectService.ConstructCanvasObject<Image>(UIPosition.Get(
                new UIAnchored { anchor = 0, offset = MenuWidth / 2 + GapSize * 2, size = MenuWidth },
                new UIStretch { anchorMin = 0, anchorMax = 1, offsetMin = GapSize * 2, offsetMax = -GapSize * 2 }
            ), "Background", out var backgroundImage);
            backgroundImage.color = new Color(0, 0, 0, 0.5f);
            backgroundImage.sprite = panelImage;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.pixelsPerUnitMultiplier = PixelPerUnitMultiplier;

            // Header
            DedicatedObjectService.ConstructCanvasObject<Image>(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -HeaderHeight / 2, size = HeaderHeight }
            ), "Header", out var headerImage, panel);
            headerImage.color = new Color(0, 0, 0, 0.5f);
            headerImage.sprite = panelImage;
            headerImage.type = Image.Type.Sliced;
            headerImage.pixelsPerUnitMultiplier = PixelPerUnitMultiplier;

            // Close Button
            ConstructButton(UIPosition.Get(
                new UIAnchored { anchor = 1f, offset = ItemRadius + GapSize, size = ItemSize },
                new UIAnchored { anchor = 1f, offset = -(ItemRadius + GapSize), size = ItemSize }
            ), "X", Color.gray, out var closeButton, panel);
            closeButton.onClick.AddListener(() => Toggle());

            // Tabs
            ConstructTerminal(panel);
            ConstructButtonPanel(panel);
            ConstructValueWatcherPanel(panel);

            // Terminal Button
            ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0.02f, anchorMax = 0.32f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -(ItemRadius + GapSize), size = ItemSize }
            ), "Terminal", Color.gray, out var terminalButton, panel);
            TerminalButton = terminalButton;
            TerminalButton.onClick.AddListener(() => SelectTab("Terminal"));

            // Button Panel Button
            ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0.34f, anchorMax = 0.65f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -(ItemRadius + GapSize), size = ItemSize }
            ), "Buttons", Color.gray, out var buttonPanelButton, panel);
            ButtonPanelButton = buttonPanelButton;
            ButtonPanelButton.onClick.AddListener(() => SelectTab("ButtonPanel"));

            // Value Watcher Panel Button
            ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0.67f, anchorMax = 0.98f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -(ItemRadius + GapSize), size = ItemSize }
            ), "Values", Color.gray, out var valueWatcherPanelButton, panel);
            ValueWatcherPanelButton = valueWatcherPanelButton;
            ValueWatcherPanelButton.onClick.AddListener(() => SelectTab("ValueWatcherPanel"));
        }

        private void ConstructTerminal(GameObject parent)
        {
            // Terminal
            Terminal = DedicatedObjectService.ConstructCanvasEmpty(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -HeaderHeight }
            ), "Terminal", parent);

            // Terminal ScrollView
            GameObject scrollView = DedicatedObjectService.ConstructCanvasObject<ScrollRect>(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -GapSize },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -GapSize }
            ), "ScrollView", out var scrollRect, Terminal);
            TerminalScrollRect = scrollRect;
            GameObject viewport = DedicatedObjectService.ConstructCanvasObject<Mask>(UIPosition.FullScreen, "Viewport", out var maskComponent, scrollView);
            maskComponent.showMaskGraphic = false;
            viewport.AddComponent<Image>().isMaskingGraphic = true;
            scrollRect.horizontal = false;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            // Terminal Text
            GameObject textObject = ConstructText(UIPosition.FullScreen, "", Color.white, FontSize, TextAlignmentOptions.TopLeft, out var textComponent, viewport);
            TerminalText = textComponent;
            scrollRect.content = TerminalText.GetComponent<RectTransform>();
            textComponent.overflowMode = TextOverflowModes.ScrollRect;
            textObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Clear Button
            ConstructButton(UIPosition.Get(
                new UIAnchored { anchor = 1f, offset = (2.5f * ItemSize) / 2 + GapSize, size = 2.5f * ItemSize },
                new UIAnchored { anchor = 1f, offset = -(ItemRadius + GapSize), size = ItemSize }
            ), "Clear", Color.gray, out var clearButton, Terminal);
            clearButton.onClick.AddListener(() => Clear());
        }

        private void ConstructButtonPanel(GameObject parent)
        {
            // Button Panel
            ButtonPanel = DedicatedObjectService.ConstructCanvasEmpty(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -HeaderHeight }
            ), "ButtonPanel", parent);

            // Filter
            GameObject filterObject = ConstructInput(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -GapSize },
                new UIAnchored { anchor = 1f, offset = -(ItemRadius + GapSize), size = ItemSize }
            ), "", "Search...", Color.gray, out var filterInput, ButtonPanel);
            filterInput.onValueChanged.AddListener(value => RefreshContent(ButtonPanelContent.transform, ButtonPanelContentRect, value, ItemSize));

            // Button Panel ScrollView
            GameObject scrollView = DedicatedObjectService.ConstructCanvasObject<ScrollRect>(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -GapSize },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -HeaderHeight }
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
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -HeaderHeight }
            ), "ValueWatcherPanel", parent);

            // Filter
            GameObject filterObject = ConstructInput(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -GapSize },
                new UIAnchored { anchor = 1f, offset = -(ItemRadius + GapSize), size = ItemSize }
            ), "", "Search...", Color.gray, out var filterInput, ValueWatcherPanel);
            filterInput.onValueChanged.AddListener(value => RefreshContent(ValueWatcherPanelContent.transform, ValueWatcherPanelContentRect, value, ItemSize * 2));

            // Value Watcher Panel ScrollView
            GameObject scrollView = DedicatedObjectService.ConstructCanvasObject<ScrollRect>(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -GapSize },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -HeaderHeight }
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
        private GameObject ConstructText(UIPosition pos, string text, Color color, float fontSize, TextAlignmentOptions alignment, out TextMeshProUGUI textComponent, GameObject? parent = null)
        {
            GameObject textObject = DedicatedObjectService.ConstructCanvasObject(pos, "Text", out textComponent, parent);
            textComponent.text = text;
            textComponent.color = color;
            textComponent.font = TMP_Settings.defaultFontAsset;
            textComponent.alignment = alignment;
            if (fontSize > 0) textComponent.fontSize = fontSize;

            return textObject;
        }

        public GameObject ConstructButton(UIPosition pos, string label, Color color, out Button buttonComponent, GameObject? parent = null, TextAlignmentOptions textAlignment = TextAlignmentOptions.Center, float labelPadding = 0)
        {
            GameObject buttonObject = DedicatedObjectService.ConstructCanvasObject(pos, label, out buttonComponent, parent);
            Image backgroundImage = buttonObject.AddComponent<Image>();
            backgroundImage.sprite = panelImage;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.pixelsPerUnitMultiplier = PixelPerUnitMultiplier;
            buttonComponent.targetGraphic = backgroundImage;
            buttonComponent.targetGraphic.color = color;

            ConstructText(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -labelPadding },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 }
            ), label, Color.black, -1, textAlignment, out var textComponent, buttonObject);
            textComponent.enableAutoSizing = true;
            textComponent.fontSizeMin = 1;
            textComponent.fontSizeMax = FontSize;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            textComponent.overflowMode = TextOverflowModes.Overflow;

            return buttonObject;
        }

        public GameObject ConstructInput(UIPosition pos, string label, string placeholder, Color color, out TMP_InputField inputFieldComponent, GameObject? parent = null)
        {
            GameObject inputObject = DedicatedObjectService.ConstructCanvasObject(pos, label, out inputFieldComponent, parent);
            Image backgroundImage = inputObject.AddComponent<Image>();
            backgroundImage.sprite = panelImage;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.pixelsPerUnitMultiplier = PixelPerUnitMultiplier;
            inputFieldComponent.targetGraphic = backgroundImage;
            inputFieldComponent.targetGraphic.color = color;

            if (label != "")
            {
                ConstructText(UIPosition.Get(
                    new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -(ItemSize * 2 + GapSize * 3) },
                    new UIStretch { anchorMin = 0.5f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 }
                ), label, Color.black, -1, TextAlignmentOptions.Left, out var labelText, inputObject);
                labelText.enableAutoSizing = true;
                labelText.fontSizeMin = 1;
                labelText.fontSizeMax = FontSize;
                labelText.textWrappingMode = TextWrappingModes.NoWrap;
                labelText.overflowMode = TextOverflowModes.Overflow;
                labelText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            }

            GameObject textArea = DedicatedObjectService.ConstructCanvasObject<Mask>(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -GapSize },
                new UIStretch { anchorMin = 0f, anchorMax = label == "" ? 1 : 0.5f, offsetMin = 0, offsetMax = 0 }
            ), "Text Area", out var maskComponent, inputObject);
            maskComponent.showMaskGraphic = false;
            inputFieldComponent.textViewport = textArea.GetComponent<RectTransform>();

            ConstructText(UIPosition.FullScreen, placeholder, Color.black, -1, TextAlignmentOptions.Left, out var inputText, textArea);
            inputText.enableAutoSizing = true;
            inputText.fontSizeMin = 1;
            inputText.fontSizeMax = FontSize;
            inputText.overflowMode = TextOverflowModes.Overflow;
            inputFieldComponent.textComponent = inputText;

            ConstructText(UIPosition.FullScreen, placeholder, Color.black, -1, TextAlignmentOptions.Left, out var placeholderText, textArea);
            placeholderText.enableAutoSizing = true;
            placeholderText.fontSizeMin = 1;
            placeholderText.fontSizeMax = FontSize;
            placeholderText.overflowMode = TextOverflowModes.Overflow;
            placeholderText.fontStyle = FontStyles.Italic;
            inputFieldComponent.placeholder = placeholderText;
            WrapModeFixer.Fix(inputObject, inputText, placeholderText);

            return inputObject;
        }

        public GameObject ConstructToggle(UIPosition pos, string label, Color color, out Toggle toggleComponent, GameObject? parent = null)
        {
            GameObject toggleObject = DedicatedObjectService.ConstructCanvasObject(pos, label, out toggleComponent, parent);
            Image backgroundImage = toggleObject.AddComponent<Image>();
            backgroundImage.sprite = panelImage;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.pixelsPerUnitMultiplier = PixelPerUnitMultiplier;
            toggleComponent.targetGraphic = backgroundImage;
            toggleComponent.targetGraphic.color = color;

            if (label != "")
            {
                ConstructText(UIPosition.Get(
                    new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -(ItemSize * 2 + GapSize * 3) },
                    new UIStretch { anchorMin = 0.5f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 }
                ), label, Color.black, -1, TextAlignmentOptions.Left, out var labelText, toggleObject);
                labelText.enableAutoSizing = true;
                labelText.fontSizeMin = 1;
                labelText.fontSizeMax = FontSize;
                labelText.textWrappingMode = TextWrappingModes.NoWrap;
                labelText.overflowMode = TextOverflowModes.Overflow;
                labelText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            }

            ConstructText(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = GapSize, offsetMax = -GapSize },
                new UIStretch { anchorMin = 0f, anchorMax = label == "" ? 1 : 0.5f, offsetMin = 0, offsetMax = 0 }
            ), toggleComponent.isOn ? "- True" : "- False", Color.black, -1, TextAlignmentOptions.Left, out var stateText, toggleObject);
            toggleComponent.onValueChanged.AddListener(isOn => stateText.text = isOn ? "- True" : "- False");
            stateText.enableAutoSizing = true;
            stateText.fontSizeMin = 1;
            stateText.fontSizeMax = FontSize;
            stateText.fontStyle = FontStyles.Italic;

            return toggleObject;
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
            GameObject button = ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = 0, size = ItemSize }
            ), label, color ?? Color.gray, out var buttonComponent, ButtonPanelContent, TextAlignmentOptions.Left, hotkey == null ? 0 : ItemSize * 2 + GapSize * 2);
            buttonComponent.onClick.AddListener(() => onClick());

            if (hotkey != null)
            {
                if (!HotkeyButtonMap.ContainsKey(hotkey.Value))
                {
                    HotkeyButtonMap[hotkey.Value] = new List<Button>();
                }
                HotkeyButtonMap[hotkey.Value].Add(buttonComponent);

                ConstructText(UIPosition.Get(
                    new UIAnchored { anchor = 1f, offset = -((ItemSize * 2 + GapSize) / 2), size = ItemSize * 2 + GapSize },
                    new UIAnchored { anchor = 1f, offset = -ItemRadius, size = ItemSize }
                ), hotkey.ToString() ?? "", Color.white, FontSize, TextAlignmentOptions.Center, out var textComponent, button);
            }

            RefreshContent(ButtonPanelContent.transform, ButtonPanelContentRect, "", ItemSize);

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
            GameObject inputObject = ConstructInput(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = 0, size = ItemSize }
            ), label, "Value...", color ?? Color.gray, out var inputFieldComponent, ValueWatcherPanelContent);
            inputFieldComponent.text = saveable.Get();
            IDisposable binding = saveable.Bind(value => inputFieldComponent.text = value);

            AddValuewatcherButtons(inputObject, saveable, color, out var saveButtonObject, out var restoreButtonObject);
            inputFieldComponent.onValueChanged.AddListener(value =>
            {
                saveable.Set(value);

                saveButtonObject.SetActive(saveable.IsDirty);
                restoreButtonObject.SetActive(saveable.IsDirty);
            });
            saveButtonObject.SetActive(saveable.IsDirty);
            restoreButtonObject.SetActive(saveable.IsDirty);

            RefreshContent(ValueWatcherPanelContent.transform, ValueWatcherPanelContentRect, "", ItemSize * 2);

            return new DelegateDisposable(() => RemoveValueWatcher(inputObject, saveButtonObject, restoreButtonObject, binding));
        }

        public IDisposable AddValueWatcher(string label, Saveable<int> saveable, Color? color = null)
        {
            GameObject inputObject = ConstructInput(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = 0, size = ItemSize }
            ), label, "Value...", color ?? Color.gray, out var inputFieldComponent, ValueWatcherPanelContent);
            inputFieldComponent.text = saveable.Get().ToString();
            IDisposable binding = saveable.Bind(value => inputFieldComponent.text = value.ToString());

            AddValuewatcherButtons(inputObject, saveable, color, out var saveButtonObject, out var restoreButtonObject);
            inputFieldComponent.onValueChanged.AddListener(value =>
            {
                string regex = Regex.Replace(value, "[^0-9]", "");
                if (regex == "") regex = "0";
                saveable.Set(int.Parse(regex));
                if (regex != value) inputFieldComponent.text = regex;

                saveButtonObject.SetActive(saveable.IsDirty);
                restoreButtonObject.SetActive(saveable.IsDirty);
            });
            saveButtonObject.SetActive(saveable.IsDirty);
            restoreButtonObject.SetActive(saveable.IsDirty);

            RefreshContent(ValueWatcherPanelContent.transform, ValueWatcherPanelContentRect, "", ItemSize * 2);

            return new DelegateDisposable(() => RemoveValueWatcher(inputObject, saveButtonObject, restoreButtonObject, binding));
        }

        public IDisposable AddValueWatcher(string label, Saveable<float> saveable, Color? color = null)
        {
            GameObject inputObject = ConstructInput(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = 0, size = ItemSize }
            ), label, "Value...", color ?? Color.gray, out var inputFieldComponent, ValueWatcherPanelContent);
            inputFieldComponent.text = saveable.Get().ToString();
            IDisposable binding = saveable.Bind(value => inputFieldComponent.text = value.ToString());

            AddValuewatcherButtons(inputObject, saveable, color, out var saveButtonObject, out var restoreButtonObject);
            inputFieldComponent.onValueChanged.AddListener(value =>
            {
                string regex = Regex.Match(value, @"^\d*\.?\d*").Value;
                if (regex == "") regex = "0";
                saveable.Set(float.Parse(regex));
                if (regex != value) inputFieldComponent.text = regex;

                saveButtonObject.SetActive(saveable.IsDirty);
                restoreButtonObject.SetActive(saveable.IsDirty);
            });
            saveButtonObject.SetActive(saveable.IsDirty);
            restoreButtonObject.SetActive(saveable.IsDirty);

            RefreshContent(ValueWatcherPanelContent.transform, ValueWatcherPanelContentRect, "", ItemSize * 2);

            return new DelegateDisposable(() => RemoveValueWatcher(inputObject, saveButtonObject, restoreButtonObject, binding));
        }

        public IDisposable AddValueWatcher(string label, Saveable<bool> saveable, Color? color = null)
        {
            GameObject inputObject = ConstructToggle(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = 0, size = ItemSize }
            ), label, color ?? Color.gray, out var inputFieldComponent, ValueWatcherPanelContent);
            inputFieldComponent.isOn = saveable.Get();
            IDisposable binding = saveable.Bind(value => inputFieldComponent.isOn = value);

            AddValuewatcherButtons(inputObject, saveable, color, out var saveButtonObject, out var restoreButtonObject);
            inputFieldComponent.onValueChanged.AddListener(value =>
            {
                saveable.Set(value);

                saveButtonObject.SetActive(saveable.IsDirty);
                restoreButtonObject.SetActive(saveable.IsDirty);
            });
            saveButtonObject.SetActive(saveable.IsDirty);
            restoreButtonObject.SetActive(saveable.IsDirty);

            RefreshContent(ValueWatcherPanelContent.transform, ValueWatcherPanelContentRect, "", ItemSize * 2);

            return new DelegateDisposable(() => RemoveValueWatcher(inputObject, saveButtonObject, restoreButtonObject, binding));
        }

        private void AddValuewatcherButtons(GameObject inputObject, ISaveable saveable, Color? color, out GameObject saveButtonObject, out GameObject restoreButtonObject)
        {
            GameObject saveButtonObject_ = ConstructButton(UIPosition.Get(
                new UIAnchored { anchor = 1f, offset = -(ItemRadius * 3 + GapSize), size = ItemSize },
                new UIAnchored { anchor = 1f, offset = -ItemRadius, size = ItemSize }
            ), "S", color ?? new Color(0, 0, 0, 0.5f), out var saveButtonComponent, inputObject);

            GameObject restoreButtonObject_ = ConstructButton(UIPosition.Get(
                new UIAnchored { anchor = 1f, offset = -ItemRadius, size = ItemSize },
                new UIAnchored { anchor = 1f, offset = -ItemRadius, size = ItemSize }
            ), "R", color ?? new Color(0, 0, 0, 0.5f), out var restoreButtonComponent, inputObject);

            saveButtonComponent.onClick.AddListener(() =>
            {
                saveable.Save();

                saveButtonObject_.SetActive(saveable.IsDirty);
                restoreButtonObject_.SetActive(saveable.IsDirty);
            });

            restoreButtonComponent.onClick.AddListener(() =>
            {
                saveable.Restore();

                saveButtonObject_.SetActive(saveable.IsDirty);
                restoreButtonObject_.SetActive(saveable.IsDirty);
            });

            saveButtonObject = saveButtonObject_;
            restoreButtonObject = restoreButtonObject_;
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

        private void RefreshContent(Transform contentTransform, RectTransform contentRect, string filter, float itemHeight)
        {
            int activeCount = 0;
            for (int i = 0; i < contentTransform.childCount; i++)
            {
                GameObject child = contentTransform.GetChild(i).gameObject;
                bool matchesFilter = string.IsNullOrEmpty(filter) || child.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                child.SetActive(matchesFilter);
                UIPosition.Get(
                    new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                    new UIAnchored { anchor = 1f, offset = -(activeCount * (itemHeight + GapSize) + (itemHeight / 2)), size = itemHeight }
                ).SetTransform(child.GetComponent<RectTransform>());
                if (matchesFilter) activeCount++;
            }
            contentRect.sizeDelta = new Vector2(0, activeCount * (itemHeight + GapSize) + GapSize);
        }
        #endregion

        // Setting textWrappingMode to NoWrap in late update to fix an issue where the text component resets it to Wrap when instantiated
        private class WrapModeFixer : MonoBehaviour
        {
            private TMP_Text[] _targets = null!;

            public static void Fix(GameObject host, params TMP_Text[] targets)
            {
                host.AddComponent<WrapModeFixer>()._targets = targets;
            }

            private void LateUpdate()
            {
                foreach (var t in _targets)
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                Destroy(this);
            }
        }
    }
}