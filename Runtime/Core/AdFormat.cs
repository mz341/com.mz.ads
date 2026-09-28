namespace MZ.Ads
{
    public enum AdFormat
    {
        Banner,
        Mrec,
        Interstitial,
        Rewarded,
        RewardedInterstitial,
        AppOpen
    }

    public enum BannerPosition
    {
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }

    public static class AdFormatExtensions
    {
        /// <summary>Value used for the <c>ad_format</c> analytics parameter.</summary>
        public static string ToAnalyticsName(this AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Banner: return "banner";
                case AdFormat.Mrec: return "mrec";
                case AdFormat.Interstitial: return "interstitial";
                case AdFormat.Rewarded: return "rewarded";
                case AdFormat.RewardedInterstitial: return "rewarded_interstitial";
                case AdFormat.AppOpen: return "app_open";
                default: return format.ToString().ToLowerInvariant();
            }
        }

        public static bool IsFullScreen(this AdFormat format)
        {
            return format != AdFormat.Banner && format != AdFormat.Mrec;
        }

        public static bool IsRewarded(this AdFormat format)
        {
            return format == AdFormat.Rewarded || format == AdFormat.RewardedInterstitial;
        }
    }
}
