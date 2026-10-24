using System;

namespace SlotGame.Core
{
    /// <summary>
    /// Source of random numbers used when rolling reel symbols.
    /// </summary>
    public interface IRandomService
    {
        /// <summary>Inclusive min, exclusive max — same range style as Unity's Random.Range for ints.</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    /// <summary>
    /// Default random source for a play session. A mixed GUID + tick seed means each run of the game
    /// produces a different sequence of reel results.
    /// </summary>
    public class SystemRandomService : IRandomService
    {
        private readonly Random random;

        public SystemRandomService()
        {
            random = new Random(Guid.NewGuid().GetHashCode() ^ Environment.TickCount);
        }

        public SystemRandomService(int seed)
        {
            random = new Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }
    }
}
