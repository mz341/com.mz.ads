using System.Collections.Generic;

namespace MZ.Ads
{
    /// <summary>
    /// Google's public demo ad units. They always fill with test ads and are safe to click.
    /// https://developers.google.com/admob/unity/test-ads
    /// </summary>
    public static class TestAdUnitIds
    {
        public const string AndroidAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string IosAppId = "ca-app-pub-3940256099942544~1458002511";

        private static readonly Dictionary<AdFormat, string> Android = new Dictionary<AdFormat, string>
        {
            { AdFormat.Banner, "ca-app-pub-3940256099942544/9214589741" },
            { AdFormat.Mrec, "ca-app-pub-3940256099942544/6300978111" },
            { AdFormat.Interstitial, "ca-app-pub-3940256099942544/1033173712" },
            { AdFormat.Rewarded, "ca-app-pub-3940256099942544/5224354917" },
            { AdFormat.RewardedInterstitial, "ca-app-pub-3940256099942544/5354046379" },
            { AdFormat.AppOpen, "ca-app-pub-3940256099942544/9257395921" },
        };

        private static readonly Dictionary<AdFormat, string> Ios = new Dictionary<AdFormat, string>
        {
            { AdFormat.Banner, "ca-app-pub-3940256099942544/2435281174" },
            { AdFormat.Mrec, "ca-app-pub-3940256099942544/2934735716" },
            { AdFormat.Interstitial, "ca-app-pub-3940256099942544/4411468910" },
            { AdFormat.Rewarded, "ca-app-pub-3940256099942544/1712485313" },
            { AdFormat.RewardedInterstitial, "ca-app-pub-3940256099942544/6978759866" },
            { AdFormat.AppOpen, "ca-app-pub-3940256099942544/5575463023" },
        };

        public static string ForCurrentPlatform(AdFormat format)
        {
#if UNITY_IOS
            return Ios[format];
#else
            return Android[format];
#endif
        }

        public static bool IsTestId(string adUnitId)
        {
            return !string.IsNullOrEmpty(adUnitId) && adUnitId.StartsWith("ca-app-pub-3940256099942544");
        }
    }
}
