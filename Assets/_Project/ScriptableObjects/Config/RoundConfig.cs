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
        [SerializeField] private float roundDurationSeconds = 90f;
        [SerializeField] private int stabilizationRequirement = 20;
        [SerializeField] private int heatCap = 8;

        [Header("Shards")]
        [SerializeField] private int activeShardsDefault = 4;
        [SerializeField] private int activeShardsMax = 6;
        [SerializeField] private float shardLifetimeStart = 14f;
        [SerializeField] private float shardLifetimeLate = 12f;
        [SerializeField] private int lateLifetimeAfterAccepts = 10;
        [SerializeField] private float respawnDelayMin = 0.3f;
        [SerializeField] private float respawnDelayMax = 0.8f;

        [Header("Combo")]
        [SerializeField] private int comboBonusInterval = 5;
        [SerializeField] private int comboBonusScore = 50;
        [SerializeField] private int comboHeatRelief = 1;

        [Header("Scoring")]
        [SerializeField] private int correctScore = 10;
        [SerializeField] private int heatPenaltyPerHeat = 10;
        [SerializeField] private int victoryTimeBonusPerSecond = 2;

        public float RoundDurationSeconds => roundDurationSeconds;
        public int StabilizationRequirement => stabilizationRequirement;
        public int HeatCap => heatCap;

        public int ActiveShardsDefault => activeShardsDefault;
        public int ActiveShardsMax => activeShardsMax;
        public float ShardLifetimeStart => shardLifetimeStart;
        public float ShardLifetimeLate => shardLifetimeLate;
        public int LateLifetimeAfterAccepts => lateLifetimeAfterAccepts;
        public float RespawnDelayMin => respawnDelayMin;
        public float RespawnDelayMax => respawnDelayMax;

        public int ComboBonusInterval => comboBonusInterval;
        public int ComboBonusScore => comboBonusScore;
        public int ComboHeatRelief => comboHeatRelief;

        public int CorrectScore => correctScore;
        public int HeatPenaltyPerHeat => heatPenaltyPerHeat;
        public int VictoryTimeBonusPerSecond => victoryTimeBonusPerSecond;
    }
}
