using System;
using StarforgeRelay.Persistence;
using UnityEngine;

namespace StarforgeRelay.Audio
{
    /// <summary>
    /// Plays semantic <see cref="FeedbackCue"/>s as one-shot audio (guardrails §6) and owns the central Sound
    /// on/off gate. Infra adapter like <c>ShardPool</c>: it touches <c>UnityEngine</c> audio APIs but is not a
    /// <c>MonoBehaviour</c>. The single writer of <see cref="AudioListener.volume"/> — it mutes/unmutes from
    /// <see cref="SettingsService"/> at construction and on <see cref="SettingsService.Changed"/>, so no
    /// per-source check is needed (GDD §16/§27). Dispose to unsubscribe; the composition root owns its lifetime.
    /// </summary>
    public sealed class AudioService : IDisposable
    {
        private readonly AudioCueConfig _config;
        private readonly AudioSource _source;
        private readonly SettingsService _settings;

        /// <summary>Build over the cue config, a 2D one-shot source, and the settings service; applies the gate now.</summary>
        public AudioService(AudioCueConfig config, AudioSource source, SettingsService settings)
        {
            _config = config;
            _source = source;
            _settings = settings;

            ApplyGate(_settings.Current);
            _settings.Changed += OnSettingsChanged;
        }

        /// <summary>Play the clip mapped to <paramref name="cue"/>; a no-op when the cue is unmapped or clipless.</summary>
        public void Play(FeedbackCue cue)
        {
            if (_source != null && _config != null && _config.TryGet(cue, out AudioClip clip, out float volume))
            {
                _source.PlayOneShot(clip, volume);
            }
        }

        /// <summary>Unsubscribe from settings changes (the composition root calls this on teardown).</summary>
        public void Dispose()
        {
            _settings.Changed -= OnSettingsChanged;
        }

        private void OnSettingsChanged(GameSettings settings) => ApplyGate(settings);

        // The one place that writes AudioListener.volume — the central Sound on/off gate (decision 2).
        private static void ApplyGate(GameSettings settings)
        {
            AudioListener.volume = settings.SoundEnabled ? 1f : 0f;
        }
    }
}
