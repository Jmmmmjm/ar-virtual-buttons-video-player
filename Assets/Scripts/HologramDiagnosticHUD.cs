using UnityEngine;
using Vuforia;

/// <summary>
/// Hologram Diagnostic OnGUI HUD overlay.
/// Displays live AR tracking metrics, camera pixel luminance swatches for all 3 buttons,
/// and active video channel status.
/// </summary>
public class HologramDiagnosticHUD : MonoBehaviour
{
    [SerializeField] private bool showHUD = true;
#if !ENABLE_INPUT_SYSTEM
    [SerializeField] private KeyCode toggleKey = KeyCode.H;
    [SerializeField] private KeyCode resetKey = KeyCode.R;
#endif

    private HologramButtonController buttonController;
    private HologramVideoController videoController;
    private HologramMonitorDisplay monitorDisplay;
    private ObserverBehaviour observerBehaviour;

    private void Start()
    {
        buttonController = FindFirstObjectByType<HologramButtonController>();
        videoController = FindFirstObjectByType<HologramVideoController>();
        monitorDisplay = FindFirstObjectByType<HologramMonitorDisplay>();
        observerBehaviour = FindFirstObjectByType<ObserverBehaviour>();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame)
            {
                showHUD = !showHUD;
            }
            if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetSensors();
            }
        }
#else
        if (Input.GetKeyDown(toggleKey))
        {
            showHUD = !showHUD;
        }
        if (Input.GetKeyDown(resetKey))
        {
            ResetSensors();
        }
#endif
    }

    /// <summary>
    /// Recalibrates all optical sensor baselines, clears button latching, and un-sticks state machines.
    /// </summary>
    public void ResetSensors()
    {
        if (buttonController != null)
        {
            buttonController.ResetBaselinesAndSensors();
        }
    }

    /// <summary>
    /// Resets all optical sensor baselines, stops active video playback, and powers off the holographic monitor into standby.
    /// </summary>
    public void ResetToStandby()
    {
        ResetSensors();

        if (videoController != null)
        {
            videoController.StopPlayback();
        }

        if (monitorDisplay != null)
        {
            monitorDisplay.PowerOff(instant: false);
        }
    }

    private void OnGUI()
    {
        if (!showHUD) return;

        int pad = 12;
        int boxW = 340;
        int boxH = 305;

        GUI.Box(new Rect(pad, pad, boxW, boxH), "=== AR HOLOGRAM MONITOR HUD ===");

        int y = pad + 25;
        int lineH = 20;

        // Tracking Status
        string trackStatus = observerBehaviour != null ? observerBehaviour.TargetStatus.Status.ToString() : "NO OBSERVER";
        Color statusCol = trackStatus.Contains("TRACKED") ? Color.green : Color.yellow;
        GUI.color = statusCol;
        GUI.Label(new Rect(pad + 10, y, boxW - 20, lineH), $"Target Tracking: {trackStatus}");
        y += lineH;

        // Video Status
        GUI.color = Color.white;
        string chName = videoController != null ? videoController.CurrentChannelName : "N/A";
        string timeStr = "--:--";
        if (videoController != null && videoController.IsPlaying)
        {
            double cur = videoController.CurrentPlaybackTime;
            double tot = videoController.TotalDuration;
            timeStr = $"{(int)cur / 60:00}:{(int)cur % 60:00} / {(int)tot / 60:00}:{(int)tot % 60:00}";
        }
        GUI.Label(new Rect(pad + 10, y, boxW - 20, lineH), $"Active Channel: {chName}");
        y += lineH;
        GUI.Label(new Rect(pad + 10, y, boxW - 20, lineH), $"Playback: {timeStr} {(videoController != null && videoController.IsPlaying ? "[PLAYING]" : "[IDLE]")}");
        y += lineH + 4;

        // Button Luminance Metrics
        GUI.color = Color.cyan;
        GUI.Label(new Rect(pad + 10, y, boxW - 20, lineH), "--- Button Finger Occlusion Metrics ---");
        y += lineH;

        if (buttonController != null)
        {
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUI.Label(new Rect(pad + 10, y, boxW - 20, lineH), 
                $"Ambient Ref: Cur={buttonController.RefCurrentLuminance:F0} Base={buttonController.RefBaselineLuminance:F0} Drop={buttonController.RefLuminanceDrop:F0}");
            y += lineH;

            if (buttonController.Buttons != null)
            {
                for (int i = 0; i < buttonController.Buttons.Length; i++)
                {
                    var btn = buttonController.Buttons[i];
                    string stateTag = btn.isOccluded ? "[PRESSED]" :
                                      (btn.state == HologramButtonController.VirtualButtonRig.ButtonState.Hover ? "[HOVER]" :
                                      (btn.state == HologramButtonController.VirtualButtonRig.ButtonState.Approach ? "[APPROACH]" : ""));
                    string coordStr = $"[{btn.cameraImageCoord.x:F0},{btn.cameraImageCoord.y:F0}]";
                    string dropText = $"Ch{i + 1} ({btn.name}): Cur={btn.currentLuminance:F0} Base={btn.baselineLuminance:F0} Drop={btn.luminanceDrop:F0} {coordStr} {stateTag}";
                    GUI.color = btn.isOccluded ? Color.green :
                                (btn.state == HologramButtonController.VirtualButtonRig.ButtonState.Hover ? Color.yellow :
                                (btn.state == HologramButtonController.VirtualButtonRig.ButtonState.Approach ? Color.cyan : Color.white));
                    GUI.Label(new Rect(pad + 10, y, boxW - 20, lineH), dropText);
                    y += lineH;
                }
            }
        }

        y += 6;

        // On-Screen Reset Buttons
        int btnW = (boxW - 28) / 2;
        GUI.color = new Color(0.25f, 0.95f, 1f);
        if (GUI.Button(new Rect(pad + 10, y, btnW, 26), "⟲ Reset Sensors"))
        {
            ResetSensors();
        }

        GUI.color = new Color(1f, 0.55f, 0.55f);
        if (GUI.Button(new Rect(pad + 18 + btnW, y, btnW, 26), "⟲ Full Standby"))
        {
            ResetToStandby();
        }

        y += 32;
        GUI.color = Color.gray;
        GUI.Label(new Rect(pad + 10, y, boxW - 20, lineH), "Hotkeys: [H] Toggle HUD  |  [R] Reset Sensors");
    }
}
