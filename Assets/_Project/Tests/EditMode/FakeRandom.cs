using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    /// <summary>
    /// Deterministic <see cref="IRandom"/> for tests: returns a fixed offset from the range minimum (default
    /// 0 → the first element) and a fixed float, so planner choices are reproducible without a real RNG.
    /// </summary>
    internal sealed class FakeRandom : IRandom
    {
        private readonly int _intOffset;
        private readonly float _floatResult;

        public FakeRandom(int intOffset = 0, float floatResult = 0f)
        {
            _intOffset = intOffset;
            _floatResult = floatResult;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            int value = minInclusive + _intOffset;
            return value < maxExclusive ? value : maxExclusive - 1;
        }

        public float NextFloat(float minInclusive, float maxExclusive) => _floatResult;
    }
}
