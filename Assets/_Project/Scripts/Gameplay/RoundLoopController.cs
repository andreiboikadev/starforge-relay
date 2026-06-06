using System.Collections;
using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Scene adapter that closes the Stabilize Run loop (GDD §5, §12): it receives the pure
    /// <see cref="RoundController"/> (T06) from the composition root (T12) via <see cref="Initialize"/>, routes
    /// each port's <see cref="PortSocket.InsertEvaluated"/> outcome (T09) into the round rules, ticks the round
    /// clock, consumes an accepted shard through the spawner (T10), and surfaces the round events for HUD/audio/
    /// VFX (T14/T16/T17). Thin adapter: it owns no formulas (those are the tested T01–T06 rules) and references
    /// no XRI type (it talks to <see cref="PortSocket"/> / <see cref="ShardSpawner"/>, not the socket).
    /// </summary>
    public sealed class RoundLoopController : MonoBehaviour
    {
        [Tooltip("The reactor ports whose insert outcomes drive the round.")]
        [SerializeField] private PortSocket[] _ports;

        [Tooltip("Spawner that fills the pads and consumes/respawns shards (T10).")]
        [SerializeField] private ShardSpawner _spawner;

        private RoundController _round;

        /// <summary>
        /// Receive the round graph from the composition root (T12) and wire it up. Called from the root's
        /// <c>Awake</c> (before any <c>Start</c>); the adapter does nothing until it runs.
        /// </summary>
        public void Initialize(RoundController roundController)
        {
            if (roundController == null || _ports == null || _ports.Length == 0 || _spawner == null)
            {
                Debug.LogError("[RoundLoopController] Missing round controller, ports, or spawner — round not started.", this);
                return;
            }

            _round = roundController;

            _round.CorrectInserted += OnCorrectInserted;
            _round.WrongInserted += OnWrongInserted;
            _round.ShardExpired += OnShardExpired;
            _round.Ended += OnEnded;

            _spawner.AcceptsProvider = () => _round.Stabilization;
            _spawner.ShardLifetimeExpired += OnShardLifetimeExpired;

            for (int i = 0; i < _ports.Length; i++)
            {
                if (_ports[i] != null)
                {
                    _ports[i].InsertEvaluated += OnInsertEvaluated;
                }
            }

            _round.Start();
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

            if (_round != null)
            {
                _round.CorrectInserted -= OnCorrectInserted;
                _round.WrongInserted -= OnWrongInserted;
                _round.ShardExpired -= OnShardExpired;
                _round.Ended -= OnEnded;
            }
        }

        private void Update()
        {
            if (_round != null && _round.Phase == RoundPhase.Playing)
            {
                _round.Tick(Time.deltaTime);
            }
        }

        private void OnInsertEvaluated(PortSocket port, ShardView shard, InsertOutcome outcome)
        {
            if (_round.Phase != RoundPhase.Playing)
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

        private void OnEnded(RoundEndedEvent e)
        {
            _spawner.StopRespawns();
            Debug.Log($"[Round] {e.Result} — score {e.Score}, stars {e.Stars}, stabilization {e.Stabilization}, heat {e.Heat}");
        }

        private void OnCorrectInserted(CorrectInsertEvent e) =>
            Debug.Log($"[Round] correct — combo {e.Combo}, stabilization {e.Stabilization}{(e.IsMilestone ? " (milestone)" : string.Empty)}");

        private void OnWrongInserted(WrongInsertEvent e) =>
            Debug.Log($"[Round] wrong — heat {e.Heat}");

        // A shard's lifetime ran out (T11b): apply the rule (+heat, combo reset only if held), then consume it
        // through the same pool/respawn path as an accept. ApplyExpired also self-guards on Phase.
        private void OnShardLifetimeExpired(ShardView shard, bool wasHeld)
        {
            if (_round.Phase != RoundPhase.Playing)
            {
                return;
            }

            _round.ApplyExpired(wasHeld);
            _spawner.Despawn(shard);
        }

        private void OnShardExpired(ShardExpiredEvent e) =>
            Debug.Log($"[Round] expired — heat {e.Heat}, comboReset {e.ComboWasReset}");
    }
}
