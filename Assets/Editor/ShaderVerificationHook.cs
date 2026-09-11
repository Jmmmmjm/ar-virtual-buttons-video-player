using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditor.Rendering;

[InitializeOnLoad]
public static class ShaderVerificationHook
{
    static ShaderVerificationHook()
    {
        EditorApplication.delayCall += RunBuildAndVerification;
    }

    [MenuItem("AR Hologram/Verify Hologram Shader & Material", false, 3)]
    public static void RunBuildAndVerification()
    {
        // Build the complete AR Hologram Scene with all geometries, materials, and wirings
        HologramSceneBuilder.BuildCompleteScene();

        // Run verification and audit
        VerifyHologramSystem();
    }

    public static void VerifyHologramSystem()
    {
        string reportPath = "Assets/Editor/ShaderVerificationReport.txt";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=================================================");
        sb.AppendLine("=== HOLOGRAM SHADER & MATERIAL VERIFICATION ===");
        sb.AppendLine("=================================================");
        sb.AppendLine($"Timestamp: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        int totalErrors = 0;
        int totalWarnings = 0;

        string[] shaderNames = new string[]
        {
            "Custom/HologramScreen",
            "Custom/HologramEmitterRing",
            "Custom/HologramDepthGrid"
        };

        foreach (string sName in shaderNames)
        {
            sb.AppendLine($"\n--- SHADER: {sName} ---");
            Shader shader = Shader.Find(sName);
            if (shader == null)
            {
                sb.AppendLine($"ERROR: Shader '{sName}' not found!");
                totalErrors++;
                continue;
            }

            sb.AppendLine($"Shader isSupported: {shader.isSupported}");
            int msgCount = ShaderUtil.GetShaderMessageCount(shader);
            var messages = ShaderUtil.GetShaderMessages(shader);

            int errorCount = 0;
            int warningCount = 0;
            sb.AppendLine($"Shader Message Count: {msgCount}");
            foreach (var msg in messages)
            {
                sb.AppendLine($"  [{msg.severity}] line {msg.line}: {msg.message}");
                if (msg.severity == ShaderCompilerMessageSeverity.Error) errorCount++;
                else if (msg.severity == ShaderCompilerMessageSeverity.Warning) warningCount++;
            }
            sb.AppendLine($"Compilation Result: Errors={errorCount}, Warnings={warningCount}");
            sb.AppendLine(errorCount == 0 ? "STATUS: PASS (Zero Errors)" : "STATUS: FAIL");
            totalErrors += errorCount;
            totalWarnings += warningCount;
        }

        // Check & Update Material Defaults for Mat_HologramScreen
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_HologramScreen.mat");
        if (mat != null)
        {
            sb.AppendLine("\n--- MATERIAL DEFAULTS CHECK: Mat_HologramScreen ---");
            sb.AppendLine($"Shader assigned: {mat.shader.name}");

            if (!mat.HasProperty("_HexGridIntensity") || mat.GetFloat("_HexGridIntensity") == 0f)
                mat.SetFloat("_HexGridIntensity", 0.35f);
            if (!mat.HasProperty("_HexGridScale") || mat.GetFloat("_HexGridScale") == 0f)
                mat.SetFloat("_HexGridScale", 45.0f);
            if (!mat.HasProperty("_FresnelIridescence") || mat.GetFloat("_FresnelIridescence") == 0f)
                mat.SetFloat("_FresnelIridescence", 0.85f);
            if (!mat.HasProperty("_SecondarySweepSpeed") || mat.GetFloat("_SecondarySweepSpeed") == 0f)
                mat.SetFloat("_SecondarySweepSpeed", -2.6f);
            if (!mat.HasProperty("_SecondarySweepWidth") || mat.GetFloat("_SecondarySweepWidth") == 0f)
                mat.SetFloat("_SecondarySweepWidth", 0.04f);
            if (!mat.HasProperty("_SecondarySweepIntensity") || mat.GetFloat("_SecondarySweepIntensity") == 0f)
                mat.SetFloat("_SecondarySweepIntensity", 0.35f);
            if (!mat.HasProperty("_AnamorphicStreak") || mat.GetFloat("_AnamorphicStreak") == 0f)
                mat.SetFloat("_AnamorphicStreak", 0.25f);
            EditorUtility.SetDirty(mat);

            sb.AppendLine($"_Brightness: {mat.GetFloat("_Brightness")} (Expected: 3.2)");
            sb.AppendLine($"_EmissionMultiplier: {mat.GetFloat("_EmissionMultiplier")} (Expected: 2.5)");
            sb.AppendLine($"_Saturation: {mat.GetFloat("_Saturation")} (Expected: 1.4)");
            sb.AppendLine($"_Alpha: {mat.GetFloat("_Alpha")} (Expected: 0.95)");
            sb.AppendLine($"_HexGridIntensity: {mat.GetFloat("_HexGridIntensity")} (Expected: 0.35)");
            sb.AppendLine($"_HexGridScale: {mat.GetFloat("_HexGridScale")} (Expected: 45.0)");
            sb.AppendLine($"_MacroblockGlitch: {mat.GetFloat("_MacroblockGlitch")} (Expected: 0.0)");
            sb.AppendLine($"_FresnelIridescence: {mat.GetFloat("_FresnelIridescence")} (Expected: 0.85)");
            sb.AppendLine($"_SecondarySweepSpeed: {mat.GetFloat("_SecondarySweepSpeed")} (Expected: -2.6)");
            sb.AppendLine($"_SecondarySweepWidth: {mat.GetFloat("_SecondarySweepWidth")} (Expected: 0.04)");
            sb.AppendLine($"_SecondarySweepIntensity: {mat.GetFloat("_SecondarySweepIntensity")} (Expected: 0.35)");
            sb.AppendLine($"_AnamorphicStreak: {mat.GetFloat("_AnamorphicStreak")} (Expected: 0.25)");
            sb.AppendLine($"_PhosphorGlow: {mat.GetFloat("_PhosphorGlow")} (Expected: 0.35)");
            sb.AppendLine($"_HoloColor: {mat.GetColor("_HoloColor")}");
        }

        // Create or verify test materials for EmitterRing and DepthGrid
        EnsureMaterial("Assets/Materials/Mat_HologramEmitterRing.mat", "Custom/HologramEmitterRing", sb);
        EnsureMaterial("Assets/Materials/Mat_HologramDepthGrid.mat", "Custom/HologramDepthGrid", sb);

        sb.AppendLine("\n=================================================");
        sb.AppendLine($"OVERALL VERIFICATION: Total Errors={totalErrors}, Total Warnings={totalWarnings}");
        sb.AppendLine(totalErrors == 0 ? "ALL SHADERS COMPILED CLEANLY: PASS (ZERO ERRORS)" : "VERIFICATION FAILED");
        sb.AppendLine("=================================================");

        File.WriteAllText(reportPath, sb.ToString());
        Debug.Log(sb.ToString());

        HologramSelfAudit.RunFullSelfAudit();
    }

    private static void EnsureMaterial(string matPath, string shaderName, StringBuilder sb)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader s = Shader.Find(shaderName);
        if (s == null) return;

        if (m == null)
        {
            m = new Material(s);
            AssetDatabase.CreateAsset(m, matPath);
            sb.AppendLine($"Created material: {matPath} with shader {shaderName}");
        }
        else if (m.shader != s)
        {
            m.shader = s;
            EditorUtility.SetDirty(m);
            sb.AppendLine($"Updated material: {matPath} shader to {shaderName}");
        }
    }
}
