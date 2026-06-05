using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// A scene feeder pad (GDD §10): the visual marker and the spawn/return anchor for one shard. Identity and
    /// anchor only — <see cref="ShardSpawner"/> owns spawn planning and uses each pad's array index as its
    /// <see cref="FeederPadSlot"/> id (T05). Pad light-up / colour tint is later (T17).
    /// </summary>
    public sealed class FeederPadView : MonoBehaviour
    {
        [Tooltip("Where a shard rests and lerps back to. Falls back to this pad's own transform if unset.")]
        [SerializeField] private Transform _anchor;

        /// <summary>The world anchor a shard spawns at and returns to.</summary>
        public Transform Anchor => _anchor != null ? _anchor : transform;
    }
}
