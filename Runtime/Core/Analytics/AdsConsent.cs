namespace MZ.Ads
{
    /// <summary>
    /// Consent signals derived from the UMP / IAB TCF result, in Google consent mode terms.
    /// Analytics sinks (e.g. Firebase) mirror these instead of hardcoding "granted".
    /// </summary>
    public readonly struct AdsConsent
    {
        public AdsConsent(bool gdprApplies, bool adStorage, bool adUserData, bool adPersonalization,
                          bool analyticsStorage)
        {
            GdprApplies = gdprApplies;
            AdStorage = adStorage;
            AdUserData = adUserData;
            AdPersonalization = adPersonalization;
            AnalyticsStorage = analyticsStorage;
        }

        public bool GdprApplies { get; }
        public bool AdStorage { get; }
        public bool AdUserData { get; }
        public bool AdPersonalization { get; }
        public bool AnalyticsStorage { get; }

        public static AdsConsent AllGranted(bool gdprApplies = false)
        {
            return new AdsConsent(gdprApplies, true, true, true, true);
        }

        /// <summary>
        /// Maps IAB TCF purpose consents to consent mode, following Google's TCF mapping:
        /// ad_storage = P1, ad_user_data = P1 and P7, ad_personalization = P3 and P4.
        /// </summary>
        /// <param name="purposeConsents">IABTCF_PurposeConsents, e.g. "1101…" (index 0 = purpose 1).</param>
        public static AdsConsent FromTcf(bool gdprApplies, string purposeConsents, bool analyticsStorageUnderGdpr)
        {
            if (!gdprApplies)
            {
                return AllGranted();
            }

            bool Purpose(int number)
            {
                return purposeConsents != null && purposeConsents.Length >= number && purposeConsents[number - 1] == '1';
            }

            var p1 = Purpose(1);
            return new AdsConsent(
                gdprApplies: true,
                adStorage: p1,
                adUserData: p1 && Purpose(7),
                adPersonalization: Purpose(3) && Purpose(4),
                analyticsStorage: analyticsStorageUnderGdpr);
        }

        public override string ToString()
        {
            return $"gdpr={GdprApplies} ad_storage={AdStorage} ad_user_data={AdUserData} " +
                   $"ad_personalization={AdPersonalization} analytics_storage={AnalyticsStorage}";
        }
    }
}
