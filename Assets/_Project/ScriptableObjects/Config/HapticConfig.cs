using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarforgeRelay.Audio
{
    /// <summary>
    /// Designer config mapping a <see cref="FeedbackCue"/> to a controller haptic impulse — amplitude,
    /// duration, and frequency (guardrails §6/§7). Read-only at runtime. A cue with no entry plays no haptic,
    /// so only cues that should buzz need a row (at minimum <see cref="FeedbackCue.CorrectInsert"/>, GDD §12).
    /// </summary>
    [CreateAssetMenu(fileName = "HapticConfig", menuName = "Starforge Relay/Haptic Config")]
    public sealed class HapticConfig : ScriptableObject
    {
        [Tooltip("One row per cue that should buzz. Unlisted cues send no impulse.")]
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        private Dictionary<FeedbackCue, Entry> _lookup;

        /// <summary>
        /// Resolve the impulse for <paramref name="cue"/>. Returns <see langword="false"/> when the cue is
        /// unmapped — the caller then sends no haptic.
        /// </summary>
        public bool TryGet(FeedbackCue cue, out float amplitude, out float duration, out float frequency)
        {
            _lookup ??= BuildLookup();
            if (_lookup.TryGetValue(cue, out Entry entry))
            {
                amplitude = entry.Amplitude;
                duration = entry.Duration;
                frequency = entry.Frequency;
                return true;
            }

            amplitude = 0f;
            duration = 0f;
            frequency = 0f;
            return false;
        }

        private Dictionary<FeedbackCue, Entry> BuildLookup()
        {
            var lookup = new Dictionary<FeedbackCue, Entry>(_entries.Length);
            for (int i = 0; i < _entries.Length; i++)
            {
                lookup[_entries[i].Cue] = _entries[i];
            }

            return lookup;
        }

        /// <summary>One cue → impulse mapping row.</summary>
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private FeedbackCue _cue;

            [Tooltip("Motor amplitude in the [0..1] range.")]
            [SerializeField, Range(0f, 1f)] private float _amplitude = 0.5f;

            [Tooltip("Impulse duration in seconds (GDD §12: a short pulse).")]
            [SerializeField] private float _duration = 0.1f;

            [Tooltip("Impulse frequency in Hz; 0 = device default.")]
            [SerializeField] private float _frequency;

            /// <summary>The cue this row maps.</summary>
            public FeedbackCue Cue => _cue;

            /// <summary>Motor amplitude in the [0..1] range.</summary>
            public float Amplitude => _amplitude;

            /// <summary>Impulse duration in seconds.</summary>
            public float Duration => _duration;

            /// <summary>Impulse frequency in Hz (0 = device default).</summary>
            public float Frequency => _frequency;
        }
    }
}
