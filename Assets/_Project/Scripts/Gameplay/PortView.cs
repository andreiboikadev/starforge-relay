using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Minimal placeholder visual for a reactor port (GDD §10): tints its renderer to the port colour via a
    /// cached <see cref="MaterialPropertyBlock"/> (no material instance). Driven by <see cref="PortSocket"/>.
    /// Real shape markers (circle/triangle/diamond) + glow are T18–19.
    /// </summary>
    public sealed class PortView : MonoBehaviour
    {
        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Renderer tinted to the port colour. Falls back to the first child Renderer if unset.")]
        [SerializeField] private Renderer _renderer;

        private MaterialPropertyBlock _propertyBlock;

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
            if (_renderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(s_baseColorId, ShardColorPalette.Resolve(color));
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
