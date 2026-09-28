using System;
using System.Collections.Generic;
using UnityEngine;

namespace MZ.Ads
{
    /// <summary>
    /// Ad unit IDs for one format. IDs are always a list ordered high floor → low floor.
    /// With ADS_TIERED_IDS defined every non-empty entry is used as a tier;
    /// without it only the first non-empty entry is used (single ID mode).
    /// </summary>
    [Serializable]
    public class AdUnitIds
    {
        public const int MaxTiers = 3;

#if ADS_TIERED_IDS
        public const bool TieredEnabled = true;
#else
        public const bool TieredEnabled = false;
#endif

        [Tooltip("Android ad unit IDs, highest floor first.")]
        public List<string> android = new List<string>();

        [Tooltip("iOS ad unit IDs, highest floor first.")]
        public List<string> ios = new List<string>();

        public List<string> ForCurrentPlatform()
        {
#if UNITY_IOS
            return Resolve(ios, TieredEnabled);
#else
            return Resolve(android, TieredEnabled);
#endif
        }

        /// <summary>Drops empty entries and applies the tier / single mode rule.</summary>
        public static List<string> Resolve(IList<string> source, bool tiered)
        {
            var result = new List<string>();
            if (source == null)
            {
                return result;
            }

            foreach (var id in source)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                result.Add(id.Trim());
                if (!tiered || result.Count >= MaxTiers)
                {
                    break;
                }
            }

            return result;
        }
    }
}
