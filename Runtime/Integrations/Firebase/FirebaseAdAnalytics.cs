using System.Collections.Generic;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using UnityEngine;

namespace MZ.Ads.Firebase
{
    /// <summary>
    /// Sends plugin events to Firebase Analytics and mirrors UMP consent into Firebase consent mode.
    /// Manual ad_impression is only sent when AdsConfig.sendManualAdImpression is on
    /// (leave it off when the AdMob app is linked to Firebase, which logs ad_impression itself).
    /// </summary>
    public sealed class FirebaseAdAnalytics : IAdAnalyticsSink
    {
        private const int MaxQueuedActions = 200;

        /// <summary>Set to false before the first scene loads if your game initializes Firebase itself
        /// and calls <see cref="MarkFirebaseReady"/> afterwards.</summary>
        public static bool AutoInitializeFirebase = true;

        private static FirebaseAdAnalytics _instance;
        private readonly Queue<System.Action> _queue = new Queue<System.Action>();
        private bool _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            _instance = new FirebaseAdAnalytics();
            AdsAnalytics.Register(_instance);
            if (AutoInitializeFirebase)
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.Result == DependencyStatus.Available)
                    {
                        MarkFirebaseReady();
                    }
                    else
                    {
                        AdsLog.Warning("Firebase dependencies not available: " + task.Result);
                    }
                });
            }
        }

        /// <summary>Flushes queued events. Call it after your own Firebase initialization succeeded.</summary>
        public static void MarkFirebaseReady()
        {
            if (_instance == null || _instance._ready)
            {
                return;
            }

            _instance._ready = true;
            while (_instance._queue.Count > 0)
            {
                _instance._queue.Dequeue()();
            }
        }

        public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            Run(() => FirebaseAnalytics.LogEvent(name, ToParameters(parameters)));
        }

        public void LogAdRevenue(AdRevenueInfo info)
        {
            if (!Ads.Config.sendManualAdImpression)
            {
                return;
            }

            var parameters = AdEventNames.AdImpressionParameters(info);
            Run(() => FirebaseAnalytics.LogEvent(AdEventNames.AdImpression, ToParameters(parameters)));
        }

        public void OnConsentChanged(AdsConsent consent)
        {
            Run(() => FirebaseAnalytics.SetConsent(new Dictionary<ConsentType, ConsentStatus>
            {
                { ConsentType.AdStorage, Status(consent.AdStorage) },
                { ConsentType.AdUserData, Status(consent.AdUserData) },
                { ConsentType.AdPersonalization, Status(consent.AdPersonalization) },
                { ConsentType.AnalyticsStorage, Status(consent.AnalyticsStorage) },
            }));
        }

        private static ConsentStatus Status(bool granted) => granted ? ConsentStatus.Granted : ConsentStatus.Denied;

        private void Run(System.Action action)
        {
            if (_ready)
            {
                action();
                return;
            }

            if (_queue.Count < MaxQueuedActions)
            {
                _queue.Enqueue(action);
            }
        }

        private static Parameter[] ToParameters(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null)
            {
                return new Parameter[0];
            }

            var result = new List<Parameter>(parameters.Count);
            foreach (var pair in parameters)
            {
                switch (pair.Value)
                {
                    case string text:
                        result.Add(new Parameter(pair.Key, text));
                        break;
                    case long number:
                        result.Add(new Parameter(pair.Key, number));
                        break;
                    case int number:
                        result.Add(new Parameter(pair.Key, (long)number));
                        break;
                    case double real:
                        result.Add(new Parameter(pair.Key, real));
                        break;
                    case float real:
                        result.Add(new Parameter(pair.Key, (double)real));
                        break;
                    case bool flag:
                        result.Add(new Parameter(pair.Key, flag ? 1L : 0L));
                        break;
                    case null:
                        break;
                    default:
                        result.Add(new Parameter(pair.Key, pair.Value.ToString()));
                        break;
                }
            }

            return result.ToArray();
        }
    }
}
