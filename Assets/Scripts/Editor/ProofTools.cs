using UnityEditor;
using UnityEngine;

public class ProofTools
{
    [MenuItem("Tools/Proof/Open Player Settings")]
    public static void OpenPlayerSettings()
    {
        SettingsService.OpenProjectSettings("Project/Player");
    }
}
