# AR Hologram Video Monitor & Virtual Buttons

[![Unity Version](https://img.shields.io/badge/Unity-6000.1.6f1-black.svg?style=flat&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%2017.1.0-blue.svg?style=flat)](https://unity.com/srp/Universal-Render-Pipeline)
[![AR Engine](https://img.shields.io/badge/AR%20Engine-PTC%20Vuforia%2011.4.4-orange.svg?style=flat)](https://developer.vuforia.com/)
[![Git LFS](https://img.shields.io/badge/Git%20LFS-Tracked%20(178%20MB)-green.svg?style=flat)](https://git-lfs.github.com/)
[![Self-Audit](https://img.shields.io/badge/Self--Audit-41%2F41%20Passed%20(100%25)-success.svg?style=flat)]()
[![License](https://img.shields.io/badge/License-MIT-lightgrey.svg?style=flat)](LICENSE)

An industry-standard, high-fidelity tabletop **Augmented Reality Holographic Video Monitor** built with **Unity 6**, the **Universal Render Pipeline (URP)**, and **PTC Vuforia Engine 11.4.4**.

Point your camera at the physical tabletop marker (`postcard.png`) to materialize an illuminated $24.0\text{ cm} \times 13.5\text{ cm}$ 16:9 widescreen sci-fi holographic monitor floating $9.0\text{ cm}$ above the surface. Interact with real physical touch or mouse clicks across three virtual buttons to switch between three high-tech video channels, triggering dynamic energy conduits, expanding shockwaves, 16-band reactive audio equalizer bars, and a multi-voice procedural sci-fi audio engine.

---

## Key Features

### 1. Borderless Holographic Display & Custom URP Shaders
* **Pure Borderless Luma Transparency**: Custom HLSL screen shader (`HologramScreen.shader`) uses an active pixel luma-alpha curve $\text{pow}(\text{luma}, 0.65) \times 2.2$. Pure black video backgrounds melt away into 100% optical transparency, allowing video graphics to float in real 3D space with soft edge feathering.
* **Procedural Hexagonal Honeycomb Nano-Grid**: A procedural distance-field hex grid glows faintly across active video pixels to simulate an optical nano-emitter matrix.
* **Dual Counter-Sweeping Laser Beams**: An animated primary laser sweep bar scrolls downward while a high-speed telemetry pulse scrolls upward, generating dynamic wave interference fringes where they intersect.
* **Grazing Iridescent Diffraction (Fresnel Spectral Fringe)**: Calculates view-dependent grazing angles modulated with a cosine spectral gradient, dispersing iridescent cyan, violet, and gold hues along the hologram's outer perimeter at acute viewing angles.
* **2D Macroblock Glitch & Matrix Corruption**: Procedural quantized block slices displace UVs and invert color channels during channel transitions and periodic micro-glitches.
* **Multi-Tap Phosphor Bloom & Scanlines**: High-density dynamic scanlines, subtle horizontal anamorphic bloom streaks, and continuous chromatic jitter.
* **Ethereal Base Emitter Mandala (`HologramEmitterRing.shader`)**: Additive projection mandala at $Y = 0.001\text{ m}$ on the card featuring 48 rotating degree ticks, concentric energy rings, and radial pulse waves.
* **3D Parallax Coordinate Backplane (`HologramDepthGrid.shader`)**: Positioned $1.5\text{ cm}$ behind the screen with glowing coordinate grids, crosshairs, and a rotating gimbal ring.

---

### 2. Interactive Tabletop Virtual Buttons & Physical Occlusion
* **Three Responsive Virtual Buttons**:
  * **VBtn_Mood (CH 1: CyberTech HUD - Cyan)**: Located at $(-0.060\text{ m}, 0.0005\text{ m}, -0.033\text{ m})$.
  * **VBtn_Action (CH 2: Futuristic UI - Amber)**: Located at $(0.000\text{ m}, 0.0005\text{ m}, -0.035\text{ m})$.
  * **VBtn_Sound (CH 3: Screen 03 HUD - Red)**: Located at $(+0.060\text{ m}, 0.0005\text{ m}, -0.033\text{ m})$.
* **Dual-Tier Input Detection**: Works with physical optical finger occlusion in real AR via Vuforia virtual button event handlers, and features full mouse raycast fallback for instant in-Editor testing.
* **Floating 3D Holographic Badges**: 10mm cylindrical badges hover $8.0\text{ mm}$ above each button cap, depressing smoothly with mechanical spring physics when pressed.
* **Energy Conduits & Expanding Shockwaves**: Touching a button triggers an animated high-speed energy pulse traveling along glowing conduit lines (`LineRenderer`) from the button to the central emitter mandala, accompanied by an expanding shockwave ripple quad.

---

### 3. High-Density Procedural Sci-Fi Audio Engine
* **100% In-Memory Procedural Synthesis**: Zero external `.wav` or `.mp3` dependencies. Every sound effect and drone is mathematically synthesized in real time via `AudioClip.Create`:
  * **Sub-Bass Cinematic Warp Surge**: $45\text{ Hz} \to 120\text{ Hz}$ exponential sine sweep during projector power boot.
  * **Granular Quantum Telemetry Chatter**: Stochastic micro-packet digital bleeps ($1800\text{ Hz} \to 4200\text{ Hz}$).
  * **Channel Harmonic Chords**:
    * *Cyan / Ch 1*: E Major 9 chord shimmer ($330\text{ Hz}, 415\text{ Hz}, 494\text{ Hz}, 622\text{ Hz}$).
    * *Amber / Ch 2*: D Major triad shimmer ($293\text{ Hz}, 370\text{ Hz}, 440\text{ Hz}$).
    * *Red / Ch 3*: F# Minor triad glitch chord ($370\text{ Hz}, 440\text{ Hz}, 554\text{ Hz}$).
  * **Tactical Target Lock Chirps**: Dual-tone military sweep ($3200\text{ Hz} \ \& \ 4800\text{ Hz}$).
  * **Quantum Gyro-Tick**: High-frequency optical calibration click ($5400\text{ Hz}$).
  * **Stepped Digital Arpeggio**: Rapid 4-note ascending sequence (C6, E6, G6, B6).
  * **Mainframe Relay Matrix Click**: Crisp tactile mechanical-electronic relay latching click.
  * **Combat Warning Micro-Siren**: Two-tone oscillating warble ($1200\text{ Hz} \leftrightarrow 2400\text{ Hz}$).
  * **High-Voltage Arc Spike & Thermal Plasma**: Crackling electrostatic filament sputtering.
* **Three Channel-Specific Soundscapes**:
  * **Channel 0 (Cyan - Tactical CyberTech)**: $432\text{ Hz}$ crystalline quantum resonance drone with target locks, gyro-ticks, and neural cipher telemetry.
  * **Channel 1 (Amber - Mainframe Futuristic UI)**: $110\text{ Hz} / 220\text{ Hz}$ warm analog mainframe hum with stepped digital arpeggios and relay clicks.
  * **Channel 2 (Red - Electronic Warfare Screen 03)**: $65\text{ Hz} / 130\text{ Hz}$ electric battle grid drone with high-voltage arc spikes, combat warning sirens, thermal plasma crackles, and sub-harmonic thumps.
* **16-Band Real-Time Spectrum Equalizer**: Live audio analysis feeds a physical 16-bar equalizer array positioned directly beneath the floating monitor, bouncing with spring-damper transient responsiveness.

---

## Architecture & Hierarchy

```text
ImageTarget (postcard.png, 14.0cm x 9.93cm)
│
├── HologramMonitor_Root
│   ├── FloatingScreen_Pivot (Y = 0.090m, 15° backward tilt)
│   │   ├── Hologram_Screen (0.24m x 0.135m Quad, Mat_HologramScreen)
│   │   ├── Holo_DepthBackplane (0.26m x 0.155m Quad, Mat_HologramDepthGrid)
│   │   ├── Reticle_Brackets (Holo_Corner_TL, TR, BL, BR)
│   │   ├── HUD_ChannelTitle (TextMeshPro-3D, Y = +0.084m)
│   │   ├── HUD_Timecode (TextMeshPro-3D, X = +0.075m, Y = -0.084m)
│   │   ├── HUD_Status (TextMeshPro-3D, X = -0.075m, Y = -0.084m)
│   │   └── Hologram_AudioEqualizer (16-Band Cluster, Y = -0.074m)
│   │       ├── Holo_EQ_Bar_0 ... Holo_EQ_Bar_15
│   │
│   ├── Projector_BaseAperture (Aperture emitter housing at Y = 0.001m)
│   ├── Projector_VolumetricBeam (Crossed cone quads, dynamic beam locking)
│   ├── Holo_BaseMandala (12cm rotating mandala, Mat_HologramEmitterRing)
│   ├── Holo_PhotonStream (ParticleSystem, upward quantum sparks)
│   ├── Energy_Conduits (3 LineRenderers connecting buttons to center)
│   └── Shockwave_Geometry (3 expanding ripple burst quads)
│
├── VirtualButtons
│   ├── VBtn_Mood (Pos: -0.060m, 0.0005m, -0.033m) -> Floating Badge 1
│   ├── VBtn_Action (Pos: 0.000m, 0.0005m, -0.035m) -> Floating Badge 2
│   └── VBtn_Sound (Pos: +0.060m, 0.0005m, -0.033m) -> Floating Badge 3
│
└── VideoPlayer & AudioSynthesizer Systems
```

---

## Automated Self-Audit: 41 / 41 Checks Passed (100%)

The project includes an automated test runner (`Assets/Editor/HologramSelfAudit.cs`) that performs 41 deep validation checks across the entire stack:

```text
================================================================================
=== AR HOLOGRAM VIDEO MONITOR & VIRTUAL BUTTONS: COMPREHENSIVE SELF-AUDIT ===
================================================================================
Audit Timestamp: 2026-09-11 14:06:14

--- SECTION 1: ASSET, VIDEO & SHADER INTEGRITY ---
  [1.1] ImageTarget Texture (Assets/postcard.png): PASS (Found)
  [1.2] Vuforia Configuration (Assets/Resources/VuforiaConfiguration.asset): PASS (Found)
  [1.3] Video File 1 (Video1_CyberTechHUD.mp4): PASS (17.00 MB)
  [1.4] Video File 2 (Video2_FuturisticUI.mp4): PASS (4.35 MB)
  [1.5] Video File 3 (Video3_Screen03.mp4): PASS (16.24 MB)
  [1.6] HologramScreen Shader: PASS (Found & Supported)
  [1.7] HologramEmitterRing Shader: PASS (Found & Supported)
  [1.8] HologramDepthGrid Shader: PASS (Found & Supported)
  [1.9] Holographic Material Suite: PASS (All 3 core shaders cleanly bound to runtime materials)

--- SECTION 2: SCENE HIERARCHY & IMAGE TARGET PRECISION ---
  [2.1] ImageTarget Root: PASS (Found in active scene at Vector3.zero)
        Position: (0.00, 0.00, 0.00), Rotation: (0.00, 0.00, 0.00), Scale: (1.00, 1.00, 1.00)
  [2.2] ImageTarget Sizing: PASS (Width=0.1400m [14.0cm], Height=0.0993m [9.93cm], Aspect=0.7094)
  [2.3] ARCamera: PASS (Configured with VuforiaBehaviour, MainCamera tag, and AudioListener)
  [2.4] Raycast Optical Safety: PASS (Zero interfering colliders on HologramMonitor_Root)

--- SECTION 3: TARGET & BUTTON PRECISION, CONDUITS & SHOCKWAVES ---
  [3.1] VBtn_Mood: PASS (Pos: (-0.0600, 0.0005, -0.0330), Delta: 0.00mm == 0.00mm, Trigger Collider: Yes)
  [3.2] VBtn_Action: PASS (Pos: (0.0000, 0.0005, -0.0350), Delta: 0.00mm == 0.00mm, Trigger Collider: Yes)
  [3.3] VBtn_Sound: PASS (Pos: (0.0600, 0.0005, -0.0330), Delta: 0.00mm == 0.00mm, Trigger Collider: Yes)
  [3.4] Floating 3D Holographic Badges: PASS (3 badges [10mm cylinder] hovering 8.0mm above caps)
  [3.5] HologramButtonController: PASS (Configured with multi-tier optical & touch input)
  [3.6] Interactive Button Energy Conduits: PASS (3 LineRenderer conduits connecting buttons to center mandala)
  [3.7] Expanding Shockwave Geometry: PASS (3 shockwave quads positioned at buttons for ripple burst feedback)

--- SECTION 4: SILENT VIDEO PLAYBACK & TIMESTAMPS ---
  [4.1] HologramVideoController: PASS (Found in scene with silent video playback)
  [4.2] Channel 1 (CH 1: CYBERTECH HUD): PASS (Start Offset: 10.0s, Clip: Video1_CyberTechHUD)
  [4.3] Channel 2 (CH 2: FUTURISTIC UI): PASS (Start Offset: 5.0s, Clip: Video2_FuturisticUI)
  [4.4] Channel 3 (CH 3: SCREEN 03 HUD): PASS (Start Offset: 0.0s, Clip: Video3_Screen03)

--- SECTION 5: HOLOGRAM DISPLAY & 3D GEOMETRY SUITE ---
  [5.1] HologramMonitorDisplay: PASS (Found with floating levitation & dynamic beam locking)
  [5.2] Hologram Screen Scale & Pivot: PASS (24.0cm x 13.5cm 16:9 widescreen, 9.0cm hover, 15.0° tilt)
  [5.3] Base Emitter Mandala: PASS (Quad flat at Y = 0.001m, rotated 90° on X, 12cm diameter, Mat_HologramEmitterRing)
  [5.4] 3D Parallax Depth Backplane: PASS (Quad at Z = +0.015m, 26.0cm x 15.5cm framing backplane, Mat_HologramDepthGrid)
  [5.5] 16-Band Equalizer Geometry: PASS (16 bars Holo_EQ_Bar_0 to 15 spaced Y = -0.074m, X = -0.092m to +0.092m)
  [5.6] Upward Quantum Photon Stream: PASS (ParticleSystem emitting upward soft glowing sparks)
  [5.7] Holographic Corner Reticles: PASS (4 L-shaped brackets bracketing 24cm display at ±0.120m, ±0.0675m)
  [5.8] 3D Floating Cyber HUD Telemetry: PASS (Title Y=0.084m, Timecode X=+0.075m Y=-0.084m, Status X=-0.075m Y=-0.084m)

--- SECTION 6: PROCEDURAL AUDIO & REAL-TIME SPECTRUM ENGINE ---
  [6.1] HologramAudioSynthesizer: PASS (Multi-voice procedural sci-fi UI synthesizer wired)
  [6.2] Sub-Bass Cinematic Warp Surge: PASS (45Hz–120Hz resonant sine sweep verified)
  [6.3] Granular Quantum Telemetry Chatter: PASS (1800Hz–4200Hz stochastic data chatter verified)
  [6.4] Harmonic Musical Triad Chords: PASS (Ch 1: E Maj9, Ch 2: D Maj, Ch 3: F# Min Glitch verified)
  [6.5] Expanded Procedural SFX: PASS (PowerDown, OpticsWhistle, NeuralUplink, GlitchStatic, SubThump, ThermalDischarge verified)
  [6.6] 16-Band Real-Time Equalizer Engine: PASS (Normalized [0.0 - 1.0] telemetry levels across all 16 bands)
  [6.7] Channel-Specific Audio Soundscapes: PASS (StartSciFiAmbience 0, 1, 2 configured distinct sound palettes, drone frequencies, and telemetry distributions)
  [6.8] High-Density Multi-Voice Polyphony: PASS (8-voice spatialized AudioSource pool verified under rapid concurrent micro-bursts without clipping)
  [6.9] Specialized Procedural SFX Suite: PASS (PlayTargetLock, PlayGyroTick, PlaySteppedArpeggio, PlayRelayClick, PlayWarningChirp, PlayVoltageSpike verified)

================================================================================
AUDIT SCORECARD: 41 / 41 CHECKS PASSED (100.0%)
================================================================================
```

---

## Project Structure

```text
AR Virtual Buttons Video Player/
├── Assets/
│   ├── Editor/
│   │   ├── HologramSceneBuilder.cs       # Procedural scene construction & wiring tool
│   │   ├── HologramSceneUpdater.cs       # Scene hierarchy updater & maintenance utility
│   │   ├── HologramSelfAudit.cs          # 41-check automated self-audit verification suite
│   │   ├── HologramSelfAuditReport.txt   # Latest comprehensive audit report (100% pass)
│   │   ├── ShaderVerificationHook.cs     # URP HLSL shader & material verification utility
│   │   └── ShaderVerificationReport.txt  # Shader compilation & material verification report
│   ├── Materials/
│   │   ├── Mat_HologramScreen.mat        # Borderless luma transparency screen material
│   │   ├── Mat_HologramEmitterRing.mat   # Additive base projector mandala material
│   │   ├── Mat_HologramDepthGrid.mat     # 3D parallax coordinate backplane material
│   │   ├── Mat_HologramEQBar.mat         # 16-band reactive audio equalizer material
│   │   └── Mat_BtnCh1..3.mat             # Channel theme button & badge materials
│   ├── Resources/
│   │   └── VuforiaConfiguration.asset    # Vuforia license & engine settings
│   ├── Scenes/
│   │   └── SampleScene.unity             # Primary AR Holographic Monitor scene
│   ├── Scripts/
│   │   ├── HologramButtonController.cs   # Virtual button events, optical occlusion, conduits
│   │   ├── HologramMonitorDisplay.cs     # 24cm widescreen hover, 16-band EQ, HUD telemetry
│   │   ├── HologramVideoController.cs    # 3-channel silent video switcher & crossfader
│   │   ├── HologramAudioSynthesizer.cs   # 100% procedural multi-voice sci-fi audio engine
│   │   └── HologramDiagnosticHUD.cs      # Real-time on-screen diagnostic HUD (press 'H' to toggle)
│   ├── Shaders/
│   │   ├── HologramScreen.shader         # Borderless URP HLSL hologram display shader
│   │   ├── HologramEmitterRing.shader    # Additive concentric ring mandala shader
│   │   ├── HologramDepthGrid.shader      # Additive 3D parallax coordinate backplane shader
│   │   └── HologramProjectorBeam.shader  # Volumetric aperture projection beam shader
│   ├── Videos/                           # Video loop clips (tracked via Git LFS)
│   │   ├── Video1_CyberTechHUD.mp4       # Channel 1: CyberTech HUD
│   │   ├── Video2_FuturisticUI.mp4       # Channel 2: Futuristic UI
│   │   └── Video3_Screen03.mp4           # Channel 3: Screen 03 HUD
│   └── postcard.png                      # Physical tabletop AR marker image (14.0cm x 9.93cm)
├── Packages/
│   ├── com.ptc.vuforia.engine-11.4.4.tgz # Vuforia Engine package (tracked via Git LFS)
│   ├── manifest.json
│   └── packages-lock.json
├── ProjectSettings/                      # Unity project & tag configurations
├── .gitattributes                        # Git LFS binary tracking & Unity YAML merge rules
├── .gitignore                            # Comprehensive Unity & Vuforia ignore rules
└── README.md
```

---

## Getting Started

### Prerequisites
* **Unity 6** (`6000.1.6f1` or compatible) with **Universal Render Pipeline (URP 17.1.0)**.
* **Git** and **[Git LFS](https://git-lfs.github.com/)** installed on your system.
* A standard webcam (for Unity Editor playback) or an AR-capable mobile device (iOS/Android).

### 1. Clone the Repository
Because the Vuforia package tarball (`com.ptc.vuforia.engine-11.4.4.tgz`) and video clips are stored via **Git LFS**, clone with LFS enabled:

```bash
# Clone the repository
git clone https://github.com/Jmmmmjm/ar-virtual-buttons-video-player.git

# Enter project directory
cd ar-virtual-buttons-video-player

# Ensure Git LFS files are pulled
git lfs pull
```

### 2. Print or Display the Target Marker
* Open `Assets/postcard.png`.
* Print the image on paper or display it on a tablet/phone screen laid flat on your desk (physical dimensions: $14.0\text{ cm} \times 9.93\text{ cm}$).

### 3. Open in Unity
1. Launch **Unity Hub**.
2. Click **Add** &rarr; **Add project from disk**.
3. Select the `AR Virtual Buttons Video Player` folder.
4. Ensure the Editor version is set to **Unity 6 (6000.1.6f1)** or compatible.
5. Open the project.

### 4. Running the Experience
1. In the Project window, open `Assets/Scenes/SampleScene.unity`.
2. Connect your webcam (configured under **Window** &rarr; **Vuforia Configuration** &rarr; **Camera Device**).
3. Click **Play** in Unity.
4. Point your webcam at the `postcard.png` marker.
5. **Touch the virtual buttons** with your physical finger on the paper, or **click them with your mouse** in the Game view to switch channels!
6. Press **H** at any time to toggle the live on-screen diagnostic HUD overlay.

---

## Editor Utilities

The project includes custom top-menu editor tools accessible under **`AR Hologram`**:
* **AR Hologram &rarr; Run Self Audit**: Runs the comprehensive 41-check test suite and generates `Assets/Editor/HologramSelfAuditReport.txt`.
* **AR Hologram &rarr; Build Hologram Video Scene**: Automatically provisions and wires the complete scene hierarchy from scratch.
* **AR Hologram &rarr; Apply Scene Fixes and Audit**: Applies scene geometric refinements and triggers immediate audit validation.
* **AR Hologram &rarr; Verify Hologram Shader & Material**: Validates all HLSL shaders and material parameters, generating `Assets/Editor/ShaderVerificationReport.txt`.

---

## Technical Specifications

| Parameter | Specification |
| :--- | :--- |
| **Unity Engine** | Unity 6 (`6000.1.6f1`) |
| **Render Pipeline** | Universal Render Pipeline (URP `17.1.0`) |
| **AR Framework** | PTC Vuforia Engine (`11.4.4`) |
| **Display Dimensions** | $24.0\text{ cm} \times 13.5\text{ cm}$ (16:9 Widescreen, Quad) |
| **Hover Height & Tilt** | $Y = 0.090\text{ m}$ ($9.0\text{ cm}$ hover), $15.0^\circ$ backward camera tilt |
| **Target Marker** | $14.0\text{ cm} \times 9.93\text{ cm}$ (Aspect: `0.7094`, Doraemon postcard) |
| **Audio Synthesis** | 100% Procedural In-Memory (`AudioClip.Create`), Zero External Audio Files |
| **Audio Polyphony** | 8-Voice Spatialized `AudioSource` Pool |
| **Equalizer Visualizer** | 16-Band Real-Time Dynamic Spring Spectrum ($X = -0.092\text{ m}$ to $+0.092\text{ m}$) |
| **Virtual Buttons** | 3 Optical Tabletop Occlusion Targets + Floating 3D Cylindrical Badges ($10\text{ mm} \times 8\text{ mm}$) |
| **Video Playback** | 3 Silent Channels (`VideoAudioOutputMode.None`) with instant frame seeking |

---

## License

This project is open-source and available under the [MIT License](LICENSE).
