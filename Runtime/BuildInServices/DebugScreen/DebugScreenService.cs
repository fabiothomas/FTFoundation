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

        void Inject(IDedicatedObjectService dedicatedObjectService, ILifetimeService lifetimeService)
        {
            DedicatedObjectService = dedicatedObjectService;

            ConstructObject();

            SelectTab("Terminal");
            Toggle(false);

            lifetimeService.OnUpdate(Update);
        }

        private void ConstructObject()
        {
            DebugScreenObject = DedicatedObjectService.Get();

            DedicatedObjectService.ConstructCanvas();
            GameObject panel = DedicatedObjectService.ConstructPanel(UIPosition.Get(
                new UIAnchored { anchor = 0, offset = 160f, size = 300 },
                new UIStretch { anchorMin = 0, anchorMax = 1, offsetMin = 10f, offsetMax = -10f }
            ), new Color(0, 0, 0, 0.5f));

            DedicatedObjectService.ConstructPanel(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -15f, size = 30f }
            ), new Color(0, 0, 0, 0.5f), panel);

            DedicatedObjectService.ConstructButton(UIPosition.Get(
                new UIAnchored { anchor = 1f, offset = 15f, size = 20f },
                new UIAnchored { anchor = 1f, offset = -15f, size = 20f }
            ), "X", Color.gray, panel).GetComponent<Button>().onClick.AddListener(() => Toggle());

            ConstructTerminal(panel);
            ConstructButtonPanel(panel);
            ConstructValueWatcherPanel(panel);

            TerminalButton = DedicatedObjectService.ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0.02f, anchorMax = 0.32f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -15f, size = 20f }
            ), "Terminal", Color.gray, panel).GetComponent<Button>();
            TerminalButton.onClick.AddListener(() => SelectTab("Terminal"));

            ButtonPanelButton = DedicatedObjectService.ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0.34f, anchorMax = 0.65f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -15f, size = 20f }
            ), "Buttons", Color.gray, panel).GetComponent<Button>();
            ButtonPanelButton.onClick.AddListener(() => SelectTab("ButtonPanel"));

            ValueWatcherPanelButton = DedicatedObjectService.ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0.67f, anchorMax = 0.98f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = -15f, size = 20f }
            ), "Values", Color.gray, panel).GetComponent<Button>();
            ValueWatcherPanelButton.onClick.AddListener(() => SelectTab("ValueWatcherPanel"));
        }

        private void ConstructTerminal(GameObject parent)
        {
            Terminal = DedicatedObjectService.ConstructEmptyCanvasChild(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -30 }
            ), "Terminal", parent);

            GameObject scrollView = DedicatedObjectService.ConstructScrollView(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 5, offsetMax = -5 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 5, offsetMax = -5 }
            ), null, Terminal);

            TerminalText = DedicatedObjectService.ConstructText(
                UIPosition.FullScreen, "", Color.white, 14, TextAlignmentOptions.TopLeft, scrollView
            ).GetComponent<TextMeshProUGUI>();
            TerminalScrollRect = scrollView.GetComponentInParent<ScrollRect>();

            DedicatedObjectService.ConstructButton(UIPosition.Get(
                new UIAnchored { anchor = 1f, offset = 30f, size = 50f },
                new UIAnchored { anchor = 1f, offset = -15f, size = 20f }
            ), "Clear", Color.gray, Terminal).GetComponent<Button>().onClick.AddListener(() => Clear());
        }

        private void ConstructButtonPanel(GameObject parent)
        {
            ButtonPanel = DedicatedObjectService.ConstructEmptyCanvasChild(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -30 }
            ), "ButtonPanel", parent);

            GameObject scrollView = DedicatedObjectService.ConstructScrollView(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 5, offsetMax = -5 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 5, offsetMax = -5 }
            ), null, ButtonPanel);

            ButtonPanelContent = DedicatedObjectService.ConstructEmptyCanvasChild(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIAnchored { anchor = 1f, offset = 0, size = 0 }
            ), "Content", scrollView);
            ButtonPanelContentRect = ButtonPanelContent.GetComponent<RectTransform>();
        }

        private void ConstructValueWatcherPanel(GameObject parent)
        {
            ValueWatcherPanel = DedicatedObjectService.ConstructEmptyCanvasChild(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = 0 },
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -30 }
            ), "ValueWatcherPanel", parent);
        }

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

            GameObject button = DedicatedObjectService.ConstructButton(UIPosition.Get(
                new UIStretch { anchorMin = 0f, anchorMax = 1f, offsetMin = 0, offsetMax = -30 },
                new UIAnchored { anchor = 1f, offset = -offset, size = 20f }
            ), label, color ?? Color.gray, ButtonPanelContent);
            button.GetComponent<Button>().onClick.AddListener(() => onClick());

            if (hotkey != null)
            {
                if (!HotkeyButtonMap.ContainsKey(hotkey.Value))
                {
                    HotkeyButtonMap[hotkey.Value] = new List<Button>();
                }
                HotkeyButtonMap[hotkey.Value].Add(button.GetComponent<Button>());

                DedicatedObjectService.ConstructText(UIPosition.Get(
                    new UIAnchored { anchor = 1f, offset = 15f, size = 20f },
                    new UIAnchored { anchor = 1f, offset = -10f, size = 20f }
                ), hotkey.ToString() ?? "", Color.white, 14, TextAlignmentOptions.Center, button);
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

        public IDisposable AddValueWatcher<T>(string label, Func<T> valueProvider, Color? color = null)
        {
            return new DelegateDisposable(() => RemoveValueWatcher(label));
            // Implementation here
        }

        private void RemoveValueWatcher(string label)
        {
            // Implementation here
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
    }
}