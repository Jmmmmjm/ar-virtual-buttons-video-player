using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Hologram Geometry, HUD, Animation & Scene Specialist Display Controller.
/// Coordinates the complete AR holographic monitor experience:
/// - Base Emitter Mandala: Flat circular emitter on postcard with theme color sync and breathing pulse.
/// - 16-Band Holographic Audio Equalizer: Real-time frequency visualizer bars animated via AudioSynthesizer spectrum.
/// - 3D Parallax Depth Backplane: Floating sci-fi depth grid suspended behind the primary screen.
/// - Upward Quantum Photon Sparks Stream: Soft floating particle sparks ascending from postcard to screen.
/// - Interactive Energy Conduits & Shockwaves: Conduits pulsing from pressed buttons to mandala + expanding ripple rings.
/// - Dynamic Cyber HUD Telemetry: Live data cluster readouts with subtle number flutter and rotating dial reticles.
/// - Dynamic Volumetric Beam: Locked mathematically to lens aperture and screen bottom with 0.00mm gap.
/// - Spring expand & micro-glitch animations with instant channel theme adaptation.
/// </summary>
public class HologramMonitorDisplay : MonoBehaviour
{
    [Header("=== Hologram Renderers ===")]
    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private Renderer projectorBeamRenderer;
    [SerializeField] private Renderer projectorLensRenderer;

    [Header("=== Base Emitter Mandala ===")]
    [SerializeField] private Renderer baseMandalaRenderer;

    [Header("=== 16-Band Holographic Audio Equalizer ===")]
    [SerializeField] private Transform[] eqBarTransforms;
    [SerializeField] private Renderer[] eqBarRenderers;

    [Header("=== 3D Parallax Depth Backplane ===")]
    [SerializeField] private Renderer depthBackplaneRenderer;

    [Header("=== Upward Quantum Photon Stream ===")]
    [SerializeField] private ParticleSystem photonStreamParticles;

    [Header("=== Interactive Button Energy Conduits & Shockwaves ===")]
    [SerializeField] private LineRenderer[] conduitLines;
    [SerializeField] private HologramButtonController buttonController;
    [SerializeField] private Transform[] shockwaveTransforms;
    [SerializeField] private Renderer[] shockwaveRenderers;

    [Header("=== Hologram Corner Reticles ===")]
    [SerializeField] private Transform[] cornerReticleRoots;
    [SerializeField] private Renderer[] cornerReticleRenderers;

    [Header("=== Floating Hologram Text Readouts ===")]
    [SerializeField] private TextMeshPro channelTitleText;
    [SerializeField] private TextMeshPro timecodeText;
    [SerializeField] private TextMeshPro statusBadgeText;

    [Header("=== Levitation & Projection Settings ===")]
    [SerializeField] private Transform floatingScreenRoot;
    [SerializeField] private Vector3 targetScreenScale = new Vector3(0.24f, 0.135f, 1.0f);
    [SerializeField] private float hoverFrequency = 1.4f;
    [SerializeField] private float hoverAmplitude = 0.0025f; // ±2.5 mm smooth floating oscillation
    [SerializeField] private float wobblePitchMax = 0.8f;   // ±0.8° pitch wobble
    [SerializeField] private float wobbleRollMax = 0.65f;   // ±0.65° roll wobble
    [SerializeField] private float beamWidth = 0.20f;

    [Header("=== Audio Feedback ===")]
    [SerializeField] private HologramAudioSynthesizer audioSynthesizer;

    // Runtime materials
    private Material screenMat;
    private Material beamMat;
    private Material lensMat;
    private Material reticleMat;
    private Material baseMandalaMat;
    private Material depthBackplaneMat;
    private Material eqSharedMat;

    // Runtime state
    private bool isPoweredOn = false;
    private int currentChannelIndex = 0;
    public int CurrentChannelIndex => currentChannelIndex;
    private Vector3 initialScreenLocalPos;
    private Quaternion initialScreenLocalRot;
    private Color currentThemeColor = new Color(0f, 0.9f, 1f, 1f);

    private readonly float[] eqLevels = new float[16];
    private float telemetryTimer = 0f;
    private float lockVal = 99.8f;
    private float bitrateVal = 48.2f;
    private float freqVal = 432.8f;

    private Coroutine transitionCoroutine;
    private Coroutine microGlitchCoroutine;
    private HologramVideoController videoController;

