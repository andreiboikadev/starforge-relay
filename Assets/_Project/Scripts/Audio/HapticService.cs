using System;
using StarforgeRelay.Persistence;
using UnityEngine.XR.Interaction.Toolkit.Feedback;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace StarforgeRelay.Audio
{
    /// <summary>
    /// Plays semantic <see cref="FeedbackCue"/>s as controller haptic impulses (GDD §12) and gates the rig's
    /// built-in select/hover haptics by the Haptics on/off setting (GDD §16/§27). One of the project's
    /// XRI-touching boundaries (guardrails §6) — not a <c>MonoBehaviour</c>. It pulses both controllers'
    /// <see cref="HapticImpulsePlayer"/>s and enables/disables the injected <see cref="SimpleHapticFeedback"/>
    /// components. Dispose to unsubscribe; the composition root owns its lifetime.
    /// </summary>
    public sealed class HapticService : IDisposable
    {
        private readonly HapticConfig _config;
        private readonly HapticImpulsePlayer _left;
        private readonly HapticImpulsePlayer _right;
        private readonly SimpleHapticFeedback[] _rigSelectHaptics;
        private readonly SettingsService _settings;

        /// <summary>Build over the config, both controllers' players, the rig select-haptic components, and settings.</summary>
        public HapticService(
            HapticConfig config,
            HapticImpulsePlayer left,
            HapticImpulsePlayer right,
            SimpleHapticFeedback[] rigSelectHaptics,
            SettingsService settings)
        {
            _config = config;
            _left = left;
            _right = right;
            _rigSelectHaptics = rigSelectHaptics;
            _settings = settings;

            ApplyGate(_settings.Current);
            _settings.Changed += OnSettingsChanged;
        }

        /// <summary>Pulse both controllers for <paramref name="cue"/>; a no-op when haptics are off or the cue is unmapped.</summary>
        public void Play(FeedbackCue cue)
        {
            if (!_settings.Current.HapticsEnabled || _config == null)
            {
                return;
            }

            if (!_config.TryGet(cue, out float amplitude, out float duration, out float frequency))
            {
                return;
            }

            if (_left != null)
            {
                _left.SendHapticImpulse(amplitude, duration, frequency);
            }

            if (_right != null)
            {
                _right.SendHapticImpulse(amplitude, duration, frequency);
            }
        }

        /// <summary>Unsubscribe from settings changes (the composition root calls this on teardown).</summary>
        public void Dispose()
        {
            _settings.Changed -= OnSettingsChanged;
        }

        private void OnSettingsChanged(GameSettings settings) => ApplyGate(settings);

        // Gate the rig's built-in select/hover haptics centrally: disabling a SimpleHapticFeedback component
        // unsubscribes it from its interactor's select events (its OnDisable), so "Haptics Off" is truly silent.
        private void ApplyGate(GameSettings settings)
        {
            if (_rigSelectHaptics == null)
            {
                return;
            }

            for (int i = 0; i < _rigSelectHaptics.Length; i++)
            {
                if (_rigSelectHaptics[i] != null)
                {
                    _rigSelectHaptics[i].enabled = settings.HapticsEnabled;
                }
            }
        }
    }
}
