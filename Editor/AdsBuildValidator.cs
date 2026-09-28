using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MZ.Ads.Editor
{
    /// <summary>
    /// Checks the ads setup before every Android / iOS build. Release builds fail on real problems
    /// (missing IDs, Google test IDs in production); development builds only warn.
    /// Also available from Tools > MZ Ads > Validate Setup.
    /// </summary>
    public sealed class AdsBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private static readonly (AdFormat format, string symbol)[] Formats =
        {
            (AdFormat.Banner, "ADS_BANNER"),
            (AdFormat.Mrec, "ADS_MREC"),
            (AdFormat.Interstitial, "ADS_INTERSTITIAL"),
            (AdFormat.Rewarded, "ADS_REWARDED"),
            (AdFormat.RewardedInterstitial, "ADS_REWARDED_INTERSTITIAL"),
            (AdFormat.AppOpen, "ADS_APP_OPEN"),
        };

        public void OnPreprocessBuild(BuildReport report)
        {
            var platform = report.summary.platform;
            if (platform != BuildTarget.Android && platform != BuildTarget.iOS)
            {
                return;
            }

            var development = (report.summary.options & BuildOptions.Development) != 0;
            var errors = Validate(platform, development);
            if (errors.Count > 0 && !development)
            {
                throw new BuildFailedException("[MZ.Ads] " + string.Join("\n", errors));
            }
        }

        [MenuItem("Tools/MZ Ads/Validate Setup", priority = 20)]
        private static void ValidateFromMenu()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var errors = Validate(target, development: false);
            if (errors.Count == 0)
            {
                Debug.Log("[MZ.Ads] Setup OK for " + target);
            }
        }

        /// <returns>Errors that must block a release build. Warnings are logged directly.</returns>
        public static List<string> Validate(BuildTarget platform, bool development)
        {
            var errors = new List<string>();
            var named = platform == BuildTarget.iOS ? NamedBuildTarget.iOS : NamedBuildTarget.Android;
            var symbols = AdsSymbols.Read(named);

            if (!symbols.Contains("ADS_ADMOB"))
            {
                Debug.LogWarning("[MZ.Ads] ADS_ADMOB is off for " + platform + ": the build will contain no ads.");
                return errors;
            }

            var config = AdsConfigMenu.Find();
            if (config == null)
            {
                errors.Add("MZAdsConfig asset not found in a Resources folder (Tools > MZ Ads > Config).");
                return Report(errors);
            }

            foreach (var (format, symbol) in Formats)
            {
                if (!symbols.Contains(symbol))
                {
                    continue;
                }

                var ids = config.IdsFor(format);
                var list = AdUnitIds.Resolve(platform == BuildTarget.iOS ? ids.ios : ids.android,
                                             symbols.Contains("ADS_TIERED_IDS"));
                if (list.Count == 0)
                {
                    errors.Add($"{format} is enabled ({symbol}) but has no {platform} ad unit ID.");
                    continue;
                }

                foreach (var id in list)
                {
                    if (TestAdUnitIds.IsTestId(id))
                    {
                        errors.Add($"{format} uses a Google test ad unit ID in the config ({id}).");
                    }
                }
            }

            if (platform == BuildTarget.Android && (int)PlayerSettings.Android.minSdkVersion < 23)
            {
                errors.Add("Android Minimum API Level must be 23 or higher for Google Mobile Ads 11.x.");
            }

            if (symbols.Contains("ADS_VERBOSE_LOG") && !development)
            {
                Debug.LogWarning("[MZ.Ads] ADS_VERBOSE_LOG is on for a release build.");
            }

            return Report(errors);
        }

        private static List<string> Report(List<string> errors)
        {
            foreach (var error in errors)
            {
                Debug.LogError("[MZ.Ads] " + error);
            }

            return errors;
        }
    }
}
