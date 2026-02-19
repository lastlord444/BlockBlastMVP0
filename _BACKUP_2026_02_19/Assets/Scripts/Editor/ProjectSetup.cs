using UnityEditor;
using UnityEngine;

public class ProjectSetup
{
    // CS0618: PlayerSettings obsolete overload suppressed (Unity 6 LTS geçiş dönemi,
    // NamedBuildTarget bu proje sürümünde mevcut değil)
    #pragma warning disable CS0618
    [MenuItem("Tools/Setup/Enforce RC Settings")]
    public static void EnforceRCSettings()
    {
        // 1. Company & Product Name
        PlayerSettings.companyName = "IdlGames";
        PlayerSettings.productName = "Block Blast";

        // 2. Package Name / Application Identifier
        // Note: SetApplicationIdentifier is per-platform.
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.idlgames.blockblast");
        
        // 3. Versioning (Optional, good practice)
        PlayerSettings.bundleVersion = "1.0.1";
        PlayerSettings.Android.bundleVersionCode = 2; // Incrementing
        
        // 4. Architectures & Backend
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        // 5. Build Settings (Scenes)
        var mainScene = new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true);
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { mainScene };
        
        Debug.Log($"[ProjectSetup] Enforced Settings:\n" +
                  $"- Company: {PlayerSettings.companyName}\n" +
                  $"- Product: {PlayerSettings.productName}\n" +
                  $"- Package: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)}\n" +
                  $"- Scenes: {EditorBuildSettings.scenes.Length} (MainScene only)\n" +
                  $"- Arch: IL2CPP / ARM64");
    }
    #pragma warning restore CS0618
}
