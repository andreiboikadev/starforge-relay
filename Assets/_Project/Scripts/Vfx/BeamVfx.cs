using UnityEngine;

namespace StarforgeRelay.Vfx
{
    /// <summary>
    /// Pooled correct-insert beam (GDD §17 — "short beam from port to core"): a primitive emissive bar stretched
    /// and oriented from the inserted port to the core anchor, tinted to the port colour, that auto-returns after
    /// its duration. Placeholder visual (a unit-cube/quad scaled along local Z) — final glow is T19. Owns only
    /// its own transform + renderer; no gameplay refs.
    /// </summary>
    public sealed class BeamVfx : PooledVfx
    {
        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Renderer stretched between port and core. Falls back to the first child Renderer if unset.")]
        [SerializeField] private Renderer _renderer;

        [Tooltip("Beam cross-section thickness in metres (placeholder).")]
        [SerializeField] private float _thickness = 0.03f;

        private MaterialPropertyBlock _propertyBlock;

        /// <summary>Stretch + orient the beam from <paramref name="from"/> to <paramref name="to"/>, tint it, and play.</summary>
        public void Play(Transform from, Transform to, Color tint)
        {
            if (from == null || to == null)
            {
                return;
            }

            Vector3 a = from.position;
            Vector3 b = to.position;
            Vector3 delta = b - a;
            float length = delta.magnitude;

            transform.position = (a + b) * 0.5f;
            if (length > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(delta);
            }

            transform.localScale = new Vector3(_thickness, _thickness, length);

            ApplyTint(tint);
            ScheduleReturn();
        }

        protected override void OnResetForPool()
        {
            transform.localScale = Vector3.one;
        }

        private void ApplyTint(Color tint)
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }

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
