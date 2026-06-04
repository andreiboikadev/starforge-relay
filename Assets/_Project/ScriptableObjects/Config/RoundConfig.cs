using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Designer-tunable round constants for Stabilize Run (GDD §12–13, guardrails §7). Treated as read-only
    /// at runtime: pure rules receive these values via constructor injection rather than referencing this
    /// asset directly, so the rules stay headless-testable. Runtime-mutable state (score, combo, heat,
    /// timer, progress) lives in the rule classes, never here.
    /// </summary>
    [CreateAssetMenu(menuName = "Starforge Relay/Round Config", fileName = "RoundConfig")]
    public sealed class RoundConfig : ScriptableObject
    {
        [Header("Round")]
        [SerializeField] private float _roundDurationSeconds = 90f;
        [SerializeField] private int _stabilizationRequirement = 20;
        [SerializeField] private int _heatCap = 8;

        [Header("Shards")]
        [SerializeField] private int _activeShardsDefault = 4;
        [SerializeField] private int _activeShardsMax = 6;
        [SerializeField] private int _maxShardsPerColor = 3;
        [SerializeField] private float _shardLifetimeStart = 14f;
        [SerializeField] private float _shardLifetimeLate = 12f;
        [SerializeField] private int _lateLifetimeAfterAccepts = 10;
        [SerializeField] private float _respawnDelayMin = 0.3f;
        [SerializeField] private float _respawnDelayMax = 0.8f;

        [Header("Combo")]
        [SerializeField] private int _comboBonusInterval = 5;
        [SerializeField] private int _comboBonusScore = 50;
        [SerializeField] private int _comboHeatRelief = 1;

        [Header("Scoring")]
        [SerializeField] private int _correctScore = 10;
        [SerializeField] private int _heatPenaltyPerHeat = 10;
        [SerializeField] private int _victoryTimeBonusPerSecond = 2;

        public float RoundDurationSeconds => _roundDurationSeconds;
        public int StabilizationRequirement => _stabilizationRequirement;
        public int HeatCap => _heatCap;

        public int ActiveShardsDefault => _activeShardsDefault;
        public int ActiveShardsMax => _activeShardsMax;
        public int MaxShardsPerColor => _maxShardsPerColor;
        public float ShardLifetimeStart => _shardLifetimeStart;
        public float ShardLifetimeLate => _shardLifetimeLate;
        public int LateLifetimeAfterAccepts => _lateLifetimeAfterAccepts;
        public float RespawnDelayMin => _respawnDelayMin;
        public float RespawnDelayMax => _respawnDelayMax;

        public int ComboBonusInterval => _comboBonusInterval;
        public int ComboBonusScore => _comboBonusScore;
        public int ComboHeatRelief => _comboHeatRelief;

        public int CorrectScore => _correctScore;
        public int HeatPenaltyPerHeat => _heatPenaltyPerHeat;
        public int VictoryTimeBonusPerSecond => _victoryTimeBonusPerSecond;
    }
}
