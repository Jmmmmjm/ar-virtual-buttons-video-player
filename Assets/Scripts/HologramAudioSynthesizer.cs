using System.Collections;
using UnityEngine;

/// <summary>
/// Zero-dependency Procedural Audio Synthesizer for AR Hologram UI.
/// Generates tactile and ambient sci-fi sound effects directly in memory at runtime via AudioClip.Create,
/// eliminating external WAV dependencies and providing spatialized 3D audio playback.
///
/// Features:
/// - Fast electronic data chirps & telemetry micro-bleeps (1500Hz - 3500Hz)
/// - HUD scanner sweeps and harmonic digital pings
/// - Seamless loopable futuristic computer hum / quantum resonance
/// - Tactile mechanical button clicks & channel transition glitch bursts
/// - Melodic ascending crystalline projector boot chime
/// - Multi-voice AudioSource pool allowing simultaneous overlapping playback without cut-offs
/// - Background telemetry ambience routine (StartSciFiAmbience / StopSciFiAmbience)
/// </summary>
public class HologramAudioSynthesizer : MonoBehaviour
{
    private const int SAMPLE_RATE = 44100;

    [Header("=== Voice Pool & Audio Settings ===")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] [Range(2, 16)] private int voicePoolSize = 8;
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 0.90f;
    [SerializeField] [Range(0f, 1f)] private float ambienceVolume = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float humVolume = 0.18f;

    // Procedural Audio Clips
    private AudioClip buttonPressClip;
    private AudioClip channelGlitchClip;
    private AudioClip projectorBootClip;
    private AudioClip computerHumClip;
    private AudioClip[] telemetryChirps;
    private AudioClip[] scannerSweeps;
    private AudioClip[] digitalPings;

    // Upgraded AAA Procedural Sound Clips
    private AudioClip warpSurgeClip;
    private AudioClip[] granularChatterClips;
    private AudioClip[] channelChordClips;
    private AudioClip arcDischargeClip;

    // Expanded Procedural Sci-Fi SFX Clips
    private AudioClip powerDownClip;
    private AudioClip opticsWhistleClip;
    private AudioClip neuralUplinkClip;
    private AudioClip glitchStaticClip;
    private AudioClip subThumpClip;
    private AudioClip thermalDischargeClip;

    // Specialized Channel Sci-Fi SFX Clips
    private AudioClip targetLockClip;
    private AudioClip gyroTickClip;
    private AudioClip steppedArpeggioClip;
    private AudioClip relayClickClip;
    private AudioClip warningChirpClip;
    private AudioClip voltageSpikeClip;

    // Channel-Specific Drone Loops (Cyan 432Hz, Amber 110/220Hz, Red 65/130Hz)
    private AudioClip[] channelHumClips;

    // Active Channel Index for channel-specialized soundscape
    private int currentChannelIndex = 0;
    public int CurrentChannelIndex => currentChannelIndex;

    // Public Getters for Voice Pool & Channel Soundscapes
    public AudioSource[] VoicePool => voicePool;
    public int VoicePoolSize => voicePool != null ? voicePool.Length : voicePoolSize;
    public AudioSource AmbientHumSource => ambientHumSource;
    public AudioClip[] ChannelHumClips => channelHumClips;

    public string ActivePaletteName
    {
        get
        {
            switch (currentChannelIndex)
            {
                case 0: return "Cyan Tactical Telemetry";
                case 1: return "Amber Mainframe Harmonic";
                case 2: return "Red Electronic Warfare Glitch";
                default: return "Default Sci-Fi Ambience";
            }
        }
    }

    public float ActiveDroneFrequency
    {
        get
        {
            switch (currentChannelIndex)
            {
                case 0: return 432.0f; // Crystalline 432Hz quantum resonance
                case 1: return 220.0f; // Warm analog mainframe 220Hz/110Hz hum
                case 2: return 130.0f; // Electric battle grid 130Hz/65Hz buzzing drone
                default: return 120.0f;
            }
        }
    }

    public string ActiveTelemetryDistribution
    {
        get
        {
            switch (currentChannelIndex)
            {
                case 0: return "High-Frequency Telemetry Jitter (Target Lock, Gyro Tick, Neural Uplink, Optics Whistle)";
                case 1: return "Melodic Arpeggio Cascade (Stepped Arpeggio, Relay Click, Digital Ping, Harmonic Chord)";
                case 2: return "Sub-Bass Thumps & Chaotic Glitch (Glitch Static, Warning Chirp, Voltage Spike, Thermal Discharge)";
                default: return "Standard Sci-Fi Telemetry";
            }
        }
    }

    // Real-Time Equalizer & Telemetry Engine State
    private readonly float[] rawSpectrum = new float[256];
    private readonly float[] smoothedBands = new float[16];
    private readonly float[] bandImpulses = new float[16];
    private float lastAudioActivityTime = 0f;

    // Multi-voice AudioSource Pool
    private AudioSource[] voicePool;
    private int nextVoiceIndex = 0;
    private AudioSource ambientHumSource;

    // Runtime state
    private bool isInitialized = false;
    private Coroutine ambienceCoroutine;

    public bool IsAmbienceRunning => ambienceCoroutine != null;

    private void Awake()
    {
        InitializeAudio();
    }

    private void OnDisable()
    {
        StopSciFiAmbience();
    }

    private void OnDestroy()
    {
        StopSciFiAmbience();
    }

    public void InitializeAudio()
    {
        if (isInitialized) return;

        // 1. Primary AudioSource setup
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        ConfigureSpatialAudioSource(audioSource);

        // 2. Multi-Voice AudioSource Pool setup
        Transform poolRoot = transform.Find("AudioVoicePool");
        if (poolRoot == null)
        {
            GameObject poolObj = new GameObject("AudioVoicePool");
            poolObj.transform.SetParent(transform, false);
            poolRoot = poolObj.transform;
        }

        voicePool = new AudioSource[voicePoolSize];
        for (int i = 0; i < voicePoolSize; i++)
        {
            Transform child = poolRoot.Find($"Voice_{i}");
            AudioSource src;
            if (child == null)
            {
                GameObject voiceObj = new GameObject($"Voice_{i}");
                voiceObj.transform.SetParent(poolRoot, false);
                src = voiceObj.AddComponent<AudioSource>();
            }
            else
            {
                src = child.GetComponent<AudioSource>() ?? child.gameObject.AddComponent<AudioSource>();
            }
            ConfigureSpatialAudioSource(src);
            voicePool[i] = src;
        }

        // Dedicated looping AudioSource for ambient quantum computer hum
        Transform humChild = poolRoot.Find("Voice_AmbientHum");
        if (humChild == null)
        {
            GameObject humObj = new GameObject("Voice_AmbientHum");
            humObj.transform.SetParent(poolRoot, false);
            ambientHumSource = humObj.AddComponent<AudioSource>();
        }
        else
        {
            ambientHumSource = humChild.GetComponent<AudioSource>() ?? humChild.gameObject.AddComponent<AudioSource>();
        }
        ConfigureSpatialAudioSource(ambientHumSource);
        ambientHumSource.loop = true;

        // 3. Procedurally synthesize all sound clips into memory
        buttonPressClip = SynthesizeButtonPress(0.08f);
        channelGlitchClip = SynthesizeChannelGlitch(0.25f);
        projectorBootClip = SynthesizeProjectorBoot(0.75f);
        computerHumClip = SynthesizeComputerHum(2.0f);
        telemetryChirps = SynthesizeTelemetryChirps();
        scannerSweeps = SynthesizeScannerSweeps();
        digitalPings = SynthesizeDigitalPings();

        // Elevated AAA Procedural Audio Synthesis (Zero WAV dependencies)
        warpSurgeClip = SynthesizeWarpSurge(1.05f);
        granularChatterClips = SynthesizeGranularChatter();
        channelChordClips = SynthesizeChannelChords();
        arcDischargeClip = SynthesizeArcDischarge(0.18f);

        // Expanded Over-The-Top Procedural Sci-Fi Synthesizers (Zero WAV dependencies)
        powerDownClip = SynthesizePowerDown(0.95f);
        opticsWhistleClip = SynthesizeOpticsWhistle(0.22f);
        neuralUplinkClip = SynthesizeNeuralUplink(0.085f);
        glitchStaticClip = SynthesizeGlitchStatic(0.16f);
        subThumpClip = SynthesizeSubThump(0.38f);
        thermalDischargeClip = SynthesizeThermalDischarge(0.24f);

        // Specialized Channel Procedural Sci-Fi Audio Clips
        targetLockClip = SynthesizeTargetLock(0.070f);
        gyroTickClip = SynthesizeGyroTick(0.025f);
        steppedArpeggioClip = SynthesizeSteppedArpeggio(0.140f);
        relayClickClip = SynthesizeRelayClick(0.045f);
        warningChirpClip = SynthesizeWarningChirp(0.095f);
        voltageSpikeClip = SynthesizeVoltageSpike(0.085f);

        // Channel-Specific Looping Ambient Drones (Cyan 432Hz, Amber 110/220Hz, Red 65/130Hz)
        channelHumClips = SynthesizeChannelHums(2.0f);
        computerHumClip = channelHumClips[0];

        isInitialized = true;
    }

    private void ConfigureSpatialAudioSource(AudioSource src)
    {
        src.playOnAwake = false;
        src.spatialBlend = 1.0f; // 100% 3D spatialized
        src.minDistance = 0.05f;
        src.maxDistance = 3.5f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.dopplerLevel = 0.0f;
    }

    #region Voice Pool Playback Engine
    private void PlayVoice(AudioClip clip, float volume, float pitch)
    {
        if (clip == null) return;
        if (!isInitialized) InitializeAudio();

        AudioSource chosenVoice = null;

        // Find an idle voice
        if (voicePool != null && voicePool.Length > 0)
        {
            for (int i = 0; i < voicePool.Length; i++)
            {
                if (!voicePool[i].isPlaying)
                {
                    chosenVoice = voicePool[i];
                    break;
                }
            }

            // If all busy, round-robin steal oldest voice
            if (chosenVoice == null)
            {
                chosenVoice = voicePool[nextVoiceIndex];
                nextVoiceIndex = (nextVoiceIndex + 1) % voicePool.Length;
            }
        }
        else
        {
            chosenVoice = audioSource;
        }

        if (chosenVoice != null)
        {
            chosenVoice.pitch = pitch;
            chosenVoice.volume = Mathf.Clamp01(volume * masterVolume);
            chosenVoice.clip = clip;
            chosenVoice.Play();
        }
    }
    #endregion

