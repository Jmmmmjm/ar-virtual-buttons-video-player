using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Vuforia;

/// <summary>
/// Hologram Interactive Virtual Button Controller.
/// Provides high-performance, enterprise-grade smart button detection:
/// 1. Optical Sensing Pipeline:
///    - Direct optical finger occlusion: ΔL = L_baseline - L_current (50-150 LSB signal).
///    - Throttled sampling (~30Hz) aligned with camera sensor refresh rate for smooth 60fps rendering.
///    - Top-Center global ambient reference patch for Common-Mode Rejection (CMRR).
///    - Asymmetric baseline drift: freezes adaptation whenever a finger interacts to prevent resting finger corruption.
///    - Distance-adaptive kernel scaling (scales sampling radii based on camera distance).
///    - Schmitt trigger hysteresis (T_high=24, T_low=12) across all states to eliminate noise chatter.
///    - Winner-take-all spatial arbitration (prevents hand swipes from multi-triggering).
/// 2. Multi-Stage Tactile Audio Feedback:
///    - Approach: PlayGyroTick (high-frequency optical clock tick)
///    - Hover: PlayTargetLock (tactical dual-tone lock sweep)
///    - Pressed: PlayButtonPress + PlayChannelChord
///    - Release: PlayRelayClick (mechanical latch snap)
///    - Cooldown Reject: PlayWarningChirp (combat warning warble)
/// 3. Physical Touchscreen / Mouse Raycast fallback.
/// 4. Mechanical button cap depression & floating badge dynamics.
/// </summary>
public class HologramButtonController : MonoBehaviour
{
    [System.Serializable]
    public class VirtualButtonRig
    {
        public string name = "Button";
        public int channelIndex = 0;
        public Transform buttonRoot;
        public Transform buttonCap;
        public Renderer indicatorRenderer;
        public Color activeColor = Color.cyan;
        public Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Header("Floating Holographic Badge (8mm Above Cap)")]
        public Transform floatingBadge;
        public Renderer badgeRenderer;

        // Progressive State Machine
        public enum ButtonState { Idle, Approach, Hover, Pressed }
        [HideInInspector] public ButtonState state = ButtonState.Idle;

