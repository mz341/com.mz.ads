using System;
using UnityEngine;

namespace MZ.Ads
{
    /// <summary>
    /// The only class game code needs. Every call is safe to make even when ADS_ADMOB is not defined:
    /// banners do nothing, Show* returns false and rewarded calls report failure.
    /// </summary>
    public static class Ads
    {
        private const string AdsRemovedKey = "mz_ads_removed";
        private const string SessionKey = "mz_ads_sessions";

        private static IAdsProvider _provider;
        private static bool _sessionCounted;

        /// <summary>Raised once when the SDK finished initializing (after consent).</summary>
        public static event Action OnInitialized;

        /// <summary>Every paid impression, revenue in currency units. Hook your own tracking here.</summary>
        public static event Action<AdRevenueInfo> OnAdRevenue;

        /// <summary>A full-screen ad opened. Use it to pause music, timers, etc.</summary>
        public static event Action<AdFormat> OnFullScreenAdOpened;

        /// <summary>A full-screen ad closed (or failed after opening).</summary>
        public static event Action<AdFormat> OnFullScreenAdClosed;

        public static event Action<AdsConsent> OnConsentChanged;

        public static AdsConfig Config => AdsConfig.Load();

        public static bool IsAvailable => _provider != null;

        public static bool IsInitialized => _provider != null && _provider.IsInitialized;

        /// <summary>1 on the first launch, then 2, 3…</summary>
        public static int SessionNumber => PlayerPrefs.GetInt(SessionKey, 0);

        public static bool IsFirstSession => SessionNumber <= 1;

        /// <summary>Runs UMP consent (if configured), initializes AdMob and preloads ads.</summary>
        public static void Initialize(Action onComplete = null)
        {
            CountSession();
            if (_provider == null)
            {
                AdsLog.Warning("Ads.Initialize called but no provider is available. Enable ADS_ADMOB " +
                               "(Tools > MZ Ads > Ads Window) and install Google Mobile Ads.");
                onComplete?.Invoke();
                return;
            }

            _provider.Initialize(Config, () =>
            {
                OnInitialized?.Invoke();
                onComplete?.Invoke();
            });
        }

        // ---- Banner / MREC ----

        public static void ShowBanner()
        {
            if (CanShowWithRemoveAds(AdFormat.Banner))
            {
                _provider?.ShowBanner(AdFormat.Banner);
            }
        }

        public static void HideBanner() => _provider?.HideBanner(AdFormat.Banner);

        public static void DestroyBanner() => _provider?.DestroyBanner(AdFormat.Banner);

        public static void ShowMrec()
        {
            if (CanShowWithRemoveAds(AdFormat.Mrec))
            {
                _provider?.ShowBanner(AdFormat.Mrec);
            }
        }

        public static void HideMrec() => _provider?.HideBanner(AdFormat.Mrec);

        public static void DestroyMrec() => _provider?.DestroyBanner(AdFormat.Mrec);

        // ---- Full screen ----

        public static bool IsInterstitialReady() => IsReady(AdFormat.Interstitial);
        public static bool IsRewardedReady() => IsReady(AdFormat.Rewarded);
        public static bool IsRewardedInterstitialReady() => IsReady(AdFormat.RewardedInterstitial);
        public static bool IsAppOpenReady() => IsReady(AdFormat.AppOpen);

        public static bool IsReady(AdFormat format)
        {
            return _provider != null && CanShowWithRemoveAds(format) && _provider.IsReady(format);
        }

        /// <param name="placement">Where in the game the ad is shown (e.g. "level_end"). Sent to analytics.</param>
        public static bool ShowInterstitial(string placement, Action onClosed = null, Action<string> onFailed = null)
        {
            return Show(new ShowRequest(AdFormat.Interstitial, placement, null, onFailed, onClosed));
        }

