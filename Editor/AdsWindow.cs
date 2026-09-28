using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MZ.Ads.Editor
{
    /// <summary>
    /// Tools > MZ Ads > Ads Window: ad IDs on top, scripting symbols (Add / Remove) below.
    /// Everything follows the active build platform (File > Build Settings): on Android you edit the
    /// Android IDs and symbols, after switching to iOS the same window edits the iOS ones.
    /// </summary>
    public sealed class AdsWindow : EditorWindow
    {
        private static readonly (AdFormat format, string symbol, string label)[] Formats =
        {
            (AdFormat.Banner, "ADS_BANNER", "Banner Ad Id"),
            (AdFormat.Mrec, "ADS_MREC", "MREC Ad Id"),
            (AdFormat.Interstitial, "ADS_INTERSTITIAL", "Interstitial Ad Id"),
            (AdFormat.Rewarded, "ADS_REWARDED", "Rewarded Ad Id"),
            (AdFormat.RewardedInterstitial, "ADS_REWARDED_INTERSTITIAL", "Rewarded Interstitial Ad Id"),
            (AdFormat.AppOpen, "ADS_APP_OPEN", "App Open Ad Id"),
        };

        private Vector2 _scroll;
        private AdsConfig _config;
        private HashSet<string> _symbols = new HashSet<string>();
        private GUIStyle _header;

        private static bool IsIos => EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS;

        private static bool IsMobileTarget =>
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ||
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;

        /// <summary>Symbols are read and written for the active build target only.</summary>
        private static NamedBuildTarget ActiveTarget =>
            NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        [MenuItem("Tools/MZ Ads/Ads Window", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<AdsWindow>("AdsWindow");
            window.minSize = new Vector2(520, 560);
        }

        private void OnEnable()
        {
            RefreshSymbols();
            EditorUserBuildSettings.activeBuildTargetChanged += OnPlatformChanged;
        }

        private void OnDisable()
        {
            EditorUserBuildSettings.activeBuildTargetChanged -= OnPlatformChanged;
        }

        private void OnFocus()
        {
            RefreshSymbols();
        }

        private void OnPlatformChanged()
        {
            RefreshSymbols();
        }

        private void OnGUI()
        {
            _header ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            _config ??= AdsConfigMenu.Find();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            GUILayout.Label($"MZ Ads (AdMob)  —  {EditorUserBuildSettings.activeBuildTarget}",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });

            if (!IsMobileTarget)
            {
                EditorGUILayout.HelpBox("Switch the platform to Android or iOS in File > Build Settings. " +
                                        "IDs and symbols follow the active platform.", MessageType.Warning);
            }

            GUILayout.Space(4);
            DrawIds();
            GUILayout.Space(10);
            DrawSymbols();
            GUILayout.Space(10);
            DrawTools();

            EditorGUILayout.EndScrollView();
        }

        // ---------------- IDs ----------------

        private void DrawIds()
        {
            GUILayout.Label("Ads keys :", _header);
            DrawAppId();

            if (_config == null)
            {
                EditorGUILayout.HelpBox("Config asset not found.", MessageType.Warning);
                if (GUILayout.Button("Create Config"))
                {
                    AdsConfigMenu.SelectOrCreate();
                    _config = AdsConfigMenu.Find();
                }

                return;
            }

            var tiered = _symbols.Contains("ADS_TIERED_IDS");
            var anyFormat = false;
            EditorGUI.BeginChangeCheck();

            foreach (var (format, symbol, label) in Formats)
            {
                if (!_symbols.Contains(symbol))
                {
                    continue;
                }

                anyFormat = true;
                var ids = _config.IdsFor(format);
                var list = IsIos ? ids.ios : ids.android;
                var count = tiered ? AdUnitIds.MaxTiers : 1;
                while (list.Count < count)
                {
                    list.Add(string.Empty);
                }

                for (var i = 0; i < count; i++)
                {
                    var rowLabel = tiered ? $"{label} Tier {i + 1}" : label;
                    list[i] = EditorGUILayout.TextField(rowLabel, list[i]).Trim();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssetIfDirty(_config);
            }

            if (!anyFormat)
            {
                EditorGUILayout.HelpBox("Add an ad format symbol below to show its ID field.", MessageType.Info);
            }
        }

        /// <summary>AdMob App ID lives in Google Mobile Ads Settings; edited here for convenience.</summary>
        private static void DrawAppId()
        {
            var settings = GmaSettings.Load();
            if (settings == null)
            {
                return;
            }

            var property = IsIos ? "GoogleMobileAdsIOSAppId" : "GoogleMobileAdsAndroidAppId";
            var current = settings.Get(property);
            var next = EditorGUILayout.TextField("AdMob App Id", current).Trim();
            if (next != current)
            {
                settings.Set(property, next);
            }

            if (IsIos)
            {
                var attKey = "UserTrackingUsageDescription";
                var att = settings.Get(attKey);
                var nextAtt = EditorGUILayout.TextField(new GUIContent("ATT Message", "Text of the iOS tracking popup."), att);
                if (nextAtt != att)
                {
                    settings.Set(attKey, nextAtt);
                }
            }
        }

        // ---------------- Symbols ----------------

        private void DrawSymbols()
        {
            GUILayout.Label("Symbols :", _header);
            foreach (var symbol in AdsSymbols.All)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(symbol.Label + " :", symbol.Help), GUILayout.Width(EditorGUIUtility.labelWidth));
                var enabled = _symbols.Contains(symbol.Name);
                if (GUILayout.Button(enabled ? "Remove" : "Add"))
                {
                    SetSymbol(symbol.Name, !enabled);
                }

                EditorGUILayout.EndHorizontal();
            }

            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("Compiling…", MessageType.None);
            }
        }

        private void SetSymbol(string symbol, bool enabled)
        {
            AdsSymbols.Set(ActiveTarget, symbol, enabled);
            RefreshSymbols();
        }

        private void RefreshSymbols()
        {
            _symbols = AdsSymbols.Read(ActiveTarget);
            Repaint();
        }

        // ---------------- Tools ----------------

        private void DrawTools()
        {
            GUILayout.Label("Tools :", _header);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Advanced Settings"))
            {
                AdsConfigMenu.SelectOrCreate();
            }

            if (GUILayout.Button("Validate Setup"))
            {
                var target = IsIos ? BuildTarget.iOS : BuildTarget.Android;
                if (AdsBuildValidator.Validate(target, development: false).Count == 0)
                {
                    Debug.Log("[MZ.Ads] Setup OK for " + target);
                }
            }

            if (GUILayout.Button("Recommended Symbols"))
            {
                foreach (var name in new[] { "ADS_ADMOB", "ADS_BANNER", "ADS_INTERSTITIAL", "ADS_REWARDED", "ADS_APP_OPEN" })
                {
                    AdsSymbols.Set(ActiveTarget, name, true);
                }

                RefreshSymbols();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Prefabs (add to scene) :", _header);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Google CMP"))
            {
                AdsPrefabMenu.AddGoogleCmp();
            }

            if (GUILayout.Button("Ads Initializer"))
            {
                AdsPrefabMenu.AddAdsInitializer();
            }

            if (GUILayout.Button("App Tracking (iOS)"))
            {
                AdsPrefabMenu.AddAppTracking();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Reflection wrapper: GoogleMobileAdsSettings is internal to the GMA editor assembly.</summary>
        private sealed class GmaSettings
        {
            private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            private readonly UnityEngine.Object _instance;
            private readonly Type _type;

            private GmaSettings(Type type, UnityEngine.Object instance)
            {
                _type = type;
                _instance = instance;
            }

            public static GmaSettings Load()
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings");
                    if (type == null)
                    {
                        continue;
                    }

                    var load = type.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    return load?.Invoke(null, null) is UnityEngine.Object instance ? new GmaSettings(type, instance) : null;
                }

                return null;
            }

            public string Get(string property)
            {
                return _type.GetProperty(property, Flags)?.GetValue(_instance) as string ?? string.Empty;
            }

            public void Set(string property, string value)
            {
                _type.GetProperty(property, Flags)?.SetValue(_instance, value);
                EditorUtility.SetDirty(_instance);
                AssetDatabase.SaveAssetIfDirty(_instance);
            }
        }
    }
}
