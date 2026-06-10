using StarforgeRelay.Persistence;

namespace StarforgeRelay.Tests.EditMode
{
    /// <summary>
    /// In-memory <see cref="ISettingsStore"/> test double — keeps <see cref="SettingsService"/> tests headless
    /// (no <c>PlayerPrefs</c>). Records <see cref="SaveCount"/> so tests can assert persist-on-change.
    /// </summary>
    internal sealed class FakeSettingsStore : ISettingsStore
    {
        private GameSettings _saved;

        public FakeSettingsStore(GameSettings initial)
        {
            _saved = initial;
        }

        /// <summary>How many times <see cref="Save"/> has been called.</summary>
        public int SaveCount { get; private set; }

        public GameSettings Load() => _saved;

        public void Save(GameSettings settings)
        {
            _saved = settings;
            SaveCount++;
        }
    }
}
