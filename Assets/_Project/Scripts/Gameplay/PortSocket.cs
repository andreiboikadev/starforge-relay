using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Adapter wrapping an <see cref="XRSocketInteractor"/> as a reactor port (GDD §10). The socket accepts
    /// <b>any</b> shard (a shared interaction layer, NOT a colour-gated filter — guardrails §6 critical note);
    /// on select it validates colour in code via <see cref="PortValidationService"/> (T04): a match is a correct
    /// insert (accepted), a mismatch is force-ejected. Emits only the port-level <see cref="InsertOutcome"/> —
    /// scoring/heat/combo and the rich CorrectInsert/WrongInsert events are RoundController's (wired T11); beams
    /// and consuming the shard to the pool are T11/T17. The wrong-insert return-to-pad is wired here (T10) via
    /// <see cref="ShardMotion"/>.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class PortSocket : MonoBehaviour
    {
        [Tooltip("The colour this port accepts (GDD §12): a shard of this colour inserts correctly, any other is ejected.")]
        [SerializeField] private ShardColor _portColor;

        [Tooltip("Socket interactor for this port. Falls back to GetComponent if unset.")]
        [SerializeField] private XRSocketInteractor _socket;

        [Tooltip("Optional view tinted to the port colour. Falls back to a child PortView if unset.")]
        [SerializeField] private PortView _portView;

        private readonly PortValidationService _validation = new PortValidationService();

        /// <summary>The colour this port accepts.</summary>
        public ShardColor PortColor => _portColor;

        /// <summary>Raised after a shard is socketed and validated. Outcome is Correct or Wrong (never NoPenalty).</summary>
        public event Action<PortSocket, ShardView, InsertOutcome> InsertEvaluated;

        /// <summary>
        /// Force-release the shard this socket currently holds, so the round loop (T11) can consume a correct
        /// insert — never pool a socket-held shard (T09). Call from outside the select callback (the consumer
        /// defers a frame); the <c>SelectExit</c> runs synchronously here. No-op when nothing is socketed.
        /// </summary>
        public void ReleaseSelected()
        {
            if (_socket == null || _socket.interactionManager == null || !_socket.hasSelection)
            {
                return;
            }

            IXRSelectInteractable interactable = _socket.firstInteractableSelected;
            if (interactable != null && _socket.IsSelecting(interactable))
            {
                _socket.interactionManager.SelectExit((IXRSelectInteractor)_socket, interactable);
            }
        }

        private void Awake()
        {
            if (_socket == null)
            {
                _socket = GetComponent<XRSocketInteractor>();
            }

            if (_portView == null)
            {
                _portView = GetComponentInChildren<PortView>();
            }
        }

        private void Start()
        {
            if (_portView != null)
            {
                _portView.SetColor(_portColor);
            }
        }

        private void OnEnable()
        {
            if (_socket != null)
            {
                _socket.selectEntered.AddListener(OnSocketSelectEntered);
            }
        }

        private void OnDisable()
        {
            if (_socket != null)
            {
                _socket.selectEntered.RemoveListener(OnSocketSelectEntered);
            }
        }

        private void OnSocketSelectEntered(SelectEnterEventArgs args)
        {
            ShardView shard = args.interactableObject.transform.GetComponent<ShardView>();
            if (shard == null)
            {
                return;
            }

            InsertOutcome outcome = _validation.Validate(shard.Color, _portColor);

            if (outcome == InsertOutcome.Wrong)
            {
                StartCoroutine(EjectRoutine(args.interactableObject, shard));
            }

            InsertEvaluated?.Invoke(this, shard, outcome);
        }

        // Reject a wrong-colour shard. We defer one frame (force-exiting from inside the select event would be
        // re-entrant), then force the socket to release via the current (non-obsolete) SelectExit overload —
        // socketActive alone doesn't reliably drop an already-held target while keepSelectedTargetValid is on.
        // Once released, hand off to ShardMotion to lerp the shard back to its feeder pad (T10) — this replaces
        // the T09 placeholder shove; recycleDelayTime guards against an immediate re-select.
        private IEnumerator EjectRoutine(IXRSelectInteractable interactable, ShardView shard)
        {
            yield return null;

            if (_socket.interactionManager != null && _socket.IsSelecting(interactable))
            {
                _socket.interactionManager.SelectExit((IXRSelectInteractor)_socket, interactable);
            }

            // After SelectExit (never before — the socket would re-snap it), send the shard home.
            if (shard != null)
            {
                shard.GetComponent<ShardMotion>()?.ReturnToPad();
            }
        }
    }
}
