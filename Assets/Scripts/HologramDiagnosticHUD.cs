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
#endif

    private HologramButtonController buttonController;
    private HologramVideoController videoController;
    private ObserverBehaviour observerBehaviour;

    private void Start()
    {
        buttonController = FindFirstObjectByType<HologramButtonController>();
        videoController = FindFirstObjectByType<HologramVideoController>();
        observerBehaviour = FindFirstObjectByType<ObserverBehaviour>();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame)
        {
            showHUD = !showHUD;
        }
#else
        if (Input.GetKeyDown(toggleKey))
        {
            showHUD = !showHUD;
        }
#endif
    }

    private void OnGUI()
    {
        if (!showHUD) return;

        int pad = 12;
        int boxW = 320;
        int boxH = 260;

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
        y += lineH + 5;

        // Button Luminance Metrics
        GUI.color = Color.cyan;
        GUI.Label(new Rect(pad + 10, y, boxW - 20, lineH), "--- Button Finger Occlusion Metrics ---");
        y += lineH;

        if (buttonController != null && buttonController.Buttons != null)
        {
            for (int i = 0; i < buttonController.Buttons.Length; i++)
            {
                var btn = buttonController.Buttons[i];
                string stateTag = btn.isOccluded ? "[PRESSED]" :
                                  (btn.state == HologramButtonController.VirtualButtonRig.ButtonState.Hover ? "[HOVER]" :
                                  (btn.state == HologramButtonController.VirtualButtonRig.ButtonState.Approach ? "[APPROACH]" : ""));
                string dropText = $"Ch{i + 1} ({btn.name}): Cur={btn.currentLuminance:F0} Drop={btn.luminanceDrop:F0} {stateTag}";
                GUI.color = btn.isOccluded ? Color.green :
                            (btn.state == HologramButtonController.VirtualButtonRig.ButtonState.Hover ? Color.yellow :
                            (btn.state == HologramButtonController.VirtualButtonRig.ButtonState.Approach ? Color.cyan : Color.white));
                GUI.Label(new Rect(pad + 10, y, boxW - 20, lineH), dropText);
                y += lineH;
            }
        }

        y += 5;
        GUI.color = Color.gray;
        GUI.Label(new Rect(pad + 10, y, boxW - 20, lineH), "Press [H] to toggle HUD");
    }
}
