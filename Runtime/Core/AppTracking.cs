using System;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace MZ.Ads
{
    /// <summary>Mirrors ATTrackingManagerAuthorizationStatus.</summary>
    public enum AppTrackingStatus
    {
        NotDetermined = 0,
        Restricted = 1,
        Denied = 2,
        Authorized = 3
    }

    /// <summary>
    /// iOS App Tracking Transparency (ATT). On Android and in the Editor every call reports Authorized
    /// immediately, so game code can call it on all platforms.
    /// The popup text comes from Google Mobile Ads Settings (User Tracking Usage Description).
    /// </summary>
    public static class AppTracking
    {
        private static Action<AppTrackingStatus> _pending;

        public static AppTrackingStatus Status
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return (AppTrackingStatus)_MZAttStatus();
#else
                return AppTrackingStatus.Authorized;
#endif
            }
        }

        /// <summary>
        /// Shows the ATT popup if the user has not answered yet; otherwise returns the stored answer.
        /// iOS only shows the popup while the app is active, so call it after the first frame.
        /// </summary>
        public static void Request(Action<AppTrackingStatus> onComplete)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (Status != AppTrackingStatus.NotDetermined)
            {
                onComplete?.Invoke(Status);
                return;
            }

            _pending += onComplete;
            _MZAttRequest(OnNativeResult);
#else
            onComplete?.Invoke(AppTrackingStatus.Authorized);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private delegate void NativeCallback(int status);

        [DllImport("__Internal")]
        private static extern int _MZAttStatus();

        [DllImport("__Internal")]
        private static extern void _MZAttRequest(NativeCallback callback);

        [AOT.MonoPInvokeCallback(typeof(NativeCallback))]
        private static void OnNativeResult(int status)
        {
            var callback = _pending;
            _pending = null;
            AdsLog.Info("ATT result: " + (AppTrackingStatus)status);
            callback?.Invoke((AppTrackingStatus)status);
        }
#endif
    }
}
