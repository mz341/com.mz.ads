using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MZ.Ads
{
    public interface IKeyValueStore
    {
        string GetString(string key, string fallback);
        void SetString(string key, string value);
        void Save();
    }

    public sealed class PlayerPrefsStore : IKeyValueStore
    {
        public string GetString(string key, string fallback) => PlayerPrefs.GetString(key, fallback);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Save() => PlayerPrefs.Save();
    }

    /// <summary>
    /// Accumulates per-user ad revenue (currency units) and reports:
    /// - milestones: each threshold fires once per user (LTV events for campaign optimisation);
    /// - batches: the accumulated value every time it reaches the batch threshold (value events for tROAS).
    /// Values are persisted with InvariantCulture so comma-decimal locales cannot corrupt them.
    /// </summary>
    public sealed class RevenueTracker
    {
        private const string TotalKey = "mz_ads_revenue_total";
        private const string BatchKey = "mz_ads_revenue_batch";
        private const string MilestonesKey = "mz_ads_revenue_milestones";

        private readonly IKeyValueStore _store;
        private readonly List<double> _milestones;
        private readonly double _batchThreshold;

        public RevenueTracker(IKeyValueStore store, IEnumerable<double> milestones, double batchThreshold)
        {
            _store = store;
            _milestones = new List<double>(milestones ?? new double[0]);
            _milestones.Sort();
            _batchThreshold = batchThreshold;
        }

        public double Total => Read(TotalKey);

        public struct Result
        {
            public List<double> MilestonesReached;
            public double BatchValue; // > 0 when a batch event should fire
        }

        public Result Add(double value)
        {
            var result = new Result { MilestonesReached = new List<double>() };
            if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
            {
                return result;
            }

            var total = Read(TotalKey) + value;
            Write(TotalKey, total);

            var fired = _store.GetString(MilestonesKey, string.Empty);
            var firedSet = new HashSet<string>(fired.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
            foreach (var milestone in _milestones)
            {
                var key = milestone.ToString(CultureInfo.InvariantCulture);
                if (total >= milestone && !firedSet.Contains(key))
                {
                    firedSet.Add(key);
                    result.MilestonesReached.Add(milestone);
                }
            }

            if (result.MilestonesReached.Count > 0)
            {
                _store.SetString(MilestonesKey, string.Join(";", firedSet));
            }

            if (_batchThreshold > 0)
            {
                var batch = Read(BatchKey) + value;
                if (batch >= _batchThreshold)
                {
                    result.BatchValue = batch;
                    batch = 0;
                }

                Write(BatchKey, batch);
            }

            _store.Save();
            return result;
        }

        private double Read(string key)
        {
            var raw = _store.GetString(key, "0");
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
        }

        private void Write(string key, double value)
        {
            _store.SetString(key, value.ToString("R", CultureInfo.InvariantCulture));
        }
    }
}
