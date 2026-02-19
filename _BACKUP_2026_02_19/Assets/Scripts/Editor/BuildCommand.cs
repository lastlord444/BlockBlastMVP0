using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;

public class BuildCommand
{
    [MenuItem("Build/SwitchToAndroid")]
    public static void SwitchToAndroid()
    {
        Debug.Log("Switching to Android Platform...");
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }

    [MenuItem("Build/BuildAndroid")]
    public static void BuildAndroidOnly()
    {
        Debug.Log("Starting Android Build...");

        // 1. Configure Settings (Android)
        // Ensure we are on Android?
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.LogError("Current Platform is NOT Android. Please run 'Build/SwitchToAndroid' first.");
            return;
        }

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        // Ensure Application Identifier is set
        if (PlayerSettings.applicationIdentifier == "com.Company.ProductName" || string.IsNullOrEmpty(PlayerSettings.applicationIdentifier))
        {
             PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.idligames.blockblastMVP");
        }
        PlayerSettings.bundleVersion = "1.0";

        Debug.Log("Build Settings Configured: Android, IL2CPP, ARM64, ID: " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));

        // 2. Build Options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/MainScene.unity" };
        buildPlayerOptions.locationPathName = "Builds/Android/BlockBlast.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        // Use Development build for logging support on device
        buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging;

        Debug.Log("Building Android APK to: " + buildPlayerOptions.locationPathName);

        // 3. Execute Build
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            Debug.Log("Output: " + summary.outputPath);
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed. See Console for details.");
            // Log details loop...
        }
    }
    [MenuItem("Build/BuildAndRun")]
    public static void BuildAndRun()
    {
        Debug.Log("Starting Android Build & Run...");

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.LogError("Current Platform is NOT Android. Please run 'Build/SwitchToAndroid' first.");
            return;
        }

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    
        // Ensure Application Identifier is set
        if (PlayerSettings.applicationIdentifier == "com.Company.ProductName" || string.IsNullOrEmpty(PlayerSettings.applicationIdentifier))
        {
             PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.idligames.blockblastMVP");
        }
        PlayerSettings.bundleVersion = "1.0";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/MainScene.unity" };
        buildPlayerOptions.locationPathName = "Builds/Android/BlockBlast.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.AutoRunPlayer;

        Debug.Log("Building & Running Android APK...");

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build & Run succeeded.");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build & Run failed.");
        }
    }
}
