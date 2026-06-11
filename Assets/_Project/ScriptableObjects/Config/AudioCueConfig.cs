using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarforgeRelay.Audio
{
    /// <summary>
    /// Designer config mapping a <see cref="FeedbackCue"/> to an <see cref="AudioClip"/> and playback volume
    /// (guardrails §6/§7). Read-only at runtime. A cue with no entry — or an entry with no clip — is a silent
    /// skip, so optional cues (e.g. <see cref="FeedbackCue.TimedOut"/>) simply stay unassigned.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCueConfig", menuName = "Starforge Relay/Audio Cue Config")]
    public sealed class AudioCueConfig : ScriptableObject
    {
        [Tooltip("One row per cue that should play a sound. Unlisted cues are silent.")]
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        private Dictionary<FeedbackCue, Entry> _lookup;

        /// <summary>
        /// Resolve the clip and volume for <paramref name="cue"/>. Returns <see langword="false"/> when the cue
        /// is unmapped or its clip is unset — the caller then plays nothing.
        /// </summary>
        public bool TryGet(FeedbackCue cue, out AudioClip clip, out float volume)
        {
            _lookup ??= BuildLookup();
            if (_lookup.TryGetValue(cue, out Entry entry) && entry.Clip != null)
            {
                clip = entry.Clip;
                volume = entry.Volume;
                return true;
            }

            clip = null;
            volume = 0f;
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

        /// <summary>One cue → clip + volume mapping row.</summary>
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private FeedbackCue _cue;

            [Tooltip("Clip to play for this cue; leave empty for a silent cue.")]
            [SerializeField] private AudioClip _clip;

            [Tooltip("Playback volume. GDD §18: grab/release short; correct satisfying not loud; wrong soft (§12 'no harsh sound').")]
            [SerializeField, Range(0f, 1f)] private float _volume = 1f;

            /// <summary>The cue this row maps.</summary>
            public FeedbackCue Cue => _cue;

            /// <summary>The clip to play (may be null = silent).</summary>
            public AudioClip Clip => _clip;

            /// <summary>Playback volume in the [0..1] range.</summary>
            public float Volume => _volume;
        }
    }
}
