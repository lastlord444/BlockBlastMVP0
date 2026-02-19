using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ProofSteps : MonoBehaviour {
    [MenuItem("Tools/Proof/Select Canvas")]
    public static void SelectCanvas() {
        var c = FindFirstObjectByType<CanvasScaler>();
        if(c) {
            Selection.activeGameObject = c.gameObject;
            EditorGUIUtility.PingObject(c.gameObject);
        } else {
            Debug.LogError("ProofSteps: CanvasScaler not found!");
        }
    }

    [MenuItem("Tools/Proof/Select BoardConfig")]
    public static void SelectBoardConfig() {
        var guids = AssetDatabase.FindAssets("BoardConfig t:ScriptableObject");
        if(guids.Length > 0) {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        } else {
            Debug.LogError("ProofSteps: BoardConfig asset not found!");
        }
    }

    [MenuItem("Tools/Proof/Open Console")]
    public static void OpenConsole() {
        EditorApplication.ExecuteMenuItem("Window/General/Console");
    }
}