    #region Public Playback & Telemetry Triggers
    public void PlayButtonPress()
    {
        PlayVoice(buttonPressClip, 0.85f, Random.Range(0.96f, 1.04f));
        InjectEqualizerImpulse(0.70f, 6, 12);
    }

    public void PlayChannelGlitch()
    {
        PlayVoice(channelGlitchClip, 0.75f, Random.Range(0.95f, 1.05f));
        PlayGlitchStatic();
        InjectEqualizerImpulse(0.85f, 2, 8);
    }

    public void PlayProjectorBoot()
    {
        PlayVoice(projectorBootClip, 0.90f, 1.0f);
        PlayWarpSurge();
        PlayOpticsWhistle();
        InjectEqualizerImpulse(1.0f, 0, 15);
    }

    /// <summary>
    /// Plays the Sub-Bass Cinematic Warp Surge: Deep resonant 45Hz–120Hz exponential sine sweep on power boot.
    /// </summary>
    public void PlayWarpSurge()
    {
        if (warpSurgeClip == null && !isInitialized) InitializeAudio();
        PlayVoice(warpSurgeClip, 0.95f, 1.0f);
        InjectEqualizerImpulse(0.95f, 0, 4);
    }

    /// <summary>
    /// Plays the Hologram Power-Down / De-Rez Dissolve SFX:
    /// Descending cyber-resonant warp dissipation (220Hz down to 35Hz with exponential decay and analog saturation).
    /// </summary>
    public void PlayPowerDown()
    {
        if (powerDownClip == null && !isInitialized) InitializeAudio();
        if (powerDownClip != null)
        {
            PlayVoice(powerDownClip, 0.92f, Random.Range(0.98f, 1.02f));
            InjectEqualizerImpulse(0.95f, 0, 6);
        }
    }

    /// <summary>
    /// Plays Optics Calibration & Laser Whistle SFX:
    /// Fast parabolic frequency sweep (2800Hz -> 5800Hz -> 3400Hz) simulating laser emitter focusing lenses.
    /// </summary>
    public void PlayOpticsWhistle()
    {
        if (opticsWhistleClip == null && !isInitialized) InitializeAudio();
        if (opticsWhistleClip != null)
        {
            PlayVoice(opticsWhistleClip, 0.78f, Random.Range(0.97f, 1.03f));
            InjectEqualizerImpulse(0.75f, 10, 15);
        }
    }

    /// <summary>
    /// Plays Neural Data-Burst / Encrypted Uplink SFX:
    /// High-speed stochastic FSK / M-ary dual-carrier cipher telemetry burst with ringing resonant filter.
    /// </summary>
    public void PlayNeuralUplink()
    {
        if (neuralUplinkClip == null && !isInitialized) InitializeAudio();
        if (neuralUplinkClip != null)
        {
            float pitch = Random.Range(0.95f, 1.08f);
            float vol = Random.Range(0.35f, 0.60f) * ambienceVolume;
            PlayVoice(neuralUplinkClip, vol, pitch);
            InjectEqualizerImpulse(0.85f, 8, 15);
        }
    }

    /// <summary>
    /// Plays Holographic Glitch Static Burst SFX:
    /// Pink noise burst with sample-and-hold gating and bitcrushed harmonics.
    /// </summary>
    public void PlayGlitchStatic()
    {
        if (glitchStaticClip == null && !isInitialized) InitializeAudio();
        if (glitchStaticClip != null)
        {
            PlayVoice(glitchStaticClip, 0.82f, Random.Range(0.94f, 1.06f));
            InjectEqualizerImpulse(0.90f, 2, 14);
        }
    }

    /// <summary>
    /// Plays Sub-Harmonic Bass Drop / Impact Thump SFX:
    /// Punchy 35Hz sub-bass transient punch.
    /// </summary>
    public void PlaySubThump()
    {
        if (subThumpClip == null && !isInitialized) InitializeAudio();
        if (subThumpClip != null)
        {
            PlayVoice(subThumpClip, 0.95f, Random.Range(0.97f, 1.03f));
            InjectEqualizerImpulse(1.0f, 0, 3);
        }
    }

    /// <summary>
    /// Plays Thermal Plasma Discharge SFX:
    /// Crackling plasma filament sputtering with random micro-sparks.
    /// </summary>
    public void PlayThermalDischarge()
    {
        if (thermalDischargeClip == null && !isInitialized) InitializeAudio();
        if (thermalDischargeClip != null)
        {
            PlayVoice(thermalDischargeClip, 0.88f, Random.Range(0.95f, 1.05f));
            InjectEqualizerImpulse(0.85f, 6, 15);
        }
    }

    /// <summary>
    /// Plays the Harmonic Musical Triad Chord for the specified channel:
    /// - Ch 0 (Cyan): E Major 9 chord shimmer (330Hz, 415Hz, 494Hz, 622Hz)
    /// - Ch 1 (Amber): D Major triad shimmer (293Hz, 370Hz, 440Hz)
    /// - Ch 2 (Red): F# Minor triad glitch chord (370Hz, 440Hz, 554Hz)
    /// Layers the Sub-Harmonic Impact Thump and Thermal Plasma Discharge for maximum cinematic punch.
    /// </summary>
    public void PlayChannelChord(int channelIndex)
    {
        if (channelChordClips == null && !isInitialized) InitializeAudio();
        if (channelChordClips == null || channelChordClips.Length == 0) return;

        int idx = Mathf.Clamp(channelIndex, 0, channelChordClips.Length - 1);
        if (channelChordClips[idx] != null)
        {
            PlayVoice(channelChordClips[idx], 0.88f, 1.0f);
            InjectEqualizerImpulse(0.90f, 3, 10);
        }

        // Layer Sub-Harmonic Bass Drop and Thermal Plasma Discharge for maximum cinematic punch
        PlaySubThump();
        PlayThermalDischarge();
    }

    /// <summary>
    /// Plays the Channel Switch Arc Discharge: Crisp high-voltage electrical spark/arc crackle.
    /// </summary>
    public void PlayArcDischarge()
    {
        if (arcDischargeClip == null && !isInitialized) InitializeAudio();
        if (arcDischargeClip != null)
        {
            PlayVoice(arcDischargeClip, 0.82f, Random.Range(0.95f, 1.06f));
            InjectEqualizerImpulse(0.85f, 7, 15);
        }
    }

    /// <summary>
    /// Plays a stochastic Granular Quantum Telemetry micro-packet bleep (1800Hz–4200Hz).
    /// </summary>
    public void PlayGranularChatter()
    {
        if (granularChatterClips == null && !isInitialized) InitializeAudio();
        if (granularChatterClips == null || granularChatterClips.Length == 0) return;

        int idx = Random.Range(0, granularChatterClips.Length);
        float pitch = Random.Range(0.92f, 1.15f);
        float vol = Random.Range(0.35f, 0.60f) * ambienceVolume;
        PlayVoice(granularChatterClips[idx], vol, pitch);
        InjectEqualizerImpulse(0.75f, 9, 15);
    }

    public void PlayDataChirp()
    {
        if (telemetryChirps == null || telemetryChirps.Length == 0) return;
        int idx = Random.Range(0, telemetryChirps.Length);
        float pitch = Random.Range(0.90f, 1.25f);
        float vol = Random.Range(0.30f, 0.55f) * ambienceVolume;
        PlayVoice(telemetryChirps[idx], vol, pitch);
        InjectEqualizerImpulse(0.60f, 8, 14);
    }

    public void PlayScannerSweep()
    {
        if (scannerSweeps == null || scannerSweeps.Length == 0) return;
        int idx = Random.Range(0, scannerSweeps.Length);
        float pitch = Random.Range(0.92f, 1.12f);
        float vol = Random.Range(0.25f, 0.45f) * ambienceVolume;
        PlayVoice(scannerSweeps[idx], vol, pitch);
        InjectEqualizerImpulse(0.65f, 4, 11);
    }

    public void PlayDigitalPing()
    {
        if (digitalPings == null || digitalPings.Length == 0) return;
        int idx = Random.Range(0, digitalPings.Length);
        float pitch = Random.Range(0.94f, 1.15f);
        float vol = Random.Range(0.25f, 0.50f) * ambienceVolume;
        PlayVoice(digitalPings[idx], vol, pitch);
        InjectEqualizerImpulse(0.70f, 7, 13);
    }

    /// <summary>
    /// Plays Tactical Target Lock Chirp: Fast dual-tone military lock-on sweep (3200Hz & 4800Hz rapid chirps).
    /// </summary>
    public void PlayTargetLock(float volume = -1f)
    {
        if (targetLockClip == null && !isInitialized) InitializeAudio();
        if (targetLockClip != null)
        {
            float pitch = Random.Range(0.97f, 1.03f);
            float vol = (volume >= 0f) ? volume : Random.Range(0.40f, 0.65f) * ambienceVolume;
            PlayVoice(targetLockClip, vol, pitch);
            InjectEqualizerImpulse(0.85f, 10, 15);
        }
    }

    /// <summary>
    /// Plays Quantum Gyro-Tick: Ultra-fast high-frequency optical tick (5400Hz 3ms click) for clock sync.
    /// </summary>
    public void PlayGyroTick(float volume = -1f)
    {
        if (gyroTickClip == null && !isInitialized) InitializeAudio();
        if (gyroTickClip != null)
        {
            float pitch = Random.Range(0.98f, 1.02f);
            float vol = (volume >= 0f) ? volume : Random.Range(0.35f, 0.55f) * ambienceVolume;
            PlayVoice(gyroTickClip, vol, pitch);
            InjectEqualizerImpulse(0.65f, 12, 15);
        }
    }

