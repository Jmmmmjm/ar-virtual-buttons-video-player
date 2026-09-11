using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Vuforia;

/// <summary>
/// Hologram Interactive Virtual Button Controller.
/// Provides multi-tiered button detection:
/// 1. Native Vuforia Virtual Button events.
/// 2. Physical Finger Occlusion (pixel luminance sampling from camera feed).
/// 3. Touchscreen / Mouse Raycast fallback.
/// 
/// Includes tactile mechanical button cap depression animation and active channel indicator LED glow.
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

        [HideInInspector] public Vector3 initialCapLocalPos;
        [HideInInspector] public Vector3 initialBadgeLocalPos;
        [HideInInspector] public float baselineLuminance = -1f;
        [HideInInspector] public float currentLuminance = 0f;
        [HideInInspector] public float luminanceDrop = 0f;
        [HideInInspector] public int triggerFrames = 0;
        [HideInInspector] public bool isOccluded = false;
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

    [Header("=== Physical Finger Occlusion Settings ===")]
    [Tooltip("Enable optical luminance occlusion so physical fingers touching the card trigger buttons")]
    [SerializeField] private bool enablePhysicalFingerTouch = true;
    [SerializeField] private float occlusionThreshold = 26f;
    [SerializeField] private int debounceFrameRequirement = 2;

    [Header("=== Mechanical Animation Settings ===")]
    [SerializeField] private float depressionDepth = 0.0010f; // 1.0 mm downward stroke (flush with rim at 0.0005m)
    [SerializeField] private float depressionSpeed = 25f;
    [SerializeField] private float buttonCooldown = 0.4f;

    // Internal state
    private Camera mainCamera;
    private float lastPressTime = -1f;
    private bool formatRegistered = false;
    private PixelFormat pixelFormat = PixelFormat.GRAYSCALE;
    private int cameraImageWidth = 0;
    private int cameraImageHeight = 0;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public VirtualButtonRig[] Buttons => buttons;
    public bool IsFormatRegistered => formatRegistered;

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

        // 2. Physical Finger Occlusion detection via camera feed
        if (enablePhysicalFingerTouch)
        {
            HandlePhysicalFingerOcclusion();
        }
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
        if (Time.time - lastPressTime < buttonCooldown) return;

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

    #region Physical Finger Occlusion (Camera Pixel Sampling)
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

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (btn.buttonRoot == null || mainCamera == null) continue;

                Vector3 screenPoint = mainCamera.WorldToScreenPoint(btn.buttonRoot.position);
                if (screenPoint.z <= 0) continue; // Behind camera

                // Normalized viewport coords
                float normX = screenPoint.x / Screen.width;
                float normY = screenPoint.y / Screen.height;

                int imgX = Mathf.Clamp(Mathf.RoundToInt(normX * cameraImageWidth), 2, cameraImageWidth - 3);
                int imgY = Mathf.Clamp(Mathf.RoundToInt((1f - normY) * cameraImageHeight), 2, cameraImageHeight - 3);

                btn.cameraImageCoord = new Vector2(imgX, imgY);

                // Sample 5x5 kernel
                float totalLum = 0f;
                int count = 0;
                for (int dy = -2; dy <= 2; dy++)
                {
                    int py = imgY + dy;
                    int rowOffset = py * cameraImageWidth;
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int px = imgX + dx;
                        totalLum += pixels[rowOffset + px];
                        count++;
                    }
                }
                float avgLum = totalLum / count;
                btn.currentLuminance = avgLum;

                // Calibrate baseline
                if (btn.baselineLuminance < 0f)
                {
                    btn.baselineLuminance = avgLum;
                }
                else
                {
                    // Slowly drift baseline to handle ambient lighting changes
                    btn.baselineLuminance = Mathf.Lerp(btn.baselineLuminance, avgLum, Time.deltaTime * 0.5f);
                }

                btn.luminanceDrop = btn.baselineLuminance - avgLum;

                if (btn.luminanceDrop > occlusionThreshold)
                {
                    btn.triggerFrames++;
                    if (btn.triggerFrames >= debounceFrameRequirement && !btn.isOccluded)
                    {
                        btn.isOccluded = true;
                        if (Time.time - lastPressTime >= buttonCooldown)
                        {
                            ExecuteButtonPress(i);
                        }
                    }
                }
                else
                {
                    btn.triggerFrames = 0;
                    btn.isOccluded = false;
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

        Vector3 depressedCapPos = btn.buttonCap != null ? btn.initialCapLocalPos - new Vector3(0f, depressionDepth, 0f) : Vector3.zero;
        Vector3 depressedBadgePos = btn.floatingBadge != null ? btn.initialBadgeLocalPos - new Vector3(0f, depressionDepth, 0f) : Vector3.zero;

        Color baseEmission = btn.activeColor;
        Color pulseEmission = btn.activeColor * 4.5f;

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

    private void HandleChannelChanged(int activeIndex, HologramVideoController.VideoChannelConfig config)
    {
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
