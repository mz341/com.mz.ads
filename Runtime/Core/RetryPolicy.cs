using System;

namespace MZ.Ads
{
    /// <summary>Exponential backoff used after every tier of a format failed to load.</summary>
    public sealed class RetryPolicy
    {
        private readonly float _baseSeconds;
        private readonly float _maxSeconds;

        public RetryPolicy(float baseSeconds, float maxSeconds)
        {
            _baseSeconds = Math.Max(0.5f, baseSeconds);
            _maxSeconds = Math.Max(_baseSeconds, maxSeconds);
        }

        public int Attempt { get; private set; }

        /// <summary>Registers a failed cycle and returns how long to wait before the next one.</summary>
        public float NextDelay()
        {
            Attempt++;
            var delay = _baseSeconds * Math.Pow(2, Attempt - 1);
            return (float)Math.Min(delay, _maxSeconds);
        }

        public void Reset()
        {
            Attempt = 0;
        }
    }
}
