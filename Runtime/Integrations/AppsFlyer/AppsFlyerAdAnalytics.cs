using System.Collections.Generic;
using System.Globalization;
using AppsFlyerSDK;
using UnityEngine;

namespace MZ.Ads.AppsFlyer
{
    /// <summary>
    /// Sends AdMob impression revenue to AppsFlyer in currency units (never micros), plus the
    /// cumulative revenue milestone / batch events. Funnel events are not forwarded.
    /// AppsFlyer itself must be initialized by the game (dev key, app ID, TCF collection).
    /// </summary>
    public sealed class AppsFlyerAdAnalytics : IAdAnalyticsSink
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            AdsAnalytics.Register(new AppsFlyerAdAnalytics());
        }

        public void LogAdRevenue(AdRevenueInfo info)
        {
            if (!Ads.Config.sendAppsFlyerAdRevenue)
            {
                return;
            }

            var data = new AFAdRevenueData(info.AdSource, MediationNetwork.GoogleAdMob, info.Currency, info.Value);
            var extra = new Dictionary<string, string>
            {
                { AdRevenueScheme.AD_UNIT, info.AdUnitId },
                { AdRevenueScheme.AD_TYPE, info.Format.ToAnalyticsName() },
                { AdRevenueScheme.PLACEMENT, info.Placement },
            };
            AppsFlyerSDK.AppsFlyer.logAdRevenue(data, extra);
        }

        public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            var isRevenueEvent = name == AdEventNames.RevenueBatch ||
                                 name.StartsWith(AdEventNames.RevenueMilestonePrefix);
            if (!isRevenueEvent)
            {
                return;
            }

            var values = new Dictionary<string, string>();
            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    values[pair.Key] = System.Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
                }
            }

            AppsFlyerSDK.AppsFlyer.sendEvent(name, values);
        }

        public void OnConsentChanged(AdsConsent consent)
        {
            // AppsFlyer reads the TCF string itself when the game calls AppsFlyer.enableTCFDataCollection(true).
        }
    }
}
