using StarforgeRelay.Gameplay;
using UnityEngine;

namespace StarforgeRelay.Vfx
{
    /// <summary>
    /// Scene adapter for the pooled, positional VFX (guardrails §6 — VFX is a peer of audio/haptics reacting to
    /// the same surfaced events; it plays no sound and touches no <c>AudioService</c>). Mirrors
    /// <c>FeedbackController</c>: the composition root injects the services/pools via <see cref="Initialize"/>;
    /// it subscribes there and unsubscribes in <see cref="OnDestroy"/>. It listens to the round loop's
    /// <b>spatial companion</b> events only — those fire from the loop's already-gated handlers, so there is no
    /// VFX while paused and no spurious spark from a leftover shard socketed after the round ends. The
    /// core-anchored visuals (state glow, combo pulse, victory/overload bursts) are <c>CoreStatePresenter</c>'s.
    /// </summary>
    public sealed class VfxController : MonoBehaviour
    {
        private RoundLoopController _roundLoop;
        private ReactorCoreView _core;
        private VfxPool<BeamVfx> _beamPool;
        private VfxPool<SparkVfx> _sparkPool;
        private VfxPool<FizzleVfx> _fizzlePool;

        /// <summary>Receive the round loop, core, and the pooled-effect pools from the composition root and subscribe.</summary>
        public void Initialize(
            RoundLoopController roundLoop,
            ReactorCoreView core,
            VfxPool<BeamVfx> beamPool,
            VfxPool<SparkVfx> sparkPool,
            VfxPool<FizzleVfx> fizzlePool)
        {
            if (roundLoop == null || core == null || beamPool == null || sparkPool == null || fizzlePool == null)
            {
                Debug.LogError("[VfxController] Missing a dependency — VFX disabled.", this);
                return;
            }

            _roundLoop = roundLoop;
            _core = core;
            _beamPool = beamPool;
            _sparkPool = sparkPool;
            _fizzlePool = fizzlePool;

            _roundLoop.CorrectInsertedAt += OnCorrectInsertedAt;
            _roundLoop.WrongInsertedAt += OnWrongInsertedAt;
            _roundLoop.ShardExpiredAt += OnShardExpiredAt;
        }

        private void OnDestroy()
        {
            if (_roundLoop != null)
            {
                _roundLoop.CorrectInsertedAt -= OnCorrectInsertedAt;
                _roundLoop.WrongInsertedAt -= OnWrongInsertedAt;
                _roundLoop.ShardExpiredAt -= OnShardExpiredAt;
            }
        }

        // Correct insert: a beam from the inserted port to the core, tinted to the port colour (GDD §17).
        private void OnCorrectInsertedAt(PortSocket port)
        {
            if (port == null)
            {
                return;
            }

            _beamPool.Get().Play(port.transform, _core.CoreAnchor, ShardColorPalette.Resolve(port.PortColor));
        }

        // Wrong insert: a spark at the rejected port (GDD §17). The dedicated port-material red flash is a T19
        // polish (it would touch PortView/PortSocket, outside this task's edit set); the spark marks the moment.
        private void OnWrongInsertedAt(PortSocket port)
        {
            if (port == null)
            {
                return;
            }

            _sparkPool.Get().Play(port.transform.position, ShardColorPalette.Resolve(port.PortColor));
        }

        // Expired shard: a fizzle at the shard's last position — read synchronously, before the loop pools it.
        private void OnShardExpiredAt(ShardView shard)
        {
            if (shard == null)
            {
                return;
            }

            _fizzlePool.Get().Play(shard.transform.position, ShardColorPalette.Resolve(shard.Color));
        }
    }
}
