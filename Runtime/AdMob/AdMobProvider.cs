using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace MZ.Ads.AdMob
{
    /// <summary>
    /// AdMob implementation behind <see cref="Ads"/>: consent → SDK init → preload, plus the show rules
    /// (interstitial interval, App Open cooldown / resume guards, pausing, Remove Ads).
    /// </summary>
    internal sealed class AdMobProvider : IAdsProvider, ISlotHost
    {
        private readonly Dictionary<AdFormat, FullScreenSlot> _fullScreen = new Dictionary<AdFormat, FullScreenSlot>();
        private readonly Dictionary<AdFormat, BannerSlot> _banners = new Dictionary<AdFormat, BannerSlot>();
        private readonly List<Action> _pendingCallbacks = new List<Action>();
        private readonly ConsentService _consent = new ConsentService();
        private readonly GamePauser _pauser = new GamePauser();

        private FrequencyCap _interstitialCap;
        private FrequencyCap _appOpenCooldown;
        private bool _initializing;
        private bool _sdkStarted;
        private bool _fullScreenShowing;
        private bool _wasBackgrounded;
        private double _backgroundedAt;
        private double _lastClickAt = double.NegativeInfinity;

        public AdsConfig Config { get; private set; }

        public bool IsInitialized { get; private set; }

        public bool IsPrivacyOptionsRequired => _consent.PrivacyOptionsRequired;

        private static double Now => Time.realtimeSinceStartupAsDouble;

        // ---- Initialization ----

        public void Initialize(AdsConfig config, Action onComplete)
        {
            if (IsInitialized)
            {
                onComplete?.Invoke();
                return;
            }

            if (onComplete != null)
            {
                _pendingCallbacks.Add(onComplete);
            }

            if (_initializing)
            {
                return;
            }

            _initializing = true;
            Config = config;
            _interstitialCap = new FrequencyCap(config.interstitialIntervalSeconds);
            _appOpenCooldown = new FrequencyCap(config.appOpenCooldownSeconds);
            if (config.interstitialDelayAfterStartSeconds > 0)
            {
                // Treat app start as the "last interstitial" so the first one waits the configured delay.
                _interstitialCap.MinIntervalSeconds = config.interstitialDelayAfterStartSeconds;
                _interstitialCap.Mark(Now);
            }

            _ = AdsRunner.Instance; // create the runner on the main thread

            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.SetiOSAppPauseOnBackground(true);
            MobileAds.SetRequestConfiguration(BuildRequestConfiguration(config));

            if (!config.gatherConsentOnInitialize)
            {
                PublishConsent();
                RequestAppTrackingThen(StartSdk);
                return;
            }

            // Consent from a previous session: start loading right away while UMP refreshes (Google's guidance).
            if (_consent.CanRequestAds)
            {
                StartSdk();
            }

            _consent.Gather(config, error =>
            {
                if (error != null)
                {
                    AdsLog.Warning("Consent flow error: " + error);
                }

                PublishConsent();
                if (_consent.CanRequestAds)
                {
                    RequestAppTrackingThen(StartSdk);
                }
                else if (!_sdkStarted)
                {
                    AdsLog.Warning("Consent does not allow ad requests. Ads stay disabled for this session.");
                    CompleteInitialization(false);
                }
            });
        }

        private static RequestConfiguration BuildRequestConfiguration(AdsConfig config)
        {
            var testDevices = new List<string> { AdRequest.TestDeviceSimulator };
            testDevices.AddRange(config.testDeviceIds);

            var request = new RequestConfiguration { TestDeviceIds = testDevices };
            switch (config.ageTreatment)
            {
                case AgeTreatment.Child:
                    request.AgeRestrictedTreatment = AgeRestrictedTreatment.Child;
                    break;
                case AgeTreatment.Teen:
                    request.AgeRestrictedTreatment = AgeRestrictedTreatment.Teen;
                    break;
                default:
                    request.AgeRestrictedTreatment = AgeRestrictedTreatment.Unspecified;
                    break;
            }

            return request;
        }

        /// <summary>iOS order recommended by Google: UMP consent → ATT popup → first ad requests.</summary>
        private void RequestAppTrackingThen(Action next)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (Config.requestAppTrackingOnIos && AppTracking.Status == AppTrackingStatus.NotDetermined)
            {
                AppTracking.Request(status => AdsRunner.OnMainThread(next));
                return;
            }
#endif
            next();
        }

        private void StartSdk()
        {
            if (_sdkStarted)
            {
                return;
            }

            _sdkStarted = true;
            AdsLog.Info("Initializing Google Mobile Ads" + (Config.UseTestAds ? " (test ads)." : "."));
            MobileAds.Initialize(status =>
            {
                AdsRunner.OnMainThread(() =>
                {
                    if (status == null)
                    {
                        AdsLog.Warning("MobileAds.Initialize returned no status.");
                    }

                    CreateSlots();
                    AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
                    CompleteInitialization(true);
                    Preload();
                });
            });
        }

        private void CompleteInitialization(bool success)
        {
            IsInitialized = success;
            _initializing = false;
            var callbacks = _pendingCallbacks.ToArray();
            _pendingCallbacks.Clear();
            foreach (var callback in callbacks)
            {
                callback();
            }
        }

        private void PublishConsent()
        {
            Ads.RaiseConsent(_consent.ReadConsent(Config));
        }

        private void CreateSlots()
        {
#if ADS_INTERSTITIAL
            AddFullScreen(new InterstitialSlot(this));
#endif
#if ADS_REWARDED
            AddFullScreen(new RewardedSlot(this));
#endif
#if ADS_REWARDED_INTERSTITIAL
            AddFullScreen(new RewardedInterstitialSlot(this));
#endif
#if ADS_APP_OPEN
            AddFullScreen(new AppOpenSlot(this));
#endif
#if ADS_BANNER
            AddBanner(new BannerSlot(AdFormat.Banner, this));
#endif
#if ADS_MREC
            AddBanner(new BannerSlot(AdFormat.Mrec, this));
#endif
            ApplyAdsRemoved(Ads.AdsRemoved);
        }

        private void AddFullScreen(FullScreenSlot slot)
        {
            slot.SetIds(Config.ResolveIds(slot.Format));
            _fullScreen[slot.Format] = slot;
        }

        private void AddBanner(BannerSlot slot)
        {
            slot.SetIds(Config.ResolveIds(slot.Format));
            _banners[slot.Format] = slot;
        }

        private void Preload()
        {
            if (Config.preloadInterstitial) Load(AdFormat.Interstitial);
            if (Config.preloadRewarded) Load(AdFormat.Rewarded);
            if (Config.preloadRewardedInterstitial) Load(AdFormat.RewardedInterstitial);
            if (Config.preloadAppOpen) Load(AdFormat.AppOpen);
        }

        // ---- Full screen ----

        public bool IsReady(AdFormat format)
        {
            return IsInitialized && _fullScreen.TryGetValue(format, out var slot) && slot.IsReady;
        }

        public void Load(AdFormat format)
        {
            if (IsInitialized && _fullScreen.TryGetValue(format, out var slot))
            {
                slot.Load();
            }
        }

        public bool Show(ShowRequest request)
        {
            if (!IsInitialized)
            {
                return Reject(request, "not_initialized");
            }

            if (!_fullScreen.TryGetValue(request.Format, out var slot))
            {
                return Reject(request, "format_disabled");
            }

            if (_fullScreenShowing)
            {
                return Reject(request, "another_ad_showing");
            }

            var gate = CheckShowRules(request.Format);
            if (gate != null)
            {
                return Reject(request, gate);
            }

            return slot.Show(request);
        }

        /// <summary>Returns a failure reason, or null when the format may show now.</summary>
        private string CheckShowRules(AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Interstitial:
                    if (!_interstitialCap.IsOpen(Now)) return "interval";
                    if (!_appOpenCooldown.IsOpen(Now)) return "cooldown";
                    return null;
                case AdFormat.AppOpen:
                    if (Config.appOpenSkipFirstSession && Ads.IsFirstSession) return "first_session";
                    if (!_appOpenCooldown.IsOpen(Now)) return "cooldown";
                    return null;
                default:
                    // Rewarded formats are user initiated and never capped.
                    return null;
            }
        }

        private bool Reject(ShowRequest request, string reason)
        {
            AdsLog.Info($"{request.Format}: rejected ({reason}).");
            AdsAnalytics.ShowFailed(Config ?? Ads.Config, request.Format, request.Placement, reason);
            request.OnFailed?.Invoke(reason);
            return false;
        }

        // ---- ISlotHost ----

        public void NotifyFullScreenWillShow(AdFormat format)
        {
            // Capture timeScale / audio before the SDK (or the Editor placeholder) touches them.
            _fullScreenShowing = true;
            if (Config.pauseGameDuringFullScreenAds)
            {
                _pauser.Pause();
            }
        }

        public void NotifyFullScreenOpened(AdFormat format)
        {
            Ads.RaiseFullScreenOpened(format);
        }

        public void NotifyFullScreenClosed(AdFormat format, bool rewardEarned)
        {
            _fullScreenShowing = false;
            _pauser.Resume();

            // Every full-screen ad starts the App Open cooldown, so App Open never follows another ad.
            _appOpenCooldown.Mark(Now);

            if (format == AdFormat.Interstitial || (format.IsRewarded() && rewardEarned && Config.rewardedResetsInterstitialInterval))
            {
                _interstitialCap.MinIntervalSeconds = Config.interstitialIntervalSeconds;
                _interstitialCap.Mark(Now);
            }

            Ads.RaiseFullScreenClosed(format);
        }

        public void NotifyClicked(AdFormat format)
        {
            _lastClickAt = Now;
        }

        // ---- App Open on resume ----

        private void OnAppStateChanged(AppState state)
        {
            if (state == AppState.Background)
            {
                _wasBackgrounded = true;
                _backgroundedAt = Now;
                return;
            }

            if (state != AppState.Foreground || !_wasBackgrounded || !Config.appOpenOnResume)
            {
                return;
            }

            _wasBackgrounded = false;
            var backgroundSeconds = Now - _backgroundedAt;
            var returnedFromAdClick = _lastClickAt >= _backgroundedAt - 2;
            if (_fullScreenShowing || returnedFromAdClick || backgroundSeconds < Config.appOpenMinBackgroundSeconds)
            {
                AdsLog.Info("App Open skipped on resume (ad showing, ad click or short background).");
                return;
            }

            Show(new ShowRequest(AdFormat.AppOpen, "resume", null, null, null));
        }

        // ---- Banner / MREC ----

        public void ShowBanner(AdFormat format)
        {
            if (!IsInitialized)
            {
                // Remember the intent: show as soon as the SDK is ready.
                _pendingCallbacks.Add(() => ShowBanner(format));
                return;
            }

            if (_banners.TryGetValue(format, out var slot))
            {
                slot.Show();
            }
            else
            {
                AdsLog.Warning($"{format} requested but its scripting symbol is not enabled.");
            }
        }

        public void HideBanner(AdFormat format)
        {
            if (_banners.TryGetValue(format, out var slot))
            {
                slot.Hide();
            }
        }

        public void DestroyBanner(AdFormat format)
        {
            if (_banners.TryGetValue(format, out var slot))
            {
                slot.Destroy();
            }
        }

        // ---- Remove Ads / privacy / debugging ----

        public void OnAdsRemovedChanged(bool removed)
        {
            if (IsInitialized)
            {
                ApplyAdsRemoved(removed);
                if (!removed)
                {
                    Preload();
                }
            }
        }

        private void ApplyAdsRemoved(bool removed)
        {
            foreach (var slot in _fullScreen.Values)
            {
                slot.SetEnabled(!removed || Config.IsKeptWhenAdsRemoved(slot.Format));
            }

            if (removed)
            {
                foreach (var banner in _banners.Values)
                {
                    banner.Destroy();
                }
            }
        }

        public void ShowPrivacyOptions(Action<string> onComplete)
        {
            _consent.ShowPrivacyOptions(error =>
            {
                PublishConsent();
                onComplete?.Invoke(error);
            });
        }

        public void OpenAdInspector()
        {
            MobileAds.OpenAdInspector(error =>
            {
                if (error != null)
                {
                    AdsLog.Warning("Ad Inspector: " + error.GetMessage());
                }
            });
        }
    }
}
