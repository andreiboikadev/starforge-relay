using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Adapter for a single grabbable energy shard (GDD §10). Owns the shard's colour identity and tint, and
    /// restores a clean state for pooling (guardrails §6 Shard Views, §12). It deliberately does <b>not</b>
    /// reference XRI: grabbing is provided by the prefab's <c>XRGrabInteractable</c> component plus the rig's
    /// Near-Far interactor, so the runtime assembly stays XR-free (T08). Round rules (score/heat/spawn) live
    /// elsewhere. Lifetime, lerp-back and grab-state tracking arrive with the spawner (T10).
    /// </summary>
    public sealed class ShardView : MonoBehaviour
    {
        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Renderer tinted to the shard colour. Falls back to the first child Renderer if unset.")]
        [SerializeField] private Renderer _renderer;

        [Tooltip("Grab/socket collider, re-enabled on pool reuse. Falls back to the first child Collider if unset.")]
        [SerializeField] private Collider _collider;

        private MaterialPropertyBlock _propertyBlock;

        /// <summary>The shard's colour identity (GDD §12), assigned by the spawner/pool via <see cref="SetColor"/>.</summary>
        public ShardColor Color { get; private set; }

        private void Awake()
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }

            if (_collider == null)
            {
                _collider = GetComponentInChildren<Collider>();
            }
        }

        /// <summary>Assign the shard's colour and tint its base colour. Cheap — safe to call on every spawn.</summary>
        public void SetColor(ShardColor color)
        {
            Color = color;
            ApplyTint(ResolveColor(color));
        }

        /// <summary>
        /// Restore a clean state for pool reuse (guardrails §12): re-enable the collider a socket (T09) may
        /// have disabled. Grab-interactable / lifetime / grab-state resets are added when those mechanics exist.
        /// </summary>
        public void ResetForPool()
        {
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }

        private void ApplyTint(UnityEngine.Color color)
        {
            if (_renderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(s_baseColorId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        // Primitive placeholder palette (GDD §17 colours). Real glow/emission/halos are T19.
        private static UnityEngine.Color ResolveColor(ShardColor color) => color switch
        {
            ShardColor.Solar => new UnityEngine.Color(1f, 0.78f, 0.2f),
            ShardColor.Ion => new UnityEngine.Color(0.2f, 0.8f, 1f),
            ShardColor.Pulse => new UnityEngine.Color(1f, 0.25f, 0.8f),
            _ => UnityEngine.Color.white
        };
    }
}
