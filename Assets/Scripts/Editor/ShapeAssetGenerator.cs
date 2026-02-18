#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Common.Shapes;
using System.IO;

public class ShapeAssetGenerator
{
    [MenuItem("Block Blast/Generate Classic Shapes")]
    public static void GenerateShapes()
    {
        string path = "Assets/Resources/Shapes";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        CreateShape("Single_1x1", ShapeType.Single, Color.red, new List<ShapeBlock> { new(0,0) });
        
        CreateShape("Line_2", ShapeType.Line2, Color.cyan, new List<ShapeBlock> { new(0,0), new(0,1) });
        CreateShape("Line_2_V", ShapeType.Line2, Color.cyan, new List<ShapeBlock> { new(0,0), new(1,0) });

        CreateShape("Line_3", ShapeType.Line3, Color.blue, new List<ShapeBlock> { new(0,0), new(0,1), new(0,2) });
        CreateShape("Line_3_V", ShapeType.Line3, Color.blue, new List<ShapeBlock> { new(0,0), new(1,0), new(2,0) });

        CreateShape("Line_4", ShapeType.Line4, Color.yellow, new List<ShapeBlock> { new(0,0), new(0,1), new(0,2), new(0,3) });
        CreateShape("Line_4_V", ShapeType.Line4, Color.yellow, new List<ShapeBlock> { new(0,0), new(1,0), new(2,0), new(3,0) });

        CreateShape("Square_2x2", ShapeType.Square2x2, Color.green, new List<ShapeBlock> { new(0,0), new(0,1), new(1,0), new(1,1) });
        
        CreateShape("Square_3x3", ShapeType.Square3x3, Color.magenta, new List<ShapeBlock> { 
            new(0,0), new(0,1), new(0,2),
            new(1,0), new(1,1), new(1,2),
            new(2,0), new(2,1), new(2,2)
        });

        // L Shapes
        CreateShape("L3", ShapeType.L3, new Color(1f, 0.5f, 0f), new List<ShapeBlock> { new(0,0), new(1,0), new(1,1) }); // 2x2 L
        CreateShape("L4", ShapeType.L4, new Color(1f, 0.5f, 0f), new List<ShapeBlock> { new(0,0), new(1,0), new(2,0), new(2,1) }); // 3x2 L

        // T Shape
        CreateShape("T4", ShapeType.T4, Color.white, new List<ShapeBlock> { new(0,0), new(0,1), new(0,2), new(1,1) });

        AssetDatabase.Refresh();
        Debug.Log("Shapes generated in Assets/Resources/Shapes");
    }

    private static void CreateShape(string name, ShapeType type, Color color, List<ShapeBlock> blocks)
    {
        string path = $"Assets/Resources/Shapes/{name}.asset";
        
        var asset = ScriptableObject.CreateInstance<ShapeData>();
        
        // Use reflection to set private fields since we are in a generator
        // Alternatively, open ShapeData and make fields public or add init method.
        // For now, let's assume we can modify ShapeData to have a public Init/Set method OR use SerializedObject.
        
        // Better: Use SerializedObject to avoid modifying ShapeData just for this tool if possible,
        // but ShapeData is inside `Common.Shapes`.
        
        // Let's rely on JSON overwrite or SerializedObject.
        // Since we are creating new instance, direct field access is blocked.
        // We will modify ShapeData.cs temporarily to have 'SetData' or use SerializedObject.
        
        UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(asset);
        so.FindProperty("_shapeType").enumValueIndex = (int)type;
        so.FindProperty("_shapeColor").colorValue = color;
        
        var blocksProp = so.FindProperty("_blocks");
        blocksProp.ClearArray();
        blocksProp.arraySize = blocks.Count;
        for(int i=0; i<blocks.Count; i++)
        {
            var elem = blocksProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("localRow").intValue = blocks[i].localRow;
            elem.FindPropertyRelative("localCol").intValue = blocks[i].localCol;
        }
        so.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(asset, path);
    }
}
#endif
