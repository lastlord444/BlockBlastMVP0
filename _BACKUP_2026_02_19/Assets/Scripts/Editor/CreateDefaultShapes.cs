using UnityEngine;
using UnityEditor;
using Common.Shapes;
using System.Collections.Generic;
using System.IO;

public class CreateDefaultShapes
{
    [MenuItem("BlockBlast/Create Default Shapes")]
    public static void Create()
    {
        string path = "Assets/Resources/Shapes";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        // Single (1x1)
        CreateShape(ShapeType.Single, new List<ShapeBlock> { new ShapeBlock(0,0) }, Color.red, "Single");

        // Line 2 (Horizontal)
        CreateShape(ShapeType.Line2, new List<ShapeBlock> { new ShapeBlock(0,0), new ShapeBlock(0,1) }, Color.cyan, "Line2");
        // Line 2 (Vertical) - Should we create rotated versions? Game handles rotation? 
        // Usually Block Blast has fixed shapes. Or we provide both orientations. 
        // For MVP, I'll stick to basic versions. BlockBlast has no rotation usually (unless powerup).
        // I'll add vertical versions distinct names? 
        // Common Block Blast has: 1x1, 2x1, 1x2, 3x1, 1x3, 4x1, 1x4, 5x1, 1x5, 2x2, 3x3, L shapes (4 rotations).
        // I'll create a few variations.
        
        CreateShape(ShapeType.Line2, new List<ShapeBlock> { new ShapeBlock(0,0), new ShapeBlock(1,0) }, Color.cyan, "Line2_V");

        // Line 3
        CreateShape(ShapeType.Line3, new List<ShapeBlock> { new ShapeBlock(0,0), new ShapeBlock(0,1), new ShapeBlock(0,2) }, Color.blue, "Line3");
        CreateShape(ShapeType.Line3, new List<ShapeBlock> { new ShapeBlock(0,0), new ShapeBlock(1,0), new ShapeBlock(2,0) }, Color.blue, "Line3_V");

        // Square 2x2
        CreateShape(ShapeType.Square2x2, new List<ShapeBlock> { 
            new ShapeBlock(0,0), new ShapeBlock(0,1), 
            new ShapeBlock(1,0), new ShapeBlock(1,1) 
        }, Color.green, "Square2x2");

        // L3 (Corner)
        CreateShape(ShapeType.L3, new List<ShapeBlock> { new ShapeBlock(0,0), new ShapeBlock(1,0), new ShapeBlock(1,1) }, Color.yellow, "L3");

        // T4
        CreateShape(ShapeType.T4, new List<ShapeBlock> { new ShapeBlock(0,0), new ShapeBlock(0,1), new ShapeBlock(0,2), new ShapeBlock(1,1) }, Color.magenta, "T4");

        // Z4
        CreateShape(ShapeType.Z4, new List<ShapeBlock> { new ShapeBlock(0,0), new ShapeBlock(0,1), new ShapeBlock(1,1), new ShapeBlock(1,2) }, Color.red, "Z4");
        CreateShape(ShapeType.Z4, new List<ShapeBlock> { new ShapeBlock(0,1), new ShapeBlock(1,0), new ShapeBlock(1,1), new ShapeBlock(2,0) }, Color.red, "Z4_V");

        // S4
        CreateShape(ShapeType.S4, new List<ShapeBlock> { new ShapeBlock(0,1), new ShapeBlock(0,2), new ShapeBlock(1,0), new ShapeBlock(1,1) }, Color.green, "S4");
        CreateShape(ShapeType.S4, new List<ShapeBlock> { new ShapeBlock(0,0), new ShapeBlock(1,0), new ShapeBlock(1,1), new ShapeBlock(2,1) }, Color.green, "S4_V");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created default shapes (including S/Z/T) in " + path);
    }

    private static void CreateShape(ShapeType type, List<ShapeBlock> blocks, Color color, string name)
    {
        ShapeData asset = ScriptableObject.CreateInstance<ShapeData>();
        asset.Init(type, color, blocks);
        
        string fullPath = $"Assets/Resources/Shapes/{name}.asset";
        AssetDatabase.CreateAsset(asset, fullPath);
    }
}
