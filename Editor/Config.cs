using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FTFoundation.Core;
using UnityEditor;
using UnityEngine;

namespace FTFoundation.Editor
{
    public class Config : EditorWindow
    {
        // ── Data model ────────────────────────────────────────────────────────────────────────

        private class ServiceEntry
        {
            public Type Interface = null!;
            public Type Implementation = null!;
            public ServiceType Lifetime;
            public BuildTargetProfile Profiles;
            public BuildTargetPlatform Platforms;
            public int Priority;
            public bool IsFallback;
        }

        private class InterfaceGroup
        {
            public Type Interface = null!;
            public List<ServiceEntry> Entries = new();
            public bool Foldout = true;
        }

        // ── State ─────────────────────────────────────────────────────────────────────────────

        private List<InterfaceGroup> _groups = new();
        private Vector2 _scrollPos;
        private int _profileIndex;  // 0 = All
        private int _platformIndex; // 0 = All

        private static readonly BuildTargetProfile[] s_profiles =
        {
            BuildTargetProfile.Editor,
            BuildTargetProfile.Development,
            BuildTargetProfile.Staging,
            BuildTargetProfile.Production
        };

        private static readonly string[] s_profileOptions = { "All Environments", "Editor", "Development", "Staging", "Production" };

        private static readonly BuildTargetPlatform[] s_platforms =
        {
            BuildTargetPlatform.Desktop,
            BuildTargetPlatform.Mobile,
            BuildTargetPlatform.Console,
            BuildTargetPlatform.Web,
            BuildTargetPlatform.Windows,
            BuildTargetPlatform.macOS,
            BuildTargetPlatform.Linux,
            BuildTargetPlatform.Android,
            BuildTargetPlatform.iOS,
            BuildTargetPlatform.Switch,
            BuildTargetPlatform.PlayStation,
            BuildTargetPlatform.Xbox,
        };

        private static readonly string[] s_platformOptions =
        {
            "All Platforms",
            "Desktop", "Mobile", "Console", "Web",
            "Windows", "macOS", "Linux",
            "Android", "iOS",
            "Switch", "PlayStation", "Xbox",
        };

        // ── Styles (lazy-initialised) ─────────────────────────────────────────────────────────

        private GUIStyle _headerStyle = null!;
        private GUIStyle _colHeaderRowStyle = null!;
        private GUIStyle _rowEven = null!;
        private GUIStyle _rowOdd = null!;
        private GUIStyle _tagLabelStyle = null!;
        private GUIStyle _tooltipStyle = null!;
        private Color _headerColor;
        private string _hoveredTooltip = "";

        private static readonly Color s_winnerColor = new(0.28f, 0.75f, 0.28f);
        private static readonly Color s_activeColor = new(0.85f, 0.75f, 0.20f);
        private static readonly Color s_fallbackColor = new(0.45f, 0.65f, 0.95f);
        private static readonly Color s_inactiveColor = new(0.45f, 0.45f, 0.45f);

        // ── Menu entry ────────────────────────────────────────────────────────────────────────

        [MenuItem("Window/FT Foundation")]
        public static void ShowWindow()
        {
            var win = GetWindow<Config>("FT Foundation");
            win.minSize = new Vector2(680, 300);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────────────────

        private void OnEnable() { _headerStyle = null!; Refresh(); }

        private void OnFocus() => Refresh();

        // ── Data collection ───────────────────────────────────────────────────────────────────

        private void Refresh()
        {
            _groups = new List<InterfaceGroup>();
            var grouped = new Dictionary<Type, InterfaceGroup>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetCustomAttribute<ServiceAssemblyAttribute>() == null) continue;

                foreach (var t in assembly.GetTypes())
                {
                    var svcAttr = (ServiceAttribute)t.GetCustomAttribute(typeof(ServiceAttribute), inherit: true);
                    if (svcAttr == null) continue;

                    if (!grouped.TryGetValue(svcAttr.Interface, out var group))
                    {
                        group = new InterfaceGroup { Interface = svcAttr.Interface };
                        grouped[svcAttr.Interface] = group;
                        _groups.Add(group);
                    }

                    group.Entries.Add(new ServiceEntry
                    {
                        Interface = svcAttr.Interface,
                        Implementation = t,
                        Lifetime = svcAttr.Type,
                        Profiles = t.GetCustomAttribute<ServiceBuildProfileAttribute>()?.Profiles ?? BuildTargetProfile.All,
                        Platforms = t.GetCustomAttribute<ServiceBuildPlatformAttribute>()?.Platforms ?? BuildTargetPlatform.All,
                        Priority = t.GetCustomAttribute<ServicePriorityAttribute>()?.Priority ?? 0,
                        IsFallback = t.GetCustomAttribute<ServiceFallbackAttribute>() != null
                    });
                }
            }

