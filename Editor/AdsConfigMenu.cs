using System.IO;
using UnityEditor;
using UnityEngine;

namespace MZ.Ads.Editor
{
    public static class AdsConfigMenu
    {
        public const string DefaultFolder = "Assets/MZAds/Resources";
        public static string DefaultPath => DefaultFolder + "/" + AdsConfig.ResourceName + ".asset";

        [MenuItem("Tools/MZ Ads/Config", priority = 2)]
        public static void SelectOrCreate()
        {
            var config = Find() ?? Create();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        [MenuItem("Tools/MZ Ads/Google Mobile Ads Settings (App IDs)", priority = 3)]
        public static void OpenGmaSettings()
        {
            if (!EditorApplication.ExecuteMenuItem("Assets/Google Mobile Ads/Settings..."))
            {
                Debug.LogWarning("[MZ.Ads] Google Mobile Ads menu not found. Is the com.google.ads.mobile package installed?");
            }
        }

        public static AdsConfig Find()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(AdsConfig)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Resources/") && Path.GetFileNameWithoutExtension(path) == AdsConfig.ResourceName)
                {
                    return AssetDatabase.LoadAssetAtPath<AdsConfig>(path);
                }
            }

            return null;
        }

        private static AdsConfig Create()
        {
            Directory.CreateDirectory(DefaultFolder);
            var config = ScriptableObject.CreateInstance<AdsConfig>();
            AssetDatabase.CreateAsset(config, DefaultPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[MZ.Ads] Created " + DefaultPath);
            return config;
        }
    }
}
