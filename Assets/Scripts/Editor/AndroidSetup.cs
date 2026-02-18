using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public class AndroidSetup
{
    [MenuItem("Build/Setup Android Portrait")]
    public static void Setup()
    {
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        
        EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        Debug.Log("Android Portrait Setup Complete.");
    }
}
