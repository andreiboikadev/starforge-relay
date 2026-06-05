using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// The display tint for each <see cref="ShardColor"/> (GDD §17 palette). Single source shared by shard and
    /// port views so a Solar shard and a Solar port read as the same colour — essential for a colour-matching
    /// game. Placeholder primitive colours; real glow/materials are T19.
    /// </summary>
    public static class ShardColorPalette
    {
        /// <summary>The placeholder tint for <paramref name="color"/>.</summary>
        public static Color Resolve(ShardColor color) => color switch
        {
            ShardColor.Solar => new Color(1f, 0.78f, 0.2f),
            ShardColor.Ion => new Color(0.2f, 0.8f, 1f),
            ShardColor.Pulse => new Color(1f, 0.25f, 0.8f),
            _ => Color.white
        };
    }
}
