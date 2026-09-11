using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Hologram Video Controller.
/// Coordinates VideoPlayer playback across 3 distinct channels with precise timestamp offsets:
/// - Channel 1: CyberTech HUD (Starts at 0:10)
/// - Channel 2: Futuristic UI (Starts at 0:05)
/// - Channel 3: Screen 03 (Starts at 0:00)
///
/// Configured for completely silent video playback (VideoAudioOutputMode.None, muted audio source),
/// delegating all acoustic feedback to the procedural HologramAudioSynthesizer.
/// </summary>
public class HologramVideoController : MonoBehaviour
{
    [Serializable]
    public class VideoChannelConfig
    {
        public string channelName = "Channel";
        public VideoClip videoClip;
        public float startOffsetSeconds = 0f;
        public Color channelThemeColor = new Color(0.2f, 0.9f, 1f, 1f);
    }

    [Header("=== Video Player Components ===")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource videoAudioSource;
    [SerializeField] private RenderTexture displayRenderTexture;

    [Header("=== Hologram Subsystems ===")]
    [SerializeField] private HologramMonitorDisplay monitorDisplay;
    [SerializeField] private HologramAudioSynthesizer audioSynthesizer;

    [Header("=== Channel Configurations ===")]
    [SerializeField] private VideoChannelConfig[] channels = new VideoChannelConfig[3]
    {
        new VideoChannelConfig { channelName = "CH 1: CYBERTECH HUD", startOffsetSeconds = 10f, channelThemeColor = new Color(0f, 0.9f, 1f, 1f) },
        new VideoChannelConfig { channelName = "CH 2: FUTURISTIC UI",  startOffsetSeconds = 5f,  channelThemeColor = new Color(1f, 0.75f, 0.1f, 1f) },
        new VideoChannelConfig { channelName = "CH 3: SCREEN 03 HUD",  startOffsetSeconds = 0f,  channelThemeColor = new Color(1f, 0.25f, 0.4f, 1f) }
    };

    [Header("=== Playback Settings ===")]
    [SerializeField] private bool autoPlayFirstChannel = false;
    [SerializeField] private bool loopVideo = true;

    // Runtime state
    private int currentChannelIndex = -1;
    private bool isPreparing = false;
    private float targetStartTime = 0f;

    public int CurrentChannelIndex => currentChannelIndex;
    public bool IsPreparing => isPreparing;
    public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;
    public double CurrentPlaybackTime => videoPlayer != null ? videoPlayer.time : 0;
    public double TotalDuration => videoPlayer != null && videoPlayer.clip != null ? videoPlayer.clip.length : 0;

    public string CurrentChannelName
    {
        get
        {
            if (currentChannelIndex >= 0 && currentChannelIndex < channels.Length)
                return channels[currentChannelIndex].channelName;
            return "STANDBY";
        }
    }

    public event Action<int, VideoChannelConfig> OnChannelChanged;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        if (autoPlayFirstChannel && channels.Length > 0)
        {
            SwitchToChannel(0);
        }
    }

    private void OnDisable()
    {
        if (audioSynthesizer != null)
        {
            audioSynthesizer.StopSciFiAmbience();
        }
    }