    /// <summary>
    /// Plays Stepped Digital Arpeggio: Rapid 4-note ascending digital arpeggio (C6, E6, G6, B6 harmonic sequence).
    /// </summary>
    public void PlaySteppedArpeggio(float volume = -1f)
    {
        if (steppedArpeggioClip == null && !isInitialized) InitializeAudio();
        if (steppedArpeggioClip != null)
        {
            float pitch = Random.Range(0.98f, 1.02f);
            float vol = (volume >= 0f) ? volume : Random.Range(0.42f, 0.65f) * ambienceVolume;
            PlayVoice(steppedArpeggioClip, vol, pitch);
            InjectEqualizerImpulse(0.80f, 7, 14);
        }
    }

    /// <summary>
    /// Plays Mainframe Relay Matrix Click: Crisp tactile mechanical-electronic relay latching click.
    /// </summary>
    public void PlayRelayClick(float volume = -1f)
    {
        if (relayClickClip == null && !isInitialized) InitializeAudio();
        if (relayClickClip != null)
        {
            float pitch = Random.Range(0.95f, 1.05f);
            float vol = (volume >= 0f) ? volume : Random.Range(0.40f, 0.62f) * ambienceVolume;
            PlayVoice(relayClickClip, vol, pitch);
            InjectEqualizerImpulse(0.70f, 4, 10);
        }
    }

    /// <summary>
    /// Plays Combat Warning Micro-Siren: Rapid two-tone oscillating warble (1200Hz - 2400Hz at 35Hz rate).
    /// </summary>
    public void PlayWarningChirp(float volume = -1f)
    {
        if (warningChirpClip == null && !isInitialized) InitializeAudio();
        if (warningChirpClip != null)
        {
            float pitch = Random.Range(0.96f, 1.04f);
            float vol = (volume >= 0f) ? volume : Random.Range(0.42f, 0.68f) * ambienceVolume;
            PlayVoice(warningChirpClip, vol, pitch);
            InjectEqualizerImpulse(0.85f, 6, 13);
        }
    }

    /// <summary>
    /// Plays High-Voltage Arc Spike: Sharp electrostatic zap with micro-echo.
    /// </summary>
    public void PlayVoltageSpike(float volume = -1f)
    {
        if (voltageSpikeClip == null && !isInitialized) InitializeAudio();
        if (voltageSpikeClip != null)
        {
            float pitch = Random.Range(0.95f, 1.06f);
            float vol = (volume >= 0f) ? volume : Random.Range(0.45f, 0.70f) * ambienceVolume;
            PlayVoice(voltageSpikeClip, vol, pitch);
            InjectEqualizerImpulse(0.90f, 8, 15);
        }
    }

