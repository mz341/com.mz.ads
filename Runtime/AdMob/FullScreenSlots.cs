using System;
using GoogleMobileAds.Api;

namespace MZ.Ads.AdMob
{
    internal sealed class InterstitialSlot : FullScreenSlot
    {
        private InterstitialAd _ad;

        public InterstitialSlot(ISlotHost host) : base(AdFormat.Interstitial, host) { }

        protected override void NativeLoad(string adUnitId, AdRequest request, Action<bool, int, string> done)
        {
            InterstitialAd.Load(adUnitId, request, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    done(false, error?.GetCode() ?? -1, error?.GetMessage() ?? "null ad");
                    return;
                }

                _ad = ad;
                ad.OnAdFullScreenContentOpened += () => AdsRunner.OnMainThread(HandleOpened);
                ad.OnAdFullScreenContentClosed += () => AdsRunner.OnMainThread(HandleClosed);
                ad.OnAdFullScreenContentFailed += e => AdsRunner.OnMainThread(() => HandleShowFailed(e?.GetMessage()));
                ad.OnAdClicked += () => AdsRunner.OnMainThread(HandleClicked);
                ad.OnAdPaid += value =>
                {
                    var info = ad.GetResponseInfo();
                    AdsRunner.OnMainThread(() => HandlePaid(value, info));
                };
                done(true, 0, null);
            });
        }

        protected override bool NativeCanShow() => _ad != null && _ad.CanShowAd();

        protected override void NativeShow(Action onReward) => _ad.Show();

        protected override void DestroyNative()
        {
            _ad?.Destroy();
            _ad = null;
        }
    }

    internal sealed class RewardedSlot : FullScreenSlot
    {
        private RewardedAd _ad;

        public RewardedSlot(ISlotHost host) : base(AdFormat.Rewarded, host) { }

        protected override void NativeLoad(string adUnitId, AdRequest request, Action<bool, int, string> done)
        {
            RewardedAd.Load(adUnitId, request, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    done(false, error?.GetCode() ?? -1, error?.GetMessage() ?? "null ad");
                    return;
                }

                _ad = ad;
                ad.OnAdFullScreenContentOpened += () => AdsRunner.OnMainThread(HandleOpened);
                ad.OnAdFullScreenContentClosed += () => AdsRunner.OnMainThread(HandleClosed);
                ad.OnAdFullScreenContentFailed += e => AdsRunner.OnMainThread(() => HandleShowFailed(e?.GetMessage()));
                ad.OnAdClicked += () => AdsRunner.OnMainThread(HandleClicked);
                ad.OnAdPaid += value =>
                {
                    var info = ad.GetResponseInfo();
                    AdsRunner.OnMainThread(() => HandlePaid(value, info));
                };
                done(true, 0, null);
            });
        }

        protected override bool NativeCanShow() => _ad != null && _ad.CanShowAd();

        protected override void NativeShow(Action onReward) => _ad.Show(reward => onReward());

        protected override void DestroyNative()
        {
            _ad?.Destroy();
            _ad = null;
        }
    }

    internal sealed class RewardedInterstitialSlot : FullScreenSlot
    {
        private RewardedInterstitialAd _ad;

        public RewardedInterstitialSlot(ISlotHost host) : base(AdFormat.RewardedInterstitial, host) { }

        protected override void NativeLoad(string adUnitId, AdRequest request, Action<bool, int, string> done)
        {
            RewardedInterstitialAd.Load(adUnitId, request, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    done(false, error?.GetCode() ?? -1, error?.GetMessage() ?? "null ad");
                    return;
                }

                _ad = ad;
                ad.OnAdFullScreenContentOpened += () => AdsRunner.OnMainThread(HandleOpened);
                ad.OnAdFullScreenContentClosed += () => AdsRunner.OnMainThread(HandleClosed);
                ad.OnAdFullScreenContentFailed += e => AdsRunner.OnMainThread(() => HandleShowFailed(e?.GetMessage()));
                ad.OnAdClicked += () => AdsRunner.OnMainThread(HandleClicked);
                ad.OnAdPaid += value =>
                {
                    var info = ad.GetResponseInfo();
                    AdsRunner.OnMainThread(() => HandlePaid(value, info));
                };
                done(true, 0, null);
            });
        }

        protected override bool NativeCanShow() => _ad != null && _ad.CanShowAd();

        protected override void NativeShow(Action onReward) => _ad.Show(reward => onReward());

        protected override void DestroyNative()
        {
            _ad?.Destroy();
            _ad = null;
        }
    }

    internal sealed class AppOpenSlot : FullScreenSlot
    {
        private AppOpenAd _ad;

        public AppOpenSlot(ISlotHost host) : base(AdFormat.AppOpen, host) { }

        /// <summary>Google: App Open ads expire after four hours.</summary>
        protected override TimeSpan Expiry => TimeSpan.FromHours(4);

        protected override void NativeLoad(string adUnitId, AdRequest request, Action<bool, int, string> done)
        {
            AppOpenAd.Load(adUnitId, request, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    done(false, error?.GetCode() ?? -1, error?.GetMessage() ?? "null ad");
                    return;
                }

                _ad = ad;
                ad.OnAdFullScreenContentOpened += () => AdsRunner.OnMainThread(HandleOpened);
                ad.OnAdFullScreenContentClosed += () => AdsRunner.OnMainThread(HandleClosed);
                ad.OnAdFullScreenContentFailed += e => AdsRunner.OnMainThread(() => HandleShowFailed(e?.GetMessage()));
                ad.OnAdClicked += () => AdsRunner.OnMainThread(HandleClicked);
                ad.OnAdPaid += value =>
                {
                    var info = ad.GetResponseInfo();
                    AdsRunner.OnMainThread(() => HandlePaid(value, info));
                };
                done(true, 0, null);
            });
        }

        protected override bool NativeCanShow() => _ad != null && _ad.CanShowAd();

        protected override void NativeShow(Action onReward) => _ad.Show();

        protected override void DestroyNative()
        {
            _ad?.Destroy();
            _ad = null;
        }
    }
}
