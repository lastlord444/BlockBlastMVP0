using UnityEngine;
using UnityEditor;
using Common.GameModes;
using Common;
using Common.Interfaces;
using Common.Shapes;
using Common.Levels;
using Common.Models;
using Common.Enums;
using System.Linq;
using System.Collections.Generic;

public class SceneAutoRepair : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Block Blast/Repair Scene References")]
    public static void RepairScene()
    {
        RepairGameManager();
        RepairBoardRenderer();
        SetupValidator();
    }

    private static void SetupValidator()
    {
        var manager = GameObject.FindObjectOfType<BlockBlastGameManager>();
        if (manager == null) return;

        var validator = manager.GetComponent<StartupReferenceValidator>();
        if (validator == null) validator = manager.gameObject.AddComponent<StartupReferenceValidator>();

        SerializedObject so = new SerializedObject(validator);
        so.FindProperty("_gameManager").objectReferenceValue = manager;
        
        string[] guids = AssetDatabase.FindAssets("t:BoardConfig");
        if (guids.Length > 0)
        {
            var config = AssetDatabase.LoadAssetAtPath<BoardConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
            so.FindProperty("_boardConfig").objectReferenceValue = config;
            Debug.Log($"Validator Configured with {config.name}");
        }
        
        so.ApplyModifiedProperties();
    }

    private static void RepairGameManager()
    {
        var manager = GameObject.FindObjectOfType<BlockBlastGameManager>();
        if (manager == null)
        {
            Debug.LogError("No BlockBlastGameManager found!");
            return;
        }

        SerializedObject so = new SerializedObject(manager);

        // 1. Board Config
        var configProp = so.FindProperty("_boardConfig");
        if (configProp.objectReferenceValue == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:BoardConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var config = AssetDatabase.LoadAssetAtPath<BoardConfig>(path);
                configProp.objectReferenceValue = config;
                Debug.Log($"Assigned BoardConfig: {config.name}");
            }
        }

        // 2. Board Renderer
        var rendererProp = so.FindProperty("_boardRenderer");
        if (rendererProp.objectReferenceValue == null)
        {
            var renderer = GameObject.FindObjectOfType<UnityGameBoardRenderer>();
            if (renderer != null)
            {
                rendererProp.objectReferenceValue = renderer;
                Debug.Log($"Assigned BoardRenderer: {renderer.name}");
            }
        }

        // 3. Input Manager
        var inputProp = so.FindProperty("_inputManager");
        if (inputProp.objectReferenceValue == null)
        {
            var input = GameObject.FindObjectOfType<BlockBlastInputManager>();
            if (input != null)
            {
                inputProp.objectReferenceValue = input;
                Debug.Log($"Assigned InputManager: {input.name}");
            }
        }

        // 4. Shape Database
        var shapeDbProp = so.FindProperty("_shapeDatabase");
        string[] shapeGuids = AssetDatabase.FindAssets("t:ShapeData");
        var shapes = shapeGuids.Select(g => AssetDatabase.LoadAssetAtPath<ShapeData>(AssetDatabase.GUIDToAssetPath(g))).Where(s => s != null).ToArray();
        
        shapeDbProp.arraySize = shapes.Length;
        for (int i = 0; i < shapes.Length; i++)
        {
            shapeDbProp.GetArrayElementAtIndex(i).objectReferenceValue = shapes[i];
        }
        Debug.Log($"Assigned {shapes.Length} ShapeData assets.");

        // 5. Adventure Database
        var advProp = so.FindProperty("_adventureDatabase");
        if (advProp.objectReferenceValue == null)
        {
            string[] advGuids = AssetDatabase.FindAssets("t:LevelPatternDatabaseSO");
            if (advGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(advGuids[0]);
                var db = AssetDatabase.LoadAssetAtPath<LevelPatternDatabaseSO>(path);
                advProp.objectReferenceValue = db;
                Debug.Log($"Assigned AdventureDatabase: {db.name}");
            }
        }

        so.ApplyModifiedProperties();
        Debug.Log("GameManager Repair Complete!");
    }

    private static void RepairBoardRenderer()
    {
        var renderer = GameObject.FindObjectOfType<UnityGameBoardRenderer>();
        if (renderer == null) return;

        SerializedObject so = new SerializedObject(renderer);
        var tilesProp = so.FindProperty("_gridTiles");

        // If empty or user requests force update
        // Let's force update to ensure valid list
        
        List<TileModel> newModels = new List<TileModel>();

        // Find Prefabs
        GameObject availablePrefab = FindPrefab("AvailableTilePrefab");
        GameObject unavailablePrefab = FindPrefab("UnavailableTilePrefab");
        GameObject icePrefab = FindPrefab("IceTilePrefab");
        GameObject stonePrefab = FindPrefab("StoneTilePrefab");

        if (availablePrefab) newModels.Add(new TileModel { _group = TileGroup.Available, _prefab = availablePrefab });
        if (unavailablePrefab) newModels.Add(new TileModel { _group = TileGroup.Unavailable, _prefab = unavailablePrefab });
        if (icePrefab) newModels.Add(new TileModel { _group = TileGroup.Ice, _prefab = icePrefab });
        if (stonePrefab) newModels.Add(new TileModel { _group = TileGroup.Stone, _prefab = stonePrefab });

        // Apply
        tilesProp.arraySize = newModels.Count;
        for(int i=0; i<newModels.Count; i++)
        {
            var element = tilesProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("_group").enumValueIndex = (int)newModels[i]._group;
            element.FindPropertyRelative("_prefab").objectReferenceValue = newModels[i]._prefab;
        }

        so.ApplyModifiedProperties();
        Debug.Log($"BoardRenderer Repair Complete! Assigned {newModels.Count} tiles.");
    }

    private static GameObject FindPrefab(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null;
    }


    // Helper class for list construction
    public class TileModel
    {
        public TileGroup _group;
        public GameObject _prefab;
    }
#endif
}
