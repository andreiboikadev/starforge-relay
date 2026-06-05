using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// The reactor core (GDD §10 Star Core): the central visual anchor the player stabilises. T10 is a
    /// placeholder primitive that only exposes a <see cref="CoreAnchor"/> for beams / charge VFX to target later
    /// (T17); it holds no state. Core states (Dormant/Charging/Heating/Stabilized) and glow are T17/T19.
    /// </summary>
    public sealed class ReactorCoreView : MonoBehaviour
    {
        [Tooltip("Point that beams / charge VFX aim at (T17). Falls back to this transform if unset.")]
        [SerializeField] private Transform _coreAnchor;

        /// <summary>The point beams / VFX target (T17). Defaults to this transform.</summary>
        public Transform CoreAnchor => _coreAnchor != null ? _coreAnchor : transform;
    }
}
