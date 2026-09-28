using System.Collections.Generic;
using NUnit.Framework;

namespace MZ.Ads.Tests
{
    public class TierAndIdTests
    {
        [Test]
        public void SingleMode_UsesOnlyFirstNonEmptyId()
        {
            var ids = AdUnitIds.Resolve(new List<string> { " ", "high", "mid", "low" }, tiered: false);
            CollectionAssert.AreEqual(new[] { "high" }, ids);
        }

        [Test]
        public void TieredMode_KeepsOrderAndCapsAtThree()
        {
            var ids = AdUnitIds.Resolve(new List<string> { "high", "", "mid", "low", "extra" }, tiered: true);
            CollectionAssert.AreEqual(new[] { "high", "mid", "low" }, ids);
        }

        [Test]
        public void TierSequence_WalksHighToLowThenStops()
        {
            var sequence = new TierSequence(new List<string> { "a", "b", "c" });
            Assert.AreEqual("a", sequence.CurrentId);
            Assert.AreEqual(1, sequence.CurrentTier);
            Assert.IsTrue(sequence.Advance());
            Assert.AreEqual("b", sequence.CurrentId);
            Assert.IsTrue(sequence.Advance());
            Assert.AreEqual(3, sequence.CurrentTier);
            Assert.IsFalse(sequence.Advance());
            sequence.Reset();
            Assert.AreEqual("a", sequence.CurrentId);
        }

        [Test]
        public void TierSequences_AreIndependent()
        {
            // The old plugin shared one tier counter between two loads; each slot must own its own.
            var first = new TierSequence(new List<string> { "a1", "a2" });
            var second = new TierSequence(new List<string> { "b1", "b2" });
            first.Advance();
            Assert.AreEqual("a2", first.CurrentId);
            Assert.AreEqual("b1", second.CurrentId);
        }
    }

    public class RetryAndCapTests
    {
        [Test]
        public void Retry_DoublesUntilMax()
        {
            var retry = new RetryPolicy(2, 10);
            Assert.AreEqual(2f, retry.NextDelay());
            Assert.AreEqual(4f, retry.NextDelay());
            Assert.AreEqual(8f, retry.NextDelay());
            Assert.AreEqual(10f, retry.NextDelay());
            retry.Reset();
            Assert.AreEqual(2f, retry.NextDelay());
        }

        [Test]
        public void FrequencyCap_BlocksInsideInterval()
        {
            var cap = new FrequencyCap(30);
            Assert.IsTrue(cap.IsOpen(0));
            cap.Mark(100);
            Assert.IsFalse(cap.IsOpen(110));
            Assert.AreEqual(20, cap.SecondsRemaining(110), 0.001);
            Assert.IsTrue(cap.IsOpen(130));
        }

        [Test]
        public void FrequencyCap_ZeroIntervalNeverBlocks()
        {
            var cap = new FrequencyCap(0);
            cap.Mark(5);
            Assert.IsTrue(cap.IsOpen(5));
        }
    }

    public class RevenueTests
    {
        [Test]
        public void Micros_AreConvertedToCurrencyUnits()
        {
            var info = new AdRevenueInfo(AdFormat.Interstitial, "unit", "level_end", 1, "AdMob Network", 2500, "USD", "Precise");
            Assert.AreEqual(0.0025, info.Value, 1e-12);
            Assert.AreEqual(2500, info.ValueMicros);
        }

        [Test]
        public void AdImpression_UsesDocumentedParameterMapping()
        {
            var info = new AdRevenueInfo(AdFormat.Rewarded, "ca-app-pub-x/1", "shop", 2, "Meta Audience Network", 1_000_000, "USD", "Estimated");
            var p = AdEventNames.AdImpressionParameters(info);

            Assert.AreEqual("AdMob", p["ad_platform"]);
            Assert.AreEqual("Meta Audience Network", p["ad_source"]);
            Assert.AreEqual("rewarded", p["ad_format"]);          // format, never an instance name
            Assert.AreEqual("ca-app-pub-x/1", p["ad_unit_name"]);  // ad unit, never a format
            Assert.AreEqual(1.0, (double)p["value"], 1e-12);        // units, never micros
            Assert.AreEqual("USD", p["currency"]);
        }

