using System.Collections;
using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Visual for a reactor port (GDD §10): tints its renderer to the port colour via a cached
    /// <see cref="MaterialPropertyBlock"/> (no material instance), and plays a brief red flash on a wrong insert
    /// (GDD §17). Driven by <see cref="PortSocket"/>; the wrong-insert flash is invoked from the gated VFX path
    /// (<c>VfxController</c>) so it never fires after the round ends or while paused. Shape markers live as child
    /// meshes on the port.
    /// </summary>
    public sealed class PortView : MonoBehaviour
    {
        private const float FlashDuration = 0.25f;

        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");
        // Hot red wrong-insert flash (HDR so it blooms), GDD §17.
        private static readonly Color s_flashColor = new Color(2.4f, 0.18f, 0.12f, 1f);

        [Tooltip("Renderer tinted to the port colour. Falls back to the first child Renderer if unset.")]
        [SerializeField] private Renderer _renderer;

        private MaterialPropertyBlock _propertyBlock;
        private Color _steadyColor = Color.white;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }
        }

        /// <summary>Tint the port to <paramref name="color"/>'s shared palette colour (single-sourced with shards).</summary>
        public void SetColor(ShardColor color)
        {
            _steadyColor = ShardColorPalette.Resolve(color);
            Apply(_steadyColor);
        }

        /// <summary>
        /// Brief red flash on a wrong insert (GDD §17), then lerp back to the steady palette tint. Scaled time, so
        /// it freezes while paused; a re-trigger restarts cleanly. Restores to the cached palette colour set in
        /// <see cref="SetColor"/> — never a read-back of the property block.
        /// </summary>
        public void FlashWrongInsert()
        {
            if (_renderer == null || !isActiveAndEnabled)
            {
                return;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            Apply(s_flashColor);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / FlashDuration; // Scaled — freezes at timeScale 0 (pause).
                Apply(Color.Lerp(s_flashColor, _steadyColor, t));
                yield return null;
            }

            Apply(_steadyColor);
            _flashRoutine = null;
        }

        private void Apply(Color color)
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
    }
}