    /// <summary>
    /// Fills a 16-element float array with normalized energy levels [0.0 - 1.0] across 16 frequency bands,
    /// combining active audio envelope, multi-octave harmonic modulation, and live procedural chatter
    /// so the 16-band holographic equalizer visualizer pulses rhythmically in real time!
    /// </summary>
    public void GetEqualizerLevels(float[] outBands)
    {
        if (outBands == null || outBands.Length == 0) return;
        int count = Mathf.Min(outBands.Length, 16);

        float dt = Time.deltaTime > 0f ? Time.deltaTime : 0.016f;
        float t = Time.time;

        // 1. Detect active voice playback envelope across audio source pool and ambient hum
        bool anyVoicePlaying = (audioSource != null && audioSource.isPlaying) ||
                               (ambientHumSource != null && ambientHumSource.isPlaying);
        if (!anyVoicePlaying && voicePool != null)
        {
            for (int v = 0; v < voicePool.Length; v++)
            {
                if (voicePool[v] != null && voicePool[v].isPlaying)
                {
                    anyVoicePlaying = true;
                    lastAudioActivityTime = t;
                    break;
                }
            }
        }
        else if (anyVoicePlaying)
        {
            lastAudioActivityTime = t;
        }

        float activityEnvelope = Mathf.Clamp01(1.0f - (t - lastAudioActivityTime) * 0.5f);
        if (anyVoicePlaying) activityEnvelope = Mathf.Max(activityEnvelope, 0.65f);

        // 2. Real-time audio FFT spectrum data sampling (when listener is active)
        bool hasSpectrum = false;
        if (AudioListener.pause == false)
        {
            try
            {
                AudioListener.GetSpectrumData(rawSpectrum, 0, FFTWindow.BlackmanHarris);
                for (int s = 0; s < 32; s++)
                {
                    if (rawSpectrum[s] > 0.0001f)
                    {
                        hasSpectrum = true;
                        break;
                    }
                }
            }
            catch
            {
                hasSpectrum = false;
            }
        }

        // 3. Compute normalized level for each of the 16 frequency bands
        for (int i = 0; i < count; i++)
        {
            // Decay procedural impulse from sound events
            bandImpulses[i] = Mathf.MoveTowards(bandImpulses[i], 0f, dt * 2.8f);

            // Spectrum contribution
            float spectrumLevel = 0f;
            if (hasSpectrum)
            {
                int startBin = Mathf.Clamp(Mathf.RoundToInt(Mathf.Pow(2f, i * (8f / 16f))), 0, rawSpectrum.Length - 2);
                int endBin = Mathf.Clamp(Mathf.RoundToInt(Mathf.Pow(2f, (i + 1) * (8f / 16f))), startBin + 1, rawSpectrum.Length);
                float sum = 0f;
                for (int b = startBin; b < endBin; b++) sum += rawSpectrum[b];
                spectrumLevel = Mathf.Clamp01((sum / (endBin - startBin)) * 45f);
            }

            // Multi-octave harmonic modulation simulating real-time sci-fi frequency dynamics
            float harmonicMod;
            if (i < 4)
            {
                // Sub & Bass bands: slow, resonant, chest-thumping pulses (2.4Hz & 1.2Hz)
                harmonicMod = 0.35f + 0.30f * Mathf.Sin(t * 2.4f + i * 0.4f) * Mathf.Cos(t * 1.2f);
            }
            else if (i < 10)
            {
                // Mid bands: rhythmic harmonic wave (3.6Hz & 5.2Hz)
                harmonicMod = 0.30f + 0.30f * Mathf.Sin(t * 3.6f + i * 0.5f) + 0.15f * Mathf.Cos(t * 5.2f + i);
            }
            else
            {
                // High & Telemetry bands: rapid micro-flutter (6.8Hz & 8.4Hz) representing stochastic data decoding
                harmonicMod = 0.25f + 0.30f * Mathf.Sin(t * 6.8f + i * 0.7f) * Mathf.Cos(t * 3.4f);
            }

            // Channel-specific energy profile modulation:
            // - Channel 0: High-frequency telemetry jitter across bands 8–15
            // - Channel 1: Melodic arpeggio cascades bouncing across mid bands 4–11
            // - Channel 2: Heavy sub-bass thumps and chaotic glitch spikes across bands 0–7 and 12–15
            float channelMod = 0f;
            switch (currentChannelIndex)
            {
                case 0:
                    if (i >= 8)
                    {
                        float jitterSpeed = 16.0f + (i - 8) * 2.2f;
                        float jitter = Mathf.Sin(t * jitterSpeed + i * 1.7f) * 0.22f +
                                       (Mathf.PerlinNoise(t * 24f, i * 1.8f) - 0.5f) * 0.26f;
                        channelMod = Mathf.Max(0f, 0.25f + jitter);
                    }
                    break;

                case 1:
                    if (i >= 4 && i <= 11)
                    {
                        float cascadePhase = Mathf.PingPong(t * 6.2f, 7.0f); // Bounces smoothly 0 to 7 across 8 mid bands
                        float dist = Mathf.Abs((i - 4) - cascadePhase);
                        float cascade = Mathf.Clamp01(1.0f - dist * 0.55f) * 0.42f;
                        float harmony = 0.15f * Mathf.Sin(t * 4.8f + (i - 4) * 0.785f);
                        channelMod = cascade + harmony;
                    }
                    break;

                case 2:
                    if (i <= 7)
                    {
                        // Heavy sub-bass thumps (punchy 3.2Hz heartbeat thump)
                        float bassPulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 3.2f)), 6f) * 0.50f;
                        float subRumble = 0.20f * Mathf.Sin(t * 1.8f + i * 0.4f);
                        channelMod = bassPulse + subRumble;
                    }
                    else if (i >= 12)
                    {
                        // Chaotic glitch spikes jumping across bands 12-15
                        bool isSpike = Mathf.PerlinNoise(t * 28f + i * 5.3f, 0.42f) > 0.65f;
                        float spike = isSpike ? (0.35f + 0.40f * Mathf.Sin(t * 45f + i * 3.14f)) : 0f;
                        channelMod = Mathf.Clamp01(spike);
                    }
                    break;
            }

            // Combine active audio envelope, spectrum, harmonic modulation, channel profile, and live procedural chatter impulses
            float targetLevel = Mathf.Clamp01(
                spectrumLevel * 0.60f +
                (harmonicMod + channelMod) * activityEnvelope * 0.55f +
                bandImpulses[i] * 0.85f
            );

            // Responsive filter: snappy attack (28x) for transient clicks, smooth analog release (9x)
            float speed = (targetLevel > smoothedBands[i]) ? 28f : 9f;
            smoothedBands[i] = Mathf.MoveTowards(smoothedBands[i], targetLevel, dt * speed);

            outBands[i] = Mathf.Clamp01(smoothedBands[i]);
        }
    }

    private void InjectEqualizerImpulse(float intensity, int minBand, int maxBand)
    {
        lastAudioActivityTime = Time.time;
        minBand = Mathf.Clamp(minBand, 0, 15);
        maxBand = Mathf.Clamp(maxBand, minBand, 15);
        for (int i = minBand; i <= maxBand; i++)
        {
            float spread = 1.0f - Mathf.Abs(i - (minBand + maxBand) * 0.5f) / ((maxBand - minBand + 1) * 0.5f + 0.01f);
            bandImpulses[i] = Mathf.Max(bandImpulses[i], intensity * Mathf.Clamp01(spread + 0.35f));
        }
    }
    #endregion

    #region Sci-Fi Background Ambience Telemetry Routine
    /// <summary>
    /// Starts channel-specific background sci-fi telemetry routine:
    /// - Loops channel-tailored ambient quantum computer hum / reactor drone
    ///   * Ch 0 (Cyan): 432Hz high crystalline quantum resonance
    ///   * Ch 1 (Amber): 110Hz/220Hz warm analog mainframe reactor hum
    ///   * Ch 2 (Red): 65Hz/130Hz buzzing electric battle grid drone
    /// - Fires high-density micro-telemetry at rapid intervals (0.04s to 0.14s)
    /// - Multi-voice burst polyphony firing 2-3 overlapping voices concurrently or in cascades
    /// - Channel-specific telemetry palettes:
    ///   * Ch 0: Tactical Target Locks, Gyro-Ticks, Neural Ciphers, Optics Laser sweeps, Crystal pings
    ///   * Ch 1: Stepped Digital Arpeggios, Relay Matrix Clicks, Parities, Harmonic pings, Tape pulses
    ///   * Ch 2: Glitch Static bursts, Combat Warning chirps, Voltage Spikes, Thermal Plasma, Sub-Thumps
    /// </summary>
    public void StartSciFiAmbience()
    {
        StartSciFiAmbience(currentChannelIndex);
    }

    public void StartSciFiAmbience(int channelIndex)
    {
        if (!isInitialized) InitializeAudio();

        currentChannelIndex = Mathf.Clamp(channelIndex, 0, 2);

        // Update ambient drone frequency & timbre per channel
        if (ambientHumSource != null && channelHumClips != null && currentChannelIndex < channelHumClips.Length)
        {
            AudioClip targetHum = channelHumClips[currentChannelIndex];
            if (targetHum != null)
            {
                if (ambientHumSource.clip != targetHum || !ambientHumSource.isPlaying)
                {
                    ambientHumSource.clip = targetHum;
                    ambientHumSource.volume = humVolume * masterVolume;
                    ambientHumSource.pitch = 1.0f;
                    ambientHumSource.Play();
                }
            }
        }

        if (ambienceCoroutine == null)
        {
            ambienceCoroutine = StartCoroutine(SciFiAmbienceRoutine());
        }
    }

    /// <summary>
    /// Stops the sci-fi background telemetry ambience routine and halts ambient computer hum.
    /// </summary>
    public void StopSciFiAmbience()
    {
        if (ambienceCoroutine != null)
        {
            StopCoroutine(ambienceCoroutine);
            ambienceCoroutine = null;
        }

        if (ambientHumSource != null && ambientHumSource.isPlaying)
        {
            ambientHumSource.Stop();
        }
    }

    private IEnumerator SciFiAmbienceRoutine()
    {
        // Initial soft buffer so it doesn't clash with boot chime or glitch burst
        yield return new WaitForSeconds(0.18f);

        while (true)
        {
            // Rapid irregular interval between 0.04s and 0.14s for high-density Hollywood/AAA sci-fi telemetry
            float interval = Random.Range(0.04f, 0.14f);
            yield return new WaitForSeconds(interval);

            // Primary voice: Trigger micro-sound aligned with active channel soundscape
            TriggerChannelMicroSound(currentChannelIndex);

            // Multi-voice burst polyphony: Frequently fire 2 to 3 micro-sounds concurrently or in cascades
            // 55% chance to fire a 2nd overlapping voice
            if (Random.value < 0.55f)
            {
                // Micro-stagger cascade of 15-35ms (or concurrent instant fire)
                if (Random.value < 0.70f)
                {
                    yield return new WaitForSeconds(Random.Range(0.015f, 0.035f));
                }
                TriggerChannelMicroSound(currentChannelIndex);

                // 28% chance to cascade a 3rd overlapping voice in the 8-voice AudioSource pool
                if (Random.value < 0.28f)
                {
                    yield return new WaitForSeconds(Random.Range(0.015f, 0.035f));
                    TriggerChannelMicroSound(currentChannelIndex);
                }
            }
        }
    }

    /// <summary>
    /// Fires channel-specific micro-telemetry sound according to specialized sound palettes.
    /// </summary>
    private void TriggerChannelMicroSound(int channel)
    {
        float roll = Random.value;
        switch (channel)
        {
            case 0:
                // Channel 0 (Cyan - Tactical CyberTech):
                // Dominated by Tactical Target Locks, Gyro-Ticks, FSK Neural Cipher telemetry, Optics Laser sweeps, and Crystal Bell pings.
                // Pristine, ultra-fast, military satellite decoding feel.
                if (roll < 0.24f)
                {
                    PlayTargetLock();
                }
                else if (roll < 0.48f)
                {
                    PlayGyroTick();
                }
                else if (roll < 0.68f)
                {
                    PlayNeuralUplink();
                }
                else if (roll < 0.83f)
                {
                    PlayOpticsWhistle();
                }
                else if (roll < 0.94f)
                {
                    PlayDigitalPing();
                }
                else
                {
                    PlayGranularChatter();
                }
                break;

            case 1:
                // Channel 1 (Amber - Mainframe Futuristic UI):
                // Dominated by Stepped Digital Arpeggios, Relay Matrix Clicks, Dual-Tone Parities, Harmonic Pings, and Tape-Head pulses.
                // Melodic, calculating, warm retro-future supercomputer feel.
                if (roll < 0.26f)
                {
                    PlaySteppedArpeggio();
                }
                else if (roll < 0.52f)
                {
                    PlayRelayClick();
                }
                else if (roll < 0.72f)
                {
                    PlayDataChirp();
                }
                else if (roll < 0.88f)
                {
                    PlayDigitalPing();
                }
                else if (roll < 0.95f)
                {
                    PlayGranularChatter();
                }
                else
                {
                    PlayButtonPress();
                }
                break;

            case 2:
                // Channel 2 (Red - Electronic Warfare Screen 03):
                // Dominated by Glitch Static bursts, Combat Warning chirps, Voltage Spikes, Thermal Plasma crackles, Sub-Harmonic Thumps, and Stutter telemetry.
                // Aggressive, chaotic, high-energy electronic warfare feel.
                if (roll < 0.24f)
                {
                    PlayGlitchStatic();
                }
                else if (roll < 0.46f)
                {
                    PlayWarningChirp();
                }
                else if (roll < 0.64f)
                {
                    PlayVoltageSpike();
                }
                else if (roll < 0.80f)
                {
                    PlayThermalDischarge();
                }
                else if (roll < 0.90f)
                {
                    PlaySubThump();
                }
                else
                {
                    PlayGranularChatter();
                }
                break;

            default:
                PlayDataChirp();
                break;
        }
    }
    #endregion

    #region Procedural Audio Synthesis Algorithms
    // Tactile button click: 2ms transient click impulse + rapid frequency drop + exponential decay
    private AudioClip SynthesizeButtonPress(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // Mechanical transient click impulse in first 3ms
            float click = (t < 0.003f) ? (Random.value * 2f - 1f) * 0.4f : 0f;

            // Frequency sweeps from 2400Hz down to 1000Hz
            float freq = Mathf.Lerp(2400f, 1000f, Mathf.Sqrt(progress));
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
            float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t)) * 0.2f;

            // Sharp exponential decay
            float env = Mathf.Exp(-22.0f * progress);

            samples[i] = Mathf.Clamp((sine * 0.7f + square + click) * env, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_BtnPress", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Sci-fi channel switch glitch burst: Downward pitch slide + digital sample-and-hold noise
    private AudioClip SynthesizeChannelGlitch(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        float heldNoise = 0f;
        int noiseHoldSamples = 20;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // Pitch drop 2000Hz down to 180Hz
            float freq = Mathf.Lerp(2000f, 180f, progress);
            float carrier = Mathf.Sin(2f * Mathf.PI * freq * t);

            // Bitcrushed stepped noise
            if (i % noiseHoldSamples == 0)
            {
                heldNoise = (Random.value * 2f - 1f) * 0.35f;
            }

            // Gated stutter modulation
            float stutter = Mathf.Sin(2f * Mathf.PI * 45f * t) > 0f ? 1.0f : 0.3f;
            float env = Mathf.Pow(1.0f - progress, 1.7f);

            samples[i] = Mathf.Clamp((carrier * 0.65f + heldNoise) * stutter * env, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_Glitch", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Melodic ascending projector boot chime: 4-note ascending futuristic crystalline arpeggio
    private AudioClip SynthesizeProjectorBoot(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        // Musical frequencies: C5, E5, G5, C6 (with C7 crystal octave)
        float[] noteFreqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
        float[] noteTimes = new float[] { 0.00f, 0.12f, 0.24f, 0.36f };

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float totalTone = 0f;

            for (int n = 0; n < noteFreqs.Length; n++)
            {
                if (t >= noteTimes[n])
                {
                    float noteT = t - noteTimes[n];
                    float f = noteFreqs[n];

                    // Fundamental + subtle 2nd harmonic
                    float tone = Mathf.Sin(2f * Mathf.PI * f * noteT) * 0.6f +
                                 Mathf.Sin(2f * Mathf.PI * (f * 2f) * noteT) * 0.25f;

                    // Extra crystal shimmer for final top note
                    if (n == noteFreqs.Length - 1)
                    {
                        tone += Mathf.Sin(2f * Mathf.PI * (f * 3f) * noteT) * 0.15f;
                    }

                    // Smooth attack (15ms) + exponential ringing decay
                    float attack = Mathf.Clamp01(noteT / 0.015f);
                    float decay = Mathf.Exp(-7.5f * noteT);
                    totalTone += tone * attack * decay;
                }
            }

            samples[i] = Mathf.Clamp(totalTone * 0.65f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_Boot", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Seamless loopable futuristic computer hum / quantum resonance (2.0s loop)
    // All frequencies are exact multiples of 0.5Hz (1/2.0s) guaranteeing 100% click-free loop points
    private AudioClip SynthesizeComputerHum(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;

            // Sub bass resonance (60Hz = 120 cycles)
            float sub = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.40f;

            // Core computer drone (120Hz = 240 cycles)
            float core = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.30f;

            // Soft quantum harmonic (240Hz = 480 cycles)
            float harmonic = Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.15f;

            // Binaural detune pulsation (122Hz = 244 cycles; 2Hz undulating beat)
            float binaural = Mathf.Sin(2f * Mathf.PI * 122f * t) * 0.12f;

            // Subtle high shimmering overtone (720Hz = 1440 cycles) with 1Hz amplitude modulation
            float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1.0f * t);
            float shimmer = Mathf.Sin(2f * Mathf.PI * 720f * t) * 0.04f * lfo;

            samples[i] = Mathf.Clamp(sub + core + harmonic + binaural + shimmer, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_ComputerHum", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Fast electronic data chirps / telemetry micro-bleeps (1500Hz - 3500Hz)
    private AudioClip[] SynthesizeTelemetryChirps()
    {
        AudioClip[] clips = new AudioClip[5];

        // 1. Ascending dual-tone chirp (1800Hz -> 3200Hz, 0.045s)
        clips[0] = GenerateChirp("Holo_Chirp_Ascend", 0.045f, (t, p) =>
        {
            float freq = Mathf.Lerp(1800f, 3200f, p);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.8f + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.2f;
            return tone * Mathf.Exp(-22f * p);
        });

        // 2. Descending telemetry pip (3400Hz -> 1700Hz, 0.040s)
        clips[1] = GenerateChirp("Holo_Chirp_Descend", 0.040f, (t, p) =>
        {
            float freq = Mathf.Lerp(3400f, 1700f, p);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-24f * p);
        });

        // 3. Fast FM micro-bleep (Carrier 2800Hz, Modulator 380Hz, 0.050s)
        clips[2] = GenerateChirp("Holo_Chirp_FMPulse", 0.050f, (t, p) =>
        {
            float mod = Mathf.Sin(2f * Mathf.PI * 380f * t) * 0.6f;
            float phase = 2f * Mathf.PI * 2800f * t + mod;
            return Mathf.Sin(phase) * Mathf.Exp(-16f * p);
        });

        // 4. Double-pip telemetry burst (2300Hz & 3100Hz pulse pair, 0.065s)
        clips[3] = GenerateChirp("Holo_Chirp_DoublePip", 0.065f, (t, p) =>
        {
            if (t < 0.025f)
            {
                float p1 = t / 0.025f;
                return Mathf.Sin(2f * Mathf.PI * 2300f * t) * Mathf.Exp(-20f * p1);
            }
            else if (t > 0.035f)
            {
                float p2 = (t - 0.035f) / 0.030f;
                return Mathf.Sin(2f * Mathf.PI * 3100f * (t - 0.035f)) * Mathf.Exp(-20f * p2);
            }
            return 0f;
        });

        // 5. Tri-tone stepped data packet (2000Hz, 2650Hz, 3350Hz, 0.060s)
        clips[4] = GenerateChirp("Holo_Chirp_TriStep", 0.060f, (t, p) =>
        {
            float freq = (p < 0.33f) ? 2000f : (p < 0.66f ? 2650f : 3350f);
            float stepProgress = (p % 0.33f) / 0.33f;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-14f * stepProgress);
        });

        return clips;
    }

    private AudioClip GenerateChirp(string name, float duration, System.Func<float, float, float> sampleFunc)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;
            samples[i] = Mathf.Clamp(sampleFunc(t, progress), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // HUD scanner sweeps
    private AudioClip[] SynthesizeScannerSweeps()
    {
        AudioClip[] clips = new AudioClip[2];

        // 1. Upward resonant scanner sweep (850Hz -> 2700Hz, 0.22s)
        int count1 = Mathf.RoundToInt(SAMPLE_RATE * 0.22f);
        float[] samples1 = new float[count1];
        for (int i = 0; i < count1; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / count1;
            float freq = Mathf.Lerp(850f, 2700f, Mathf.Pow(progress, 1.4f));
            float carrier = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.2f;
            float am = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 35f * t);
            float env = Mathf.Sin(progress * Mathf.PI);
            samples1[i] = Mathf.Clamp(carrier * am * env, -1f, 1f);
        }
        clips[0] = AudioClip.Create("Holo_Scanner_Up", count1, 1, SAMPLE_RATE, false);
        clips[0].SetData(samples1, 0);

        // 2. Downward radar harmonic sweep (2800Hz -> 900Hz, 0.20s)
        int count2 = Mathf.RoundToInt(SAMPLE_RATE * 0.20f);
        float[] samples2 = new float[count2];
        for (int i = 0; i < count2; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / count2;
            float freq = Mathf.Lerp(2800f, 900f, Mathf.Pow(progress, 0.8f));
            float carrier = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.75f + Mathf.Sin(3f * Mathf.PI * freq * t) * 0.25f;
            float env = Mathf.Sin(progress * Mathf.PI * 0.95f);
            samples2[i] = Mathf.Clamp(carrier * env, -1f, 1f);
        }
        clips[1] = AudioClip.Create("Holo_Scanner_Down", count2, 1, SAMPLE_RATE, false);
        clips[1].SetData(samples2, 0);

        return clips;
    }

    // Harmonic digital pings (pristine bell/sonar pings)
    private AudioClip[] SynthesizeDigitalPings()
    {
        AudioClip[] clips = new AudioClip[3];

        // 1. Crystal HUD Ping (Fundamental 1760Hz [A6] + 3520Hz [A7] + 2640Hz [E7], 0.28s)
        int count1 = Mathf.RoundToInt(SAMPLE_RATE * 0.28f);
        float[] samples1 = new float[count1];
        for (int i = 0; i < count1; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / count1;
            float tone = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.55f +
                         Mathf.Sin(2f * Mathf.PI * 3520f * t) * 0.28f +
                         Mathf.Sin(2f * Mathf.PI * 2640f * t) * 0.17f;
            float env = Mathf.Exp(-14f * progress);
            samples1[i] = Mathf.Clamp(tone * env, -1f, 1f);
        }
        clips[0] = AudioClip.Create("Holo_Ping_Crystal", count1, 1, SAMPLE_RATE, false);
        clips[0].SetData(samples1, 0);

        // 2. High Harmonic Radar Ping (Fundamental 2093Hz [C7] + 3139Hz [G7] + 4250Hz inharmonic, 0.25s)
        int count2 = Mathf.RoundToInt(SAMPLE_RATE * 0.25f);
        float[] samples2 = new float[count2];
        for (int i = 0; i < count2; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / count2;
            float tone = Mathf.Sin(2f * Mathf.PI * 2093f * t) * 0.60f +
                         Mathf.Sin(2f * Mathf.PI * 3139f * t) * 0.25f +
                         Mathf.Sin(2f * Mathf.PI * 4250f * t) * 0.15f;
            float env = Mathf.Exp(-16f * progress);
            samples2[i] = Mathf.Clamp(tone * env, -1f, 1f);
        }
        clips[1] = AudioClip.Create("Holo_Ping_HighBell", count2, 1, SAMPLE_RATE, false);
        clips[1].SetData(samples2, 0);

        // 3. Soft Harmonic Sonar Pip (Fundamental 1568Hz [G6] + 3136Hz octave, 0.22s)
        int count3 = Mathf.RoundToInt(SAMPLE_RATE * 0.22f);
        float[] samples3 = new float[count3];
        for (int i = 0; i < count3; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / count3;
            float tone = Mathf.Sin(2f * Mathf.PI * 1568f * t) * 0.70f +
                         Mathf.Sin(2f * Mathf.PI * 3136f * t) * 0.30f;
            float env = Mathf.Exp(-18f * progress);
            samples3[i] = Mathf.Clamp(tone * env, -1f, 1f);
        }
        clips[2] = AudioClip.Create("Holo_Ping_SonarPip", count3, 1, SAMPLE_RATE, false);
        clips[2].SetData(samples3, 0);

        return clips;
    }

    // Sub-Bass Cinematic Warp Surge: Deep resonant 45Hz–120Hz exponential sine sweep on power boot
    private AudioClip SynthesizeWarpSurge(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // 45Hz to 120Hz exponential sine sweep
            float freq = 45f * Mathf.Pow(120f / 45f, progress);
            phase += 2f * Mathf.PI * freq / SAMPLE_RATE;

            // Deep resonant harmonic construction
            float fundamental = Mathf.Sin(phase);
            float subOctave = Mathf.Sin(phase * 0.5f) * 0.28f;       // 22.5Hz - 60Hz ultra-low rumble
            float secondHarmonic = Mathf.Sin(phase * 2.0f) * 0.35f;  // 90Hz - 240Hz punch
            float thirdHarmonic = Mathf.Sin(phase * 3.0f) * 0.15f;   // Resonance shimmer

            // Soft saturation / analog limiter drive
            float tone = (fundamental * 0.75f + subOctave + secondHarmonic + thirdHarmonic);
            float saturated = (float)System.Math.Tanh(tone * 1.45f);

            // Envelope: 40ms smooth cosine attack + exponential decay body
            float attack = (t < 0.040f) ? Mathf.Sin((t / 0.040f) * Mathf.PI * 0.5f) : 1.0f;
            float release = Mathf.Pow(1.0f - progress, 1.25f);

            samples[i] = Mathf.Clamp(saturated * attack * release * 0.95f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_WarpSurge", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Granular Quantum Telemetry Chatter: Rapid stochastic micro-packet digital bleeps (1800Hz–4200Hz)
    // simulating live encrypted satellite data decoding
    private AudioClip[] SynthesizeGranularChatter()
    {
        AudioClip[] clips = new AudioClip[6];

        // 1. Binary FSK data packet (alternating 2100Hz and 3800Hz, 8ms bits, 0.055s)
        clips[0] = GenerateGranularPacket("Holo_Chatter_FSK", 0.055f, (t, p) =>
        {
            int bit = Mathf.FloorToInt(t / 0.008f) % 2;
            float freq = (bit == 0) ? 2100f : 3800f;
            float carrier = Mathf.Sin(2f * Mathf.PI * freq * t);
            float bitProgress = (t % 0.008f) / 0.008f;
            float bitEnv = Mathf.Sin(bitProgress * Mathf.PI);
            return carrier * bitEnv * Mathf.Exp(-12f * p);
        });

        // 2. Quantum Grain Cluster: 4 stochastic micro-grains hopping (1900Hz, 3200Hz, 2700Hz, 4100Hz, 0.060s)
        float[] freqs2 = { 1900f, 3200f, 2700f, 4100f };
        clips[1] = GenerateGranularPacket("Holo_Chatter_GrainCluster", 0.060f, (t, p) =>
        {
            int grainIdx = Mathf.Clamp(Mathf.FloorToInt(p * freqs2.Length), 0, freqs2.Length - 1);
            float grainP = (p * freqs2.Length) % 1.0f;
            float carrier = Mathf.Sin(2f * Mathf.PI * freqs2[grainIdx] * t);
            float grainEnv = Mathf.Sin(grainP * Mathf.PI);
            return carrier * grainEnv;
        });

        // 3. Ascending Stepped 5-bit Cipher Packet (2000Hz, 2500Hz, 3000Hz, 3600Hz, 4200Hz, 0.050s)
        clips[2] = GenerateGranularPacket("Holo_Chatter_Cipher", 0.050f, (t, p) =>
        {
            float freq = Mathf.Lerp(2000f, 4200f, Mathf.Floor(p * 5f) / 4f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.85f +
                         Mathf.Sin(4f * Mathf.PI * freq * t) * 0.15f;
            float stepP = (p * 5f) % 1.0f;
            return tone * Mathf.Exp(-18f * stepP);
        });

        // 4. Dual-tone encrypted parity handshake (2300Hz + 3650Hz, 0.045s)
        clips[3] = GenerateGranularPacket("Holo_Chatter_Parity", 0.045f, (t, p) =>
        {
            float tone1 = Mathf.Sin(2f * Mathf.PI * 2300f * t) * 0.6f;
            float tone2 = Mathf.Sin(2f * Mathf.PI * 3650f * t) * 0.4f;
            float am = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 220f * t);
            return (tone1 + tone2) * am * Mathf.Exp(-20f * p);
        });

        // 5. Cascade Micro-Bleep (Fast frequency chirp glide 4200Hz -> 1800Hz with FM flutter, 0.055s)
        clips[4] = GenerateGranularPacket("Holo_Chatter_Cascade", 0.055f, (t, p) =>
        {
            float freq = Mathf.Lerp(4200f, 1800f, p);
            float mod = Mathf.Sin(2f * Mathf.PI * 480f * t) * 0.5f;
            float phase = 2f * Mathf.PI * freq * t + mod;
            return Mathf.Sin(phase) * Mathf.Exp(-16f * p);
        });

        // 6. Rapid Gated Micro-Stutter (3150Hz gated at 80Hz, 0.050s)
        clips[5] = GenerateGranularPacket("Holo_Chatter_Stutter", 0.050f, (t, p) =>
        {
            float carrier = Mathf.Sin(2f * Mathf.PI * 3150f * t);
            float gate = Mathf.Sin(2f * Mathf.PI * 80f * t) > 0f ? 1.0f : 0.15f;
            return carrier * gate * Mathf.Exp(-15f * p);
        });

        return clips;
    }

    private AudioClip GenerateGranularPacket(string name, float duration, System.Func<float, float, float> sampleFunc)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;
            samples[i] = Mathf.Clamp(sampleFunc(t, progress) * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Harmonic Musical Triad Chords:
    // Cyan / Ch 1: E Major 9 chord shimmer (harmonic frequencies 330Hz, 415Hz, 494Hz, 622Hz)
    // Amber / Ch 2: D Major triad shimmer (293Hz, 370Hz, 440Hz)
    // Red / Ch 3: F# Minor triad glitch chord (370Hz, 440Hz, 554Hz)
    private AudioClip[] SynthesizeChannelChords()
    {
        AudioClip[] chords = new AudioClip[3];

        // Ch 1: Cyan E Major 9 chord shimmer (330, 415, 494, 622Hz)
        chords[0] = SynthesizeHarmonicChord("Holo_Chord_E_Maj9", new float[] { 330f, 415f, 494f, 622f }, 0.75f, false);

        // Ch 2: Amber D Major triad shimmer (293, 370, 440Hz)
        chords[1] = SynthesizeHarmonicChord("Holo_Chord_D_Maj", new float[] { 293f, 370f, 440f }, 0.70f, false);

        // Ch 3: Red F# Minor triad glitch chord (370, 440, 554Hz)
        chords[2] = SynthesizeHarmonicChord("Holo_Chord_Fs_Min_Glitch", new float[] { 370f, 440f, 554f }, 0.65f, true);

        return chords;
    }

    private AudioClip SynthesizeHarmonicChord(string name, float[] freqs, float duration, bool isGlitch)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        float heldNoise = 0f;
        int noiseHoldSamples = 24;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            float chordSum = 0f;
            for (int n = 0; n < freqs.Length; n++)
            {
                float f = freqs[n];

                // Fundamental + chorus detune (+/- 0.35Hz for lush analog shimmer)
                float tone = Mathf.Sin(2f * Mathf.PI * f * t) * 0.60f +
                             Mathf.Sin(2f * Mathf.PI * (f + 0.35f) * t) * 0.20f +
                             Mathf.Sin(2f * Mathf.PI * (f - 0.35f) * t) * 0.20f;

                // High crystal octave shimmer (2f)
                tone += Mathf.Sin(2f * Mathf.PI * (f * 2.0f) * t) * 0.15f;

                chordSum += tone / freqs.Length;
            }

            // Envelope: 25ms smooth attack + exponential chime decay
            float attack = (t < 0.025f) ? Mathf.Sin((t / 0.025f) * Mathf.PI * 0.5f) : 1.0f;
            float decay = Mathf.Exp(-5.5f * progress);

            if (isGlitch)
            {
                // Bitcrush / sample-and-hold step modulation for Red channel cyber-glitch
                if (i % noiseHoldSamples == 0)
                {
                    heldNoise = (Random.value * 2f - 1f) * 0.25f;
                }
                float stutter = (t < 0.18f && Mathf.Sin(2f * Mathf.PI * 55f * t) < 0f) ? 0.3f : 1.0f;
                chordSum = (chordSum * 0.75f + heldNoise) * stutter;
                decay = Mathf.Exp(-6.5f * progress);
            }

            samples[i] = Mathf.Clamp(chordSum * attack * decay * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Channel Switch Arc Discharge: Crisp high-voltage electrical spark/arc crackle
    private AudioClip SynthesizeArcDischarge(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // Initial breakdown spark snap in first 3ms
            float spark = (t < 0.003f) ? (Random.value * 2f - 1f) * 0.85f : 0f;

            // Stochastic electrical sputtering crackle impulses
            float crackle = (Random.value < 0.08f) ? (Random.value * 2f - 1f) * 0.65f : 0f;

            // 120Hz / 240Hz mains voltage arc resonance
            float mainsHum = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.35f +
                             Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.20f;

            // High-frequency bandpass fizz (3200Hz - 6400Hz noise)
            float fizz = (Random.value * 2f - 1f) * Mathf.Sin(2f * Mathf.PI * 4800f * t) * 0.40f;

            // Exponential decay envelope
            float env = Mathf.Exp(-14f * progress);

            float totalArc = (spark + crackle + (mainsHum + fizz) * 0.7f) * env;
            samples[i] = Mathf.Clamp((float)System.Math.Tanh(totalArc * 1.6f) * 0.90f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_ArcDischarge", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Hologram Power-Down / De-Rez Dissolve: Descending cyber-resonant warp dissipation (220Hz down to 35Hz with exponential decay and analog saturation)
    private AudioClip SynthesizePowerDown(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // Descending cyber-resonant frequency: 220Hz sweeping down to 35Hz
            float freq = 220f * Mathf.Pow(35f / 220f, Mathf.Pow(progress, 0.85f));
            phase += 2f * Mathf.PI * freq / SAMPLE_RATE;

            // Cyber-resonant harmonic construction: fundamental + sub-octave rumble + 3rd harmonic dissipation
            float fundamental = Mathf.Sin(phase);
            float subOctave = Mathf.Sin(phase * 0.5f) * 0.35f;
            float harmonic3 = Mathf.Sin(phase * 3.0f) * (1.0f - progress) * 0.25f;

            // Holographic de-rez dissipation tremolo slowing down as quantum field collapses (40Hz down to 6Hz)
            float lfoFreq = Mathf.Lerp(40f, 6f, progress);
            float warpLFO = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * lfoFreq * t);

            // Analog tape/transformer saturation drive
            float raw = (fundamental * 0.70f + subOctave + harmonic3) * warpLFO;
            float saturated = (float)System.Math.Tanh(raw * 1.55f);

            // Smooth attack (12ms) + exponential dissipation decay
            float attack = (t < 0.012f) ? Mathf.Sin((t / 0.012f) * Mathf.PI * 0.5f) : 1.0f;
            float decay = Mathf.Exp(-4.2f * progress) * (1.0f - progress);

            samples[i] = Mathf.Clamp(saturated * attack * decay * 0.95f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_PowerDown", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Optics Calibration & Laser Whistle: Fast parabolic frequency sweep (2800Hz -> 5800Hz -> 3400Hz) simulating laser emitter focusing lenses
    private AudioClip SynthesizeOpticsWhistle(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        float apexProgress = 0.45f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // Parabolic frequency trajectory: 2800Hz -> 5800Hz peak -> 3400Hz settling
            float freq;
            if (progress < apexProgress)
            {
                float p = progress / apexProgress;
                freq = Mathf.Lerp(2800f, 5800f, Mathf.Sin(p * Mathf.PI * 0.5f));
            }
            else
            {
                float p = (progress - apexProgress) / (1.0f - apexProgress);
                freq = Mathf.Lerp(5800f, 3400f, Mathf.Sin(p * Mathf.PI * 0.5f));
            }

            phase += 2f * Mathf.PI * freq / SAMPLE_RATE;

            // Primary laser focusing tone + refractive crystal overtone (1.5x and 2x)
            float carrier = Mathf.Sin(phase) * 0.70f;
            float refraction = Mathf.Sin(phase * 1.5f) * 0.20f;
            float sheen = Mathf.Sin(phase * 2.0f) * 0.10f;

            // High-speed optical servo micro-tremolo (65Hz vibrato)
            float servoAM = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 65f * t);

            // Envelope: 8ms fast attack, full body, smooth exponential ring-out
            float attack = (t < 0.008f) ? (t / 0.008f) : 1.0f;
            float env = attack * Mathf.Sin(progress * Mathf.PI * 0.95f) * Mathf.Exp(-3.0f * progress);

            samples[i] = Mathf.Clamp((carrier + refraction + sheen) * servoAM * env * 0.88f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_OpticsWhistle", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Neural Data-Burst / Encrypted Uplink: High-speed stochastic FSK / M-ary dual-carrier cipher telemetry burst with ringing resonant filter
    private AudioClip SynthesizeNeuralUplink(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        // M-ary FSK dual-carrier frequency banks
        float[] fskLow = { 2200f, 2650f, 3100f, 3550f };
        float[] fskHigh = { 4200f, 4800f, 5450f, 6200f };

        float phaseLow = 0f;
        float phaseHigh = 0f;
        float ringPhase = 0f;

        float symbolDuration = 0.006f; // 6ms symbols -> ~14 symbols in 85ms

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            int symbolIndex = Mathf.FloorToInt(t / symbolDuration);
            float symbolP = (t % symbolDuration) / symbolDuration;

            // Deterministic pseudo-random seed per symbol for crisp repeatable telemetry
            int hash = (symbolIndex * 19937 + 7) & 0x7FFFFFFF;
            int idxLow = hash % fskLow.Length;
            int idxHigh = ((hash >> 3) ^ (symbolIndex * 31)) % fskHigh.Length;

            float freq1 = fskLow[Mathf.Abs(idxLow)];
            float freq2 = fskHigh[Mathf.Abs(idxHigh)];

            phaseLow += 2f * Mathf.PI * freq1 / SAMPLE_RATE;
            phaseHigh += 2f * Mathf.PI * freq2 / SAMPLE_RATE;
            ringPhase += 2f * Mathf.PI * 3750f / SAMPLE_RATE;

            // Dual-carrier M-ary synthesis
            float tone1 = Mathf.Sin(phaseLow) * 0.50f;
            float tone2 = Mathf.Sin(phaseHigh) * 0.35f;

            // Ringing resonant filter excitation on each symbol boundary
            float ringEnv = Mathf.Exp(-18f * symbolP);
            float resonantRing = Mathf.Sin(ringPhase) * ringEnv * 0.25f;

            // Symbol windowing (half-sine window per symbol to prevent harsh clicking)
            float symbolWindow = Mathf.Sin(symbolP * Mathf.PI);

            float combined = (tone1 + tone2) * symbolWindow + resonantRing;

            // Overall burst envelope with exponential decay
            float burstEnv = Mathf.Exp(-11f * progress);

            // Saturated limiter
            float outSample = (float)System.Math.Tanh(combined * 1.8f) * burstEnv * 0.85f;
            samples[i] = Mathf.Clamp(outSample, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_NeuralUplink", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Holographic Glitch Static Burst: Pink noise burst with sample-and-hold gating and bitcrushed harmonics
    private AudioClip SynthesizeGlitchStatic(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        // Paul Kellet refined 3-pole pink noise filter generator
        float b0 = 0f, b1 = 0f, b2 = 0f, b3 = 0f, b4 = 0f, b5 = 0f, b6 = 0f;

        float heldSample = 0f;
        int holdCounter = 0;
        int holdInterval = 14;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // White noise source
            float white = (Random.value * 2f - 1f);

            // Pink noise filter update (rich 1/f spectral slope)
            b0 = 0.99886f * b0 + white * 0.0555179f;
            b1 = 0.99332f * b1 + white * 0.0750759f;
            b2 = 0.96900f * b2 + white * 0.1538520f;
            b3 = 0.86650f * b3 + white * 0.3104856f;
            b4 = 0.55000f * b4 + white * 0.5329522f;
            b5 = -0.7616f * b5 - white * 0.0168980f;
            float pink = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362f) * 0.12f;
            b6 = white * 0.115926f;

            // Sample-and-Hold decimation / gating
            if (holdCounter <= 0)
            {
                heldSample = pink;
                holdInterval = Random.Range(8, 28);
                holdCounter = holdInterval;
            }
            holdCounter--;

            // Bitcrush / harmonic quantization (5-bit steps)
            float steps = 16f;
            float crushed = Mathf.Round(heldSample * steps) / steps;

            // Scanline sync dropout stutter (50Hz gated modulation)
            float stutter = (Mathf.Sin(2f * Mathf.PI * 50f * t) > -0.2f) ? 1.0f : 0.08f;

            // Fast transient attack + shaped power release
            float attack = (t < 0.004f) ? (t / 0.004f) : 1.0f;
            float env = attack * Mathf.Pow(1.0f - progress, 1.8f);

            float outSample = (crushed * 0.75f + pink * 0.25f) * stutter * env * 1.5f;
            samples[i] = Mathf.Clamp((float)System.Math.Tanh(outSample) * 0.90f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_GlitchStatic", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Sub-Harmonic Bass Drop / Impact Thump: Punchy 35Hz sub-bass transient punch
    private AudioClip SynthesizeSubThump(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // Punchy transient pitch sweep: 130Hz rapidly dropping down to 35Hz in first 35ms,
            // then locking into sustained deep 35Hz sub-bass
            float freq;
            if (t < 0.035f)
            {
                float pt = t / 0.035f;
                freq = Mathf.Lerp(130f, 35f, Mathf.Pow(pt, 0.45f));
            }
            else
            {
                freq = 35f;
            }

            phase += 2f * Mathf.PI * freq / SAMPLE_RATE;

            // Sub-bass fundamental (35Hz) + punchy 2nd harmonic (70Hz) + sub-felt resonance (17.5Hz)
            float sub = Mathf.Sin(phase) * 0.70f;
            float punch2nd = Mathf.Sin(phase * 2f) * 0.28f * Mathf.Exp(-12f * progress);
            float subFelt = Mathf.Sin(phase * 0.5f) * 0.20f;

            // Transient click spike in first 2ms for acoustic speaker impact
            float click = (t < 0.002f) ? (1.0f - t / 0.002f) * 0.5f : 0f;

            // Saturated analog wave shaper (Tanh saturation)
            float raw = (sub + punch2nd + subFelt + click);
            float saturated = (float)System.Math.Tanh(raw * 2.2f);

            // Fast cosine attack (2ms) + smooth exponential tail
            float attack = (t < 0.002f) ? Mathf.Sin((t / 0.002f) * Mathf.PI * 0.5f) : 1.0f;
            float decay = Mathf.Exp(-6.5f * progress) * (1.0f - progress * 0.7f);

            samples[i] = Mathf.Clamp(saturated * attack * decay * 0.95f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_SubThump", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Thermal Plasma Discharge: Crackling plasma filament sputtering with random micro-sparks
    private AudioClip SynthesizeThermalDischarge(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        float phase1 = 0f;
        float phase2 = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // Dual ionic plasma filament resonance: 480Hz and 820Hz with slight micro-jitter
            float freq1 = 480f + (Random.value - 0.5f) * 35f;
            float freq2 = 820f + (Random.value - 0.5f) * 50f;

            phase1 += 2f * Mathf.PI * freq1 / SAMPLE_RATE;
            phase2 += 2f * Mathf.PI * freq2 / SAMPLE_RATE;

            float filamentTones = Mathf.Sin(phase1) * 0.35f + Mathf.Sin(phase2) * 0.30f;

            // Random Poisson micro-sparks (high voltage snap pulses)
            float spark = 0f;
            if (Random.value < 0.06f * (1.0f - progress * 0.5f))
            {
                spark = (Random.value * 2f - 1f) * 0.85f;
            }

            // High frequency plasma sizzle (bandpassed white noise 3800Hz - 6500Hz)
            float sizzle = (Random.value * 2f - 1f) * Mathf.Sin(2f * Mathf.PI * 5200f * t) * 0.35f;

            // Sputtering envelope: plasma discharge filament collapse
            float sputter = (Random.value > 0.12f) ? 1.0f : 0.25f;

            float env = Mathf.Exp(-11f * progress);

            float total = (filamentTones + spark + sizzle) * sputter * env;
            float saturated = (float)System.Math.Tanh(total * 1.8f);

            samples[i] = Mathf.Clamp(saturated * 0.90f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_ThermalDischarge", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Synthesizes 3 channel-specific seamless looping ambient drone soundscapes (2.0s duration)
    // Frequencies are exact multiples of 0.5Hz guaranteeing 100% click-free loop points
    private AudioClip[] SynthesizeChannelHums(float duration)
    {
        AudioClip[] hums = new AudioClip[3];
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);

        // Ch 0 (Cyan): 432Hz high crystalline quantum resonance
        float[] samples0 = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;

            // 432Hz fundamental (864 cycles)
            float fund = Mathf.Sin(2f * Mathf.PI * 432f * t) * 0.35f;

            // 864Hz octave harmonic (1728 cycles)
            float oct = Mathf.Sin(2f * Mathf.PI * 864f * t) * 0.20f;

            // Subtle binaural chorus detune (433Hz = 866 cycles; 431Hz = 862 cycles) -> 1Hz undulating shimmer
            float chorusA = Mathf.Sin(2f * Mathf.PI * 433f * t) * 0.12f;
            float chorusB = Mathf.Sin(2f * Mathf.PI * 431f * t) * 0.12f;

            // High crystalline overtone: 1296Hz (2592 cycles) with 0.5Hz amplitude modulation
            float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
            float crystal = Mathf.Sin(2f * Mathf.PI * 1296f * t) * 0.05f * lfo;

            // Sub-harmonic anchor: 216Hz (432 cycles)
            float sub = Mathf.Sin(2f * Mathf.PI * 216f * t) * 0.16f;

            samples0[i] = Mathf.Clamp(fund + oct + chorusA + chorusB + crystal + sub, -1f, 1f);
        }
        hums[0] = AudioClip.Create("Holo_Hum_Ch0_Cyan", sampleCount, 1, SAMPLE_RATE, false);
        hums[0].SetData(samples0, 0);

        // Ch 1 (Amber): 110Hz/220Hz warm analog mainframe reactor hum
        float[] samples1 = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;

            // Sub-bass resonance (110Hz = 220 cycles)
            float sub = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.40f;

            // Core mainframe hum (220Hz = 440 cycles)
            float core = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.32f;

            // Warm 3rd harmonic (330Hz = 660 cycles)
            float harm3 = Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.15f;

            // 4th harmonic (440Hz = 880 cycles)
            float harm4 = Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.08f;

            // Warm transformer beat (221Hz = 442 cycles; 1Hz throbbing warmth)
            float beat = Mathf.Sin(2f * Mathf.PI * 221f * t) * 0.12f;

            // Soft analog tape/transformer saturation
            float raw = (sub + core + harm3 + harm4 + beat);
            float saturated = (float)System.Math.Tanh(raw * 1.45f) * 0.85f;

            samples1[i] = Mathf.Clamp(saturated, -1f, 1f);
        }
        hums[1] = AudioClip.Create("Holo_Hum_Ch1_Amber", sampleCount, 1, SAMPLE_RATE, false);
        hums[1].SetData(samples1, 0);

        // Ch 2 (Red): 65Hz/130Hz buzzing electric battle grid drone
        float[] samples2 = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;

            // 65Hz electric grid fundamental (130 cycles)
            float sub = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.38f;

            // 130Hz buzzing core harmonic (260 cycles)
            float core = Mathf.Sin(2f * Mathf.PI * 130f * t) * 0.30f;

            // Buzzing odd harmonics (390Hz = 780 cycles, 650Hz = 1300 cycles)
            float buzz1 = Mathf.Sin(2f * Mathf.PI * 390f * t) * 0.15f;
            float buzz2 = Mathf.Sin(2f * Mathf.PI * 650f * t) * 0.08f;

            // Electric grid tension beat (66Hz = 132 cycles; 1Hz battle tension pulse)
            float gridPulse = Mathf.Sin(2f * Mathf.PI * 66f * t) * 0.14f;

            // Asymmetric clipping / grid saturation for electric battle-grid texture
            float raw = (sub + core + buzz1 + buzz2 + gridPulse);
            float saturated = (float)System.Math.Tanh(raw * 1.75f) * 0.88f;

            samples2[i] = Mathf.Clamp(saturated, -1f, 1f);
        }
        hums[2] = AudioClip.Create("Holo_Hum_Ch2_Red", sampleCount, 1, SAMPLE_RATE, false);
        hums[2].SetData(samples2, 0);

        return hums;
    }

    // Tactical Target Lock Chirp: Fast dual-tone military lock-on sweep (3200Hz & 4800Hz rapid chirps)
    private AudioClip SynthesizeTargetLock(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float outSample = 0f;

            // Phase 1: 3200Hz Target Acquisition Sweep (0.000s - 0.032s)
            if (t < 0.032f)
            {
                float p1 = t / 0.032f;
                float freq1 = Mathf.Lerp(2800f, 3600f, p1);
                float tone1 = Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.75f +
                              Mathf.Sin(4f * Mathf.PI * freq1 * t) * 0.20f;
                float env1 = Mathf.Exp(-18f * p1);
                outSample = tone1 * env1;
            }
            // Micro-gate pause between sweeps (0.032s - 0.036s)
            // Phase 2: 4800Hz Confirmed Lock Sweep (0.036s - 0.070s)
            else if (t >= 0.036f)
            {
                float t2 = t - 0.036f;
                float p2 = Mathf.Clamp01(t2 / (duration - 0.036f));
                float freq2 = Mathf.Lerp(4400f, 5200f, p2);
                float tone2 = Mathf.Sin(2f * Mathf.PI * freq2 * t2) * 0.80f +
                              Mathf.Sin(4f * Mathf.PI * freq2 * t2) * 0.15f +
                              Mathf.Sin(2f * Mathf.PI * 9600f * t2) * 0.10f;
                float env2 = Mathf.Exp(-20f * p2);
                outSample = tone2 * env2;
            }

            samples[i] = Mathf.Clamp(outSample * 0.88f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_TargetLock", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Quantum Gyro-Tick: Ultra-fast high-frequency optical tick (5400Hz 3ms click) for clock sync
    private AudioClip SynthesizeGyroTick(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;

            // Optical click transient in first 0.8ms
            float click = (t < 0.0008f) ? (Random.value * 2f - 1f) * 0.40f : 0f;

            // 5400Hz optical carrier + 10800Hz overtone
            float carrier = Mathf.Sin(2f * Mathf.PI * 5400f * t) * 0.80f +
                            Mathf.Sin(2f * Mathf.PI * 10800f * t) * 0.20f;

            // Ultra-fast 3ms optical ring decay
            float env = Mathf.Exp(-120f * t);

            samples[i] = Mathf.Clamp((carrier + click) * env * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_GyroTick", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Stepped Digital Arpeggio: Rapid 4-note ascending digital arpeggio (C6, E6, G6, B6 harmonic sequence)
    private AudioClip SynthesizeSteppedArpeggio(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        // Harmonic sequence: C6, E6, G6, B6
        float[] freqs = new float[] { 1046.50f, 1318.51f, 1567.98f, 1975.53f };
        float stepDuration = duration / freqs.Length;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            int stepIndex = Mathf.Clamp(Mathf.FloorToInt(t / stepDuration), 0, freqs.Length - 1);
            float stepT = t - stepIndex * stepDuration;
            float stepP = stepT / stepDuration;

            float f = freqs[stepIndex];
            // Sine fundamental + subtle 2nd harmonic + digital pulse component
            float tone = Mathf.Sin(2f * Mathf.PI * f * stepT) * 0.65f +
                         Mathf.Sin(4f * Mathf.PI * f * stepT) * 0.20f +
                         Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * stepT)) * 0.12f;

            // 1.5ms attack and steep decay per step
            float attack = (stepT < 0.0015f) ? (stepT / 0.0015f) : 1.0f;
            float decay = Mathf.Exp(-16f * stepP);

            float sample = tone * attack * decay;
            samples[i] = Mathf.Clamp((float)System.Math.Tanh(sample * 1.5f) * 0.85f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_SteppedArpeggio", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Mainframe Relay Matrix Click: Crisp tactile mechanical-electronic relay latching click
    private AudioClip SynthesizeRelayClick(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // Solenoid magnetic armature impulse (0 - 3.5ms)
            float armature = 0f;
            if (t < 0.0035f)
            {
                float ap = t / 0.0035f;
                float freq = Mathf.Lerp(900f, 350f, ap);
                armature = Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - ap);
            }

            // Mechanical leaf-spring contact closure latch at 5ms
            float contact = 0f;
            if (t >= 0.005f)
            {
                float cp = (t - 0.005f);
                float ping = Mathf.Sin(2f * Mathf.PI * 3400f * cp) * 0.65f +
                             Mathf.Sin(2f * Mathf.PI * 6800f * cp) * 0.25f;
                contact = ping * Mathf.Exp(-55f * cp);
            }

            // Micro-snap transient
            float snap = (t < 0.001f || (t > 0.005f && t < 0.006f)) ? (Random.value * 2f - 1f) * 0.35f : 0f;

            float env = Mathf.Exp(-24f * progress);
            float total = (armature * 0.55f + contact * 0.80f + snap) * env;

            samples[i] = Mathf.Clamp((float)System.Math.Tanh(total * 1.6f) * 0.88f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_RelayClick", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Combat Warning Micro-Siren: Rapid two-tone oscillating warble (1200Hz - 2400Hz at 35Hz rate)
    private AudioClip SynthesizeWarningChirp(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // 35Hz LFO warble modulating frequency between 1200Hz and 2400Hz
            float lfo = Mathf.Sin(2f * Mathf.PI * 35f * t);
            float freq = 1800f + 600f * lfo;

            phase += 2f * Mathf.PI * freq / SAMPLE_RATE;

            // Siren timbre: fundamental + 2nd harmonic + square wave edge
            float tone = Mathf.Sin(phase) * 0.65f +
                         Mathf.Sin(phase * 2f) * 0.22f +
                         Mathf.Sign(Mathf.Sin(phase)) * 0.12f;

            // Fast 3ms attack + exponential body decay
            float attack = (t < 0.003f) ? (t / 0.003f) : 1.0f;
            float env = attack * Mathf.Exp(-5.5f * progress);

            float outSample = (float)System.Math.Tanh(tone * 1.5f) * env * 0.85f;
            samples[i] = Mathf.Clamp(outSample, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_WarningChirp", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // High-Voltage Arc Spike: Sharp electrostatic zap with micro-echo
    private AudioClip SynthesizeVoltageSpike(float duration)
    {
        int sampleCount = Mathf.RoundToInt(SAMPLE_RATE * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / sampleCount;

            // Primary electrostatic snap in first 2ms
            float snap = (t < 0.002f) ? (Random.value * 2f - 1f) * 0.90f : 0f;

            // Resonant electrostatic arc ping (4600Hz decaying rapidly)
            float arcPing = Mathf.Sin(2f * Mathf.PI * 4600f * t) * 0.70f * Mathf.Exp(-40f * t);

            // Micro-echo at 20ms simulating dielectric bounce
            float echo = 0f;
            if (t >= 0.020f)
            {
                float et = t - 0.020f;
                float echoSnap = (et < 0.0015f) ? (Random.value * 2f - 1f) * 0.40f : 0f;
                float echoPing = Mathf.Sin(2f * Mathf.PI * 3600f * et) * 0.45f * Mathf.Exp(-40f * et);
                echo = echoSnap + echoPing;
            }

            float env = Mathf.Exp(-12f * progress);
            float total = (snap + arcPing + echo) * env;

            samples[i] = Mathf.Clamp((float)System.Math.Tanh(total * 1.7f) * 0.90f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Holo_VoltageSpike", sampleCount, 1, SAMPLE_RATE, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion
}