    private static readonly int HoloColorId = Shader.PropertyToID("_HoloColor");
    private static readonly int GlitchIntensityId = Shader.PropertyToID("_GlitchIntensity");
    private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
    private static readonly int BeamColorId = Shader.PropertyToID("_BeamColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionMultiplierId = Shader.PropertyToID("_EmissionMultiplier");
    private static readonly int GridColorId = Shader.PropertyToID("_GridColor");

    // Public Getters
    public bool IsPoweredOn => isPoweredOn;
    public Vector3 TargetScreenScale => targetScreenScale;
    public float HoverAmplitude => hoverAmplitude;
    public Transform[] CornerReticleRoots => cornerReticleRoots;
    public Renderer[] CornerReticleRenderers => cornerReticleRenderers;
    public Renderer BaseMandalaRenderer => baseMandalaRenderer;
    public Renderer DepthBackplaneRenderer => depthBackplaneRenderer;
    public ParticleSystem PhotonStreamParticles => photonStreamParticles;
    public Transform[] EqBarTransforms => eqBarTransforms;
    public Renderer[] EqBarRenderers => eqBarRenderers;
    public LineRenderer[] ConduitLines => conduitLines;
    public HologramButtonController ButtonController => buttonController;
    public Transform[] ShockwaveTransforms => shockwaveTransforms;
    public Renderer[] ShockwaveRenderers => shockwaveRenderers;

    private void Awake()
    {
        CacheMaterials();
        if (floatingScreenRoot != null)
        {
            initialScreenLocalPos = floatingScreenRoot.localPosition;
            initialScreenLocalRot = floatingScreenRoot.localRotation;
        }
        else
        {
            initialScreenLocalPos = new Vector3(0f, 0.090f, 0.010f);
            initialScreenLocalRot = Quaternion.Euler(15f, 0f, 0f);
        }

        if (audioSynthesizer == null)
        {
            audioSynthesizer = GetComponent<HologramAudioSynthesizer>() ?? GetComponentInParent<HologramAudioSynthesizer>() ?? FindFirstObjectByType<HologramAudioSynthesizer>();
        }

        if (buttonController == null)
        {
            buttonController = GetComponentInParent<HologramButtonController>() ?? FindFirstObjectByType<HologramButtonController>();
        }

        if (targetScreenScale == Vector3.zero)
        {
            targetScreenScale = new Vector3(0.24f, 0.135f, 1.0f);
        }

        PowerOff(instant: true);
    }

    private void OnEnable()
    {
        if (buttonController == null)
        {
            buttonController = GetComponentInParent<HologramButtonController>() ?? FindFirstObjectByType<HologramButtonController>();
        }
        if (buttonController != null)
        {
            buttonController.OnButtonPressed -= HandleButtonPressed;
            buttonController.OnButtonPressed += HandleButtonPressed;
        }
    }

    private void Start()
    {
        videoController = GetComponentInParent<HologramVideoController>() ?? FindFirstObjectByType<HologramVideoController>();
        if (audioSynthesizer == null)
        {
            audioSynthesizer = GetComponentInParent<HologramAudioSynthesizer>() ?? FindFirstObjectByType<HologramAudioSynthesizer>();
        }

        if (buttonController == null)
        {
            buttonController = GetComponentInParent<HologramButtonController>() ?? FindFirstObjectByType<HologramButtonController>();
        }
        if (buttonController != null)
        {
            buttonController.OnButtonPressed -= HandleButtonPressed;
            buttonController.OnButtonPressed += HandleButtonPressed;
        }

        InitConduits();

        // Standby state: Screen, beam, reticles, HUD, and mandala hidden until a channel is triggered
        PowerOff(instant: true);

        // Start periodic micro-glitch routine
        if (microGlitchCoroutine != null) StopCoroutine(microGlitchCoroutine);
        microGlitchCoroutine = StartCoroutine(PeriodicMicroGlitchRoutine());
    }

    private void OnDisable()
    {
        if (buttonController != null)
        {
            buttonController.OnButtonPressed -= HandleButtonPressed;
        }
        if (audioSynthesizer != null)
        {
            audioSynthesizer.StopSciFiAmbience();
        }
        if (microGlitchCoroutine != null)
        {
            StopCoroutine(microGlitchCoroutine);
            microGlitchCoroutine = null;
        }
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    private void CacheMaterials()
    {
        if (screenRenderer != null)
        {
            screenMat = screenRenderer.material;
        }
        if (projectorBeamRenderer != null)
        {
            beamMat = projectorBeamRenderer.material;
            foreach (var r in projectorBeamRenderer.GetComponentsInChildren<Renderer>(true))
            {
                if (r != projectorBeamRenderer)
                {
                    r.sharedMaterial = beamMat;
                }
            }
        }
        if (projectorLensRenderer != null)
        {
            lensMat = projectorLensRenderer.material;
        }

        // Reticle material
        if (cornerReticleRenderers != null && cornerReticleRenderers.Length > 0)
        {
            for (int i = 0; i < cornerReticleRenderers.Length; i++)
            {
                if (cornerReticleRenderers[i] != null)
                {
                    if (reticleMat == null)
                    {
                        reticleMat = cornerReticleRenderers[i].material;
                    }
                    else
                    {
                        cornerReticleRenderers[i].sharedMaterial = reticleMat;
                    }
                }
            }
        }

        // Base Emitter Mandala material
        if (baseMandalaRenderer != null)
        {
            baseMandalaMat = baseMandalaRenderer.material;
        }

        // 3D Parallax Depth Backplane material
        if (depthBackplaneRenderer != null)
        {
            depthBackplaneMat = depthBackplaneRenderer.material;
        }

        // Equalizer Bars shared material
        if (eqBarRenderers != null && eqBarRenderers.Length > 0 && eqBarRenderers[0] != null)
        {
            eqSharedMat = eqBarRenderers[0].material;
            for (int i = 1; i < eqBarRenderers.Length; i++)
            {
                if (eqBarRenderers[i] != null)
                {
                    eqBarRenderers[i].sharedMaterial = eqSharedMat;
                }
            }
        }
    }

    private void InitConduits()
    {
        if (conduitLines == null) return;
        for (int i = 0; i < conduitLines.Length; i++)
        {
            if (conduitLines[i] != null)
            {
                ResetConduitLine(conduitLines[i], currentThemeColor);
            }
        }
    }

    private void ResetConduitLine(LineRenderer line, Color themeCol)
    {
        if (line == null) return;
        line.enabled = true;
        line.startWidth = 0.0020f;
        line.endWidth = 0.0012f;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(themeCol * 0.7f, 0f), new GradientColorKey(themeCol * 1.4f, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.25f, 0f), new GradientAlphaKey(0.65f, 1f) }
        );
        line.colorGradient = grad;
    }

