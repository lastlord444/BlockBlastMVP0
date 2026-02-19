using UnityEditor;
using UnityEngine;
using System.Linq;

public class ReleaseBuilder
{
    // CS0618: PlayerSettings obsolete overload suppressed (Unity 6 LTS geçiş dönemi)
    #pragma warning disable CS0618

    [MenuItem("Tools/Build/Build Android RC")]
    public static void BuildAndroidRC()
    {
        Debug.Log("Starting RC Build Process...");

        // 0. Enforce Settings First
        ProjectSetup.EnforceRCSettings();

        // 1. Configure Settings (Redundant but safe)
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        // Ensure KeyStore? (Skipping for MVP/Debug RC, Unity uses debug keystore by default)

        // 2. Build Variables
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
            
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmm");
        string buildPath = $"Builds/Android/BlockBlast_RC_{timestamp}.apk";

        // 3. Execute Build
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.AutoRunPlayer; // Try to Run on Device

        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes. Path: {buildPath}");
            EditorUtility.RevealInFinder(buildPath);
        }

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError("Build failed");
        }
    }

    #pragma warning restore CS0618
}