        [HideInInspector] public Vector3 initialCapLocalPos;
        [HideInInspector] public Vector3 initialBadgeLocalPos;
        [HideInInspector] public float baselineLuminance = -1f;
        [HideInInspector] public float currentLuminance = 0f;
        [HideInInspector] public float innerLuminance = 0f;
        [HideInInspector] public float outerLuminance = 0f;
        [HideInInspector] public float luminanceDrop = 0f;
        [HideInInspector] public float netContrast = 0f;
        [HideInInspector] public int triggerFrames = 0;
        [HideInInspector] public bool isOccluded = false;
        [HideInInspector] public bool isAnimating = false;
        [HideInInspector] public Vector2 cameraImageCoord;
        [HideInInspector] public Material runtimeMat;
        [HideInInspector] public Material badgeRuntimeMat;
    }

    /// <summary>
    /// Event dispatched when a virtual button is pressed.
    /// Passes channel index and button world position for Energy Conduit and Shockwave particle integration.
    /// </summary>
    public event Action<int, Vector3> OnButtonPressed;

    [Header("=== Target & Subsystem References ===")]
    [SerializeField] private HologramVideoController videoController;
    [SerializeField] private HologramAudioSynthesizer audioSynthesizer;
    [SerializeField] private ObserverBehaviour observerBehaviour;

    [Header("=== Button Rigs ===")]
    [SerializeField] private VirtualButtonRig[] buttons = new VirtualButtonRig[3];

    [Header("=== Optical Sensing Pipeline ===")]
    [Tooltip("Enable optical luminance occlusion so physical fingers touching the card trigger buttons")]
    [SerializeField] private bool enablePhysicalFingerTouch = true;
    [Tooltip("Target sampling interval in seconds (~30Hz matches camera sensor update rate)")]
    [SerializeField] private float sampleInterval = 0.033f;

    [Header("=== Schmitt Trigger & Hysteresis ===")]
    [Tooltip("Activation threshold (T_high) in LSB for press commitment")]
    [SerializeField] private float activationThreshold = 24f;
    [Tooltip("Deactivation threshold (T_low) in LSB for release commitment")]
    [SerializeField] private float deactivationThreshold = 12f;
    [Tooltip("Consecutive frames above T_high required to trigger")]
    [SerializeField] private int debounceFrameRequirement = 2;

    [Header("=== Progressive Hover & Approach Thresholds ===")]
    [Tooltip("Approach threshold for magnetic badge elevation and high-frequency tick")]
    [SerializeField] private float approachThreshold = 8f;
    [Tooltip("Hover threshold for badge pulse and tactical lock tone")]
    [SerializeField] private float hoverThreshold = 16f;

    [Header("=== Common-Mode Rejection (Global Ambient Reference) ===")]
    [Tooltip("Local coordinates on ImageTarget for ambient reference patch (Top-Center)")]
    [SerializeField] private Vector3 referenceLocalPos = new Vector3(0f, 0.001f, 0.035f);
    [Tooltip("Weight of global ambient luminance drop subtraction [0.0 - 1.0]")]
    [SerializeField] private float commonModeRejectionWeight = 0.25f;

    [Header("=== Distance-Adaptive Annular Sampling ===")]
    [Tooltip("Inner sampling kernel radius at far distance (e.g. 0.50m)")]
    [SerializeField] private int minInnerKernelRadius = 2;
    [Tooltip("Inner sampling kernel radius at close distance (e.g. 0.15m)")]
    [SerializeField] private int maxInnerKernelRadius = 4;
    [Tooltip("Outer annular ring radius offset from inner radius")]
    [SerializeField] private int annularRingOffset = 4;
    [SerializeField] private float nearDistance = 0.15f;
    [SerializeField] private float farDistance = 0.50f;

    [Header("=== Asymmetric Baseline Drift ===")]
    [Tooltip("Baseline drift lerp rate when idle (adapts to slow natural room lighting)")]
    [SerializeField] private float baselineAdaptSpeed = 0.5f;

    [Header("=== Mechanical Animation Settings ===")]
    [SerializeField] private float depressionDepth = 0.0010f; // 1.0 mm downward stroke (flush with rim at 0.0005m)
    [SerializeField] private float depressionSpeed = 25f;
    [SerializeField] private float buttonCooldown = 0.4f;

    // Internal state
    private Camera mainCamera;
    private float lastPressTime = -1f;
    private float lastWarningSoundTime = -1f;
    private float lastSampleTime = -1f;
    private int activeChannelIndex = -1;
    private bool formatRegistered = false;
    private PixelFormat pixelFormat = PixelFormat.GRAYSCALE;
    private int cameraImageWidth = 0;
    private int cameraImageHeight = 0;

    // Ambient Reference State
    private float refBaselineLuminance = -1f;
    private float refCurrentLuminance = 0f;
    private float refLuminanceDrop = 0f;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    // Public Getters for Self-Audit and Inspection
    public VirtualButtonRig[] Buttons => buttons;
    public bool IsFormatRegistered => formatRegistered;
    public float ActivationThreshold => activationThreshold;
    public float DeactivationThreshold => deactivationThreshold;
    public float ApproachThreshold => approachThreshold;
    public float HoverThreshold => hoverThreshold;
    public Vector3 ReferenceLocalPos => referenceLocalPos;
    public float CommonModeRejectionWeight => commonModeRejectionWeight;
    public int MinInnerKernelRadius => minInnerKernelRadius;
    public int MaxInnerKernelRadius => maxInnerKernelRadius;
    public float NearDistance => nearDistance;
    public float FarDistance => farDistance;
    public float OcclusionThreshold => activationThreshold;
    public float ButtonCooldown => buttonCooldown;
    public int DebounceFrameRequirement => debounceFrameRequirement;

    private void Awake()
    {
        if (observerBehaviour == null)
        {
            observerBehaviour = GetComponentInParent<ObserverBehaviour>() ?? GetComponent<ObserverBehaviour>();
        }

        if (videoController == null)
        {
            videoController = GetComponentInParent<HologramVideoController>() ?? FindFirstObjectByType<HologramVideoController>();
        }

        if (audioSynthesizer == null)
        {
            audioSynthesizer = GetComponentInParent<HologramAudioSynthesizer>() ?? FindFirstObjectByType<HologramAudioSynthesizer>();
        }

        InitializeButtonRigs();
    }

    private void Start()
    {
        mainCamera = Camera.main;

        VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
        if (videoController != null)
        {
            videoController.OnChannelChanged += HandleChannelChanged;
            activeChannelIndex = videoController.CurrentChannelIndex;
        }
    }

    private void OnDestroy()
    {
        VuforiaApplication.Instance.OnVuforiaStarted -= OnVuforiaStarted;
        if (videoController != null)
        {
            videoController.OnChannelChanged -= HandleChannelChanged;
        }
    }

    private void InitializeButtonRigs()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn.buttonCap != null)
            {
                btn.initialCapLocalPos = btn.buttonCap.localPosition;
            }
            if (btn.indicatorRenderer != null)
            {
                btn.runtimeMat = btn.indicatorRenderer.material;
            }

            // Floating 3D Holographic Badge resolution (hovering 8mm above cap)
            if (btn.floatingBadge == null && btn.buttonRoot != null)
            {
                Transform badgeT = btn.buttonRoot.Find($"{btn.name}_Badge") ??
                                   btn.buttonRoot.Find("Badge") ??
                                   btn.buttonRoot.Find("Holo_Badge");
                if (badgeT != null)
                {
                    btn.floatingBadge = badgeT;
                }
            }

            if (btn.floatingBadge != null)
            {
                btn.initialBadgeLocalPos = btn.floatingBadge.localPosition;
                if (btn.badgeRenderer == null)
                {
                    btn.badgeRenderer = btn.floatingBadge.GetComponent<Renderer>() ?? btn.floatingBadge.GetComponentInChildren<Renderer>();
                }
                if (btn.badgeRenderer != null)
                {
                    btn.badgeRuntimeMat = btn.badgeRenderer.material;
                }
            }

            UpdateButtonGlow(i, false);
        }
    }

    private void OnVuforiaStarted()
    {
        RegisterCameraFormat();
    }

    private void RegisterCameraFormat()
    {
        if (VuforiaBehaviour.Instance != null && VuforiaBehaviour.Instance.CameraDevice != null)
        {
            bool success = VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(pixelFormat, true);
            formatRegistered = success;
            Debug.Log($"[HologramButtonController] Vuforia camera pixel format {pixelFormat} registered: {success}");
        }
    }

    private void Update()
    {
        // 1. Mouse / Touchscreen tap fallback
        HandleScreenInput();

        // 2. Physical Finger Occlusion detection via camera feed (throttled to sensor refresh rate)
        if (enablePhysicalFingerTouch && Time.time - lastSampleTime >= sampleInterval)
        {
            lastSampleTime = Time.time;
            HandlePhysicalFingerOcclusion();
        }

        // 3. Progressive Hover Visual Dynamics (badge magnetic lift & cyber pulse)
        UpdateHoverVisuals();
    }

    #region Screen Input (Mouse / Touch)
    private void HandleScreenInput()
    {
        bool inputDetected = false;
        Vector2 inputPosition = Vector2.zero;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputDetected = true;
            inputPosition = Mouse.current.position.ReadValue();
        }

        if (!inputDetected || mainCamera == null) return;
        if (Time.time - lastPressTime < buttonCooldown)
        {
            if (Time.time - lastWarningSoundTime >= 0.25f && audioSynthesizer != null)
            {
                lastWarningSoundTime = Time.time;
                audioSynthesizer.PlayWarningChirp(0.20f);
            }
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(inputPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (hit.collider.transform == buttons[i].buttonRoot ||
                    hit.collider.transform.IsChildOf(buttons[i].buttonRoot))
                {
                    ExecuteButtonPress(i);
                    break;
                }
            }
        }
    }
    #endregion

    #region Physical Finger Occlusion (Direct Optical Pipeline + Common-Mode Shield)
    private void HandlePhysicalFingerOcclusion()
    {
        if (!formatRegistered || VuforiaBehaviour.Instance == null || VuforiaBehaviour.Instance.CameraDevice == null)
        {
            if (Time.frameCount % 60 == 0) RegisterCameraFormat();
            return;
        }

        if (observerBehaviour != null && (observerBehaviour.TargetStatus.Status == Status.NO_POSE || observerBehaviour.TargetStatus.Status == Status.LIMITED))
        {
            return;
        }

        Vuforia.Image image = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(pixelFormat);
        if (image == null || image.Pixels == null || image.Pixels.Length == 0) return;

        cameraImageWidth = image.Width;
        cameraImageHeight = image.Height;
        byte[] pixels = image.Pixels;

        Transform targetTransform = observerBehaviour != null ? observerBehaviour.transform : transform;

        // 1. Distance-Adaptive Kernel Calculation
        float camDist = mainCamera != null ? Vector3.Distance(mainCamera.transform.position, targetTransform.position) : 0.35f;
        float distFactor = Mathf.Clamp01((camDist - nearDistance) / Mathf.Max(0.01f, farDistance - nearDistance));
        int innerRadius = Mathf.RoundToInt(Mathf.Lerp(maxInnerKernelRadius, minInnerKernelRadius, distFactor));

        // 2. Top-Center Global Ambient Reference Sampling (Common-Mode Rejection)
        Vector3 refWorldPos = targetTransform.TransformPoint(referenceLocalPos);
        Vector3 refScreenPoint = mainCamera != null ? mainCamera.WorldToScreenPoint(refWorldPos) : Vector3.zero;
        if (refScreenPoint.z > 0f)
        {
            float normRefX = Mathf.Clamp01(refScreenPoint.x / Screen.width);
            float normRefY = Mathf.Clamp01(refScreenPoint.y / Screen.height);
            int refImgX = Mathf.Clamp(Mathf.RoundToInt(normRefX * cameraImageWidth), 2, cameraImageWidth - 3);
            int refImgY = Mathf.Clamp(Mathf.RoundToInt((1f - normRefY) * cameraImageHeight), 2, cameraImageHeight - 3);

            float refSum = 0f;
            int refCount = 0;
            for (int dy = -2; dy <= 2; dy++)
            {
                int row = (refImgY + dy) * cameraImageWidth;
                for (int dx = -2; dx <= 2; dx++)
                {
                    refSum += pixels[row + (refImgX + dx)];
                    refCount++;
                }
            }
            refCurrentLuminance = refCount > 0 ? refSum / refCount : refCurrentLuminance;

            if (refBaselineLuminance < 0f)
            {
                refBaselineLuminance = refCurrentLuminance;
            }
            else
            {
                refBaselineLuminance = Mathf.Lerp(refBaselineLuminance, refCurrentLuminance, Time.deltaTime * baselineAdaptSpeed);
            }
            refLuminanceDrop = Mathf.Max(0f, refBaselineLuminance - refCurrentLuminance);
        }

        // 3. Per-Button Optical Occlusion Sampling
        float highestDrop = -999f;
        int bestCandidate = -1;

        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn.buttonRoot == null || mainCamera == null) continue;

            Vector3 screenPoint = mainCamera.WorldToScreenPoint(btn.buttonRoot.position);
            if (screenPoint.z <= 0) continue; // Behind camera

            float normX = Mathf.Clamp01(screenPoint.x / Screen.width);
            float normY = Mathf.Clamp01(screenPoint.y / Screen.height);

            int imgX = Mathf.Clamp(Mathf.RoundToInt(normX * cameraImageWidth), innerRadius + 1, cameraImageWidth - innerRadius - 2);
            int imgY = Mathf.Clamp(Mathf.RoundToInt((1f - normY) * cameraImageHeight), innerRadius + 1, cameraImageHeight - innerRadius - 2);

            btn.cameraImageCoord = new Vector2(imgX, imgY);

            // Sample Button Cap Kernel (distance-adaptive radius)
            float innerSum = 0f;
            int innerCount = 0;
            for (int dy = -innerRadius; dy <= innerRadius; dy++)
            {
                int rowOffset = (imgY + dy) * cameraImageWidth;
                for (int dx = -innerRadius; dx <= innerRadius; dx++)
                {
                    innerSum += pixels[rowOffset + (imgX + dx)];
                    innerCount++;
                }
            }
            float innerAvg = innerCount > 0 ? innerSum / innerCount : 0f;
            btn.innerLuminance = innerAvg;
            btn.currentLuminance = innerAvg;

            // Calibrate Baseline
            if (btn.baselineLuminance < 0f)
            {
                btn.baselineLuminance = innerAvg;
            }
            else if (btn.state == VirtualButtonRig.ButtonState.Idle && btn.luminanceDrop < approachThreshold)
            {
                // Slowly drift baseline ONLY when idle and unoccluded
                btn.baselineLuminance = Mathf.Lerp(btn.baselineLuminance, innerAvg, Time.deltaTime * baselineAdaptSpeed);
            }

            // Raw Luminance Drop (Physical finger blocking camera pixels)
            float rawDrop = Mathf.Max(0f, btn.baselineLuminance - innerAvg);

            // Subtract global ambient drop (CMRR) with safety clamp
            float netMetric = rawDrop - Mathf.Min(rawDrop * 0.4f, refLuminanceDrop * commonModeRejectionWeight);
            btn.luminanceDrop = netMetric;
            btn.netContrast = netMetric;

            // Winner-Take-All competition
            if (netMetric >= activationThreshold && netMetric > highestDrop)
            {
                highestDrop = netMetric;
                bestCandidate = i;
            }
        }

        // 4. Schmitt Trigger Hysteresis & Winner-Take-All State Machine
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            float metric = btn.luminanceDrop;

            if (btn.state == VirtualButtonRig.ButtonState.Pressed)
            {
                // Deactivation hysteresis trip point (12 LSB)
                if (metric < deactivationThreshold)
                {
                    btn.state = VirtualButtonRig.ButtonState.Idle;
                    btn.triggerFrames = 0;
                    btn.isOccluded = false;

                    if (audioSynthesizer != null)
                    {
                        audioSynthesizer.PlayRelayClick(0.30f);
                    }
                    UpdateButtonGlow(i, btn.channelIndex == activeChannelIndex);
                }
            }
            else
            {
                // Any state (Idle, Approach, Hover) can directly trigger Press if above threshold
                if (metric >= activationThreshold && i == bestCandidate)
                {
                    btn.triggerFrames++;
                    if (btn.triggerFrames >= debounceFrameRequirement)
                    {
                        if (Time.time - lastPressTime >= buttonCooldown)
                        {
                            btn.state = VirtualButtonRig.ButtonState.Pressed;
                            btn.isOccluded = true;
                            ExecuteButtonPress(i);
                        }
                        else if (Time.time - lastWarningSoundTime >= 0.25f && audioSynthesizer != null)
                        {
                            lastWarningSoundTime = Time.time;
                            audioSynthesizer.PlayWarningChirp(0.20f);
                        }
                    }
                }
                else if (metric >= hoverThreshold)
                {
                    btn.triggerFrames = 0;
                    if (btn.state != VirtualButtonRig.ButtonState.Hover)
                    {
                        btn.state = VirtualButtonRig.ButtonState.Hover;
                        if (audioSynthesizer != null)
                        {
                            audioSynthesizer.PlayTargetLock(0.35f);
                        }
                    }
                }
                else if (metric >= approachThreshold)
                {
                    btn.triggerFrames = 0;
                    if (btn.state != VirtualButtonRig.ButtonState.Approach)
                    {
                        btn.state = VirtualButtonRig.ButtonState.Approach;
                        if (audioSynthesizer != null)
                        {
                            audioSynthesizer.PlayGyroTick(0.30f);
                        }
                    }
                }
                else if (metric < (approachThreshold - 3f)) // Hysteresis exit: 5 LSB
                {
                    btn.triggerFrames = 0;
                    if (btn.state != VirtualButtonRig.ButtonState.Idle)
                    {
                        btn.state = VirtualButtonRig.ButtonState.Idle;
                        btn.isOccluded = false;
                        UpdateButtonGlow(i, btn.channelIndex == activeChannelIndex);
                    }
                }
            }
        }
    }
    #endregion

    #region Progressive Hover Dynamics
    private void UpdateHoverVisuals()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn.floatingBadge == null || btn.isAnimating) continue;

            float targetYOffset = 0f;
            Color emissionColor = btn.channelIndex == activeChannelIndex ? btn.activeColor * 2.5f : btn.inactiveColor * 0.75f;
            bool activeHover = false;

            if (btn.state == VirtualButtonRig.ButtonState.Hover)
            {
                activeHover = true;
                targetYOffset = 0.0020f; // +2.0mm magnetic lift
                float pulse = 1f + 0.45f * Mathf.Sin(Time.time * 16f);
                emissionColor = btn.activeColor * (3.0f * pulse);
            }
            else if (btn.state == VirtualButtonRig.ButtonState.Approach)
            {
                activeHover = true;
                targetYOffset = 0.0012f; // +1.2mm pre-lift
                emissionColor = btn.activeColor * 2.8f;
            }

            Vector3 targetPos = btn.initialBadgeLocalPos + new Vector3(0f, targetYOffset, 0f);
            if (Vector3.SqrMagnitude(btn.floatingBadge.localPosition - targetPos) > 0.0000001f)
            {
                btn.floatingBadge.localPosition = Vector3.Lerp(btn.floatingBadge.localPosition, targetPos, Time.deltaTime * 14f);
            }

            if (activeHover && btn.badgeRuntimeMat != null && btn.badgeRuntimeMat.HasProperty(EmissionColorId))
            {
                btn.badgeRuntimeMat.SetColor(EmissionColorId, emissionColor);
            }
        }
    }
    #endregion

    #region Execution & Mechanical Animation
    /// <summary>
    /// Triggers mechanical button cap depression, floating badge motion & emission pulse, and dispatches the OnButtonPressed event.
    /// Accessible publicly so visual conduit controllers and shockwave emitters can synchronize effects.
    /// </summary>
    public void TriggerButtonVisuals(int index)
    {
        if (index < 0 || index >= buttons.Length) return;
        var btn = buttons[index];

        // Animate mechanical depression & floating badge pulse
        StartCoroutine(AnimateDepression(btn));

        // Dispatch OnButtonPressed event with channel index and button world position
        Vector3 worldPos = btn.buttonCap != null ? btn.buttonCap.position :
                           (btn.buttonRoot != null ? btn.buttonRoot.position : transform.position);
        OnButtonPressed?.Invoke(btn.channelIndex, worldPos);
    }

    public void ExecuteButtonPress(int index)
    {
        if (index < 0 || index >= buttons.Length) return;
        lastPressTime = Time.time;
        activeChannelIndex = buttons[index].channelIndex;

        Debug.Log($"[HologramButtonController] Activating Channel {index + 1}: {buttons[index].name}");

        // Play procedural audio feedback: tactile button pip + harmonic channel triad chord
        if (audioSynthesizer != null)
        {
            audioSynthesizer.PlayButtonPress();
            audioSynthesizer.PlayChannelChord(buttons[index].channelIndex);
        }

        // Trigger mechanical depression, badge pulse, and dispatch OnButtonPressed event
        TriggerButtonVisuals(index);

        // Switch Video Channel
        if (videoController != null)
        {
            videoController.SwitchToChannel(buttons[index].channelIndex);
        }
    }

    private IEnumerator AnimateDepression(VirtualButtonRig btn)
    {
        if (btn.buttonCap == null && btn.floatingBadge == null) yield break;

        btn.isAnimating = true;
        Vector3 depressedCapPos = btn.buttonCap != null ? btn.initialCapLocalPos - new Vector3(0f, depressionDepth, 0f) : Vector3.zero;
        Vector3 depressedBadgePos = btn.floatingBadge != null ? btn.initialBadgeLocalPos - new Vector3(0f, depressionDepth, 0f) : Vector3.zero;

        Color baseEmission = btn.activeColor;
        Color pulseEmission = btn.activeColor * 4.5f;

        try
        {
            // Downward stroke & badge flare
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * depressionSpeed;
                float smoothT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

                if (btn.buttonCap != null)
                {
                    btn.buttonCap.localPosition = Vector3.Lerp(btn.initialCapLocalPos, depressedCapPos, smoothT);
                }
                if (btn.floatingBadge != null)
                {
                    btn.floatingBadge.localPosition = Vector3.Lerp(btn.initialBadgeLocalPos, depressedBadgePos, smoothT);
                }

                // Pulse badge emission intensity
                if (btn.badgeRuntimeMat != null && btn.badgeRuntimeMat.HasProperty(EmissionColorId))
                {
                    btn.badgeRuntimeMat.SetColor(EmissionColorId, Color.Lerp(baseEmission, pulseEmission, smoothT));
                }

                yield return null;
            }

            yield return new WaitForSeconds(0.08f);

            // Spring rebound & emission settle
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * (depressionSpeed * 0.75f);
                float smoothT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

                if (btn.buttonCap != null)
                {
                    btn.buttonCap.localPosition = Vector3.Lerp(depressedCapPos, btn.initialCapLocalPos, smoothT);
                }
                if (btn.floatingBadge != null)
                {
                    btn.floatingBadge.localPosition = Vector3.Lerp(depressedBadgePos, btn.initialBadgeLocalPos, smoothT);
                }

                // Settle badge emission intensity
                if (btn.badgeRuntimeMat != null && btn.badgeRuntimeMat.HasProperty(EmissionColorId))
                {
                    btn.badgeRuntimeMat.SetColor(EmissionColorId, Color.Lerp(pulseEmission, baseEmission * 2.5f, smoothT));
                }

                yield return null;
            }

            if (btn.buttonCap != null) btn.buttonCap.localPosition = btn.initialCapLocalPos;
            if (btn.floatingBadge != null) btn.floatingBadge.localPosition = btn.initialBadgeLocalPos;
        }
        finally
        {
            btn.isAnimating = false;
        }
    }

    private void HandleChannelChanged(int activeIndex, HologramVideoController.VideoChannelConfig config)
    {
        activeChannelIndex = activeIndex;
        for (int i = 0; i < buttons.Length; i++)
        {
            UpdateButtonGlow(i, buttons[i].channelIndex == activeIndex);
        }
    }

    private void UpdateButtonGlow(int index, bool isActive)
    {
        var btn = buttons[index];
        Color targetCapColor = isActive ? btn.activeColor * 2.2f : btn.inactiveColor;
        Color targetBadgeColor = isActive ? btn.activeColor * 2.5f : btn.inactiveColor * 0.75f;

        if (btn.runtimeMat != null)
        {
            if (btn.runtimeMat.HasProperty(EmissionColorId))
            {
                btn.runtimeMat.SetColor(EmissionColorId, targetCapColor);
            }
            if (btn.runtimeMat.HasProperty(BaseColorId))
            {
                btn.runtimeMat.SetColor(BaseColorId, isActive ? btn.activeColor : btn.inactiveColor);
            }
        }

        if (btn.badgeRuntimeMat != null)
        {
            if (btn.badgeRuntimeMat.HasProperty(EmissionColorId))
            {
                btn.badgeRuntimeMat.SetColor(EmissionColorId, targetBadgeColor);
            }
            if (btn.badgeRuntimeMat.HasProperty(BaseColorId))
            {
                btn.badgeRuntimeMat.SetColor(BaseColorId, isActive ? btn.activeColor : btn.inactiveColor);
            }
        }
    }
    #endregion
}