    private void Update()
    {
        if (!isPoweredOn) return;

        float t = Time.time;

        // 1. Multi-Frequency 3D Floating Hover
        if (floatingScreenRoot != null)
        {
            float yOffset = Mathf.Sin(t * hoverFrequency) * 0.0018f +
                            Mathf.Sin(t * hoverFrequency * 2.14f + 0.5f) * 0.0007f;

            float pitchWobble = Mathf.Sin(t * 0.95f) * wobblePitchMax;
            float rollWobble = Mathf.Cos(t * 1.35f + 0.8f) * wobbleRollMax;
            float yawWobble = Mathf.Sin(t * 0.72f) * 0.35f;

            floatingScreenRoot.localPosition = initialScreenLocalPos + new Vector3(0f, yOffset, 0f);
            floatingScreenRoot.localRotation = initialScreenLocalRot * Quaternion.Euler(pitchWobble, yawWobble, rollWobble);
        }

        // 2. Corner Reticles Smooth Pulse & Subtle Orbital Dial Roll
        if (reticleMat != null && transitionCoroutine == null)
        {
            float pulse = 1.0f + 0.38f * Mathf.Sin(t * 3.8f);
            Color pulsedColor = currentThemeColor * pulse * 1.4f;
            if (reticleMat.HasProperty(EmissionColorId)) reticleMat.SetColor(EmissionColorId, pulsedColor);
            if (reticleMat.HasProperty(BaseColorId)) reticleMat.SetColor(BaseColorId, currentThemeColor);
        }

        UpdateReticleDials(t);

        // 3. Base Emitter Mandala Theme Breathing Pulse
        if (baseMandalaMat != null && transitionCoroutine == null)
        {
            float mandalaPulse = 2.4f + 0.8f * Mathf.Sin(t * 3.2f);
            if (baseMandalaMat.HasProperty(EmissionMultiplierId))
            {
                baseMandalaMat.SetFloat(EmissionMultiplierId, mandalaPulse);
            }
            if (baseMandalaMat.HasProperty(BaseColorId))
            {
                baseMandalaMat.SetColor(BaseColorId, currentThemeColor);
            }
        }

        // 4. 3D Parallax Depth Backplane Color Sync
        if (depthBackplaneMat != null && transitionCoroutine == null)
        {
            if (depthBackplaneMat.HasProperty(GridColorId))
            {
                depthBackplaneMat.SetColor(GridColorId, currentThemeColor);
            }
        }

        // 5. 16-Band Holographic Audio Equalizer (Transients @ Time.deltaTime * 24f)
        UpdateAudioEqualizer();

        // 6. Real-time HUD Timecode & Sci-Fi Telemetry Flutter
        UpdateHUDTimecode();
        UpdateTelemetryReadout();
    }

    private void UpdateReticleDials(float t)
    {
        if (cornerReticleRoots != null)
        {
            for (int i = 0; i < cornerReticleRoots.Length; i++)
            {
                if (cornerReticleRoots[i] != null)
                {
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    float dialWobble = Mathf.Sin(t * 1.8f + i * 1.57f) * 1.5f * dir;
                    cornerReticleRoots[i].localRotation = Quaternion.Euler(0f, 0f, dialWobble);
                }
            }
        }
    }

    private void UpdateAudioEqualizer()
    {
        if (audioSynthesizer == null || eqBarTransforms == null || eqBarTransforms.Length == 0) return;

        audioSynthesizer.GetEqualizerLevels(eqLevels);
        float dt = Time.deltaTime;
        float eqBaseY = -0.074f;

        for (int i = 0; i < eqBarTransforms.Length; i++)
        {
            if (eqBarTransforms[i] == null) continue;
            float level = (i < eqLevels.Length) ? eqLevels[i] : 0f;

            float targetHeight = Mathf.Lerp(0.002f, 0.016f, level);
            float currentHeight = Mathf.Lerp(eqBarTransforms[i].localScale.y, targetHeight, dt * 24f);

            Vector3 s = eqBarTransforms[i].localScale;
            s.y = currentHeight;
            eqBarTransforms[i].localScale = s;

            Vector3 p = eqBarTransforms[i].localPosition;
            p.y = eqBaseY + currentHeight * 0.5f;
            eqBarTransforms[i].localPosition = p;
        }

        if (eqSharedMat != null)
        {
            if (eqSharedMat.HasProperty(BaseColorId)) eqSharedMat.SetColor(BaseColorId, currentThemeColor);
            if (eqSharedMat.HasProperty(EmissionColorId)) eqSharedMat.SetColor(EmissionColorId, currentThemeColor * 2.2f);
        }
    }

