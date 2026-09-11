using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Video;
using Vuforia;

/// <summary>
/// Hologram Geometry, HUD, Animation & Scene Specialist Scene Builder.
/// Automated builder for the industry-standard AR Holographic Video Monitor & Virtual Buttons experience.
/// Constructs the complete scene hierarchy, generates all required materials & render textures,
/// connects all component references, and saves to Assets/Scenes/SampleScene.unity.
/// </summary>
public static class HologramSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";
    private const string POSTCARD_TEXTURE_PATH = "Assets/postcard.png";
    private const string RENDER_TEXTURE_PATH = "Assets/RenderTextures/RT_HologramDisplay.renderTexture";

    [MenuItem("AR Hologram/Build Hologram Video Scene", false, 1)]
    public static void BuildSceneMenu()
    {
        BuildCompleteScene();
    }

    public static void BuildCompleteScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[HologramSceneBuilder] Cannot rebuild scene while in Play Mode. Please exit Play Mode first.");
            return;
        }

        Debug.Log("[HologramSceneBuilder] Starting AR Hologram Scene generation...");

        // Ensure directories
        EnsureDirectory("Assets/Materials");
        EnsureDirectory("Assets/RenderTextures");
        EnsureDirectory("Assets/Scenes");

        // 1. Create / Load Materials & RenderTexture
        RenderTexture renderTexture = CreateOrGetRenderTexture();
        Material screenMaterial = CreateOrGetScreenMaterial(renderTexture);
        Material reticleMaterial = CreateOrGetMaterial("Assets/Materials/Mat_HologramReticle.mat", new Color(0.1f, 0.9f, 1f, 0.9f), 0.2f, 0.8f, new Color(0f, 1.8f, 2.5f));
        Material baseMandalaMat = CreateOrGetMaterialWithShader("Assets/Materials/Mat_HologramEmitterRing.mat", "Custom/HologramEmitterRing");
        Material depthBackplaneMat = CreateOrGetMaterialWithShader("Assets/Materials/Mat_HologramDepthGrid.mat", "Custom/HologramDepthGrid");
        Material eqBarMat = CreateOrGetMaterial("Assets/Materials/Mat_HologramEQBar.mat", new Color(0.1f, 0.9f, 1f, 0.9f), 0.2f, 0.8f, new Color(0f, 1.8f, 2.5f));
        Material projectorBaseMat = CreateOrGetMaterial("Assets/Materials/Mat_ProjectorBase.mat", new Color(0.12f, 0.14f, 0.18f), 0.85f, 0.2f);
        Material projectorEmitterMat = CreateOrGetMaterial("Assets/Materials/Mat_ProjectorEmitter.mat", new Color(0.1f, 0.9f, 1f), 0.1f, 0.9f, new Color(0f, 1.2f, 1.8f));
        Material projectorBeamMat = CreateOrGetBeamMaterial();
        Material buttonRimMat = CreateOrGetMaterial("Assets/Materials/Mat_ButtonRim.mat", new Color(0.18f, 0.2f, 0.22f), 0.9f, 0.1f);
        Material btnCh1Mat = CreateOrGetMaterial("Assets/Materials/Mat_BtnCh1_Cyan.mat", new Color(0f, 0.8f, 1f), 0.2f, 0.8f, new Color(0f, 1.5f, 2.2f));
        Material btnCh2Mat = CreateOrGetMaterial("Assets/Materials/Mat_BtnCh2_Amber.mat", new Color(1f, 0.7f, 0.1f), 0.2f, 0.8f, new Color(2.2f, 1.4f, 0.2f));
        Material btnCh3Mat = CreateOrGetMaterial("Assets/Materials/Mat_BtnCh3_Red.mat", new Color(1f, 0.2f, 0.35f), 0.2f, 0.8f, new Color(2.2f, 0.3f, 0.6f));

        // 2. Setup New Empty Scene
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 3. Create AR Camera
        GameObject arCamObj = new GameObject("ARCamera");
        Camera cam = arCamObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;
        arCamObj.tag = "MainCamera";
        arCamObj.AddComponent<AudioListener>();
        arCamObj.AddComponent<UniversalAdditionalCameraData>();
        arCamObj.AddComponent<VuforiaBehaviour>();

        // 4. Directional Light
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.95f, 0.98f, 1.0f);
        light.intensity = 1.1f;
        lightObj.transform.position = new Vector3(0f, 3f, 0f);
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 5. Image Target (postcard.png)
        GameObject targetObj = new GameObject("ImageTarget");
        targetObj.transform.position = Vector3.zero;
        targetObj.transform.rotation = Quaternion.identity;
        targetObj.transform.localScale = Vector3.one;

        ImageTargetBehaviour observer = targetObj.AddComponent<ImageTargetBehaviour>();
        DefaultObserverEventHandler eventHandler = targetObj.AddComponent<DefaultObserverEventHandler>();
        ConfigureImageTargetObserver(targetObj, POSTCARD_TEXTURE_PATH);

        // 6. Hologram Monitor Root (Pure floating 3D holographic display)
        GameObject holoRoot = new GameObject("HologramMonitor_Root");
        holoRoot.transform.SetParent(targetObj.transform, false);
        holoRoot.transform.localPosition = Vector3.zero;
        holoRoot.transform.localRotation = Quaternion.identity;

        // Audio synthesizer & audio sources
        HologramAudioSynthesizer audioSynth = holoRoot.AddComponent<HologramAudioSynthesizer>();
        AudioSource audioSource = holoRoot.AddComponent<AudioSource>();

        // Video Player Coordinator (Silent video playback)
        HologramVideoController videoController = holoRoot.AddComponent<HologramVideoController>();
        VideoPlayer videoPlayer = holoRoot.AddComponent<VideoPlayer>();

        // Base Emitter Mandala: Quad lying flat at Y = 0.001m (rotated 90° on X, 12cm diameter)
        GameObject mandalaObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mandalaObj.name = "Holo_BaseMandala";
        mandalaObj.transform.SetParent(holoRoot.transform, false);
        mandalaObj.transform.localPosition = new Vector3(0f, 0.001f, 0f);
        mandalaObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        mandalaObj.transform.localScale = new Vector3(0.12f, 0.12f, 1f); // 12cm diameter
        StripCollider(mandalaObj);
        MeshRenderer mandalaRenderer = mandalaObj.GetComponent<MeshRenderer>();
        mandalaRenderer.sharedMaterial = baseMandalaMat;

        // Upward Quantum Photon Stream: ParticleSystem emitting soft glowing sparks from card to screen
        GameObject photonObj = new GameObject("Holo_PhotonStream");
        photonObj.transform.SetParent(holoRoot.transform, false);
        photonObj.transform.localPosition = new Vector3(0f, 0.002f, 0f);
        photonObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem photonPS = photonObj.AddComponent<ParticleSystem>();
        var mainMod = photonPS.main;
        mainMod.playOnAwake = false;
        mainMod.loop = true;
        mainMod.startSpeed = 0.12f;
        mainMod.startLifetime = 0.75f;
        mainMod.startSize = 0.003f;
        mainMod.startColor = new Color(0f, 0.9f, 1f, 0.85f);
        mainMod.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emissionMod = photonPS.emission;
        emissionMod.rateOverTime = 30f;

        var shapeMod = photonPS.shape;
        shapeMod.shapeType = ParticleSystemShapeType.Box;
        shapeMod.scale = new Vector3(0.08f, 0.04f, 0.01f);

        var colOverLife = photonPS.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient photonGrad = new Gradient();
        photonGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.cyan, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.3f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLife.color = photonGrad;

        ParticleSystemRenderer psRenderer = photonObj.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null) psRenderer.sharedMaterial = reticleMaterial;

        // Floating Screen Root (Suspended 9.0cm cleanly in air above postcard, tilted 15° back toward user)
        GameObject screenRoot = new GameObject("FloatingScreen_Pivot");
        screenRoot.transform.SetParent(holoRoot.transform, false);
        screenRoot.transform.localPosition = new Vector3(0f, 0.090f, 0.010f);
        screenRoot.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

        // Screen Quad (16:9 widescreen, 24cm x 13.5cm, pure borderless)
        GameObject screenQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screenQuad.name = "Hologram_Screen";
        screenQuad.transform.SetParent(screenRoot.transform, false);
        screenQuad.transform.localPosition = Vector3.zero;
        screenQuad.transform.localRotation = Quaternion.identity;
        screenQuad.transform.localScale = new Vector3(0.24f, 0.135f, 1f); // 16:9 ratio, 24cm widescreen
        StripCollider(screenQuad);
        MeshRenderer screenRenderer = screenQuad.GetComponent<MeshRenderer>();
        screenRenderer.sharedMaterial = screenMaterial;

        // 3D Parallax Depth Backplane: Quad at Z = +0.015m behind screen using Mat_HologramDepthGrid.mat
        GameObject backplaneObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backplaneObj.name = "Holo_DepthBackplane";
        backplaneObj.transform.SetParent(screenRoot.transform, false);
        backplaneObj.transform.localPosition = new Vector3(0f, 0f, 0.015f);
        backplaneObj.transform.localRotation = Quaternion.identity;
        backplaneObj.transform.localScale = new Vector3(0.26f, 0.155f, 1f);
        StripCollider(backplaneObj);
        MeshRenderer backplaneRenderer = backplaneObj.GetComponent<MeshRenderer>();
        backplaneRenderer.sharedMaterial = depthBackplaneMat;

        // 16-Band Holographic Audio Equalizer Bars (Holo_EQ_Bar_0 to 15, Y = -0.074m, X = -0.092m to +0.092m)
        List<Transform> eqTransforms = new List<Transform>();
        List<Renderer> eqRenderers = new List<Renderer>();

        for (int i = 0; i < 16; i++)
        {
            GameObject eqBar = GameObject.CreatePrimitive(PrimitiveType.Quad);
            eqBar.name = $"Holo_EQ_Bar_{i}";
            eqBar.transform.SetParent(screenRoot.transform, false);

            float x = Mathf.Lerp(-0.092f, 0.092f, (float)i / 15f);
            eqBar.transform.localPosition = new Vector3(x, -0.074f, 0f);
            eqBar.transform.localRotation = Quaternion.identity;
            eqBar.transform.localScale = new Vector3(0.0075f, 0.002f, 1f);
            StripCollider(eqBar);

            MeshRenderer barRenderer = eqBar.GetComponent<MeshRenderer>();
            barRenderer.sharedMaterial = eqBarMat;
            eqTransforms.Add(eqBar.transform);
            eqRenderers.Add(barRenderer);
        }

        // Holographic 3D HUD Text Labels
        TextMeshPro titleTMP = CreateHUDText("HUD_ChannelTitle", screenRoot.transform, new Vector3(0f, 0.084f, -0.002f), "STANDBY // SELECT CHANNEL", 0.010f, new Color(0.2f, 0.9f, 1f), new Vector2(0.32f, 0.04f));
        TextMeshPro timecodeTMP = CreateHUDText("HUD_Timecode", screenRoot.transform, new Vector3(0.075f, -0.084f, -0.002f), "--:-- / --:--", 0.0085f, Color.white, new Vector2(0.18f, 0.035f));
        TextMeshPro statusTMP = CreateHUDText("HUD_Status", screenRoot.transform, new Vector3(-0.075f, -0.084f, -0.002f), "[QUANTUM LOCK: 99.8%] [BITRATE: 48.2 MB/s] [FREQ: 432.8 MHz] [FRAME: LIVE]", 0.0085f, new Color(0.2f, 0.9f, 1f), new Vector2(0.32f, 0.035f));

        // 4 Sleek Floating Holographic Corner Reticles/Brackets under FloatingScreen_Pivot
        List<Renderer> reticleRenderers = new List<Renderer>();
        List<Transform> reticleRoots = new List<Transform>();

        GameObject cornerTL = CreateCornerReticle("Holo_Corner_TL", screenRoot.transform, new Vector3(-0.120f, 0.0675f, -0.002f), true, true, reticleMaterial, reticleRenderers);
        reticleRoots.Add(cornerTL.transform);

        GameObject cornerTR = CreateCornerReticle("Holo_Corner_TR", screenRoot.transform, new Vector3(0.120f, 0.0675f, -0.002f), true, false, reticleMaterial, reticleRenderers);
        reticleRoots.Add(cornerTR.transform);

        GameObject cornerBL = CreateCornerReticle("Holo_Corner_BL", screenRoot.transform, new Vector3(-0.120f, -0.0675f, -0.002f), false, true, reticleMaterial, reticleRenderers);
        reticleRoots.Add(cornerBL.transform);

        GameObject cornerBR = CreateCornerReticle("Holo_Corner_BR", screenRoot.transform, new Vector3(0.120f, -0.0675f, -0.002f), false, false, reticleMaterial, reticleRenderers);
        reticleRoots.Add(cornerBR.transform);

        // 7. Virtual Buttons (exact coordinates from source AR Virtual Button)
        // Button 1: Left (-0.060, 0.0005, -0.033) -> VBtn_Mood
        // Button 2: Center (0.000, 0.0005, -0.035) -> VBtn_Action
        // Button 3: Right (+0.060, 0.0005, -0.033) -> VBtn_Sound
        Vector3[] btnCoords = new Vector3[]
        {
            new Vector3(-0.060f, 0.0005f, -0.033f),
            new Vector3( 0.000f, 0.0005f, -0.035f),
            new Vector3( 0.060f, 0.0005f, -0.033f)
        };

        GameObject btn1Obj = CreateVirtualButton("VBtn_Mood", targetObj.transform, btnCoords[0], buttonRimMat, btnCh1Mat, "CH 1\nMOOD", new Color(0f, 0.9f, 1f));
        GameObject btn2Obj = CreateVirtualButton("VBtn_Action", targetObj.transform, btnCoords[1], buttonRimMat, btnCh2Mat, "CH 2\nACTION", new Color(1f, 0.75f, 0.1f));
        GameObject btn3Obj = CreateVirtualButton("VBtn_Sound", targetObj.transform, btnCoords[2], buttonRimMat, btnCh3Mat, "CH 3\nSOUND", new Color(1f, 0.25f, 0.4f));

        // Connect HologramButtonController
        HologramButtonController btnController = targetObj.AddComponent<HologramButtonController>();
        SetSerializedField(btnController, "videoController", videoController);
        SetSerializedField(btnController, "audioSynthesizer", audioSynth);
        SetSerializedField(btnController, "observerBehaviour", observer);
        SetSerializedField(btnController, "activationThreshold", 20f);
        SetSerializedField(btnController, "deactivationThreshold", 10f);
        SetSerializedField(btnController, "approachThreshold", 6f);
        SetSerializedField(btnController, "hoverThreshold", 14f);

        ConfigureButtonRig(btnController, 0, btn1Obj, 0, new Color(0f, 0.9f, 1f));
        ConfigureButtonRig(btnController, 1, btn2Obj, 1, new Color(1f, 0.75f, 0.1f));
        ConfigureButtonRig(btnController, 2, btn3Obj, 2, new Color(1f, 0.25f, 0.4f));

        // 8. 3 Interactive Energy Conduits & Expanding Shockwaves
        List<LineRenderer> conduits = new List<LineRenderer>();
        List<Transform> shockwaveTrans = new List<Transform>();
        List<Renderer> shockwaveRends = new List<Renderer>();
        Vector3 centerMandalaPos = new Vector3(0f, 0.001f, 0f);

        for (int i = 0; i < 3; i++)
        {
            // Energy Conduit Line connecting virtual button to central mandala
            GameObject conduitObj = new GameObject($"Holo_Conduit_{i}");
            conduitObj.transform.SetParent(holoRoot.transform, false);
            conduitObj.transform.localPosition = Vector3.zero;
            conduitObj.transform.localRotation = Quaternion.identity;

            LineRenderer lr = conduitObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.SetPosition(0, btnCoords[i]);
            lr.SetPosition(1, centerMandalaPos);
            lr.startWidth = 0.0020f;
            lr.endWidth = 0.0012f;
            lr.sharedMaterial = reticleMaterial;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            conduits.Add(lr);

            // Expanding Shockwave Ripple Quad lying flat at button location
            GameObject swObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            swObj.name = $"Holo_Shockwave_{i}";
            swObj.transform.SetParent(holoRoot.transform, false);
            swObj.transform.localPosition = btnCoords[i] + new Vector3(0f, 0.0008f, 0f);
            swObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            swObj.transform.localScale = Vector3.zero;
            StripCollider(swObj);

            MeshRenderer swRenderer = swObj.GetComponent<MeshRenderer>();
            swRenderer.sharedMaterial = baseMandalaMat;
            swRenderer.enabled = false;

            shockwaveTrans.Add(swObj.transform);
            shockwaveRends.Add(swRenderer);
        }

        // 9. Connect HologramMonitorDisplay with full visual suite
        HologramMonitorDisplay monitorDisplay = holoRoot.AddComponent<HologramMonitorDisplay>();
        SetSerializedField(monitorDisplay, "screenRenderer", screenRenderer);
        SetSerializedField(monitorDisplay, "baseMandalaRenderer", mandalaRenderer);
        SetSerializedField(monitorDisplay, "depthBackplaneRenderer", backplaneRenderer);
        SetSerializedField(monitorDisplay, "photonStreamParticles", photonPS);
        SetSerializedField(monitorDisplay, "buttonController", btnController);
        SetSerializedField(monitorDisplay, "channelTitleText", titleTMP);
        SetSerializedField(monitorDisplay, "timecodeText", timecodeTMP);
        SetSerializedField(monitorDisplay, "statusBadgeText", statusTMP);
        SetSerializedField(monitorDisplay, "floatingScreenRoot", screenRoot.transform);
        SetSerializedField(monitorDisplay, "audioSynthesizer", audioSynth);
        SetSerializedFieldVector3(monitorDisplay, "targetScreenScale", new Vector3(0.24f, 0.135f, 1.0f));
        SetSerializedFieldFloat(monitorDisplay, "hoverAmplitude", 0.0025f);
        SetSerializedFieldFloat(monitorDisplay, "beamWidth", 0.20f);
        SetSerializedArray(monitorDisplay, "cornerReticleRoots", reticleRoots.ToArray());
        SetSerializedArray(monitorDisplay, "cornerReticleRenderers", reticleRenderers.ToArray());
        SetSerializedArray(monitorDisplay, "eqBarTransforms", eqTransforms.ToArray());
        SetSerializedArray(monitorDisplay, "eqBarRenderers", eqRenderers.ToArray());
        SetSerializedArray(monitorDisplay, "conduitLines", conduits.ToArray());
        SetSerializedArray(monitorDisplay, "shockwaveTransforms", shockwaveTrans.ToArray());
        SetSerializedArray(monitorDisplay, "shockwaveRenderers", shockwaveRends.ToArray());

        // 10. Wire HologramVideoController
        SetSerializedField(videoController, "videoPlayer", videoPlayer);
        SetSerializedField(videoController, "videoAudioSource", audioSource);
        SetSerializedField(videoController, "displayRenderTexture", renderTexture);
        SetSerializedField(videoController, "monitorDisplay", monitorDisplay);
        SetSerializedField(videoController, "audioSynthesizer", audioSynth);

        // Assign downloaded VideoClips
        AssignVideoChannels(videoController);

        // 11. Diagnostic HUD
        GameObject hudObj = new GameObject("Hologram_DiagnosticHUD");
        hudObj.AddComponent<HologramDiagnosticHUD>();

        // 12. Save Scene
        EditorSceneManager.SaveScene(newScene, SCENE_PATH);
        Debug.Log($"[HologramSceneBuilder] Scene successfully built and saved to {SCENE_PATH}!");
    }

    private static GameObject CreateCornerReticle(string name, Transform parent, Vector3 localPos, bool isTop, bool isLeft, Material mat, List<Renderer> reticleRenderers)
    {
        GameObject cornerRoot = new GameObject(name);
        cornerRoot.transform.SetParent(parent, false);
        cornerRoot.transform.localPosition = localPos;
        cornerRoot.transform.localRotation = Quaternion.identity;
        cornerRoot.transform.localScale = Vector3.one;

        float armLength = 0.016f;
        float armThickness = 0.0022f;

        // Horizontal arm
        GameObject hArm = GameObject.CreatePrimitive(PrimitiveType.Quad);
        hArm.name = $"{name}_ArmH";
        hArm.transform.SetParent(cornerRoot.transform, false);
        float hOffset = (isLeft ? 1f : -1f) * (armLength * 0.5f);
        hArm.transform.localPosition = new Vector3(hOffset, 0f, 0f);
        hArm.transform.localRotation = Quaternion.identity;
        hArm.transform.localScale = new Vector3(armLength, armThickness, 1f);
        StripCollider(hArm);
        MeshRenderer mrH = hArm.GetComponent<MeshRenderer>();
        mrH.sharedMaterial = mat;
        reticleRenderers.Add(mrH);

        // Vertical arm
        GameObject vArm = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vArm.name = $"{name}_ArmV";
        vArm.transform.SetParent(cornerRoot.transform, false);
        float vOffset = (isTop ? -1f : 1f) * (armLength * 0.5f);
        vArm.transform.localPosition = new Vector3(0f, vOffset, 0f);
        vArm.transform.localRotation = Quaternion.identity;
        vArm.transform.localScale = new Vector3(armThickness, armLength, 1f);
        StripCollider(vArm);
        MeshRenderer mrV = vArm.GetComponent<MeshRenderer>();
        mrV.sharedMaterial = mat;
        reticleRenderers.Add(mrV);

        return cornerRoot;
    }

    private static void AssignVideoChannels(HologramVideoController controller)
    {
        VideoClip clip1 = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Videos/Video1_CyberTechHUD.mp4");
        VideoClip clip2 = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Videos/Video2_FuturisticUI.mp4");
        VideoClip clip3 = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Videos/Video3_Screen03.mp4");

        SerializedObject so = new SerializedObject(controller);
        SerializedProperty channelsProp = so.FindProperty("channels");
        if (channelsProp != null && channelsProp.arraySize >= 3)
        {
            SetChannelProp(channelsProp.GetArrayElementAtIndex(0), "CH 1: CYBERTECH HUD", clip1, 10.0f, new Color(0f, 0.9f, 1f));
            SetChannelProp(channelsProp.GetArrayElementAtIndex(1), "CH 2: FUTURISTIC UI", clip2, 5.0f, new Color(1f, 0.75f, 0.1f));
            SetChannelProp(channelsProp.GetArrayElementAtIndex(2), "CH 3: SCREEN 03 HUD", clip3, 0.0f, new Color(1f, 0.25f, 0.4f));
            so.ApplyModifiedProperties();
        }
    }

    private static void SetChannelProp(SerializedProperty prop, string name, VideoClip clip, float offset, Color color)
    {
        prop.FindPropertyRelative("channelName").stringValue = name;
        prop.FindPropertyRelative("videoClip").objectReferenceValue = clip;
        prop.FindPropertyRelative("startOffsetSeconds").floatValue = offset;
        prop.FindPropertyRelative("channelThemeColor").colorValue = color;
    }

    private static GameObject CreateVirtualButton(string name, Transform parent, Vector3 localPos, Material rimMat, Material capMat, string label, Color labelCol)
    {
        GameObject btnRoot = new GameObject(name);
        btnRoot.transform.SetParent(parent, false);
        btnRoot.transform.localPosition = localPos;
        btnRoot.transform.localRotation = Quaternion.identity;

        SphereCollider col = btnRoot.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.012f;
        col.center = new Vector3(0f, 0.002f, 0f);

        // Button Outer Rim Chassis
        GameObject rimObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rimObj.name = $"{name}_Rim";
        rimObj.transform.SetParent(btnRoot.transform, false);
        rimObj.transform.localPosition = new Vector3(0f, 0.0005f, 0f);
        rimObj.transform.localScale = new Vector3(0.022f, 0.001f, 0.022f);
        StripCollider(rimObj);
        rimObj.GetComponent<MeshRenderer>().sharedMaterial = rimMat;

        // Button Pushable Cap
        GameObject capObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        capObj.name = $"{name}_Cap";
        capObj.transform.SetParent(btnRoot.transform, false);
        capObj.transform.localPosition = new Vector3(0f, 0.0015f, 0f);
        capObj.transform.localScale = new Vector3(0.018f, 0.0015f, 0.018f);
        StripCollider(capObj);
        capObj.GetComponent<MeshRenderer>().sharedMaterial = capMat;

        // Floating 3D Holographic Badge (10mm diameter cylinder hovering 8mm above cap: 0.0015m + 0.008m = 0.0095m)
        GameObject badgeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        badgeObj.name = $"{name}_Badge";
        badgeObj.transform.SetParent(btnRoot.transform, false);
        badgeObj.transform.localPosition = new Vector3(0f, 0.0095f, 0f);
        badgeObj.transform.localScale = new Vector3(0.010f, 0.0006f, 0.010f); // 10mm diameter
        StripCollider(badgeObj);
        badgeObj.GetComponent<MeshRenderer>().sharedMaterial = capMat;

        // Floating 3D Label
        CreateHUDText($"{name}_Label", btnRoot.transform, new Vector3(0f, 0.004f, 0.015f), label, 0.005f, labelCol, new Vector2(0.2f, 0.05f));

        return btnRoot;
    }

    private static void ConfigureButtonRig(HologramButtonController controller, int index, GameObject btnObj, int channelIdx, Color activeCol)
    {
        SerializedObject so = new SerializedObject(controller);
        SerializedProperty buttonsProp = so.FindProperty("buttons");
        if (buttonsProp != null && index < buttonsProp.arraySize)
        {
            SerializedProperty rigProp = buttonsProp.GetArrayElementAtIndex(index);
            rigProp.FindPropertyRelative("name").stringValue = btnObj.name;
            rigProp.FindPropertyRelative("channelIndex").intValue = channelIdx;
            rigProp.FindPropertyRelative("buttonRoot").objectReferenceValue = btnObj.transform;
            rigProp.FindPropertyRelative("buttonCap").objectReferenceValue = btnObj.transform.Find($"{btnObj.name}_Cap");
            rigProp.FindPropertyRelative("indicatorRenderer").objectReferenceValue = btnObj.transform.Find($"{btnObj.name}_Cap")?.GetComponent<Renderer>();
            rigProp.FindPropertyRelative("floatingBadge").objectReferenceValue = btnObj.transform.Find($"{btnObj.name}_Badge");
            rigProp.FindPropertyRelative("badgeRenderer").objectReferenceValue = btnObj.transform.Find($"{btnObj.name}_Badge")?.GetComponent<Renderer>();
            rigProp.FindPropertyRelative("activeColor").colorValue = activeCol;
            rigProp.FindPropertyRelative("inactiveColor").colorValue = new Color(0.2f, 0.2f, 0.2f, 1f);
            so.ApplyModifiedProperties();
        }
    }

    private static void ConfigureImageTargetObserver(GameObject targetObj, string texturePath)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture != null)
        {
            float aspect = (float)texture.height / texture.width;
            float width = 0.14f;
            float height = width * aspect;

            SerializedObject so = new SerializedObject(targetObj.GetComponent<ImageTargetBehaviour>());
            SerializedProperty trackableNameProp = so.FindProperty("mTrackableName");
            SerializedProperty widthProp = so.FindProperty("mWidth");
            SerializedProperty heightProp = so.FindProperty("mHeight");
            SerializedProperty aspectProp = so.FindProperty("mAspectRatio");
            SerializedProperty initProp = so.FindProperty("mInitializedInEditor");
            SerializedProperty upgradeProp = so.FindProperty("mTrackingOptimizationNeedsUpgrade");
            SerializedProperty previewVisibleProp = so.FindProperty("PreviewVisible");
            SerializedProperty typeProp = so.FindProperty("mImageTargetType");
            SerializedProperty runtimeTexProp = so.FindProperty("mRuntimeTexture");

            if (trackableNameProp != null) trackableNameProp.stringValue = "postcard";
            if (aspectProp != null) aspectProp.floatValue = 0.7093822f;
            if (widthProp != null) widthProp.floatValue = 0.14f;
            if (heightProp != null) heightProp.floatValue = 0.099313505f;
            if (initProp != null) initProp.intValue = 1;
            if (upgradeProp != null) upgradeProp.intValue = 0;
            if (previewVisibleProp != null) previewVisibleProp.intValue = 1;
            if (typeProp != null) typeProp.intValue = 3;
            if (runtimeTexProp != null) runtimeTexProp.objectReferenceValue = texture;

            so.ApplyModifiedProperties();
        }
    }

    private static RenderTexture CreateOrGetRenderTexture()
    {
        RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RENDER_TEXTURE_PATH);
        if (rt == null)
        {
            rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32)
            {
                name = "RT_HologramDisplay",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            AssetDatabase.CreateAsset(rt, RENDER_TEXTURE_PATH);
            AssetDatabase.SaveAssets();
        }
        return rt;
    }

    private static Material CreateOrGetScreenMaterial(RenderTexture rt)
    {
        string path = "Assets/Materials/Mat_HologramScreen.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Custom/HologramScreen");

        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader && shader != null)
        {
            mat.shader = shader;
        }

        if (rt != null) mat.SetTexture("_BaseMap", rt);
        mat.SetColor("_HoloColor", new Color(0.2f, 0.85f, 1f, 1f));
        mat.SetFloat("_Brightness", 3.2f);
        mat.SetFloat("_Alpha", 0.95f);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateOrGetBeamMaterial()
    {
        string path = "Assets/Materials/Mat_ProjectorBeam.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Custom/HologramProjectorBeam");

        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader && shader != null)
        {
            mat.shader = shader;
        }

        mat.SetColor("_BeamColor", new Color(0.1f, 0.75f, 1f, 0.5f));
        mat.SetFloat("_Intensity", 2.0f);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateOrGetMaterialWithShader(string path, string shaderName)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find(shaderName);

        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader && shader != null)
        {
            mat.shader = shader;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateOrGetMaterial(string path, Color baseColor, float smoothness, float metallic, Color? emission = null)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader uLitShader = Shader.Find("Universal Render Pipeline/Lit");

        if (mat == null)
        {
            mat = new Material(uLitShader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);

        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static TextMeshPro CreateHUDText(string name, Transform parent, Vector3 localPos, string text, float fontSize, Color color, Vector2 sizeDelta)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        textObj.transform.localPosition = localPos;
        textObj.transform.localRotation = Quaternion.identity;

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = sizeDelta;

        return tmp;
    }

    private static void StripCollider(GameObject go)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
    }

    private static void SetSerializedField(Object target, string fieldName, Object val)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = val;
            so.ApplyModifiedProperties();
        }
    }

    private static void SetSerializedFieldVector3(Object target, string fieldName, Vector3 val)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.vector3Value = val;
            so.ApplyModifiedProperties();
        }
    }

    private static void SetSerializedFieldFloat(Object target, string fieldName, float val)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.floatValue = val;
            so.ApplyModifiedProperties();
        }
    }

    private static void SetSerializedArray<T>(Object target, string fieldName, T[] elements) where T : Object
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.arraySize = elements.Length;
            for (int i = 0; i < elements.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = elements[i];
            }
            so.ApplyModifiedProperties();
        }
    }

    private static void EnsureDirectory(string dir)
    {
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
