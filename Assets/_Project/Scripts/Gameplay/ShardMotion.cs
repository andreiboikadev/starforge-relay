using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Drives a free shard's motion (GDD §10, §12): a gentle idle bob + spin while it rests on its home pad, and
    /// a scripted lerp back to that pad when it is released in empty space — no physics / ballistics
    /// (guardrails §13). The one XRI-touching shard adapter: it polls the shard's
    /// <see cref="XRBaseInteractable.isSelected"/> to know when an interactor (a hand <i>or</i> a holding socket)
    /// owns the transform and then stays out of the way (<see cref="ShardView"/> stays XRI-free). Poll-based — no
    /// event subscriptions, so there is no subscribe/unsubscribe lifecycle to leak across pooling. Its state
    /// (home, return timer) must be reset on pool reuse (T11).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class ShardMotion : MonoBehaviour
    {
        [Header("Idle")]
        [Tooltip("Vertical bob amplitude in metres while resting on the pad.")]
        [SerializeField] private float _bobAmplitude = 0.02f;

        [Tooltip("Bob cycles per second.")]
        [SerializeField] private float _bobFrequency = 0.8f;

        [Tooltip("Idle spin about world up, in degrees per second.")]
        [SerializeField] private float _spinSpeed = 30f;

        [Header("Return to pad (GDD §12)")]
        [Tooltip("Seconds a shard dropped in empty space waits before lerping home (~0.5-1.0 s).")]
        [SerializeField] private float _returnDelay = 0.7f;

        [Tooltip("Return lerp speed in metres per second.")]
        [SerializeField] private float _returnSpeed = 1.5f;

        [Tooltip("Grab/select state source. Falls back to the sibling XRGrabInteractable if unset.")]
        [SerializeField] private XRGrabInteractable _grabInteractable;

        private Transform _home;
        private bool _wasSelected;
        private bool _waiting;
        private bool _returning;
        private float _waitTimer;

        private void Awake()
        {
            if (_grabInteractable == null)
            {
                _grabInteractable = GetComponent<XRGrabInteractable>();
            }
        }

        /// <summary>Assign the pad anchor this shard rests at and returns to (called by the spawner on spawn).</summary>
        public void SetHome(Transform home)
        {
            _home = home;
            _wasSelected = false;
            _waiting = false;
            _returning = false;
        }

        /// <summary>
        /// Snap back to the home pad <b>immediately</b> — used by a wrong-insert eject. The shard sits inside
        /// the socket trigger, so it must clear the volume in a single frame or the socket's
        /// <c>keepSelectedTargetValid</c> re-snaps it (the T09 lesson) — hence an instant teleport, not the eased
        /// <see cref="_returning"/> lerp used for empty-space drops. Null-safe; call <b>after</b> the socket has
        /// released the shard.
        /// </summary>
        public void ReturnToPad()
        {
            if (_home == null)
            {
                return;
            }

            _wasSelected = false;
            _waiting = false;
            _returning = false;
            transform.position = _home.position;
        }

        /// <summary>True while a hand (or a holding socket) selects the shard. Read by the spawner (T11b) so it
        /// can slow the lifetime while grabbed (GDD §10) without itself referencing XRI.</summary>
        public bool IsHeld => _grabInteractable != null && _grabInteractable.isSelected;

        /// <summary>
        /// True while a <b>hand</b> (a non-socket interactor) selects the shard — selected, but not by an
        /// <see cref="XRSocketInteractor"/>. The spawner reads this (T16) to emit grab/release cues without
        /// itself referencing XRI; a socket snap therefore raises no grab cue.
        /// </summary>
        public bool IsHeldByHand =>
            _grabInteractable != null
            && _grabInteractable.isSelected
            && !(_grabInteractable.firstInteractorSelecting is XRSocketInteractor);

        /// <summary>
        /// Force the shard out of whatever interactor holds it, so it can be pooled safely — never pool a
        /// selected interactable (the T09/T11 hazard). No-op when nothing selects it.
        /// </summary>
        public void ForceRelease()
        {
            if (_grabInteractable != null
                && _grabInteractable.interactionManager != null
                && _grabInteractable.isSelected)
            {
                _grabInteractable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_grabInteractable);
            }
        }

        private void Update()
        {
            bool selected = _grabInteractable != null && _grabInteractable.isSelected;

            if (selected)
            {
                // A hand or a holding socket owns the transform; stay clear and drop any pending return.
                _wasSelected = true;
                _waiting = false;
                _returning = false;
                return;
            }

            if (_wasSelected)
            {
                // Just released into empty space: wait, then lerp home (GDD §12).
                _wasSelected = false;
                if (_home != null)
                {
                    _waiting = true;
                    _waitTimer = _returnDelay;
                }
            }

            if (_home == null)
            {
                return;
            }

            if (_waiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _waiting = false;
                    _returning = true;
                }

                return;
            }

            if (_returning)
            {
                transform.position = Vector3.MoveTowards(transform.position, _home.position, _returnSpeed * Time.deltaTime);
                if (transform.position == _home.position)
                {
                    _returning = false;
                }

                return;
            }

            // Resting on the pad: gentle bob + spin.
            float bob = Mathf.Sin(Time.time * _bobFrequency * 2f * Mathf.PI) * _bobAmplitude;
            transform.position = _home.position + new Vector3(0f, bob, 0f);
            transform.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
