using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MZ.Ads
{
    /// <summary>
    /// "AppTracking" prefab (iOS): shows the App Tracking Transparency popup on its own.
    /// Not needed when you use GoogleCMP / AdsInitializer with "Request App Tracking On iOS" enabled
    /// in the config: they already ask after the consent form. Use this to ask at a custom moment.
    /// </summary>
    [AddComponentMenu("MZ Ads/App Tracking (iOS)")]
    public sealed class AppTrackingPrompt : MonoBehaviour
    {
        [Serializable]
        public class StatusEvent : UnityEvent<AppTrackingStatus> { }

        [SerializeField] private bool requestOnStart = true;

        [Tooltip("Seconds to wait before asking (the popup only appears once the app is active).")]
        [SerializeField] private float delaySeconds = 0.5f;

        [SerializeField] private StatusEvent onCompleted = new StatusEvent();

        private IEnumerator Start()
        {
            if (!requestOnStart)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(delaySeconds);
            Request();
        }

        public void Request()
        {
            AppTracking.Request(status => onCompleted.Invoke(status));
        }
    }
}