    private void UpdateTelemetryReadout()
    {
        if (statusBadgeText == null || !isPoweredOn) return;

        telemetryTimer += Time.deltaTime;

        // Channel-specific telemetry flutter rate & readout profile
        // Channel 0: High-frequency jitter at ~20Hz (0.05s)
        // Channel 1: Melodic cadence at ~12Hz (0.08s)
        // Channel 2: Chaotic pulse at ~15Hz (0.065s)
        float refreshInterval = (currentChannelIndex == 0) ? 0.05f : (currentChannelIndex == 1 ? 0.08f : 0.065f);

        if (telemetryTimer > refreshInterval)
        {
            telemetryTimer = 0f;

            // Compute channel-specific band energy distributions from live equalizer levels
            float subBassEnergy = 0f;
            for (int b = 0; b <= 7; b++) subBassEnergy += eqLevels[b];
            subBassEnergy /= 8f;

            float midEnergy = 0f;
            for (int b = 4; b <= 11; b++) midEnergy += eqLevels[b];
            midEnergy /= 8f;

            float hfEnergy = 0f;
            for (int b = 8; b <= 15; b++) hfEnergy += eqLevels[b];
            hfEnergy /= 8f;

            float glitchSpike = (eqLevels[14] + eqLevels[15]) * 0.5f;

            switch (currentChannelIndex)
            {
                case 0:
                    // Channel 0: High-frequency telemetry jitter across bands 8–15
                    lockVal = 99.8f + Random.Range(-0.15f, 0.15f) * (1f + hfEnergy);
                    bitrateVal = 48.0f + (hfEnergy * 18.5f) + Random.Range(-0.4f, 0.4f);
                    freqVal = 432.0f + (hfEnergy * 12.8f) + Random.Range(-0.5f, 0.5f);
                    statusBadgeText.text = $"[CYBERTECH CH1] [LOCK: {lockVal:F1}%] [HF JITTER: {hfEnergy * 100f:F1}%] [BITRATE: {bitrateVal:F1} MB/s] [FREQ: {freqVal:F1} MHz]";
                    break;

                case 1:
                    // Channel 1: Melodic arpeggio cascades bouncing across mid bands 4–11
                    lockVal = 99.7f + 0.25f * Mathf.Sin(Time.time * 4.2f);
                    bitrateVal = 46.5f + (midEnergy * 15.2f);
                    freqVal = 220.0f + (midEnergy * 24.0f);
                    statusBadgeText.text = $"[FUTURISTIC CH2] [LOCK: {lockVal:F1}%] [ARPEGGIO RES: {midEnergy * 100f:F1}%] [BITRATE: {bitrateVal:F1} MB/s] [FREQ: {freqVal:F1} MHz]";
                    break;

                case 2:
                    // Channel 2: Heavy sub-bass thumps and chaotic glitch spikes across bands 0–7 and 12–15
                    lockVal = 99.5f + (glitchSpike > 0.65f ? Random.Range(-1.8f, 0.3f) : Random.Range(-0.2f, 0.2f));
                    bitrateVal = 52.0f + (subBassEnergy * 22.0f) + (glitchSpike * 14.0f);
                    freqVal = 130.0f + (subBassEnergy * 16.0f);
                    statusBadgeText.text = $"[SCREEN 03 CH3] [LOCK: {lockVal:F1}%] [SUB-BASS: {subBassEnergy * 100f:F1}%] [GLITCH SPIKE: {glitchSpike * 100f:F1}%] [FRAME: LIVE]";
                    break;

                default:
                    lockVal = 99.7f + Random.Range(0f, 0.25f);
                    bitrateVal = 47.9f + Random.Range(0f, 0.6f);
                    freqVal = 432.4f + Random.Range(0f, 0.8f);
                    statusBadgeText.text = $"[QUANTUM LOCK: {lockVal:F1}%] [BITRATE: {bitrateVal:F1} MB/s] [FREQ: {freqVal:F1} MHz] [FRAME: LIVE]";
                    break;
            }
        }
    }

    private void LateUpdate()
    {
        if (isPoweredOn && projectorBeamRenderer != null && projectorLensRenderer != null && screenRenderer != null)
        {
            AlignProjectorBeam();
        }
    }

