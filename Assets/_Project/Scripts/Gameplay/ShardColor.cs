namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// The three shard / reactor-port colors (GDD §12). Shared color vocabulary used by shards, ports,
    /// port validation (T04), and shard-spawn planning (T05) — defined here as the foundation type.
    /// </summary>
    public enum ShardColor
    {
        Solar,
        Ion,
        Pulse
    }
}