        [Test]
        public void MilestoneNames_AreStableAndCultureInvariant()
        {
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                Assert.AreEqual("ads_rev_0_01", AdEventNames.Milestone(0.01));
                Assert.AreEqual("ads_rev_1_00", AdEventNames.Milestone(1));
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        [Test]
        public void CustomEventNames_AvoidFirebaseReservedNames()
        {
            var reserved = new[] { "ad_click", "ad_reward", "ad_query", "ad_exposure", "ad_activeview", "adunit_exposure" };
            foreach (var name in new[]
                     {
                         AdEventNames.Opportunity, AdEventNames.Show, AdEventNames.ShowFailed,
                         AdEventNames.LoadFailed, AdEventNames.Click, AdEventNames.Reward, AdEventNames.RevenueBatch,
                         AdEventNames.Milestone(0.01)
                     })
            {
                CollectionAssert.DoesNotContain(reserved, name);
                Assert.LessOrEqual(name.Length, 40);
            }
        }

        [Test]
        public void Tracker_FiresMilestonesOnceAndBatches()
        {
            var store = new MemoryStore();
            var tracker = new RevenueTracker(store, new[] { 0.01, 0.05 }, 0.02);

            var r1 = tracker.Add(0.012);
            CollectionAssert.AreEqual(new[] { 0.01 }, r1.MilestonesReached);
            Assert.AreEqual(0, r1.BatchValue);

            var r2 = tracker.Add(0.012);
            Assert.IsEmpty(r2.MilestonesReached);                 // 0.01 must not fire twice
            Assert.AreEqual(0.024, r2.BatchValue, 1e-9);          // batch reached 0.02

            var r3 = tracker.Add(0.03);
            CollectionAssert.AreEqual(new[] { 0.05 }, r3.MilestonesReached);
            Assert.AreEqual(0.054, tracker.Total, 1e-9);

            // A new tracker over the same storage keeps the history.
            var reloaded = new RevenueTracker(store, new[] { 0.01, 0.05 }, 0.02);
            Assert.IsEmpty(reloaded.Add(0.001).MilestonesReached);
        }

        [Test]
        public void Tracker_IgnoresInvalidValues()
        {
            var tracker = new RevenueTracker(new MemoryStore(), new[] { 0.01 }, 0.01);
            Assert.IsEmpty(tracker.Add(double.NaN).MilestonesReached);
            Assert.IsEmpty(tracker.Add(-1).MilestonesReached);
            Assert.AreEqual(0, tracker.Total);
        }

        private sealed class MemoryStore : IKeyValueStore
        {
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();
            public string GetString(string key, string fallback) => _values.TryGetValue(key, out var v) ? v : fallback;
            public void SetString(string key, string value) => _values[key] = value;
            public void Save() { }
        }
    }

    public class ConsentTests
    {
        [Test]
        public void OutsideGdpr_EverythingGranted()
        {
            var consent = AdsConsent.FromTcf(false, "", analyticsStorageUnderGdpr: false);
            Assert.IsTrue(consent.AdStorage && consent.AdUserData && consent.AdPersonalization && consent.AnalyticsStorage);
        }

        [Test]
        public void Gdpr_MapsTcfPurposes()
        {
            // Purposes 1..7: 1=yes 2=yes 3=no 4=yes 5=yes 6=yes 7=yes
            var consent = AdsConsent.FromTcf(true, "1101111", analyticsStorageUnderGdpr: true);
            Assert.IsTrue(consent.AdStorage);           // P1
            Assert.IsTrue(consent.AdUserData);          // P1 + P7
            Assert.IsFalse(consent.AdPersonalization);  // P3 missing
        }

        [Test]
        public void Gdpr_EmptyStringDeniesEverything()
        {
            var consent = AdsConsent.FromTcf(true, "", analyticsStorageUnderGdpr: false);
            Assert.IsFalse(consent.AdStorage || consent.AdUserData || consent.AdPersonalization || consent.AnalyticsStorage);
        }
    }
}
