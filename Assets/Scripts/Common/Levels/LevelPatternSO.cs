using System.Collections.Generic;
using UnityEngine;
using Match3.Core.Structs;

namespace Common.Levels
{
    [CreateAssetMenu(fileName = "LevelPattern", menuName = "Block Blast/Level Pattern", order = 1)]
    public class LevelPatternSO : ScriptableObject
    {
        [Header("Pattern Identity")]
        public string id;

        [Header("Pattern Configuration")]
        [Tooltip("List of coordinatePairs (Row, Col) that should be pre-filled.")]
        public List<Vector2Int> PreFilledCells = new List<Vector2Int>();
        
        [Tooltip("Color for the pre-filled cells.")]
        public Color FillColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Grey default
    }
}
