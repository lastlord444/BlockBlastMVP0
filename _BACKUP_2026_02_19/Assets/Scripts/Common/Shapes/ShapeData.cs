using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.Shapes
{
    /// <summary>
    /// Block Blast için şekil tipleri
    /// </summary>
    public enum ShapeType
    {
        Single,      // 1x1
        Line2,       // 1x2 veya 2x1
        Line3,       // 1x3 veya 3x1
        Line4,       // 1x4 veya 4x1
        Square2x2,   // 2x2 kare
        Square3x3,   // 3x3 kare
        L3,          // L şekli 3 blok
        L4,          // L şekli 4 blok
        T4,          // T şekli 4 blok
        Z4,          // Z şekli 4 blok
        S4,          // S şekli 4 blok (Z'nin tersi)
        Corner2x2    // 2x2 köşe (3 blok)
    }

    /// <summary>
    /// Bir şeklinin bloklarını temsil eder
    /// </summary>
    [Serializable]
    public struct ShapeBlock
    {
        public int localRow;  // Şekil içindeki yerel satır
        public int localCol;  // Şekil içindeki yerel sütun

        public ShapeBlock(int row, int col)
        {
            localRow = row;
            localCol = col;
        }

        public override string ToString() => $"({localRow},{localCol})";
    }

    /// <summary>
    /// Block Blast şekli verisi
    /// </summary>
    [CreateAssetMenu(fileName = "ShapeData", menuName = "Block Blast/Shape Data", order = 2)]
    public class ShapeData : ScriptableObject
    {
        [Header("Şekil Bilgisi")]
        [SerializeField] private ShapeType _shapeType;
        [SerializeField] private Color _shapeColor = Color.white;

        [Header("Blok Koordinatları (shape-local)")]
        [SerializeField] private List<ShapeBlock> _blocks = new List<ShapeBlock>();

        public ShapeType ShapeType => _shapeType;
        public Color ShapeColor => _shapeColor;
        public IReadOnlyList<ShapeBlock> Blocks => _blocks;
        public int BlockCount => _blocks.Count;

        public void Init(ShapeType type, Color color, List<ShapeBlock> blocks)
        {
            _shapeType = type;
            _shapeColor = color;
            _blocks = blocks;
            NormalizeBlocks();
        }

        /// <summary>
        /// Şeklinin sınırlarını (bounding box) hesapla
        /// </summary>
        public (int rows, int cols) GetBounds()
        {
            if (_blocks.Count == 0)
                return (0, 0);

            int minRow = int.MaxValue, maxRow = int.MinValue;
            int minCol = int.MaxValue, maxCol = int.MinValue;

            foreach (var block in _blocks)
            {
                minRow = Mathf.Min(minRow, block.localRow);
                maxRow = Mathf.Max(maxRow, block.localRow);
                minCol = Mathf.Min(minCol, block.localCol);
                maxCol = Mathf.Max(maxCol, block.localCol);
            }

            return (maxRow - minRow + 1, maxCol - minCol + 1);
        }

        /// <summary>
        /// Blok koordinatlarını normalize et (0,0'dan başlasın)
        /// </summary>
        public void NormalizeBlocks()
        {
            if (_blocks.Count == 0)
                return;

            int minRow = int.MaxValue, minCol = int.MaxValue;

            foreach (var block in _blocks)
            {
                minRow = Mathf.Min(minRow, block.localRow);
                minCol = Mathf.Min(minCol, block.localCol);
            }

            // MinRow ve MinCol negatifse 0'a, değilse değeri çıkar
            int rowOffset = minRow < 0 ? -minRow : 0;
            int colOffset = minCol < 0 ? -minCol : 0;

            for (int i = 0; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                _blocks[i] = new ShapeBlock(block.localRow + rowOffset, block.localCol + colOffset);
            }
        }

        private void OnValidate()
        {
            NormalizeBlocks();
        }
    }
}
