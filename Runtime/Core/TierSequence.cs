using System.Collections.Generic;

namespace MZ.Ads
{
    /// <summary>
    /// Walks ad unit IDs from the highest tier to the lowest for one load cycle.
    /// Each ad slot owns its own instance, so two formats (or two loads) never share a counter.
    /// </summary>
    public sealed class TierSequence
    {
        private readonly List<string> _ids;
        private int _index;

        public TierSequence(List<string> ids)
        {
            _ids = ids ?? new List<string>();
        }

        public int Count => _ids.Count;

        public bool IsEmpty => _ids.Count == 0;

        /// <summary>1-based tier number of the current ID (1 = highest floor).</summary>
        public int CurrentTier => _index + 1;

        public string CurrentId => _index < _ids.Count ? _ids[_index] : null;

        /// <summary>Moves to the next lower tier. Returns false when every tier has been tried.</summary>
        public bool Advance()
        {
            if (_index + 1 >= _ids.Count)
            {
                return false;
            }

            _index++;
            return true;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}