    private void AlignProjectorBeam()
    {
        if (projectorBeamRenderer == null || projectorLensRenderer == null || screenRenderer == null) return;

        Transform beamT = projectorBeamRenderer.transform;
        Vector3 aperturePos = projectorLensRenderer.transform.position;

        float halfHeight = screenRenderer.transform.lossyScale.y * 0.5f;
        Vector3 screenBottomPos = screenRenderer.transform.position - screenRenderer.transform.up * halfHeight;

        Vector3 beamDir = screenBottomPos - aperturePos;
        float beamDist = beamDir.magnitude;
        if (beamDist < 0.0001f) return;

        Transform beamParent = beamT.parent;
        if (beamParent != null)
        {
            Vector3 localAperture = beamParent.InverseTransformPoint(aperturePos);
            Vector3 localScreenBottom = beamParent.InverseTransformPoint(screenBottomPos);
            Vector3 localBeamDir = localScreenBottom - localAperture;
            float localDist = localBeamDir.magnitude;

            beamT.localPosition = (localAperture + localScreenBottom) * 0.5f;
            beamT.localRotation = Quaternion.FromToRotation(Vector3.up, localBeamDir.normalized);
            beamT.localScale = new Vector3(beamWidth, localDist, 1f);
        }
        else
        {
            beamT.position = (aperturePos + screenBottomPos) * 0.5f;
            beamT.rotation = Quaternion.FromToRotation(Vector3.up, beamDir.normalized);
            beamT.localScale = new Vector3(beamWidth, beamDist, 1f);
        }
    }

    private void UpdateHUDTimecode()
    {
        if (timecodeText == null || videoController == null) return;

        if (videoController.IsPlaying)
        {
            double cur = videoController.CurrentPlaybackTime;
            double total = videoController.TotalDuration;
            int curMin = (int)(cur / 60);
            int curSec = (int)(cur % 60);
            int totMin = (int)(total / 60);
            int totSec = (int)(total % 60);

            timecodeText.text = $"{curMin:00}:{curSec:00} / {totMin:00}:{totSec:00}";
        }
        else
        {
            timecodeText.text = "--:-- / --:--";
        }
    }

    public void PowerOn(Color themeColor, string channelName, int channelIndex = 0)
    {
        bool wasOff = !isPoweredOn;
        isPoweredOn = true;
        currentThemeColor = themeColor;
        currentChannelIndex = channelIndex;

        if (screenRenderer != null) screenRenderer.enabled = true;
        if (projectorBeamRenderer != null)
        {
            foreach (var r in projectorBeamRenderer.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
            }
        }
        SetCornerReticlesActive(true);

        if (baseMandalaRenderer != null) baseMandalaRenderer.enabled = true;
        if (depthBackplaneRenderer != null) depthBackplaneRenderer.enabled = true;
        SetEqualizerBarsActive(true);

        if (photonStreamParticles != null)
        {
            var main = photonStreamParticles.main;
            main.startColor = themeColor;
            photonStreamParticles.Play();
        }

        if (channelTitleText != null)
        {
            channelTitleText.gameObject.SetActive(true);
            channelTitleText.text = channelName;
            channelTitleText.color = themeColor;
        }
        if (statusBadgeText != null)
        {
            statusBadgeText.gameObject.SetActive(true);
            statusBadgeText.color = themeColor;
            statusBadgeText.text = $"[QUANTUM LOCK: 99.8%] [BITRATE: 48.2 MB/s] [FREQ: 432.8 MHz] [FRAME: LIVE]";
        }
        if (timecodeText != null)
        {
            timecodeText.gameObject.SetActive(true);
        }

        if (reticleMat != null)
        {
            if (reticleMat.HasProperty(BaseColorId)) reticleMat.SetColor(BaseColorId, themeColor);
            if (reticleMat.HasProperty(EmissionColorId)) reticleMat.SetColor(EmissionColorId, themeColor * 1.8f);
        }
        if (baseMandalaMat != null)
        {
            if (baseMandalaMat.HasProperty(BaseColorId)) baseMandalaMat.SetColor(BaseColorId, themeColor);
        }
        if (depthBackplaneMat != null)
        {
            if (depthBackplaneMat.HasProperty(GridColorId)) depthBackplaneMat.SetColor(GridColorId, themeColor);
        }

        if (wasOff && audioSynthesizer != null)
        {
            audioSynthesizer.PlayProjectorBoot();
            audioSynthesizer.StartSciFiAmbience(channelIndex);
        }
        else if (audioSynthesizer != null)
        {
            audioSynthesizer.StartSciFiAmbience(channelIndex);
        }

        AlignProjectorBeam();

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        if (wasOff)
        {
            transitionCoroutine = StartCoroutine(PowerOnSpringRoutine(themeColor, channelName));
        }
        else
        {
            transitionCoroutine = StartCoroutine(GlitchTransitionRoutine(themeColor, channelName));
        }
    }