            // Sort entries per group: non-fallback first, then by descending priority
            foreach (var g in _groups)
                g.Entries = g.Entries.OrderBy(e => e.IsFallback).ThenByDescending(e => e.Priority).ToList();

            _groups = _groups.OrderBy(g => g.Interface.Name).ToList();
            Repaint();
        }

        // ── Drawing ───────────────────────────────────────────────────────────────────────────

        private void EnsureStyles()
        {
            if (_headerStyle != null && _colHeaderRowStyle?.normal.background != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                padding = new RectOffset(4, 4, 3, 0),
                fixedHeight = 0,
                overflow = new RectOffset(0, 0, 0, 3)
            };
            _headerColor = EditorGUIUtility.isProSkin
                ? new Color(0.55f, 0.85f, 1.0f)
                : new Color(0.10f, 0.35f, 0.75f);
            _headerStyle.normal.textColor = _headerColor;
            _headerStyle.onNormal.textColor = _headerColor;

            // Column header row — noticeably darker than data rows
            Color colHeaderBg = EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.12f, 0.12f) : new Color(0.58f, 0.58f, 0.58f);
            _colHeaderRowStyle = new GUIStyle
            {
                normal = { background = MakeTex(colHeaderBg) },
                padding = new RectOffset(4, 4, 4, 4)
            };

            // Data rows — higher contrast delta than before
            Color evenBg = EditorGUIUtility.isProSkin
                ? new Color(0.23f, 0.23f, 0.23f) : new Color(0.88f, 0.88f, 0.88f);
            Color oddBg = EditorGUIUtility.isProSkin
                ? new Color(0.23f, 0.23f, 0.23f) : new Color(0.88f, 0.88f, 0.88f);

            _rowEven = new GUIStyle { normal = { background = MakeTex(evenBg) }, padding = new RectOffset(4, 4, 4, 4) };
            _rowOdd = new GUIStyle { normal = { background = MakeTex(oddBg) }, padding = new RectOffset(4, 4, 4, 4) };

