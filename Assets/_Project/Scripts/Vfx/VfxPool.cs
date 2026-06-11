using System;
using UnityEngine;
using UnityEngine.Pool;

namespace StarforgeRelay.Vfx
{
    /// <summary>
    /// Pools a short-lived VFX type so a round never instantiates/destroys effects mid-play (guardrails §10/§12;
    /// ADR 0001). Mirrors <c>ShardPool</c>: wraps <see cref="ObjectPool{T}"/>, the creating step is injected as a
    /// <see cref="Func{TResult}"/> (production instantiates a prefab; a test can fake one), and reset-on-release
    /// is delegated to <see cref="PooledVfx.ResetForPool"/>. On Get it binds the instance's
    /// <see cref="PooledVfx.ReturnToPool"/> (once per instance) so the effect returns itself when its duration
    /// elapses. Infra adapter — not a <c>MonoBehaviour</c>; the composition root owns and disposes it.
    /// </summary>
    public sealed class VfxPool<T> : IDisposable where T : PooledVfx
    {
        /// <summary>Default pre-sized capacity (guardrails §12: 6–10 per frequently used effect).</summary>
        public const int DefaultCapacity = 6;

        /// <summary>Hard cap retained in the pool; releases beyond this are destroyed (guardrails §12).</summary>
        public const int MaxSize = 12;

        private readonly ObjectPool<T> _pool;
        private readonly Transform _container;

        /// <param name="create">Factory for a new effect (prod: instantiate the prefab; tests: a fake). Required.</param>
        /// <param name="container">Optional parent for pooled effects; effects are reparented here on release.</param>
        /// <param name="defaultCapacity">Pre-sized capacity (default <see cref="DefaultCapacity"/>).</param>
        /// <param name="maxSize">Hard cap retained (default <see cref="MaxSize"/>).</param>
        /// <param name="collectionCheck">Throw on double-release; <c>null</c> → on in dev/editor only (guardrails §12).</param>
        public VfxPool(
            Func<T> create,
            Transform container = null,
            int defaultCapacity = DefaultCapacity,
            int maxSize = MaxSize,
            bool? collectionCheck = null)
        {
            if (create == null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            _container = container;
            bool checkCollections = collectionCheck ?? Debug.isDebugBuild;

            _pool = new ObjectPool<T>(
                create,
                OnGet,
                OnRelease,
                OnDestroyVfx,
                checkCollections,
                defaultCapacity,
                maxSize);
        }

        /// <summary>Effects currently rented out (playing).</summary>
        public int CountActive => _pool.CountActive;

        /// <summary>Effects currently available in the pool.</summary>
        public int CountInactive => _pool.CountInactive;

        /// <summary>Rent an effect — activated and bound to auto-return to this pool. Call <c>Play(...)</c> on it.</summary>
        public T Get() => _pool.Get();

        /// <summary>Return an effect to the pool (reset, reparented, deactivated).</summary>
        public void Release(T vfx) => _pool.Release(vfx);

        /// <summary>Pre-instantiate <paramref name="count"/> effects so the first plays don't allocate (guardrails §12).</summary>
        public void Prewarm(int count)
        {
            if (count <= 0)
            {
                return;
            }

            var buffer = new T[count];
            for (int i = 0; i < count; i++)
            {
                buffer[i] = _pool.Get();
            }

            for (int i = 0; i < count; i++)
            {
                _pool.Release(buffer[i]);
            }
        }

        /// <summary>Destroy all pooled effects (editor- and play-mode safe).</summary>
        public void Dispose() => _pool.Dispose();

        private void OnGet(T vfx)
        {
            if (vfx == null)
            {
                return;
            }

            // Bind once per instance — the callback survives release, so re-Get does not re-allocate it.
            vfx.ReturnToPool ??= () => _pool.Release(vfx);
            vfx.gameObject.SetActive(true);
        }

        private void OnRelease(T vfx)
        {
            if (vfx == null)
            {
                return;
            }

            vfx.ResetForPool();

            if (_container != null)
            {
                vfx.transform.SetParent(_container, false);
            }

            vfx.gameObject.SetActive(false);
        }

        private static void OnDestroyVfx(T vfx)
        {
            if (vfx == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(vfx.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(vfx.gameObject);
            }
        }
    }
}