    public void PowerOff(bool instant = false)
    {
        isPoweredOn = false;

        if (audioSynthesizer != null)
        {
            if (!instant)
            {
                audioSynthesizer.PlayPowerDown();
            }
            audioSynthesizer.StopSciFiAmbience();
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (screenRenderer != null)
        {
            screenRenderer.enabled = false;
            screenRenderer.transform.localScale = Vector3.zero;
        }

        if (projectorBeamRenderer != null)
        {
            foreach (var r in projectorBeamRenderer.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
        }

        if (baseMandalaRenderer != null)
        {
            baseMandalaRenderer.enabled = false;
        }

        if (depthBackplaneRenderer != null)
        {
            depthBackplaneRenderer.enabled = false;
        }

        SetEqualizerBarsActive(false);

        if (photonStreamParticles != null)
        {
            photonStreamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        SetCornerReticlesActive(false);

        if (channelTitleText != null) channelTitleText.gameObject.SetActive(false);
        if (timecodeText != null) timecodeText.gameObject.SetActive(false);
        if (statusBadgeText != null) statusBadgeText.gameObject.SetActive(false);

        if (floatingScreenRoot != null)
        {
            floatingScreenRoot.localPosition = initialScreenLocalPos;
            floatingScreenRoot.localRotation = initialScreenLocalRot;
        }

        if (lensMat != null)
        {
            lensMat.SetColor(HoloColorId, Color.black);
        }
    }

    private IEnumerator PowerOnSpringRoutine(Color targetColor, string channelName)
    {
        if (audioSynthesizer != null)
        {
            audioSynthesizer.PlayOpticsWhistle();
        }

        float duration = 0.50f;
        float elapsed = 0f;

        if (screenRenderer != null) screenRenderer.transform.localScale = Vector3.zero;
        SetCornerReticlesScale(0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            float springFactor;
            if (progress < 0.65f)
            {
                float p = progress / 0.65f;
                springFactor = Mathf.SmoothStep(0f, 1.05f, p);
            }
            else
            {
                float p = (progress - 0.65f) / 0.35f;
                springFactor = Mathf.Lerp(1.05f, 1.0f, Mathf.SmoothStep(0f, 1f, p));
            }

            if (screenRenderer != null)
            {
                screenRenderer.transform.localScale = targetScreenScale * springFactor;
            }
            SetCornerReticlesScale(springFactor);

            float flareBrightness = Mathf.Lerp(5.0f, 3.2f, progress) + Random.Range(-0.2f, 0.2f);
            float glitchVal = Mathf.Sin(progress * Mathf.PI) * 0.95f;

            if (screenMat != null)
            {
                screenMat.SetFloat(GlitchIntensityId, glitchVal);
                screenMat.SetFloat(BrightnessId, flareBrightness);
                screenMat.SetColor(HoloColorId, Color.Lerp(Color.white, targetColor, progress));
            }

            if (beamMat != null)
            {
                beamMat.SetColor(BeamColorId, targetColor * (1.0f + glitchVal * 1.2f));
            }

            if (lensMat != null)
            {
                lensMat.SetColor(HoloColorId, targetColor * (1.5f + glitchVal * 2.0f));
            }

            if (reticleMat != null)
            {
                Color flareReticle = Color.Lerp(Color.white, targetColor, progress) * (1.5f + glitchVal * 1.5f);
                if (reticleMat.HasProperty(EmissionColorId)) reticleMat.SetColor(EmissionColorId, flareReticle);
                if (reticleMat.HasProperty(BaseColorId)) reticleMat.SetColor(BaseColorId, targetColor);
            }

            if (baseMandalaMat != null)
            {
                baseMandalaMat.SetColor(BaseColorId, Color.Lerp(Color.black, targetColor, progress));
                if (baseMandalaMat.HasProperty(EmissionMultiplierId))
                {
                    baseMandalaMat.SetFloat(EmissionMultiplierId, Mathf.Lerp(0.5f, 3.5f, progress) + glitchVal * 1.5f);
                }
            }

            if (depthBackplaneMat != null)
            {
                if (depthBackplaneMat.HasProperty(GridColorId))
                {
                    depthBackplaneMat.SetColor(GridColorId, Color.Lerp(Color.black, targetColor, progress));
                }
            }

            AlignProjectorBeam();
            yield return null;
        }

        if (screenRenderer != null) screenRenderer.transform.localScale = targetScreenScale;
        SetCornerReticlesScale(1f);

        if (screenMat != null)
        {
            screenMat.SetFloat(GlitchIntensityId, 0f);
            screenMat.SetFloat(BrightnessId, 3.2f);
            screenMat.SetColor(HoloColorId, targetColor);
        }

        if (beamMat != null)
        {
            beamMat.SetColor(BeamColorId, targetColor * 0.65f);
        }

        if (lensMat != null)
        {
            lensMat.SetColor(HoloColorId, targetColor * 1.8f);
        }

        if (reticleMat != null)
        {
            if (reticleMat.HasProperty(EmissionColorId)) reticleMat.SetColor(EmissionColorId, targetColor * 1.5f);
            if (reticleMat.HasProperty(BaseColorId)) reticleMat.SetColor(BaseColorId, targetColor);
        }

        if (baseMandalaMat != null)
        {
            baseMandalaMat.SetColor(BaseColorId, targetColor);
            if (baseMandalaMat.HasProperty(EmissionMultiplierId)) baseMandalaMat.SetFloat(EmissionMultiplierId, 3.0f);
        }

        if (depthBackplaneMat != null && depthBackplaneMat.HasProperty(GridColorId))
        {
            depthBackplaneMat.SetColor(GridColorId, targetColor);
        }

        transitionCoroutine = null;
    }

    public void TriggerGlitchTransition(Color newThemeColor, string channelName)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(GlitchTransitionRoutine(newThemeColor, channelName));
    }

    private IEnumerator GlitchTransitionRoutine(Color targetColor, string channelName)
    {
        currentThemeColor = targetColor;

        if (channelTitleText != null)
        {
            channelTitleText.text = channelName;
            channelTitleText.color = targetColor;
        }

        if (statusBadgeText != null)
        {
            statusBadgeText.color = targetColor;
        }

        if (reticleMat != null)
        {
            if (reticleMat.HasProperty(BaseColorId)) reticleMat.SetColor(BaseColorId, targetColor);
            if (reticleMat.HasProperty(EmissionColorId)) reticleMat.SetColor(EmissionColorId, targetColor * 1.8f);
        }

        if (baseMandalaMat != null)
        {
            baseMandalaMat.SetColor(BaseColorId, targetColor);
        }

        if (depthBackplaneMat != null && depthBackplaneMat.HasProperty(GridColorId))
        {
            depthBackplaneMat.SetColor(GridColorId, targetColor);
        }

        if (photonStreamParticles != null)
        {
            var main = photonStreamParticles.main;
            main.startColor = targetColor;
        }

        float duration = 0.38f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            float glitchVal = Mathf.Sin(progress * Mathf.PI) * 0.92f;
            float flickerBrightness = 3.2f + Random.Range(-0.3f, 1.2f) * glitchVal;

            if (screenMat != null)
            {
                screenMat.SetFloat(GlitchIntensityId, glitchVal);
                screenMat.SetFloat(BrightnessId, flickerBrightness);
                screenMat.SetColor(HoloColorId, Color.Lerp(screenMat.GetColor(HoloColorId), targetColor, progress));
            }

            if (beamMat != null)
            {
                beamMat.SetColor(BeamColorId, targetColor * (0.8f + glitchVal));
            }

            if (lensMat != null)
            {
                lensMat.SetColor(HoloColorId, targetColor * (1.2f + glitchVal * 1.6f));
            }

            if (reticleMat != null)
            {
                Color flareReticle = targetColor * (1.2f + glitchVal * 1.5f);
                if (reticleMat.HasProperty(EmissionColorId)) reticleMat.SetColor(EmissionColorId, flareReticle);
                if (reticleMat.HasProperty(BaseColorId)) reticleMat.SetColor(BaseColorId, targetColor);
            }

            if (baseMandalaMat != null)
            {
                if (baseMandalaMat.HasProperty(EmissionMultiplierId))
                {
                    baseMandalaMat.SetFloat(EmissionMultiplierId, 3.0f + glitchVal * 2.0f);
                }
            }

            yield return null;
        }

        if (screenMat != null)
        {
            screenMat.SetFloat(GlitchIntensityId, 0f);
            screenMat.SetFloat(BrightnessId, 3.2f);
            screenMat.SetColor(HoloColorId, targetColor);
        }

        if (beamMat != null)
        {
            beamMat.SetColor(BeamColorId, targetColor * 0.65f);
        }

        if (lensMat != null)
        {
            lensMat.SetColor(HoloColorId, targetColor * 1.8f);
        }

        if (reticleMat != null)
        {
            if (reticleMat.HasProperty(EmissionColorId)) reticleMat.SetColor(EmissionColorId, targetColor * 1.5f);
            if (reticleMat.HasProperty(BaseColorId)) reticleMat.SetColor(BaseColorId, targetColor);
        }

        if (baseMandalaMat != null)
        {
            baseMandalaMat.SetColor(BaseColorId, targetColor);
            if (baseMandalaMat.HasProperty(EmissionMultiplierId)) baseMandalaMat.SetFloat(EmissionMultiplierId, 3.0f);
        }

        transitionCoroutine = null;
    }

    private IEnumerator PeriodicMicroGlitchRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(3.0f, 5.0f);
            yield return new WaitForSeconds(waitTime);

            if (isPoweredOn && videoController != null && videoController.IsPlaying && transitionCoroutine == null)
            {
                float burstDuration = Random.Range(0.12f, 0.18f);
                float burstElapsed = 0f;
                float burstIntensity = Random.Range(0.25f, 0.42f);
                float burstBrightness = Random.Range(3.6f, 4.4f);

                while (burstElapsed < burstDuration)
                {
                    burstElapsed += Time.deltaTime;
                    if (screenMat != null && transitionCoroutine == null)
                    {
                        screenMat.SetFloat(GlitchIntensityId, burstIntensity);
                        screenMat.SetFloat(BrightnessId, burstBrightness);
                    }
                    yield return null;
                }

                if (screenMat != null && transitionCoroutine == null)
                {
                    screenMat.SetFloat(GlitchIntensityId, 0f);
                    screenMat.SetFloat(BrightnessId, 3.2f);
                }
            }
        }
    }

    #region Interactive Button Energy Conduits & Shockwaves
    public void HandleButtonPressed(int channelIndex, Vector3 buttonWorldPos)
    {
        // 1. High-speed energy pulse along conduit line
        if (conduitLines != null && channelIndex >= 0 && channelIndex < conduitLines.Length && conduitLines[channelIndex] != null)
        {
            StartCoroutine(ConduitPulseRoutine(conduitLines[channelIndex], currentThemeColor));
        }

        // 2. Expanding shockwave ripple around pressed button
        if (shockwaveTransforms != null && channelIndex >= 0 && channelIndex < shockwaveTransforms.Length && shockwaveTransforms[channelIndex] != null)
        {
            Renderer swR = (shockwaveRenderers != null && channelIndex < shockwaveRenderers.Length) ? shockwaveRenderers[channelIndex] : null;
            StartCoroutine(ShockwaveRippleRoutine(shockwaveTransforms[channelIndex], swR, currentThemeColor));
        }
    }

    private IEnumerator ConduitPulseRoutine(LineRenderer line, Color themeCol)
    {
        if (line == null) yield break;
        line.enabled = true;
        float duration = 0.22f;
        float elapsed = 0f;

        Gradient grad = new Gradient();
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);

            float pMin = Mathf.Clamp01(p - 0.25f);
            float pMid = p;
            float pMax = Mathf.Clamp01(p + 0.25f);

            GradientColorKey[] colKeys = new GradientColorKey[3];
            colKeys[0] = new GradientColorKey(themeCol * 0.5f, pMin);
            colKeys[1] = new GradientColorKey(Color.white * 2.2f, pMid);
            colKeys[2] = new GradientColorKey(themeCol * 0.5f, pMax);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
            alphaKeys[0] = new GradientAlphaKey(0.2f, pMin);
            alphaKeys[1] = new GradientAlphaKey(1.0f, pMid);
            alphaKeys[2] = new GradientAlphaKey(0.2f, pMax);

            grad.SetKeys(colKeys, alphaKeys);
            line.colorGradient = grad;
            line.startWidth = Mathf.Lerp(0.0035f, 0.0020f, p);
            line.endWidth = Mathf.Lerp(0.0020f, 0.0035f, p);

            yield return null;
        }

        if (baseMandalaMat != null && baseMandalaMat.HasProperty(EmissionMultiplierId))
        {
            baseMandalaMat.SetFloat(EmissionMultiplierId, 4.5f);
        }

        ResetConduitLine(line, themeCol);
    }

    private IEnumerator ShockwaveRippleRoutine(Transform swTransform, Renderer swRenderer, Color themeCol)
    {
        if (swTransform == null) yield break;
        if (swRenderer != null) swRenderer.enabled = true;

        Material swMat = swRenderer != null ? swRenderer.material : null;
        float duration = 0.36f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easeOut = Mathf.Sin(progress * Mathf.PI * 0.5f);

            float scale = Mathf.Lerp(0.005f, 0.042f, easeOut);
            swTransform.localScale = new Vector3(scale, scale, 1f);

            if (swMat != null)
            {
                float alpha = (1f - progress);
                Color c = themeCol * alpha * 2.8f;
                c.a = alpha;
                if (swMat.HasProperty(BaseColorId)) swMat.SetColor(BaseColorId, c);
                if (swMat.HasProperty(EmissionColorId)) swMat.SetColor(EmissionColorId, c);
            }

            yield return null;
        }

        if (swRenderer != null) swRenderer.enabled = false;
        swTransform.localScale = Vector3.zero;
    }
    #endregion

    private void SetEqualizerBarsActive(bool active)
    {
        if (eqBarRenderers != null)
        {
            for (int i = 0; i < eqBarRenderers.Length; i++)
            {
                if (eqBarRenderers[i] != null) eqBarRenderers[i].enabled = active;
            }
        }
        if (eqBarTransforms != null)
        {
            for (int i = 0; i < eqBarTransforms.Length; i++)
            {
                if (eqBarTransforms[i] != null)
                {
                    Vector3 s = eqBarTransforms[i].localScale;
                    s.y = active ? 0.002f : 0f;
                    eqBarTransforms[i].localScale = s;
                }
            }
        }
    }

    private void SetCornerReticlesActive(bool active)
    {
        if (cornerReticleRoots != null)
        {
            foreach (var root in cornerReticleRoots)
            {
                if (root != null) root.gameObject.SetActive(active);
            }
        }
        if (cornerReticleRenderers != null)
        {
            foreach (var r in cornerReticleRenderers)
            {
                if (r != null) r.enabled = active;
            }
        }
    }

    private void SetCornerReticlesScale(float scale)
    {
        if (cornerReticleRoots != null)
        {
            Vector3 s = Vector3.one * scale;
            foreach (var root in cornerReticleRoots)
            {
                if (root != null) root.localScale = s;
            }
        }
    }

    public void SetStandbyState()
    {
        PowerOff(instant: true);
    }
}