            _tagLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(5, 5, 1, 1)
            };

            Color tooltipBg = EditorGUIUtility.isProSkin
                ? new Color(0.13f, 0.13f, 0.13f, 1f)
                : new Color(0.92f, 0.92f, 0.92f, 1f);
            _tooltipStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { background = MakeTex(tooltipBg) },
                padding = new RectOffset(6, 6, 4, 4),
                border = new RectOffset(1, 1, 1, 1)
            };
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (Event.current.type == EventType.Layout)
                _hoveredTooltip = "";

            // ── Toolbar ───────────────────────────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Service Registry", EditorStyles.boldLabel, GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            // GUILayout.Label("Environment:", EditorStyles.toolbarButton, GUILayout.Width(85));
            int newProfile = EditorGUILayout.Popup(_profileIndex, s_profileOptions, EditorStyles.toolbarPopup, GUILayout.Width(130));
            if (newProfile != _profileIndex) _profileIndex = newProfile;
            GUILayout.Space(10);
            // GUILayout.Label("Platform:", EditorStyles.toolbarButton, GUILayout.Width(62));
            int newPlatform = EditorGUILayout.Popup(_platformIndex, s_platformOptions, EditorStyles.toolbarPopup, GUILayout.Width(130));
            if (newPlatform != _platformIndex) _platformIndex = newPlatform;
            GUILayout.Space(10);
            if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(70))) _groups.ForEach(g => g.Foldout = true);
            if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(80))) _groups.ForEach(g => g.Foldout = false);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60))) Refresh();
            EditorGUILayout.EndHorizontal();

            if (_groups.Count == 0)
            {
                EditorGUILayout.HelpBox("No [ServiceAssembly] assemblies found. Ensure at least one assembly has [assembly: ServiceAssembly].", MessageType.Info);
                return;
            }

            // ── Legend ────────────────────────────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            DrawColorDot(s_winnerColor); GUILayout.Label("Winner", GUILayout.Width(55));
            DrawColorDot(s_activeColor); GUILayout.Label("Active", GUILayout.Width(55));
            DrawColorDot(s_fallbackColor); GUILayout.Label("Fallback", GUILayout.Width(65));
            DrawColorDot(s_inactiveColor); GUILayout.Label("Inactive", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // ── Column headers (pinned above scroll) ──────────────────────────────────────────
            EditorGUILayout.BeginHorizontal(_colHeaderRowStyle);
            GUILayout.Label("Implementation", EditorStyles.miniBoldLabel, GUILayout.Width(180));
            GUILayout.Label("Lifetime", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            GUILayout.Label("Priority", EditorStyles.miniBoldLabel, GUILayout.Width(55));
            GUILayout.Label("Profiles", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            GUILayout.Label("Platforms", EditorStyles.miniBoldLabel, GUILayout.Width(80));
            GUILayout.Label("Status", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            // ── Service groups ────────────────────────────────────────────────────────────────
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var group in _groups)
            {
                DrawSeparator();
                DrawGroup(group);
            }

            EditorGUILayout.EndScrollView();

            // ── Hover tooltip ─────────────────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(_hoveredTooltip) && Event.current.type == EventType.Repaint)
            {
                var content = new GUIContent(_hoveredTooltip);
                var size = _tooltipStyle.CalcSize(content);
                var mp = Event.current.mousePosition;
                GUI.Box(new Rect(mp.x + 12, mp.y + 12, size.x, size.y), _hoveredTooltip, _tooltipStyle);
            }
        }

        private void DrawGroup(InterfaceGroup group)
        {
            BuildTargetProfile? filterProfile = _profileIndex == 0 ? null : s_profiles[_profileIndex - 1];
            BuildTargetPlatform? filterPlatform = _platformIndex == 0 ? null : s_platforms[_platformIndex - 1];

            // Foldout header — count entries active under both filters
            int activeCount = group.Entries.Count(e =>
                (!filterProfile.HasValue || e.Profiles.HasFlag(filterProfile.Value)) &&
                (!filterPlatform.HasValue || (e.Platforms & filterPlatform.Value) != 0));
            string count = (filterProfile.HasValue || filterPlatform.HasValue)
                ? $"{activeCount} / {group.Entries.Count}"
                : group.Entries.Count.ToString();

            var prevContentColor = GUI.contentColor;
            GUI.contentColor = _headerColor;
            group.Foldout = EditorGUILayout.Foldout(group.Foldout, $"{group.Interface.Name}  ({count})", true, _headerStyle);
            GUI.contentColor = prevContentColor;
            if (!group.Foldout) return;

            // Compute winners considering both filters
            var profilesToCheck = filterProfile.HasValue ? new[] { filterProfile.Value } : s_profiles;
            var winners = new HashSet<Type>();
            foreach (var p in profilesToCheck)
            {
                var w = GetWinner(group.Entries, p, filterPlatform);
                if (w is not null) winners.Add(w);
            }

            // Rows
            for (int i = 0; i < group.Entries.Count; i++)
            {
                var e = group.Entries[i];

                bool profileMatch = !filterProfile.HasValue || e.Profiles.HasFlag(filterProfile.Value);
                bool platformMatch = !filterPlatform.HasValue || (e.Platforms & filterPlatform.Value) != 0;

                if (!profileMatch || !platformMatch)
                {
                    // Still show fallbacks when no primary winner exists under the current filters
                    if (!e.IsFallback) continue;
                    bool anyWinner = group.Entries.Any(x =>
                        !x.IsFallback &&
                        (!filterProfile.HasValue || x.Profiles.HasFlag(filterProfile.Value)) &&
                        (!filterPlatform.HasValue || (x.Platforms & filterPlatform.Value) != 0));
                    if (anyWinner) continue;
                }

                GUIStyle rowStyle = i % 2 == 0 ? _rowEven : _rowOdd;
                EditorGUILayout.BeginHorizontal(rowStyle);

                GUILayout.Label(e.Implementation.Name, GUILayout.Width(180));
                GUILayout.Label(e.Lifetime.ToString(), GUILayout.Width(80));
                GUILayout.Label(e.IsFallback ? "—" : e.Priority.ToString(), GUILayout.Width(55));
                DrawProfileCell(e.Profiles);
                DrawPlatformCell(e.Platforms);
                DrawStatus(e, winners, filterProfile, filterPlatform);

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPlatformCell(BuildTargetPlatform platforms)
        {
            string text;
            string tooltip = "";

            if (platforms == BuildTargetPlatform.All)
            {
                text = "All";
            }
            else
            {
                var set = s_platformDisplayOrder.Where(p => (platforms & p) != 0).ToList();
                if (set.Count == 1)
                {
                    text = set[0].ToString();
                }
                else
                {
                    text = "Multiple";
                    tooltip = string.Join(", ", set.Select(p => p.ToString()));
                }
            }

            GUILayout.Label(text, GUILayout.Width(80));
            if (!string.IsNullOrEmpty(tooltip) && Event.current.type == EventType.Repaint)
            {
                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    _hoveredTooltip = tooltip;
            }
        }

        private static readonly BuildTargetPlatform[] s_platformDisplayOrder =
        {
            BuildTargetPlatform.Desktop,
            BuildTargetPlatform.Mobile,
            BuildTargetPlatform.Console,
            BuildTargetPlatform.Web,
            BuildTargetPlatform.Windows,
            BuildTargetPlatform.macOS,
            BuildTargetPlatform.Linux,
            BuildTargetPlatform.Android,
            BuildTargetPlatform.iOS,
            BuildTargetPlatform.Switch,
            BuildTargetPlatform.PlayStation,
            BuildTargetPlatform.Xbox,
        };

        private static string PlatformShortName(BuildTargetPlatform p) => p switch
        {
            BuildTargetPlatform.Desktop => "Desktop",
            BuildTargetPlatform.Mobile => "Mobile",
            BuildTargetPlatform.Console => "Console",
            BuildTargetPlatform.Web => "Web",
            BuildTargetPlatform.Windows => "Win",
            BuildTargetPlatform.macOS => "Mac",
            BuildTargetPlatform.Linux => "Linux",
            BuildTargetPlatform.Android => "Android",
            BuildTargetPlatform.iOS => "iOS",
            BuildTargetPlatform.Switch => "Switch",
            BuildTargetPlatform.PlayStation => "PSN",
            BuildTargetPlatform.Xbox => "Xbox",
            _ => p.ToString()
        };

        private void DrawProfileCell(BuildTargetProfile profiles)
        {
            string text;
            string tooltip = "";

            if (profiles == BuildTargetProfile.All)
            {
                text = "All";
            }
            else
            {
                var set = s_profiles.Where(p => profiles.HasFlag(p)).ToList();
                if (set.Count == 1)
                {
                    text = set[0].ToString();
                }
                else
                {
                    text = "Multiple";
                    tooltip = string.Join(", ", set.Select(p => p.ToString()));
                }
            }

            GUILayout.Label(text, GUILayout.Width(70));
            if (!string.IsNullOrEmpty(tooltip) && Event.current.type == EventType.Repaint)
            {
                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    _hoveredTooltip = tooltip;
            }
        }

        private void DrawStatus(ServiceEntry e, HashSet<Type> winners, BuildTargetProfile? filterProfile, BuildTargetPlatform? filterPlatform)
        {
            if (e.IsFallback)
            {
                DrawTag("FALLBACK", s_fallbackColor);
                return;
            }

            if (winners.Contains(e.Implementation))
            {
                DrawTag("WINNER", s_winnerColor);
                return;
            }

            bool profileMatch = !filterProfile.HasValue || e.Profiles.HasFlag(filterProfile.Value);
            bool platformMatch = !filterPlatform.HasValue || (e.Platforms & filterPlatform.Value) != 0;

            if (profileMatch && platformMatch)
            {
                DrawTag("ACTIVE", s_activeColor);
                return;
            }

            // No filter active — check if it wins any profile/platform combination
            if (!filterProfile.HasValue && !filterPlatform.HasValue)
            {
                bool winsAnywhere = s_profiles.Any(p =>
                    GetWinner(_groups.First(g => g.Interface == e.Interface).Entries, p, null) == e.Implementation);
                if (winsAnywhere)
                {
                    DrawTag("WINNER", s_winnerColor);
                    return;
                }
                if (e.Profiles != BuildTargetProfile.All)
                {
                    DrawTag("ACTIVE", s_activeColor);
                    return;
                }
            }

            DrawTag("INACTIVE", s_inactiveColor);
        }

        private void DrawTag(string label, Color color)
        {
            _tagLabelStyle.normal.textColor = color;
            GUILayout.Label(label, _tagLabelStyle, GUILayout.Width(64));
        }

        private static void DrawSeparator()
        {
            Color c = EditorGUIUtility.isProSkin ? new Color(0.08f, 0.08f, 0.08f) : new Color(0.45f, 0.45f, 0.45f);
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                GUILayout.Height(1f), GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, c);
        }

        private void DrawColorDot(Color color)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUILayout.Label("■", GUILayout.Width(14));
            GUI.color = prev;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────────────

        private static Type GetWinner(IEnumerable<ServiceEntry> entries, BuildTargetProfile profile, BuildTargetPlatform? platform)
        {
            var matched = entries
                .Where(e => !e.IsFallback &&
                            e.Profiles.HasFlag(profile) &&
                            (!platform.HasValue || (e.Platforms & platform.Value) != 0))
                .OrderByDescending(e => e.Priority)
                .ToList();

            if (matched.Count > 0) return matched[0].Implementation;

            var fallbacks = entries
                .Where(e => e.IsFallback &&
                            e.Profiles.HasFlag(profile) &&
                            (!platform.HasValue || (e.Platforms & platform.Value) != 0))
                .OrderByDescending(e => e.Priority)
                .ToList();

#pragma warning disable CS8603
            return fallbacks.Count > 0 ? fallbacks[0].Implementation : null;
#pragma warning restore CS8603
        }

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }
    }
}

