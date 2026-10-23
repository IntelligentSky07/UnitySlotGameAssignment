using System;

namespace SlotGame.Core
{
    /// <summary>
    /// Abstraction over the RNG so it can be swapped (e.g. seeded for unit tests).
    /// </summary>
    public interface IRandomService
    {
        /// <summary>Returns an int in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    /// <summary>
    /// RNG backed by System.Random. Seeded from a fresh GUID + tick count so every
    /// session is unpredictable. (UnityEngine.Random is a shared global, which makes
    /// it harder to isolate and test.)
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
