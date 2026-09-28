using UnityEngine;

namespace MZ.Ads.AdMob
{
    /// <summary>Pauses audio and timeScale while a full-screen ad is open, then restores both.</summary>
    internal sealed class GamePauser
    {
        private bool _paused;
        private float _savedTimeScale = 1f;
        private bool _savedAudioPause;

        public void Pause()
        {
            if (_paused)
            {
                return;
            }

            _paused = true;
            _savedTimeScale = Time.timeScale;
            _savedAudioPause = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        public void Resume()
        {
            if (!_paused)
            {
                return;
            }

            _paused = false;
            Time.timeScale = _savedTimeScale;
            AudioListener.pause = _savedAudioPause;
        }
    }
}
