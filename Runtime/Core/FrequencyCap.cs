namespace MZ.Ads
{
    /// <summary>
    /// Minimum-interval gate. Time is passed in (seconds, e.g. Time.realtimeSinceStartup)
    /// so the logic stays testable.
    /// </summary>
    public sealed class FrequencyCap
    {
        private double _lastEventTime = double.NegativeInfinity;

        public FrequencyCap(double minIntervalSeconds)
        {
            MinIntervalSeconds = minIntervalSeconds;
        }

        public double MinIntervalSeconds { get; set; }

        public bool IsOpen(double now)
        {
            return MinIntervalSeconds <= 0 || now - _lastEventTime >= MinIntervalSeconds;
        }

        public double SecondsRemaining(double now)
        {
            var remaining = MinIntervalSeconds - (now - _lastEventTime);
            return remaining > 0 ? remaining : 0;
        }

        public void Mark(double now)
        {
            _lastEventTime = now;
        }

        public void Reset()
        {
            _lastEventTime = double.NegativeInfinity;
        }
    }
}
