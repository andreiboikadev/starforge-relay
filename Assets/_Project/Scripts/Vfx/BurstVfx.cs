using UnityEngine;

namespace StarforgeRelay.Vfx
{
    /// <summary>
    /// Shared base for a pooled point-burst VFX: appears at a world position, tinted, plays its optional particle
    /// system (and/or emissive renderer) for the duration, then auto-returns. <see cref="SparkVfx"/> (wrong
    /// insert) and <see cref="FizzleVfx"/> (expired shard) are the concrete types — one pool + prefab each; they
    /// diverge visually at T19. Placeholder now. References no gameplay/XRI type.
    /// </summary>
    public abstract class BurstVfx : PooledVfx
    {
        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Optional particle system played on Play(). Falls back to a child ParticleSystem if unset.")]
        [SerializeField] private ParticleSystem _particles;

        [Tooltip("Optional renderer tinted to the effect colour. Falls back to a child Renderer if unset.")]
        [SerializeField] private Renderer _renderer;

        private MaterialPropertyBlock _propertyBlock;
        private bool _resolved;

        /// <summary>Place the burst at <paramref name="position"/>, tint it, and play for the duration.</summary>
        public void Play(Vector3 position, Color tint)
        {
            Resolve();
            transform.position = position;
            ApplyTint(tint);

            if (_particles != null)
            {
                _particles.Clear(true);
                _particles.Play(true);
            }

            ScheduleReturn();
        }

        protected override void OnResetForPool()
        {
            if (_particles != null)
            {
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Resolve()
        {
            if (_resolved)
            {
                return;
            }

            if (_particles == null)
            {
                _particles = GetComponentInChildren<ParticleSystem>();
            }

            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }

            _resolved = true;
        }

        private void ApplyTint(Color tint)
        {
            if (_renderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(s_baseColorId, tint);
            _propertyBlock.SetColor(s_emissionColorId, tint);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
