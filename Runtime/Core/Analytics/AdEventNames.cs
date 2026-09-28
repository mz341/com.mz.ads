using System.Collections.Generic;
using System.Globalization;

namespace MZ.Ads
{
    /// <summary>
    /// Every event and parameter name the plugin sends. Few event names, detail in parameters,
    /// to stay far below Firebase's 500 event-name limit.
    /// Custom events use the "ads_" prefix because Firebase reserves several "ad_" names
    /// (ad_click, ad_reward, ad_query, ad_exposure…) and silently drops manual events with them.
    /// </summary>
    public static class AdEventNames
    {
        public const string AdImpression = "ad_impression";
        public const string Opportunity = "ads_opportunity";
        public const string Show = "ads_show";
        public const string ShowFailed = "ads_show_failed";
        public const string LoadFailed = "ads_load_failed";
        public const string Click = "ads_click";
        public const string Reward = "ads_reward";
        public const string RevenueBatch = "ads_revenue_batch";
        public const string RevenueMilestonePrefix = "ads_rev_";

        public const string ParamFormat = "ad_format";
        public const string ParamPlacement = "placement";
        public const string ParamTier = "tier";
        public const string ParamReason = "reason";
        public const string ParamErrorCode = "error_code";
        public const string ParamValue = "value";
        public const string ParamCurrency = "currency";

        /// <summary>Milestone event name, e.g. 0.05 → "ads_rev_0_05".</summary>
        public static string Milestone(double threshold)
        {
            return RevenueMilestonePrefix + threshold.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', '_');
        }

        /// <summary>
        /// Parameters for a manual Firebase <c>ad_impression</c>, matching the Firebase ad revenue docs:
        /// https://firebase.google.com/docs/analytics/measure-ad-revenue
        /// </summary>
        public static Dictionary<string, object> AdImpressionParameters(AdRevenueInfo info)
        {
            return new Dictionary<string, object>
            {
                { "ad_platform", "AdMob" },
                { "ad_source", info.AdSource },
                { "ad_format", info.Format.ToAnalyticsName() },
                { "ad_unit_name", info.AdUnitId },
                { ParamValue, info.Value },
                { ParamCurrency, info.Currency },
            };
        }
    }
}
