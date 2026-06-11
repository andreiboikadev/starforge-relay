using System;
using System.Collections;
using StarforgeRelay.App;
using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Scene adapter that closes the Stabilize Run loop (GDD §5, §12) and implements <see cref="IRoundLifecycle"/>
    /// for the app state machine (T13). The composition root (T12) injects a <see cref="RoundController"/>
    /// factory; this adapter builds a fresh round on <see cref="StartRound"/>, routes each port's
    /// <see cref="PortSocket.InsertEvaluated"/> outcome into the rules, ticks the clock, consumes accepted
    /// shards through the spawner (T10), pauses via <c>Time.timeScale</c> + an insert gate, and re-surfaces the
    /// round events for HUD/audio/VFX (T14/T16/T17). Thin adapter: it owns no formulas and references no XRI type.
    /// </summary>
    public sealed class RoundLoopController : MonoBehaviour, IRoundLifecycle
    {
        [Tooltip("The reactor ports whose insert outcomes drive the round.")]
        [SerializeField] private PortSocket[] _ports;

        [Tooltip("Spawner that fills the pads and consumes/respawns shards (T10).")]
        [SerializeField] private ShardSpawner _spawner;

        private Func<RoundController> _roundFactory;
        private RoundController _round;
        private bool _paused;

        /// <inheritdoc />
        public event Action<RoundEndedEvent> RoundEnded;

        /// <summary>Raised when a fresh round starts (T14 — the HUD resets/refreshes).</summary>
        public event Action RoundStarted;

        /// <summary>Round events re-raised for the HUD (T14).</summary>
        public event Action<CorrectInsertEvent> CorrectInserted;

        /// <inheritdoc cref="CorrectInserted" />
        public event Action<WrongInsertEvent> WrongInserted;

        /// <inheritdoc cref="CorrectInserted" />
        public event Action<ShardExpiredEvent> ShardExpired;

        /// <summary>Live meters for the HUD (T14); 0 when no round is active. The rule services own the values.</summary>
        public int Score => _round != null ? _round.Score : 0;

        /// <inheritdoc cref="Score" />
        public int Combo => _round != null ? _round.Combo : 0;

        /// <inheritdoc cref="Score" />
        public int Heat => _round != null ? _round.Heat : 0;

        /// <inheritdoc cref="Score" />
        public int Stabilization => _round != null ? _round.Stabilization : 0;

        /// <inheritdoc cref="Score" />
        public float TimeRemaining => _round != null ? _round.TimeRemaining : 0f;

        /// <summary>True while the round is paused. The feedback layer (T16) reads this to suppress grab/release
        /// cues, which bypass the round loop's own insert/expiry gates (XRI selection runs at <c>timeScale 0</c>).</summary>
        public bool IsPaused => _paused;

        /// <summary>
        /// Receive the round factory from the composition root (T12/T13) and wire the stable scene refs (port
        /// insert events + the spawner's expiry). Does <b>not</b> start a round — the app state machine starts
        /// it on entering Playing.
        /// </summary>
        public void Initialize(Func<RoundController> roundFactory)
        {
            if (roundFactory == null || _ports == null || _ports.Length == 0 || _spawner == null)
            {
                Debug.LogError("[RoundLoopController] Missing round factory, ports, or spawner — round disabled.", this);
                return;
            }

            _roundFactory = roundFactory;
            _spawner.ShardLifetimeExpired += OnShardLifetimeExpired;

            for (int i = 0; i < _ports.Length; i++)
            {
                if (_ports[i] != null)
                {
                    _ports[i].InsertEvaluated += OnInsertEvaluated;
                }
            }
        }

        /// <inheritdoc />
        public void StartRound()
        {
            if (_roundFactory == null)
            {
                return;
            }

            // Cancel any in-flight consume from a prior round before it can touch a re-used shard.
            StopAllCoroutines();
            UnsubscribeRound();

            _round = _roundFactory();
            _round.CorrectInserted += OnCorrectInserted;
            _round.WrongInserted += OnWrongInserted;
            _round.ShardExpired += OnShardExpired;
            _round.Ended += OnEnded;

            _spawner.AcceptsProvider = () => _round.Stabilization;
            _paused = false;
            Time.timeScale = 1f;

            _round.Start();
            _spawner.BeginFill();
            RoundStarted?.Invoke();
        }

        /// <inheritdoc />
        public void PauseRound()
        {
            _paused = true;
            Time.timeScale = 0f;
        }

        /// <inheritdoc />
        public void ResumeRound()
        {
            _paused = false;
            Time.timeScale = 1f;
        }

        /// <inheritdoc />
        public void StopRound()
        {
            StopAllCoroutines();
            UnsubscribeRound();
            _round = null;
            _paused = false;
            Time.timeScale = 1f;

            if (_spawner != null)
            {
                _spawner.StopRespawns();
                _spawner.ClearActive();
            }
        }

        private void OnDisable()
        {
            if (_ports != null)
            {
                for (int i = 0; i < _ports.Length; i++)
                {
                    if (_ports[i] != null)
                    {
                        _ports[i].InsertEvaluated -= OnInsertEvaluated;
                    }
                }
            }

            if (_spawner != null)
            {
                _spawner.ShardLifetimeExpired -= OnShardLifetimeExpired;
            }

            UnsubscribeRound();
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (_round != null && !_paused && _round.Phase == RoundPhase.Playing)
            {
                _round.Tick(Time.deltaTime);
            }
        }

        private void OnInsertEvaluated(PortSocket port, ShardView shard, InsertOutcome outcome)
        {
            if (_paused || _round == null || _round.Phase != RoundPhase.Playing)
            {
                return;
            }

            if (outcome == InsertOutcome.Correct)
            {
                _round.ApplyCorrect();
                StartCoroutine(ConsumeRoutine(port, shard));
            }
            else if (outcome == InsertOutcome.Wrong)
            {
                _round.ApplyWrong();
                // The wrong shard returns to its pad via the port's eject (T10); it is not consumed.
            }
        }

        // Defer one frame to leave the socket's selectEntered callback before mutating its selection, then
        // release the accepted shard from the socket and pool it — never pool a socket-held shard (T09).
        private IEnumerator ConsumeRoutine(PortSocket port, ShardView shard)
        {
            yield return null;

            if (port != null)
            {
                port.ReleaseSelected();
            }

            if (shard != null)
            {
                _spawner.Despawn(shard);
            }
        }

        // A shard's lifetime ran out (T11b): apply the rule (+heat, combo reset only if held), then consume it
        // through the same pool/respawn path as an accept.
        private void OnShardLifetimeExpired(ShardView shard, bool wasHeld)
        {
            if (_paused || _round == null || _round.Phase != RoundPhase.Playing)
            {
                return;
            }

            _round.ApplyExpired(wasHeld);
            _spawner.Despawn(shard);
        }

        private void OnEnded(RoundEndedEvent e)
        {
            _spawner.StopRespawns();
            RoundEnded?.Invoke(e);
        }

        // Re-raise the round's events for the HUD (T14). The [Round] dev logs they replaced are gone.
        private void OnCorrectInserted(CorrectInsertEvent e) => CorrectInserted?.Invoke(e);

        private void OnWrongInserted(WrongInsertEvent e) => WrongInserted?.Invoke(e);

        private void OnShardExpired(ShardExpiredEvent e) => ShardExpired?.Invoke(e);

        private void UnsubscribeRound()
        {
            if (_round != null)
            {
                _round.CorrectInserted -= OnCorrectInserted;
                _round.WrongInserted -= OnWrongInserted;
                _round.ShardExpired -= OnShardExpired;
                _round.Ended -= OnEnded;
            }
        }
    }
}
