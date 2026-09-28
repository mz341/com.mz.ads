using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

namespace MZ.Ads.AdMob
{
    /// <summary>Callbacks from a slot back to the provider (pause, caps, App Open guards).</summary>
    internal interface ISlotHost
    {
        AdsConfig Config { get; }

        /// <summary>Called right before the SDK show call, while the game is still in its normal state.</summary>
        void NotifyFullScreenWillShow(AdFormat format);

        void NotifyFullScreenOpened(AdFormat format);
        void NotifyFullScreenClosed(AdFormat format, bool rewardEarned);
        void NotifyClicked(AdFormat format);
    }

    /// <summary>
    /// One full-screen format: its own tier sequence, its own retry policy and at most one load in flight.
    /// Subclasses only wrap the matching GMA ad type.
    /// </summary>
    internal abstract class FullScreenSlot
    {
        private enum SlotState
        {
            Idle,
            Loading,
            Ready,
            Showing
        }

        protected readonly ISlotHost Host;
        private TierSequence _tiers = new TierSequence(null);
        private readonly RetryPolicy _retry;
        private SlotState _state = SlotState.Idle;
        private DateTime _loadedAtUtc;
        private int _loadedTier;
        private string _loadedId;
        private string _lastPlacement = "unknown";
        private ShowRequest _current;
        private bool _rewardEarned;
        private Coroutine _retryRoutine;
        private bool _enabled = true;
        private bool _warnedNoIds;

        protected FullScreenSlot(AdFormat format, ISlotHost host)
        {
            Format = format;
            Host = host;
            _retry = new RetryPolicy(host.Config.retryBaseSeconds, host.Config.retryMaxSeconds);
        }

        public AdFormat Format { get; }

        protected AdsConfig Config => Host.Config;

        /// <summary>Loaded ads older than this are thrown away and reloaded.</summary>
        protected virtual TimeSpan Expiry => TimeSpan.FromHours(Math.Max(0.1, Config.fullScreenExpiryHours));

        public bool IsShowing => _state == SlotState.Showing;

        public bool IsReady => _enabled && _state == SlotState.Ready && !IsExpired && NativeCanShow();

        private bool IsExpired => DateTime.UtcNow - _loadedAtUtc > Expiry;

        public void SetIds(List<string> ids)
        {
            _tiers = new TierSequence(ids);
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled && _state != SlotState.Showing)
            {
                AdsRunner.Instance.Cancel(_retryRoutine);
                DestroyNative();
                _state = SlotState.Idle;
            }
        }

        // ---- Loading ----

        public void Load()
        {
            if (!_enabled || _state == SlotState.Loading || _state == SlotState.Showing)
            {
                return;
            }

            if (_state == SlotState.Ready)
            {
                if (!IsExpired && NativeCanShow())
                {
                    return;
                }

                AdsLog.Info($"{Format}: cached ad expired, reloading.");
            }

            DestroyNative();
            _state = SlotState.Idle;

            if (_tiers.IsEmpty)
            {
                if (!_warnedNoIds)
                {
                    _warnedNoIds = true;
                    AdsLog.Warning($"{Format}: no ad unit IDs configured for this platform.");
                }

                return;
            }

            AdsRunner.Instance.Cancel(_retryRoutine);
            _retryRoutine = null;
            _tiers.Reset();
            LoadCurrentTier();
        }

        private void LoadCurrentTier()
        {
            _state = SlotState.Loading;
            var id = _tiers.CurrentId;
            var tier = _tiers.CurrentTier;
            AdsLog.Info($"{Format}: loading tier {tier}/{_tiers.Count} ({id})");

            NativeLoad(id, new AdRequest(), (success, errorCode, errorMessage) =>
            {
                AdsRunner.OnMainThread(() => OnLoadResult(success, errorCode, errorMessage, id, tier));
            });
        }

