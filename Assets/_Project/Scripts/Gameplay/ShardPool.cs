using System;
using UnityEngine;
using UnityEngine.Pool;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Pools energy shards so a round never instantiates/destroys them mid-play (GDD §26; guardrails §12;
    /// ADR 0001). Infra adapter (not a pure rule): it wraps <see cref="ObjectPool{T}"/> and touches Unity
    /// objects, but is not a <c>MonoBehaviour</c>. The shard-creating step is injected as a
    /// <see cref="Func{TResult}"/> — production supplies one that instantiates the prefab, tests supply a fake —
    /// which keeps the pool headless-testable. Reset-on-release lives in <see cref="ShardView.ResetForPool"/>.
    /// </summary>
    public sealed class ShardPool : IDisposable
    {
        /// <summary>Pre-sized capacity (GDD max 6 active + buffer; guardrails §12).</summary>
        public const int DefaultCapacity = 8;

        /// <summary>Hard cap retained in the pool; releases beyond this are destroyed (guardrails §12).</summary>
        public const int MaxSize = 12;

        private readonly ObjectPool<ShardView> _pool;
        private readonly Transform _container;

        /// <param name="createShard">Factory for a new shard (prod: instantiate the prefab; tests: a fake). Required.</param>
        /// <param name="container">Optional parent for pooled shards; shards are reparented here on release.</param>
        /// <param name="defaultCapacity">Pre-sized capacity (default <see cref="DefaultCapacity"/>).</param>
        /// <param name="maxSize">Hard cap retained (default <see cref="MaxSize"/>).</param>
        /// <param name="collectionCheck">Throw on double-release; <c>null</c> → on in dev/editor only (guardrails §12).</param>
        public ShardPool(
            Func<ShardView> createShard,
            Transform container = null,
            int defaultCapacity = DefaultCapacity,
            int maxSize = MaxSize,
            bool? collectionCheck = null)
        {
            if (createShard == null)
            {
                throw new ArgumentNullException(nameof(createShard));
            }

            _container = container;
            bool checkCollections = collectionCheck ?? Debug.isDebugBuild;

            _pool = new ObjectPool<ShardView>(
                createShard,
                OnGet,
                OnRelease,
                OnDestroyShard,
                checkCollections,
                defaultCapacity,
                maxSize);
        }

        /// <summary>Shards currently rented out (not in the pool).</summary>
        public int CountActive => _pool.CountActive;

        /// <summary>Shards currently available in the pool.</summary>
        public int CountInactive => _pool.CountInactive;

        /// <summary>Rent a shard (activated). The caller assigns its colour via <see cref="ShardView.SetColor"/>.</summary>
        public ShardView Get() => _pool.Get();

        /// <summary>Return a shard to the pool (reset, reparented to the container, deactivated).</summary>
        public void Release(ShardView shard) => _pool.Release(shard);

        /// <summary>Pre-instantiate <paramref name="count"/> shards so the first spawns don't allocate (guardrails §12).</summary>
        public void Prewarm(int count)
        {
            if (count <= 0)
            {
                return;
            }

            var buffer = new ShardView[count];
            for (int i = 0; i < count; i++)
            {
                buffer[i] = _pool.Get();
            }

            for (int i = 0; i < count; i++)
            {
                _pool.Release(buffer[i]);
            }
        }

        /// <summary>Destroy all pooled shards (editor- and play-mode safe).</summary>
        public void Dispose() => _pool.Dispose();

        private static void OnGet(ShardView shard)
        {
            if (shard != null)
            {
                shard.gameObject.SetActive(true);
            }
        }

        private void OnRelease(ShardView shard)
        {
            if (shard == null)
            {
                return;
            }

            shard.ResetForPool();

            if (_container != null)
            {
                shard.transform.SetParent(_container, false);
            }

            shard.gameObject.SetActive(false);
        }

        private static void OnDestroyShard(ShardView shard)
        {
            if (shard == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(shard.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(shard.gameObject);
            }
        }
    }
}
