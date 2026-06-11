using System;
using System.Collections;
using UnityEngine;

namespace StarforgeRelay.Vfx
{
    /// <summary>
    /// Base for a pooled, short-lived VFX (guardrails §11/§12; GDD §26 — bursts &lt; 0.5–1.0 s): a subclass
    /// plays its visual in its own <c>Play(...)</c>, then this base returns the instance to its
    /// <see cref="VfxPool{T}"/> after the duration via the <see cref="ReturnToPool"/> callback the pool binds on
    /// Get. The return coroutine is view-owned and cancelled on disable / pool reset, so a recycled instance
    /// never carries a stale timer (no double-release). Not a gameplay object: it references no XRI type and no
    /// rules.
    /// </summary>
    public abstract class PooledVfx : MonoBehaviour
    {
        [Tooltip("How long the effect plays before returning to the pool (GDD §26: < 0.5–1.0 s).")]
        [SerializeField] private float _durationSeconds = 0.5f;

        private WaitForSeconds _wait;
        private Coroutine _returnRoutine;

        /// <summary>Returns this instance to its pool. <see cref="VfxPool{T}"/> binds it once on Get.</summary>
        public Action ReturnToPool { get; set; }

        /// <summary>Effect lifetime in seconds (GDD §26 — a T21 tuning lever).</summary>
        protected float DurationSeconds => _durationSeconds;

        /// <summary>Start the auto-return timer. Call at the end of a subclass <c>Play(...)</c>.</summary>
        protected void ScheduleReturn()
        {
            CancelReturn();
            _wait ??= new WaitForSeconds(_durationSeconds);
            if (isActiveAndEnabled)
            {
                _returnRoutine = StartCoroutine(ReturnAfter());
            }
        }

        /// <summary>Reset for pool reuse (guardrails §12): cancel the in-flight return, then subclass cleanup.</summary>
        public void ResetForPool()
        {
            CancelReturn();
            OnResetForPool();
        }

        /// <summary>Subclass cleanup on release (clear tint / particles / transform). Base already cancels the timer.</summary>
        protected virtual void OnResetForPool()
        {
        }

        private IEnumerator ReturnAfter()
        {
            yield return _wait;
            _returnRoutine = null;
            ReturnToPool?.Invoke();
        }

        private void CancelReturn()
        {
            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }
        }

        private void OnDisable() => CancelReturn();
    }
}
