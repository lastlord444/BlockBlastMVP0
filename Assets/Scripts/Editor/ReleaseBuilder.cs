using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

public class ReleaseBuilder
{
    // CS0618: PlayerSettings obsolete overload suppressed (Unity 6 LTS geçiş dönemi)
    #pragma warning disable CS0618

    [MenuItem("Tools/Build/Build Android RC (APK)")]
    public static void BuildAndroidRC()
    {
        Debug.Log("Starting RC APK Build Process...");
        BuildAndroid(false);
    }

    [MenuItem("Tools/Build/Build Android Release (AAB)")]
    public static void BuildAndroidAAB()
    {
        Debug.Log("Starting Release AAB Build Process...");
        BuildAndroid(true);
    }

    private static void BuildAndroid(bool isAppBundle)
    {
        // 0. Enforce Settings First
        ProjectSetup.EnforceRCSettings();

        // 1. Configure Settings
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.buildAppBundle = isAppBundle;

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        if (isAppBundle)
        {
            // Load Credentials from gitignored JSON file
            string credsPath = Path.Combine(Application.dataPath, "../keystore_credentials.json");
            if (File.Exists(credsPath))
            {
                try
                {
                    string json = File.ReadAllText(credsPath);
                    KeystoreConfig config = JsonUtility.FromJson<KeystoreConfig>(json);

                    if (string.IsNullOrEmpty(config.keystorePath) || !File.Exists(config.keystorePath))
                    {
                        throw new System.Exception($"Keystore path invalid in config: {config.keystorePath}");
                    }

                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName = config.keystorePath;
                    PlayerSettings.Android.keystorePass = config.keystorePass;
                    PlayerSettings.Android.keyaliasName = config.keyAlias;
                    PlayerSettings.Android.keyaliasPass = config.keyAliasPass;
                    Debug.Log($"Keystore configured from {credsPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load keystore credentials: {e.Message}");
                    throw;
                }
            }
            else
            {
                Debug.LogError($"Credentials file NOT FOUND at {credsPath}. Create it to build Release.");
                throw new System.Exception("Credentials missing for Release Build!");
            }

            // Increment Version Code automatically
            PlayerSettings.Android.bundleVersionCode++;
            Debug.Log($"Version Code incremented to: {PlayerSettings.Android.bundleVersionCode}");
        }

        // 2. Build Variables
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
            
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmm");
        string ext = isAppBundle ? "aab" : "apk";
        string buildPath = $"Builds/Android/BlockBlast_{(isAppBundle ? "Release" : "RC")}_{timestamp}.{ext}";

        // 3. Execute Build
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes. Path: {buildPath}");
            EditorUtility.RevealInFinder(buildPath);
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            // Fail loudly
            Debug.LogError("Build failed");
            throw new System.Exception("Build Pipeline Failed"); 
        }
    }

    [System.Serializable]
    private class KeystoreConfig
    {
        public string keystorePath;
        public string keystorePass;
        public string keyAlias;
        public string keyAliasPass;
    }

    #pragma warning restore CS0618
}
