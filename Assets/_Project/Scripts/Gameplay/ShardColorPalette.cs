using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// The display tint for each <see cref="ShardColor"/> (GDD §17 palette). Single source shared by shard, port,
    /// and VFX views so a Solar shard and a Solar port read as the same colour — essential for a colour-matching
    /// game. Colours are HDR (intensity above 1) so the Unlit / emissive materials bloom past the Global Volume
    /// threshold (T19); the exact intensity is a T21 device-tuning lever.
    /// </summary>
    public static class ShardColorPalette
    {
        // Intensity above 1 pushes the bright channels past the bloom threshold (1.0). The base hues stay
        // readable; T21 tunes the final value on-device.
        private const float Intensity = 2.2f;

        /// <summary>The HDR display tint for <paramref name="color"/>.</summary>
        public static Color Resolve(ShardColor color) => color switch
        {
            ShardColor.Solar => Hdr(1f, 0.78f, 0.2f),
            ShardColor.Ion => Hdr(0.2f, 0.8f, 1f),
            ShardColor.Pulse => Hdr(1f, 0.25f, 0.8f),
            _ => Color.white
        };

        private static Color Hdr(float r, float g, float b) => new Color(r * Intensity, g * Intensity, b * Intensity, 1f);
    }
}
