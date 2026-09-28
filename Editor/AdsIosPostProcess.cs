#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace MZ.Ads.Editor
{
    /// <summary>
    /// iOS build step: links AppTrackingTransparency.framework (weak, for iOS &lt; 14) for the ATT bridge,
    /// and makes sure Info.plist has NSUserTrackingUsageDescription (text from Google Mobile Ads Settings).
    /// </summary>
    public static class AdsIosPostProcess
    {
        private const string TrackingKey = "NSUserTrackingUsageDescription";
        private const string FallbackText = "This identifier will be used to deliver personalized ads to you.";

        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            var projectPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "AppTrackingTransparency.framework", true);
            project.WriteToFile(projectPath);

            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            if (!plist.root.values.ContainsKey(TrackingKey))
            {
                plist.root.SetString(TrackingKey, FallbackText);
                plist.WriteToFile(plistPath);
            }
        }
    }
}
#endif
