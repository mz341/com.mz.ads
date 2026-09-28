using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MZ.Ads
{
    /// <summary>
    /// "GoogleCMP" prefab for a small first scene (e.g. "Privacy"): runs the Google UMP consent form,
    /// then the iOS ATT popup, initializes AdMob, and finally loads the next scene.
    /// Build order example: 0 = Privacy (GoogleCMP), 1 = your game / plugin scene.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MZ Ads/Google CMP")]
    public sealed class GoogleCmp : MonoBehaviour
    {
        public enum NextStep
        {
            LoadNextBuildScene,
            LoadSceneByName,
            StayInScene
        }

        [Tooltip("What happens after consent + initialization finished.")]
        [SerializeField] private NextStep next = NextStep.LoadNextBuildScene;

        [Tooltip("Used when Next = Load Scene By Name.")]
        [SerializeField] private string sceneName = "";

        [Tooltip("Continue even if consent / init has not finished after this many seconds (0 = wait).")]
        [SerializeField] private float maxWaitSeconds = 0f;

        [Tooltip("Minimum time the scene stays visible (e.g. for a logo).")]
        [SerializeField] private float minSeconds = 0.5f;

        [SerializeField] private UnityEvent onFinished = new UnityEvent();

        private bool _finished;

        private IEnumerator Start()
        {
            // iOS only shows consent / ATT popups once the app is active: skip the first frame.
            yield return null;
            var startedAt = Time.realtimeSinceStartup;

            var done = false;
            Ads.Initialize(() => done = true);

            while (!done && (maxWaitSeconds <= 0 || Time.realtimeSinceStartup - startedAt < maxWaitSeconds))
            {
                yield return null;
            }

            while (Time.realtimeSinceStartup - startedAt < minSeconds)
            {
                yield return null;
            }

            Finish();
        }

        private void Finish()
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            onFinished.Invoke();

            switch (next)
            {
                case NextStep.LoadNextBuildScene:
                    var index = SceneManager.GetActiveScene().buildIndex + 1;
                    if (index < SceneManager.sceneCountInBuildSettings)
                    {
                        SceneManager.LoadScene(index);
                    }
                    else
                    {
                        AdsLog.Warning("GoogleCMP: no next scene in Build Settings.");
                    }

                    break;
                case NextStep.LoadSceneByName:
                    if (!string.IsNullOrEmpty(sceneName))
                    {
                        SceneManager.LoadScene(sceneName);
                    }

                    break;
            }
        }
    }
}
