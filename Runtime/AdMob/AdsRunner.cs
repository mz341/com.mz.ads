using System;
using System.Collections;
using GoogleMobileAds.Common;
using UnityEngine;

namespace MZ.Ads.AdMob
{
    /// <summary>Hidden DontDestroyOnLoad object that runs retry timers for the plugin.</summary>
    internal sealed class AdsRunner : MonoBehaviour
    {
        private static AdsRunner _instance;

        public static AdsRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[MZ.Ads]") { hideFlags = HideFlags.HideInHierarchy };
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AdsRunner>();
                }

                return _instance;
            }
        }

        /// <summary>Runs an action after a real-time delay (unaffected by timeScale).</summary>
        public Coroutine Delay(float seconds, Action action)
        {
            return StartCoroutine(DelayRoutine(seconds, action));
        }

        public void Cancel(Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }

        private static IEnumerator DelayRoutine(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action();
        }

        /// <summary>Queues an SDK callback onto the Unity main thread.</summary>
        public static void OnMainThread(Action action)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(action);
        }
    }
}
