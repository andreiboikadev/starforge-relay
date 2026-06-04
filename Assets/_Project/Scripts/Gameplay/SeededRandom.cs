using System;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Production <see cref="IRandom"/> backed by <see cref="System.Random"/> — deterministic for a given
    /// seed and free of <c>UnityEngine.Random</c>, so the same seed reproduces a run in tests and on device.
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        private readonly Random _random;

        /// <summary>Create with an explicit seed (reproducible).</summary>
        public SeededRandom(int seed) => _random = new Random(seed);

        /// <summary>Create with a time-based seed (production variety; not reproducible).</summary>
        public SeededRandom() => _random = new Random();

        public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

        public float NextFloat(float minInclusive, float maxExclusive) =>
            minInclusive + (float)_random.NextDouble() * (maxExclusive - minInclusive);
    }
}
