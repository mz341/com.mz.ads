using UnityEngine;
using UnityEngine.Events;

namespace MZ.Ads
{
    /// <summary>
    /// "AdsInitializer" prefab: put it in your first gameplay / plugin scene. It initializes the ads
    /// (consent → ATT → AdMob) and survives scene loads. Safe to have in several scenes: duplicates destroy
    /// themselves and a second Ads.Initialize() call is a no-op.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MZ Ads/Ads Initializer")]
    public sealed class AdsInitializer : MonoBehaviour
    {
        private static AdsInitializer _instance;

        [Tooltip("Call Ads.Initialize() in Start.")]
        [SerializeField] private bool initializeOnStart = true;

        [Tooltip("Keep this object alive across scene loads.")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Tooltip("Show the banner as soon as the SDK is ready.")]
        [SerializeField] private bool showBannerOnInitialized;

        [SerializeField] private UnityEvent onInitialized = new UnityEvent();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                Initialize();
            }
        }

        /// <summary>Hook this to a button / event when initializeOnStart is off.</summary>
        public void Initialize()
        {
            Ads.Initialize(() =>
            {
                if (showBannerOnInitialized)
                {
                    Ads.ShowBanner();
                }

                onInitialized.Invoke();
            });
        }
    }
}
