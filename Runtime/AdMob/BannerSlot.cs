using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

namespace MZ.Ads.AdMob
{
    /// <summary>
    /// Banner or MREC. Walks tiers only until the first fill; after that AdMob refreshes the view
    /// itself and a failed refresh keeps the previous creative on screen.
    /// </summary>
    internal sealed class BannerSlot
    {
        private readonly ISlotHost _host;
        private readonly RetryPolicy _retry;
        private TierSequence _tiers = new TierSequence(null);
        private BannerView _view;
        private bool _wantVisible;
        private bool _loading;
        private bool _loaded;
        private int _tier;
        private string _adUnitId;
        private Coroutine _retryRoutine;
        private bool _warnedNoIds;

        public BannerSlot(AdFormat format, ISlotHost host)
        {
            Format = format;
            _host = host;
            _retry = new RetryPolicy(host.Config.retryBaseSeconds, host.Config.retryMaxSeconds);
        }

        public AdFormat Format { get; }

        private AdsConfig Config => _host.Config;

        public void SetIds(List<string> ids)
        {
            _tiers = new TierSequence(ids);
        }

        public void Show()
        {
            _wantVisible = true;
            if (_view != null && _loaded)
            {
                _view.Show();
                return;
            }

            if (!_loading && _retryRoutine == null)
            {
                StartCycle();
            }
        }

        public void Hide()
        {
            _wantVisible = false;
            _view?.Hide();
        }

        public void Destroy()
        {
            _wantVisible = false;
            AdsRunner.Instance.Cancel(_retryRoutine);
            _retryRoutine = null;
            DestroyView();
        }

        private void StartCycle()
        {
            if (_tiers.IsEmpty)
            {
                if (!_warnedNoIds)
                {
                    _warnedNoIds = true;
                    AdsLog.Warning($"{Format}: no ad unit IDs configured for this platform.");
                }

                return;
            }

            _tiers.Reset();
            LoadCurrentTier();
        }

        private void LoadCurrentTier()
        {
            DestroyView();
            _adUnitId = _tiers.CurrentId;
            _tier = _tiers.CurrentTier;
            _loading = true;
            AdsLog.Info($"{Format}: loading tier {_tier}/{_tiers.Count} ({_adUnitId})");

            var view = new BannerView(_adUnitId, CreateSize(), ToAdPosition(PositionSetting()));
            _view = view;
            view.OnBannerAdLoaded += () => AdsRunner.OnMainThread(() => OnLoaded(view));
            view.OnBannerAdLoadFailed += error => AdsRunner.OnMainThread(() => OnLoadFailed(view, error));
            view.OnAdClicked += () => AdsRunner.OnMainThread(() =>
            {
                AdsAnalytics.Clicked(Config, Format, Format.ToAnalyticsName());
                _host.NotifyClicked(Format);
            });
            view.OnAdPaid += value =>
            {
                var info = view.GetResponseInfo();
                AdsRunner.OnMainThread(() => OnPaid(value, info));
            };
            view.LoadAd(new AdRequest());
        }

        private void OnLoaded(BannerView view)
        {
            if (view != _view)
            {
                return;
            }

            var firstFill = !_loaded;
            _loading = false;
            _loaded = true;
            _retry.Reset();

            if (_wantVisible)
            {
                view.Show();
            }
            else
            {
                view.Hide();
            }

            if (firstFill)
            {
                AdsLog.Info($"{Format}: loaded tier {_tier}.");
                AdsAnalytics.Shown(Config, Format, Format.ToAnalyticsName(), _tier);
            }
        }

        private void OnLoadFailed(BannerView view, LoadAdError error)
        {
            if (view != _view)
            {
                return;
            }

            var code = error?.GetCode() ?? -1;
            if (_loaded)
            {
                // Refresh failure: the previous creative stays visible.
                AdsLog.Info($"{Format}: refresh failed ({code}).");
                return;
            }

            AdsLog.Info($"{Format}: tier {_tier} failed ({code}) {error?.GetMessage()}");
            if (_tiers.Advance())
            {
                LoadCurrentTier();
                return;
            }

            _loading = false;
            DestroyView();
            AdsAnalytics.LoadFailed(Config, Format, _tier, code.ToString());
            var delay = _retry.NextDelay();
            _retryRoutine = AdsRunner.Instance.Delay(delay, () =>
            {
                _retryRoutine = null;
                if (_wantVisible)
                {
                    StartCycle();
                }
            });
        }

        private void OnPaid(AdValue value, ResponseInfo info)
        {
            if (value == null)
            {
                return;
            }

            Ads.RaiseRevenue(new AdRevenueInfo(Format, _adUnitId, Format.ToAnalyticsName(), _tier,
                FullScreenSlot.AdSourceName(info), value.Value, value.CurrencyCode, value.Precision.ToString()));
        }

        private void DestroyView()
        {
            _view?.Destroy();
            _view = null;
            _loaded = false;
            _loading = false;
        }

        private BannerPosition PositionSetting()
        {
            return Format == AdFormat.Mrec ? Config.mrecPosition : Config.bannerPosition;
        }

        private AdSize CreateSize()
        {
            if (Format == AdFormat.Mrec)
            {
                return AdSize.MediumRectangle;
            }

            return Config.adaptiveBanner
                ? AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth)
                : AdSize.Banner;
        }

        private static AdPosition ToAdPosition(BannerPosition position)
        {
            switch (position)
            {
                case BannerPosition.Top: return AdPosition.Top;
                case BannerPosition.TopLeft: return AdPosition.TopLeft;
                case BannerPosition.TopRight: return AdPosition.TopRight;
                case BannerPosition.BottomLeft: return AdPosition.BottomLeft;
                case BannerPosition.BottomRight: return AdPosition.BottomRight;
                case BannerPosition.Center: return AdPosition.Center;
                default: return AdPosition.Bottom;
            }
        }
    }
}
