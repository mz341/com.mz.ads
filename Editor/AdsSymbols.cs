using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace MZ.Ads.Editor
{
    /// <summary>The plugin's scripting define symbols and helpers to read / write them.</summary>
    public static class AdsSymbols
    {
        public struct Symbol
        {
            public string Name;
            public string Label;
            public string Help;
        }

        public static readonly Symbol[] All =
        {
            new Symbol { Name = "ADS_ADMOB", Label = "AdMob (core)", Help = "Required. Compiles the AdMob layer. Needs the Google Mobile Ads package." },
            new Symbol { Name = "ADS_BANNER", Label = "Banner", Help = "Anchored adaptive / 320x50 banner." },
            new Symbol { Name = "ADS_MREC", Label = "MREC", Help = "300x250 medium rectangle." },
            new Symbol { Name = "ADS_INTERSTITIAL", Label = "Interstitial", Help = "Full-screen interstitial with frequency cap." },
            new Symbol { Name = "ADS_REWARDED", Label = "Rewarded", Help = "Rewarded video." },
            new Symbol { Name = "ADS_REWARDED_INTERSTITIAL", Label = "Rewarded Interstitial", Help = "Rewarded interstitial." },
            new Symbol { Name = "ADS_APP_OPEN", Label = "App Open", Help = "App Open on resume / manual show." },
            new Symbol { Name = "ADS_TIERED_IDS", Label = "Three-tier IDs", Help = "Use up to 3 IDs per format (high → low floor). Off = first ID only." },
            new Symbol { Name = "ADS_FIREBASE", Label = "Firebase Analytics", Help = "Needs Firebase Analytics in the project." },
            new Symbol { Name = "ADS_APPSFLYER", Label = "AppsFlyer", Help = "Needs the AppsFlyer Unity plugin (AppsFlyer assembly)." },
            new Symbol { Name = "ADS_VERBOSE_LOG", Label = "Verbose logging", Help = "Debug logs. Turn off for release builds." },
        };

        public static readonly NamedBuildTarget[] Targets = { NamedBuildTarget.Android, NamedBuildTarget.iOS };

        public static HashSet<string> Read(NamedBuildTarget target)
        {
            var raw = PlayerSettings.GetScriptingDefineSymbols(target);
            return new HashSet<string>(raw.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0));
        }

        public static bool IsEnabled(string symbol, NamedBuildTarget target)
        {
            return Read(target).Contains(symbol);
        }

        public static void Set(NamedBuildTarget target, string symbol, bool enabled)
        {
            var symbols = Read(target);
            var changed = enabled ? symbols.Add(symbol) : symbols.Remove(symbol);
            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
            }
        }

        public static void SetForAllTargets(string symbol, bool enabled)
        {
            foreach (var target in Targets)
            {
                Set(target, symbol, enabled);
            }
        }
    }
}
