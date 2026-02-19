using UnityEditor;
using UnityEngine;

public class AndroidBuildScript
{
    [MenuItem("Build/Android")]
    public static void ConfigureAndBuild()
    {
        // 1. Switch Platform
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        // 2. Player Settings
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        // 3. Build
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/MainScene.unity" }; // Assuming MainScene is the one
        buildPlayerOptions.locationPathName = "Builds/Android/BlockBlastMVP0.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"Build failed: {report.summary.totalErrors} errors");
        }
    }
}
