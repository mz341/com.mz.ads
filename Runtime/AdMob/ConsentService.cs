using System;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace MZ.Ads.AdMob
{
    /// <summary>Google UMP consent flow plus reading the IAB TCF result it stores on the device.</summary>
    internal sealed class ConsentService
    {
        public bool CanRequestAds => ConsentInformation.CanRequestAds();

        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        /// <param name="onDone">Error message, or null when the flow finished.</param>
        public void Gather(AdsConfig config, Action<string> onDone)
        {
            var parameters = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = config.tagForUnderAgeOfConsent,
                ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = config.UseTestAds && config.forceEeaConsentInDevelopment
                        ? DebugGeography.EEA
                        : DebugGeography.Disabled,
                    TestDeviceHashedIds = config.testDeviceIds,
                }
            };

            ConsentInformation.Update(parameters, updateError =>
            {
                AdsRunner.OnMainThread(() =>
                {
                    if (updateError != null)
                    {
                        onDone(updateError.Message);
                        return;
                    }

                    // Shows the form only when consent is required and not yet obtained.
                    ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
                    {
                        AdsRunner.OnMainThread(() => onDone(showError?.Message));
                    });
                });
            });
        }

        public void ShowPrivacyOptions(Action<string> onDone)
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                AdsRunner.OnMainThread(() => onDone?.Invoke(error?.Message));
            });
        }

        public AdsConsent ReadConsent(AdsConfig config)
        {
            return AdsConsent.FromTcf(ReadGdprApplies(), ReadPurposeConsents(), config.grantAnalyticsStorageUnderGdpr);
        }

        // UMP writes the IAB TCF v2 keys to the platform's default preferences.
        private const string GdprAppliesKey = "IABTCF_gdprApplies";
        private const string PurposeConsentsKey = "IABTCF_PurposeConsents";

        private static bool ReadGdprApplies()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var prefs = AndroidDefaultPreferences())
                {
                    return prefs.Call<int>("getInt", GdprAppliesKey, 0) == 1;
                }
#elif UNITY_IOS && !UNITY_EDITOR
                // Unity PlayerPrefs on iOS reads NSUserDefaults.standardUserDefaults.
                return PlayerPrefs.GetInt(GdprAppliesKey, 0) == 1;
#else
                return false;
#endif
            }
            catch (Exception e)
            {
                AdsLog.Warning("Could not read " + GdprAppliesKey + ": " + e.Message);
                return false;
            }
        }

        private static string ReadPurposeConsents()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var prefs = AndroidDefaultPreferences())
                {
                    return prefs.Call<string>("getString", PurposeConsentsKey, string.Empty);
                }
#elif UNITY_IOS && !UNITY_EDITOR
                return PlayerPrefs.GetString(PurposeConsentsKey, string.Empty);
#else
                return string.Empty;
#endif
            }
            catch (Exception e)
            {
                AdsLog.Warning("Could not read " + PurposeConsentsKey + ": " + e.Message);
                return string.Empty;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject AndroidDefaultPreferences()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var manager = new AndroidJavaClass("android.preference.PreferenceManager"))
            {
                return manager.CallStatic<AndroidJavaObject>("getDefaultSharedPreferences", activity);
            }
        }
#endif
    }
}
