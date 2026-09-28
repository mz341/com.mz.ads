using System.Collections.Generic;

namespace MZ.Ads
{
    /// <summary>
    /// Destination for ad analytics (Firebase, AppsFlyer, your own backend…).
    /// Register with <see cref="AdsAnalytics.Register"/>. Integrations bundled with the package
    /// register themselves when their scripting define is enabled.
    /// </summary>
    public interface IAdAnalyticsSink
    {
        /// <summary>Custom event. Parameter values are string, long or double.</summary>
        void LogEvent(string name, IReadOnlyDictionary<string, object> parameters);

        /// <summary>One paid impression with revenue already converted to currency units.</summary>
        void LogAdRevenue(AdRevenueInfo info);

        void OnConsentChanged(AdsConsent consent);
    }
}
