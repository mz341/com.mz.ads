using System;

namespace MZ.Ads
{
    /// <summary>Callbacks for one full-screen show request.</summary>
    public sealed class ShowRequest
    {
        public ShowRequest(AdFormat format, string placement, Action onReward, Action<string> onFailed, Action onClosed)
        {
            Format = format;
            Placement = string.IsNullOrEmpty(placement) ? "default" : placement;
            OnReward = onReward;
            OnFailed = onFailed;
            OnClosed = onClosed;
        }

        public AdFormat Format { get; }
        public string Placement { get; }

        /// <summary>Rewarded formats only. Raised from OnUserEarnedReward, never from close.</summary>
        public Action OnReward { get; }

        /// <summary>Ad could not be shown. Argument is the reason (not_ready, cooldown, show_error…).</summary>
        public Action<string> OnFailed { get; }

        /// <summary>Ad was shown and the user closed it.</summary>
        public Action OnClosed { get; }
    }

    /// <summary>Implemented by the AdMob layer (MZ.Ads.AdMob). The core API has no SDK dependency.</summary>
    internal interface IAdsProvider
    {
        bool IsInitialized { get; }
        void Initialize(AdsConfig config, Action onComplete);
        bool IsReady(AdFormat format);
        void Load(AdFormat format);

        /// <summary>Returns false (and calls OnFailed) when the ad cannot be shown right now.</summary>
        bool Show(ShowRequest request);

        void ShowBanner(AdFormat format);
        void HideBanner(AdFormat format);
        void DestroyBanner(AdFormat format);

        bool IsPrivacyOptionsRequired { get; }
        void ShowPrivacyOptions(Action<string> onComplete);
        void OpenAdInspector();
        void OnAdsRemovedChanged(bool removed);
    }
}
