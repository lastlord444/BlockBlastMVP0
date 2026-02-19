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
            // Debug.Log($"Validator Configured with {config.name}");
        }
        
        so.ApplyModifiedProperties();
    }

    [MenuItem("Block Blast/Rebuild Gameplay UI")]
    public static void RebuildGameplayUI()
    {
        var canvas = GameObject.FindObjectOfType<Common.GameUiCanvas>();
        if (canvas == null)
        {
            Debug.LogError("GameUiCanvas not found!");
            return;
        }

        Transform gameplayPanel = canvas.transform.Find("GameplayPanel");
        if (gameplayPanel == null)
        {
            var go = new GameObject("GameplayPanel");
            go.transform.SetParent(canvas.transform, false);
            gameplayPanel = go.transform;
            
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Bottom Bar (Shape Slots)
        Transform bottomBar = gameplayPanel.Find("BottomBar");
        if (bottomBar == null)
        {
            var go = new GameObject("BottomBar");
            go.transform.SetParent(gameplayPanel, false);
            bottomBar = go.transform;

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);       // Bottom
            rect.anchorMax = new Vector2(1, 0.25f);   // 25% height
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Horizontal Layout
            var hlg = go.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 50;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
        }

        // Create 3 Slots
        var slots = new List<Common.UI.ShapeSlot>();
        for (int i = 0; i < 3; i++)
        {
            try {
                string name = $"ShapeSlot_{i}";
                Transform slotTr = bottomBar.Find(name);
                GameObject slotObj;
                if (slotTr == null)
                {
                    slotObj = new GameObject(name);
                    slotObj.transform.SetParent(bottomBar, false);
                }
                else
                {
                    slotObj = slotTr.gameObject;
                }

                var rect = slotObj.GetComponent<RectTransform>();
                if (rect == null) rect = slotObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(250, 250); // Big enough for touch

                var img = slotObj.GetComponent<UnityEngine.UI.Image>();
                if (img == null) img = slotObj.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(1, 1, 1, 0.1f); // Faint background

                var component = slotObj.GetComponent<Common.UI.ShapeSlot>();
                if (component == null) component = slotObj.AddComponent<Common.UI.ShapeSlot>();
                
                var cg = slotObj.GetComponent<CanvasGroup>();
                if (cg == null) cg = slotObj.AddComponent<CanvasGroup>();

                // Preview Root
                Transform preview = slotObj.transform.Find("PreviewRoot");
                if (preview == null)
                {
                    var pObj = new GameObject("PreviewRoot");
                    pObj.transform.SetParent(slotObj.transform, false);
                    preview = pObj.transform;
                }
                
                // Assign refs
                component.Setup(preview.GetComponent<RectTransform>(), rect, cg);
                
                slots.Add(component);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error creating slot {i}: {ex.Message}");
            }
        }

        Debug.Log("Gameplay UI Rebuilt. Checking ShapeSlots assignment...");
        
        // Auto-assign to Manager
        var manager = GameObject.FindObjectOfType<BlockBlastGameManager>();
        if (manager != null)
        {
             SerializedObject so = new SerializedObject(manager);
             var prop = so.FindProperty("_shapeSlots");
             prop.arraySize = slots.Count;
             for(int i=0; i<slots.Count; i++)
             {
                 prop.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
             }
             so.ApplyModifiedProperties();
             Debug.Log("Assigned ShapeSlots to Manager.");
        }
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
