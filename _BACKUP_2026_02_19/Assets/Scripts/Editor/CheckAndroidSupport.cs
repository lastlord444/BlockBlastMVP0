using UnityEditor;
using UnityEngine;

public class CheckAndroidSupport
{
    [MenuItem("BlockBlast/Check Android")]
    public static void Check()
    {
        bool supported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
        Debug.Log($"Android Supported: {supported}");
        Debug.Log($"Active Target: {EditorUserBuildSettings.activeBuildTarget}");
        
        string modulePath = EditorApplication.applicationPath.Replace("Editor/Unity.exe", "Editor/Data/PlaybackEngines/AndroidPlayer");
        Debug.Log($"Module Path expected: {modulePath}");
    }
}
