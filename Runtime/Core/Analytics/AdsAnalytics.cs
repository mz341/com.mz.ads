using System;
using System.Collections.Generic;

namespace MZ.Ads
{
    /// <summary>Fans ad analytics out to every registered <see cref="IAdAnalyticsSink"/>.</summary>
    public static class AdsAnalytics
    {
        private static readonly List<IAdAnalyticsSink> Sinks = new List<IAdAnalyticsSink>();
        private static RevenueTracker _tracker;
        private static AdsConsent? _lastConsent;

        public static void Register(IAdAnalyticsSink sink)
        {
            if (sink == null || Sinks.Contains(sink))
            {
                return;
            }

            Sinks.Add(sink);
            if (_lastConsent.HasValue)
            {
                Safe(() => sink.OnConsentChanged(_lastConsent.Value));
            }
        }

        public static void Unregister(IAdAnalyticsSink sink)
        {
            Sinks.Remove(sink);
        }

        public static void LogEvent(string name, Dictionary<string, object> parameters)
        {
            foreach (var sink in Sinks.ToArray())
            {
                Safe(() => sink.LogEvent(name, parameters));
            }
        }

        internal static void SetConsent(AdsConsent consent)
        {
            _lastConsent = consent;
            AdsLog.Info("Consent: " + consent);
            foreach (var sink in Sinks.ToArray())
            {
                Safe(() => sink.OnConsentChanged(consent));
            }
        }

        internal static void LogRevenue(AdRevenueInfo info, AdsConfig config)
        {
            AdsLog.Info("Paid impression: " + info);
            foreach (var sink in Sinks.ToArray())
            {
                Safe(() => sink.LogAdRevenue(info));
            }

            _tracker ??= new RevenueTracker(new PlayerPrefsStore(), config.revenueMilestones, config.revenueBatchThreshold);
            var result = _tracker.Add(info.Value);

            foreach (var milestone in result.MilestonesReached)
            {
                LogEvent(AdEventNames.Milestone(milestone), new Dictionary<string, object>
                {
                    { AdEventNames.ParamValue, milestone },
                    { AdEventNames.ParamCurrency, info.Currency },
                });
            }

            if (result.BatchValue > 0)
            {
                LogEvent(AdEventNames.RevenueBatch, new Dictionary<string, object>
                {
                    { AdEventNames.ParamValue, result.BatchValue },
                    { AdEventNames.ParamCurrency, info.Currency },
                });
            }
        }

        // ---- Funnel events ----

        internal static void Opportunity(AdsConfig config, AdFormat format, string placement)
        {
            Funnel(config, AdEventNames.Opportunity, format, placement);
        }

        internal static void Shown(AdsConfig config, AdFormat format, string placement, int tier)
        {
            Funnel(config, AdEventNames.Show, format, placement, AdEventNames.ParamTier, tier);
        }

        internal static void ShowFailed(AdsConfig config, AdFormat format, string placement, string reason)
        {
            Funnel(config, AdEventNames.ShowFailed, format, placement, AdEventNames.ParamReason, reason);
        }

        internal static void LoadFailed(AdsConfig config, AdFormat format, int tier, string errorCode)
        {
            Funnel(config, AdEventNames.LoadFailed, format, null, AdEventNames.ParamTier, tier,
                   AdEventNames.ParamErrorCode, errorCode);
        }

        internal static void Clicked(AdsConfig config, AdFormat format, string placement)
        {
            Funnel(config, AdEventNames.Click, format, placement);
        }

        internal static void Rewarded(AdsConfig config, AdFormat format, string placement)
        {
            Funnel(config, AdEventNames.Reward, format, placement);
        }

        private static void Funnel(AdsConfig config, string eventName, AdFormat format, string placement,
                                   params object[] extra)
        {
            if (!config.sendFunnelEvents || Sinks.Count == 0)
            {
                return;
            }

            var parameters = new Dictionary<string, object> { { AdEventNames.ParamFormat, format.ToAnalyticsName() } };
            if (!string.IsNullOrEmpty(placement))
            {
                parameters[AdEventNames.ParamPlacement] = Truncate(placement);
            }

            for (var i = 0; i + 1 < extra.Length; i += 2)
            {
                var value = extra[i + 1];
                if (value == null)
                {
                    continue;
                }

                if (value is int number)
                {
                    value = (long)number;
                }
                else if (value is string text)
                {
                    value = Truncate(text);
                }

                parameters[(string)extra[i]] = value;
            }

            LogEvent(eventName, parameters);
        }

        /// <summary>Firebase rejects string parameter values longer than 100 characters.</summary>
        private static string Truncate(string value)
        {
            return value.Length <= 100 ? value : value.Substring(0, 100);
        }

        private static void Safe(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                AdsLog.Warning("Analytics sink threw: " + e);
            }
        }
    }
}
