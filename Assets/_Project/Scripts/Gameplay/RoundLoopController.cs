using System.Collections;
using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Scene adapter that closes the Stabilize Run loop (GDD §5, §12): it builds the pure
    /// <see cref="RoundController"/> (T06) from <see cref="RoundConfig"/>, routes each port's
    /// <see cref="PortSocket.InsertEvaluated"/> outcome (T09) into the round rules, ticks the round clock,
    /// consumes an accepted shard through the spawner (T10), and surfaces the round events for HUD/audio/VFX
    /// (T14/T16/T17). Thin adapter: it owns no formulas (those are the tested T01–T06 rules) and references no
    /// XRI type (it talks to <see cref="PortSocket"/> / <see cref="ShardSpawner"/>, not the socket). Service
    /// composition moves to the composition root at T12.
    /// </summary>
    public sealed class RoundLoopController : MonoBehaviour
    {
        [Tooltip("Round constants (read-only at runtime).")]
        [SerializeField] private RoundConfig _config;

        [Tooltip("The reactor ports whose insert outcomes drive the round.")]
        [SerializeField] private PortSocket[] _ports;

        [Tooltip("Spawner that fills the pads and consumes/respawns shards (T10).")]
        [SerializeField] private ShardSpawner _spawner;

        private RoundController _round;

        private void Start()
        {
            if (_config == null || _ports == null || _ports.Length == 0 || _spawner == null)
            {
                Debug.LogError("[RoundLoopController] Missing config, ports, or spawner — round not started.", this);
                return;
            }

            _round = new RoundController(
                new ScoreService(_config.CorrectScore, _config.ComboBonusScore, _config.VictoryTimeBonusPerSecond, _config.HeatPenaltyPerHeat),
                new ComboTracker(_config.ComboBonusInterval),
                new HeatService(_config.HeatCap, _config.ComboHeatRelief),
                new StabilizationProgress(_config.StabilizationRequirement),
                new RoundTimer(_config.RoundDurationSeconds));

            _round.CorrectInserted += OnCorrectInserted;
            _round.WrongInserted += OnWrongInserted;
            _round.Ended += OnEnded;

            for (int i = 0; i < _ports.Length; i++)
            {
                if (_ports[i] != null)
                {
                    _ports[i].InsertEvaluated += OnInsertEvaluated;
                }
            }

            _round.Start();
            // The spawner fills the pads from its own Start (T10); the round loop only consumes/respawns from here.
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

            if (_round != null)
            {
                _round.CorrectInserted -= OnCorrectInserted;
                _round.WrongInserted -= OnWrongInserted;
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
    }
}
