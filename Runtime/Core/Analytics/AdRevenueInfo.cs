namespace MZ.Ads
{
    /// <summary>One paid impression, reported from AdMob's OnAdPaid callback.</summary>
    public readonly struct AdRevenueInfo
    {
        public const double MicrosPerUnit = 1_000_000d;

        public AdRevenueInfo(AdFormat format, string adUnitId, string placement, int tier,
                             string adSource, long valueMicros, string currency, string precision)
        {
            Format = format;
            AdUnitId = adUnitId ?? string.Empty;
            Placement = placement ?? string.Empty;
            Tier = tier;
            AdSource = string.IsNullOrEmpty(adSource) ? "unknown" : adSource;
            ValueMicros = valueMicros;
            Currency = string.IsNullOrEmpty(currency) ? "USD" : currency;
            Precision = precision ?? string.Empty;
        }

        public AdFormat Format { get; }
        public string AdUnitId { get; }
        public string Placement { get; }

        /// <summary>1-based tier that filled (1 = highest floor).</summary>
        public int Tier { get; }

        /// <summary>Network that served the ad (e.g. "AdMob Network", "Meta Audience Network").</summary>
        public string AdSource { get; }

        /// <summary>Raw AdMob value in micros. Never send this to analytics as revenue.</summary>
        public long ValueMicros { get; }

        /// <summary>Revenue in currency units (micros / 1,000,000).</summary>
        public double Value => MicrosToUnits(ValueMicros);

        public string Currency { get; }
        public string Precision { get; }

        public static double MicrosToUnits(long micros)
        {
            return micros / MicrosPerUnit;
        }

        public override string ToString()
        {
            return $"{Format.ToAnalyticsName()} {Value:0.######} {Currency} source={AdSource} tier={Tier} placement={Placement}";
        }
    }
}