    public void InitializeComponents()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        if (videoAudioSource == null)
        {
            videoAudioSource = GetComponent<AudioSource>();
            if (videoAudioSource == null) videoAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (audioSynthesizer == null)
        {
            audioSynthesizer = GetComponent<HologramAudioSynthesizer>() ?? GetComponentInParent<HologramAudioSynthesizer>() ?? FindFirstObjectByType<HologramAudioSynthesizer>();
        }

        if (monitorDisplay == null)
        {
            monitorDisplay = GetComponent<HologramMonitorDisplay>() ?? GetComponentInParent<HologramMonitorDisplay>() ?? FindFirstObjectByType<HologramMonitorDisplay>();
        }

        // Configure VideoPlayer properties
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = loopVideo;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        if (displayRenderTexture != null)
        {
            videoPlayer.targetTexture = displayRenderTexture;
        }

        // Audio setup: Video audio completely disabled per specification (pure silent video playback)
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        for (ushort i = 0; i < videoPlayer.controlledAudioTrackCount; i++)
        {
            videoPlayer.EnableAudioTrack(i, false);
        }

        if (videoAudioSource != null)
        {
            videoAudioSource.mute = true;
            videoAudioSource.volume = 0f;
            videoAudioSource.playOnAwake = false;
            if (videoAudioSource.isPlaying) videoAudioSource.Stop();
        }

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.loopPointReached += OnLoopPointReached;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.loopPointReached -= OnLoopPointReached;
        }
    }

    /// <summary>
    /// Activates and plays the specified video channel (0-indexed) with specialized sci-fi soundscape.
    /// </summary>
    public void PlayChannel(int index)
    {
        SwitchToChannel(index);
    }

    public void SwitchToChannel(int index)
    {
        if (channels == null || index < 0 || index >= channels.Length)
        {
            Debug.LogWarning($"[HologramVideoController] Invalid channel index: {index}");
            return;
        }

        if (currentChannelIndex == index && IsPlaying)
        {
            Debug.Log($"[HologramVideoController] Channel {index + 1} already active. Restarting from offset.");
            SeekToOffset(channels[index].startOffsetSeconds);
            if (audioSynthesizer != null)
            {
                audioSynthesizer.StartSciFiAmbience(currentChannelIndex);
            }
            return;
        }

        currentChannelIndex = index;
        VideoChannelConfig config = channels[index];

        Debug.Log($"[HologramVideoController] Switching to {config.channelName}, Target Offset: {config.startOffsetSeconds}s");

        // Trigger visual & audio feedback
        if (monitorDisplay != null)
        {
            monitorDisplay.PowerOn(config.channelThemeColor, config.channelName, index);
        }

        if (audioSynthesizer != null)
        {
            audioSynthesizer.PlayChannelGlitch();
        }

        // Load clip and prepare
        targetStartTime = config.startOffsetSeconds;
        isPreparing = true;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        if (config.videoClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = config.videoClip;
            videoPlayer.Prepare();
        }
        else
        {
            Debug.LogError($"[HologramVideoController] VideoClip for channel {index} is null!");
        }

        OnChannelChanged?.Invoke(index, config);
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        isPreparing = false;
        SeekToOffset(targetStartTime);

        // Strict enforcement: ensure video audio is completely disabled
        source.audioOutputMode = VideoAudioOutputMode.None;
        for (ushort i = 0; i < source.controlledAudioTrackCount; i++)
        {
            source.EnableAudioTrack(i, false);
        }
        if (videoAudioSource != null)
        {
            videoAudioSource.mute = true;
            videoAudioSource.volume = 0f;
        }

        source.Play();

        // Start rich procedural sci-fi UI ambience tailored for active channel
        if (audioSynthesizer != null)
        {
            audioSynthesizer.StartSciFiAmbience(currentChannelIndex);
        }

        Debug.Log($"[HologramVideoController] Prepared & Started {source.clip?.name} (Silent Video Mode) at time: {source.time:F2}s");
    }

    private void SeekToOffset(float offsetSeconds)
    {
        if (videoPlayer == null) return;

        double duration = videoPlayer.clip != null ? videoPlayer.clip.length : 100.0;
        double clampedTime = Mathf.Clamp(offsetSeconds, 0f, (float)duration - 0.1f);

        videoPlayer.time = clampedTime;
    }

    private void OnLoopPointReached(VideoPlayer source)
    {
        if (loopVideo && currentChannelIndex >= 0 && currentChannelIndex < channels.Length)
        {
            // Re-seek to the configured channel start offset on loop!
            SeekToOffset(channels[currentChannelIndex].startOffsetSeconds);
            source.Play();

            if (audioSynthesizer != null && !audioSynthesizer.IsAmbienceRunning)
            {
                audioSynthesizer.StartSciFiAmbience(currentChannelIndex);
            }
        }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"[HologramVideoController] VideoPlayer Error: {message}");
        isPreparing = false;
    }

    public void StopPlayback()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        currentChannelIndex = -1;

        if (audioSynthesizer != null)
        {
            audioSynthesizer.StopSciFiAmbience();
        }

        if (monitorDisplay != null)
        {
            monitorDisplay.SetStandbyState();
        }
    }
}
