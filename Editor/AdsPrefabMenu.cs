using UnityEditor;
using UnityEngine;

namespace MZ.Ads.Editor
{
    /// <summary>Adds the plugin prefabs to the open scene (GameObject > MZ Ads, or the Ads Window buttons).</summary>
    public static class AdsPrefabMenu
    {
        private const string Folder = "Packages/com.mz.ads/Prefabs/";

        [MenuItem("GameObject/MZ Ads/Google CMP (consent scene)", priority = 10)]
        public static void AddGoogleCmp() => Add("GoogleCMP");

        [MenuItem("GameObject/MZ Ads/Ads Initializer", priority = 11)]
        public static void AddAdsInitializer() => Add("AdsInitializer");

        [MenuItem("GameObject/MZ Ads/App Tracking (iOS)", priority = 12)]
        public static void AddAppTracking() => Add("AppTracking");

        private static GameObject FindPrefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + name + ".prefab");
            if (prefab != null)
            {
                return prefab;
            }

            // Imported from a .unitypackage into Assets/ or another folder: search by name.
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            return null;
        }

        private static void Add(string name)
        {
            var prefab = FindPrefab(name);
            if (prefab == null)
            {
                Debug.LogError("[MZ.Ads] Prefab not found: " + name);
                return;
            }

            var existing = Object.FindObjectOfType(prefab.GetComponent<MonoBehaviour>().GetType());
            if (existing != null)
            {
                Debug.LogWarning($"[MZ.Ads] {name} is already in the scene.");
                Selection.activeObject = existing;
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add " + name);
            Selection.activeObject = instance;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(instance.scene);
        }
    }
}