        private void OnLoadResult(bool success, int errorCode, string errorMessage, string id, int tier)
        {
            if (!_enabled)
            {
                DestroyNative();
                _state = SlotState.Idle;
                return;
            }

            if (success)
            {
                _state = SlotState.Ready;
                _loadedAtUtc = DateTime.UtcNow;
                _loadedTier = tier;
                _loadedId = id;
                _retry.Reset();
                AdsLog.Info($"{Format}: loaded tier {tier}.");
                return;
            }

            AdsLog.Info($"{Format}: tier {tier} failed ({errorCode}) {errorMessage}");
            if (_tiers.Advance())
            {
                LoadCurrentTier();
                return;
            }

            // Every tier failed in this cycle: one analytics event, then back off.
            _state = SlotState.Idle;
            AdsAnalytics.LoadFailed(Config, Format, tier, errorCode.ToString());
            var delay = _retry.NextDelay();
            AdsLog.Info($"{Format}: all tiers failed, retry #{_retry.Attempt} in {delay:0}s.");
            _retryRoutine = AdsRunner.Instance.Delay(delay, () =>
            {
                _retryRoutine = null;
                Load();
            });
        }

        // ---- Showing ----

        public bool Show(ShowRequest request)
        {
            if (!IsReady)
            {
                var reason = _state == SlotState.Showing ? "already_showing" : "not_ready";
                Fail(request, reason);
                if (_state == SlotState.Idle || (_state == SlotState.Ready && IsExpired))
                {
                    Load();
                }

                return false;
            }

            _current = request;
            _lastPlacement = request.Placement;
            _rewardEarned = false;
            _state = SlotState.Showing;
            AdsLog.Info($"{Format}: showing (placement={request.Placement}, tier={_loadedTier}).");

            Host.NotifyFullScreenWillShow(Format);
            try
            {
                NativeShow(() => AdsRunner.OnMainThread(() => OnRewardEarned(request)));
            }
            catch (Exception e)
            {
                AdsLog.Warning($"{Format}: show threw {e.Message}");
                HandleShowFailed(e.Message);
                return false;
            }

            return true;
        }

        private void Fail(ShowRequest request, string reason)
        {
            AdsLog.Info($"{Format}: cannot show ({reason}).");
            AdsAnalytics.ShowFailed(Config, Format, request.Placement, reason);
            request.OnFailed?.Invoke(reason);
        }

        private void OnRewardEarned(ShowRequest request)
        {
            // Captured request, so a reward delivered after close still reaches the right caller.
            if (_current == request)
            {
                _rewardEarned = true;
            }

            AdsAnalytics.Rewarded(Config, Format, request.Placement);
            request.OnReward?.Invoke();
        }

        // ---- Native event handlers (subclasses call these on the main thread) ----

        protected void HandleOpened()
        {
            AdsAnalytics.Shown(Config, Format, _lastPlacement, _loadedTier);
            Host.NotifyFullScreenOpened(Format);
        }

        protected void HandleClosed()
        {
            var request = _current;
            var rewarded = _rewardEarned;
            _current = null;
            DestroyNative();
            _state = SlotState.Idle;
            Host.NotifyFullScreenClosed(Format, rewarded);
            request?.OnClosed?.Invoke();
            Load();
        }

        protected void HandleShowFailed(string message)
        {
            var request = _current;
            _current = null;
            DestroyNative();
            _state = SlotState.Idle;
            Host.NotifyFullScreenClosed(Format, false);
            if (request != null)
            {
                Fail(request, "show_error");
            }

            AdsLog.Warning($"{Format}: failed to show: {message}");
            Load();
        }

        protected void HandleClicked()
        {
            AdsAnalytics.Clicked(Config, Format, _lastPlacement);
            Host.NotifyClicked(Format);
        }

        protected void HandlePaid(AdValue value, ResponseInfo responseInfo)
        {
            if (value == null)
            {
                return;
            }

            Ads.RaiseRevenue(new AdRevenueInfo(
                Format,
                _loadedId,
                _lastPlacement,
                _loadedTier,
                AdSourceName(responseInfo),
                value.Value,
                value.CurrencyCode,
                value.Precision.ToString()));
        }

        internal static string AdSourceName(ResponseInfo responseInfo)
        {
            try
            {
                return responseInfo?.GetLoadedAdapterResponseInfo()?.AdSourceName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ---- GMA wrappers ----

        /// <param name="done">(success, errorCode, errorMessage). Must be called exactly once.</param>
        protected abstract void NativeLoad(string adUnitId, AdRequest request, Action<bool, int, string> done);

        protected abstract bool NativeCanShow();

        protected abstract void NativeShow(Action onReward);

        protected abstract void DestroyNative();
    }
}
