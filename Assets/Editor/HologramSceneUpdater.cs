using UnityEditor;
using UnityEngine;

public static class HologramSceneUpdater
{
    private const string SESSION_KEY = "HologramSceneUpdater_Executed_v5";

    [InitializeOnLoadMethod]
    private static void OnEditorReload()
    {
        if (!SessionState.GetBool(SESSION_KEY, false))
        {
            SessionState.SetBool(SESSION_KEY, true);
            EditorApplication.delayCall += () =>
            {
                ExecuteUpdate();
            };
        }
    }

    [MenuItem("AR Hologram/Apply Scene Fixes and Audit", false, 3)]
    public static void ExecuteUpdate()
    {
        Debug.Log("[HologramSceneUpdater] Starting scene build and audit...");
        HologramSceneBuilder.BuildCompleteScene();
        bool passed = HologramSelfAudit.RunFullSelfAudit();
        Debug.Log($"[HologramSceneUpdater] Completed. All passed: {passed}");
    }
}
