namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Randomness seam for testable gameplay rules (guardrails §17). Inject a seeded implementation
    /// (<see cref="SeededRandom"/>) in production and a deterministic fake in tests — never
    /// <c>UnityEngine.Random</c>, so <see cref="ShardSpawnPlanner"/> runs headless and reproducibly.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Uniform integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>Uniform float in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
        float NextFloat(float minInclusive, float maxExclusive);
    }
}
