using System;
using System.Collections.Generic;
using NUnit.Framework;
using StarforgeRelay.Gameplay;
using UnityEngine;

namespace StarforgeRelay.Tests.EditMode
{
    /// <summary>
    /// Headless EditMode coverage for <see cref="ShardPool"/>'s wiring (not Unity's <c>ObjectPool</c>): the
    /// create-func is a fake that news a bare <see cref="ShardView"/>, so no prefab or XR runtime is needed.
    /// Grab feel is verified separately by the XR Device Simulator / Quest smoke (T08 brief).
    /// </summary>
    public sealed class ShardPoolTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            _created.Clear();
        }

        private ShardView CreateShard()
        {
            var go = new GameObject("TestShard");
            _created.Add(go);
            return go.AddComponent<ShardView>();
        }

        private ShardPool NewPool(int defaultCapacity = 2, int maxSize = 4)
        {
            return new ShardPool(CreateShard, null, defaultCapacity, maxSize, collectionCheck: true);
        }

        [Test]
        public void Get_Returns_An_Active_Shard()
        {
            ShardPool pool = NewPool();

            ShardView shard = pool.Get();

            Assert.IsNotNull(shard);
            Assert.IsTrue(shard.gameObject.activeSelf);
            Assert.AreEqual(1, pool.CountActive);
        }

        [Test]
        public void Release_Deactivates_And_Returns_To_Pool()
        {
            ShardPool pool = NewPool();
            ShardView shard = pool.Get();

            pool.Release(shard);

            Assert.IsFalse(shard.gameObject.activeSelf);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);
        }

        [Test]
        public void Get_Reuses_A_Released_Shard()
        {
            ShardPool pool = NewPool();
            ShardView first = pool.Get();
            pool.Release(first);

            ShardView second = pool.Get();

            Assert.AreSame(first, second, "the pool must reuse a released shard, not allocate a new one");
        }

        [Test]
        public void Prewarm_Fills_The_Pool_Without_Active_Shards()
        {
            ShardPool pool = NewPool();

            pool.Prewarm(2);

            Assert.AreEqual(2, pool.CountInactive);
            Assert.AreEqual(0, pool.CountActive);
        }

        [Test]
        public void SetColor_Stores_The_Color()
        {
            ShardPool pool = NewPool();
            ShardView shard = pool.Get();

            shard.SetColor(ShardColor.Ion);

            Assert.AreEqual(ShardColor.Ion, shard.Color);
        }

        [Test]
        public void Null_Factory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ShardPool(null));
        }

        [Test]
        public void Double_Release_Throws_When_Collection_Checked()
        {
            ShardPool pool = NewPool();
            ShardView shard = pool.Get();
            pool.Release(shard);

            Assert.Throws<InvalidOperationException>(() => pool.Release(shard));
        }
    }
}
