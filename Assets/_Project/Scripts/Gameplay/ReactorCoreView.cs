using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// The reactor core (GDD §10 Star Core): the central visual anchor the player stabilises. T17 drives its
    /// display states (Dormant → Charging → Heating → AlmostStable → Stabilized / Overloaded) via emissive tint +
    /// intensity, an idle ring spin, and short core-anchored bursts (combo pulse / victory / overload) — fake
    /// glow, no URP bloom (GDD §17/§26; guardrails §18). <b>View only</b>: <c>CoreStatePresenter</c> decides the
    /// state, this view decides the look; it subscribes to nothing and reads no heat/stabilization. Animations
    /// run on scaled time, so they freeze while paused. Glow comes from the dedicated emissive core material
    /// assigned at T19; palette/intensities are T21 device-tuning levers.
    /// </summary>
    public sealed class ReactorCoreView : MonoBehaviour
    {
        private const float GlowLerpSpeed = 4f;
        private const float HeatingPulseFrequency = 8f;
        private const float ComboPulseDecay = 2.5f;

        private static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color s_dormantColor = new Color(0.25f, 0.32f, 0.55f);
        private static readonly Color s_chargeColor = new Color(0.30f, 0.85f, 1.30f);
        private static readonly Color s_heatColor = new Color(1.40f, 0.42f, 0.12f);
        private static readonly Color s_stableColor = new Color(1.50f, 1.40f, 1.05f);

        [Tooltip("Point that beams / charge VFX aim at (T17). Falls back to this transform if unset.")]
        [SerializeField] private Transform _coreAnchor;

        [Tooltip("Renderer tinted/brightened per state via a MaterialPropertyBlock. Falls back to a child Renderer.")]
        [SerializeField] private Renderer _glowRenderer;

        [Tooltip("Optional ring spun while the core is active (placeholder).")]
        [SerializeField] private Transform _ring;

        [Tooltip("Optional combo-milestone burst (GDD §17 ring pulse). A glow spike plays as fallback if unset.")]
        [SerializeField] private ParticleSystem _comboPulse;

        [Tooltip("Optional victory burst (GDD §17). The Stabilized glow carries it if unset.")]
        [SerializeField] private ParticleSystem _victoryBurst;

        [Tooltip("Optional overload vent (GDD §17). The Overloaded glow carries it if unset.")]
        [SerializeField] private ParticleSystem _overloadVent;

        private MaterialPropertyBlock _propertyBlock;
        private Color _currentColor;
        private Color _targetColor;
        private float _currentIntensity;
        private float _targetIntensity;
        private float _ringSpeedDegrees;
        private float _heatingPulse;
        private float _comboPulseBoost;

        /// <summary>The point beams / VFX target (T17). Defaults to this transform.</summary>
        public Transform CoreAnchor => _coreAnchor != null ? _coreAnchor : transform;

        private void Awake()
        {
            if (_glowRenderer == null)
            {
                _glowRenderer = GetComponentInChildren<Renderer>();
            }

            // Default to Dormant so there is no bright-core flash before the first state is pushed.
            ApplyStateTargets(CoreVisualState.Dormant);
            _currentColor = _targetColor;
            _currentIntensity = _targetIntensity;
            ApplyGlow(_currentColor, _currentIntensity);
        }

        /// <summary>Set the steady core state (the look lerps toward it). Called by <c>CoreStatePresenter</c>.</summary>
        public void SetVisualState(CoreVisualState state) => ApplyStateTargets(state);

        /// <summary>Combo-milestone pulse (GDD §17): play the burst if assigned, else a brief glow spike.</summary>
        public void PlayComboPulse()
        {
            if (_comboPulse != null)
            {
                _comboPulse.Play(true);
            }
            else
            {
                _comboPulseBoost = 1f;
            }
        }

        /// <summary>Victory burst (GDD §17). The Stabilized state glow carries it if no particle is assigned.</summary>
        public void PlayVictory()
        {
            if (_victoryBurst != null)
            {
                _victoryBurst.Play(true);
            }
        }

        /// <summary>Overload vent (GDD §17). The Overloaded state glow carries it if no particle is assigned.</summary>
        public void PlayOverload()
        {
            if (_overloadVent != null)
            {
                _overloadVent.Play(true);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime; // scaled — freezes with the pause's timeScale 0.

            _currentColor = Color.Lerp(_currentColor, _targetColor, deltaTime * GlowLerpSpeed);
            _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, deltaTime * GlowLerpSpeed);

            float intensity = _currentIntensity + _comboPulseBoost;
            if (_heatingPulse > 0f)
            {
                intensity *= 1f + (_heatingPulse * Mathf.Sin(Time.time * HeatingPulseFrequency));
            }

            ApplyGlow(_currentColor, intensity);
            _comboPulseBoost = Mathf.MoveTowards(_comboPulseBoost, 0f, deltaTime * ComboPulseDecay);

            if (_ring != null && _ringSpeedDegrees != 0f)
            {
                _ring.Rotate(0f, 0f, _ringSpeedDegrees * deltaTime, Space.Self);
            }
        }

        // Per-state palette + intensity + ring speed + heating pulse (final-ish; T21 tunes on device).
        private void ApplyStateTargets(CoreVisualState state)
        {
            switch (state)
            {
                case CoreVisualState.Charging:
                    _targetColor = s_chargeColor;
                    _targetIntensity = 0.6f;
                    _ringSpeedDegrees = 40f;
                    _heatingPulse = 0f;
                    break;
                case CoreVisualState.Heating:
                    _targetColor = s_heatColor;
                    _targetIntensity = 0.8f;
                    _ringSpeedDegrees = 60f;
                    _heatingPulse = 0.4f;
                    break;
                case CoreVisualState.AlmostStable:
                    _targetColor = s_chargeColor;
                    _targetIntensity = 1.1f;
                    _ringSpeedDegrees = 80f;
                    _heatingPulse = 0f;
                    break;
                case CoreVisualState.Stabilized:
                    _targetColor = s_stableColor;
                    _targetIntensity = 1.6f;
                    _ringSpeedDegrees = 140f;
                    _heatingPulse = 0f;
                    break;
                case CoreVisualState.Overloaded:
                    _targetColor = s_heatColor;
                    _targetIntensity = 1.3f;
                    _ringSpeedDegrees = 0f;
                    _heatingPulse = 0.6f;
                    break;
                default: // Dormant — dim, slow idle ring (GDD §10).
                    _targetColor = s_dormantColor;
                    _targetIntensity = 0.15f;
                    _ringSpeedDegrees = 10f;
                    _heatingPulse = 0f;
                    break;
            }
        }

        private void ApplyGlow(Color color, float intensity)
        {
            if (_glowRenderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _glowRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(s_baseColorId, color);
            _propertyBlock.SetColor(s_emissionColorId, color * Mathf.Max(0f, intensity));
            _glowRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
