using System;
using System.Collections.Generic;
using UnityEngine;

namespace MZ.Ads
{
    public enum AgeTreatment
    {
        Unspecified,
        Child,
        Teen
    }

    /// <summary>
    /// All plugin settings. Create it with Tools > MZ Ads > Config; it must live in a Resources
    /// folder as "MZAdsConfig". Values that should change without a build (intervals, on/off)
    /// can be overridden at runtime through <see cref="Ads.Config"/> (e.g. from Remote Config).
    /// </summary>
    [CreateAssetMenu(fileName = ResourceName, menuName = "MZ Ads/Config")]
    public class AdsConfig : ScriptableObject
    {
        public const string ResourceName = "MZAdsConfig";

        [Header("Ad unit IDs (highest floor first)")]
        public AdUnitIds banner = new AdUnitIds();
        public AdUnitIds mrec = new AdUnitIds();
        public AdUnitIds interstitial = new AdUnitIds();
        public AdUnitIds rewarded = new AdUnitIds();
        public AdUnitIds rewardedInterstitial = new AdUnitIds();
        public AdUnitIds appOpen = new AdUnitIds();

        [Header("Testing")]
        [Tooltip("Use Google's demo ad units in the Editor and in Development builds.")]
        public bool useTestAdsInDevelopment = true;

        [Tooltip("Hashed device IDs that always receive test ads (from logcat / Xcode console).")]
        public List<string> testDeviceIds = new List<string>();

        [Header("Privacy")]
        [Tooltip("Run the UMP consent flow during Ads.Initialize().")]
        public bool gatherConsentOnInitialize = true;

        [Tooltip("iOS: show the App Tracking Transparency popup after the consent form and before ads load.")]
        public bool requestAppTrackingOnIos = true;

        [Tooltip("Development builds on test devices: pretend the user is in the EEA so the consent form shows.")]
        public bool forceEeaConsentInDevelopment;

        [Tooltip("Tell UMP the user is under the age of consent.")]
        public bool tagForUnderAgeOfConsent;

        [Tooltip("AdMob age-restricted treatment (COPPA / families).")]
        public AgeTreatment ageTreatment = AgeTreatment.Unspecified;

        [Tooltip("Firebase analytics_storage when GDPR applies. TCF has no signal for it.")]
        public bool grantAnalyticsStorageUnderGdpr = true;

        [Header("Loading")]
        [Tooltip("Formats preloaded right after initialization.")]
        public bool preloadInterstitial = true;
        public bool preloadRewarded = true;
        public bool preloadRewardedInterstitial = true;
        public bool preloadAppOpen = true;

        [Tooltip("First backoff delay after every tier failed (seconds). Doubles each cycle.")]
        public float retryBaseSeconds = 2f;
        public float retryMaxSeconds = 64f;

        [Tooltip("Loaded full-screen ads older than this are discarded and reloaded (hours).")]
        public float fullScreenExpiryHours = 1f;

        [Header("Show rules")]
        [Tooltip("Minimum seconds between two interstitials.")]
        public float interstitialIntervalSeconds = 30f;

        [Tooltip("Seconds after app start before the first interstitial may show.")]
        public float interstitialDelayAfterStartSeconds = 0f;

        [Tooltip("Reset the interstitial interval after a rewarded ad was watched.")]
        public bool rewardedResetsInterstitialInterval = true;

        [Tooltip("Show App Open when the app returns to the foreground.")]
        public bool appOpenOnResume = true;

        [Tooltip("Minimum seconds the app must stay in background before App Open shows on resume.")]
        public float appOpenMinBackgroundSeconds = 3f;

        [Tooltip("Minimum seconds between App Open ad and any other full-screen ad.")]
        public float appOpenCooldownSeconds = 30f;

        [Tooltip("Do not show App Open during the user's first session.")]
        public bool appOpenSkipFirstSession = true;

        [Tooltip("Pause AudioListener and set timeScale to 0 while a full-screen ad is open.")]
        public bool pauseGameDuringFullScreenAds = true;

        [Header("Banner / MREC")]
        public BannerPosition bannerPosition = BannerPosition.Bottom;
        [Tooltip("Anchored adaptive banner (recommended) instead of fixed 320x50.")]
        public bool adaptiveBanner = true;
        public BannerPosition mrecPosition = BannerPosition.Bottom;

        [Header("Remove Ads")]
        [Tooltip("Formats that keep working after the user bought Remove Ads.")]
        public bool keepRewardedWhenAdsRemoved = true;
        public bool keepRewardedInterstitialWhenAdsRemoved = true;

        [Header("Analytics")]
        [Tooltip("Send ads_opportunity / ads_show / ads_show_failed / ads_load_failed / ads_click / ads_reward.")]
        public bool sendFunnelEvents = true;

        [Tooltip("Log Firebase ad_impression manually. Keep OFF when the AdMob app is linked to Firebase " +
                 "(Firebase then logs ad_impression automatically and a manual event would double count).")]
        public bool sendManualAdImpression = false;

        [Tooltip("Send AdMob revenue to AppsFlyer (requires ADS_APPSFLYER).")]
        public bool sendAppsFlyerAdRevenue = true;

        [Tooltip("Cumulative ad revenue (USD) milestones. Each fires once per user, e.g. 0.01 -> ads_rev_0_01.")]
        public List<double> revenueMilestones = new List<double> { 0.01, 0.05, 0.1, 0.5, 1.0 };

        [Tooltip("Fire 'ads_revenue_batch' with the accumulated value each time it reaches this amount (0 = off). " +
                 "Useful as a value conversion for tROAS campaigns.")]
        public double revenueBatchThreshold = 0.01;

        private static AdsConfig _cached;

        public static AdsConfig Load()
        {
            if (_cached != null)
            {
                return _cached;
            }

            _cached = Resources.Load<AdsConfig>(ResourceName);
            if (_cached == null)
            {
                AdsLog.Warning("MZAdsConfig not found in a Resources folder. Using defaults (no ad unit IDs). " +
                               "Create one with Tools > MZ Ads > Config.");
                _cached = CreateInstance<AdsConfig>();
            }

            return _cached;
        }

        public AdUnitIds IdsFor(AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Banner: return banner;
                case AdFormat.Mrec: return mrec;
                case AdFormat.Interstitial: return interstitial;
                case AdFormat.Rewarded: return rewarded;
                case AdFormat.RewardedInterstitial: return rewardedInterstitial;
                case AdFormat.AppOpen: return appOpen;
                default: throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        /// <summary>Resolved IDs for the running platform, switched to Google test IDs when configured.</summary>
        public List<string> ResolveIds(AdFormat format)
        {
            if (UseTestAds)
            {
                return new List<string> { TestAdUnitIds.ForCurrentPlatform(format) };
            }

            return IdsFor(format).ForCurrentPlatform();
        }

        public bool UseTestAds => useTestAdsInDevelopment && (Application.isEditor || Debug.isDebugBuild);

        public bool IsKeptWhenAdsRemoved(AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Rewarded: return keepRewardedWhenAdsRemoved;
                case AdFormat.RewardedInterstitial: return keepRewardedInterstitialWhenAdsRemoved;
                default: return false;
            }
        }
    }
}