        /// <param name="onReward">Called only when the user earned the reward.</param>
        public static bool ShowRewarded(string placement, Action onReward, Action<string> onFailed = null, Action onClosed = null)
        {
            return Show(new ShowRequest(AdFormat.Rewarded, placement, onReward, onFailed, onClosed));
        }

        public static bool ShowRewardedInterstitial(string placement, Action onReward, Action<string> onFailed = null,
                                                    Action onClosed = null)
        {
            return Show(new ShowRequest(AdFormat.RewardedInterstitial, placement, onReward, onFailed, onClosed));
        }

        /// <summary>Manually show App Open (e.g. after a splash screen). Resume shows are automatic.</summary>
        public static bool ShowAppOpen(string placement = "manual", Action onClosed = null, Action<string> onFailed = null)
        {
            return Show(new ShowRequest(AdFormat.AppOpen, placement, null, onFailed, onClosed));
        }

        /// <summary>Starts loading a format now (normally done automatically).</summary>
        public static void Load(AdFormat format)
        {
            if (CanShowWithRemoveAds(format))
            {
                _provider?.Load(format);
            }
        }

        private static bool Show(ShowRequest request)
        {
            AdsAnalytics.Opportunity(Config, request.Format, request.Placement);

            if (_provider == null)
            {
                Fail(request, "no_provider");
                return false;
            }

            if (!CanShowWithRemoveAds(request.Format))
            {
                Fail(request, "ads_removed");
                return false;
            }

            return _provider.Show(request);
        }

        private static void Fail(ShowRequest request, string reason)
        {
            AdsAnalytics.ShowFailed(Config, request.Format, request.Placement, reason);
            request.OnFailed?.Invoke(reason);
        }

        // ---- Remove Ads ----

        public static bool AdsRemoved => PlayerPrefs.GetInt(AdsRemovedKey, 0) == 1;

        /// <summary>Call after a successful Remove Ads purchase (or restore). Rewarded formats keep working.</summary>
        public static void SetAdsRemoved(bool removed)
        {
            PlayerPrefs.SetInt(AdsRemovedKey, removed ? 1 : 0);
            PlayerPrefs.Save();
            _provider?.OnAdsRemovedChanged(removed);
        }

        private static bool CanShowWithRemoveAds(AdFormat format)
        {
            return !AdsRemoved || Config.IsKeptWhenAdsRemoved(format);
        }

        // ---- Privacy / debugging ----

        /// <summary>True when the user must be able to reopen the consent form (show a Privacy button).</summary>
        public static bool IsPrivacyOptionsRequired => _provider != null && _provider.IsPrivacyOptionsRequired;

        /// <param name="onComplete">Error message, or null on success.</param>
        public static void ShowPrivacyOptions(Action<string> onComplete = null)
        {
            if (_provider == null)
            {
                onComplete?.Invoke("no_provider");
                return;
            }

            _provider.ShowPrivacyOptions(onComplete);
        }

        /// <summary>Opens Google's Ad Inspector (test devices / development builds).</summary>
        public static void OpenAdInspector() => _provider?.OpenAdInspector();

        // ---- Provider hooks ----

        internal static void RegisterProvider(IAdsProvider provider)
        {
            _provider = provider;
        }

        internal static void RaiseRevenue(AdRevenueInfo info)
        {
            AdsAnalytics.LogRevenue(info, Config);
            OnAdRevenue?.Invoke(info);
        }

        internal static void RaiseFullScreenOpened(AdFormat format) => OnFullScreenAdOpened?.Invoke(format);

        internal static void RaiseFullScreenClosed(AdFormat format) => OnFullScreenAdClosed?.Invoke(format);

        internal static void RaiseConsent(AdsConsent consent)
        {
            AdsAnalytics.SetConsent(consent);
            OnConsentChanged?.Invoke(consent);
        }

        private static void CountSession()
        {
            if (_sessionCounted)
            {
                return;
            }

            _sessionCounted = true;
            PlayerPrefs.SetInt(SessionKey, SessionNumber + 1);
            PlayerPrefs.Save();
        }
    }
}
