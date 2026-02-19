using UnityEngine;

namespace Common
{
    /// <summary>
    /// Block Blast için board konfigürasyonu.
    /// ScriptableObject olarak Runtime/Data altında kaydedilecek.
    /// </summary>
    [CreateAssetMenu(fileName = "BoardConfig", menuName = "Block Blast/Board Config", order = 1)]
    public class BoardConfig : ScriptableObject
    {
        [Header("Board Boyutu")]
        [SerializeField] private int _rowCount = 10;
        [SerializeField] private int _columnCount = 10;

        [Header("Tile Ayarları")]
        [SerializeField] private float _tileSize = 0.6f;

        [Header("Oyun Ayarları")]
        [SerializeField] private int _nextPieceCount = 3;
        [SerializeField] private int _scorePerLine = 100;
        [SerializeField] private int _comboMultiplier = 2;

        // Properties
        public int RowCount => _rowCount;
        public int ColumnCount => _columnCount;
        public float TileSize => _tileSize;
        public int NextPieceCount => _nextPieceCount;
        public int ScorePerLine => _scorePerLine;
        public int ComboMultiplier => _comboMultiplier;

        /// <summary>
        /// Grid merkezini hesapla (board'u ekranın ortasına yerleştir)
        /// </summary>
        public Vector3 GetOriginPosition()
        {
            var offsetY = Mathf.Floor(_rowCount / 2.0f) * _tileSize;
            var offsetX = Mathf.Floor(_columnCount / 2.0f) * _tileSize;
            return new Vector3(-offsetX, offsetY);
        }

        /// <summary>
        /// Grid pozisyonundan dünya koordinatına çevir
        /// </summary>
        public Vector3 GetWorldPosition(int rowIndex, int columnIndex)
        {
            var origin = GetOriginPosition();
            return new Vector3(columnIndex, -rowIndex) * _tileSize + origin;
        }

        /// <summary>
        /// Grid pozisyonunu sınır kontrolü yapmadan döndür (Raw)
        /// </summary>
        public (int row, int col) GetGridPosition(Vector3 worldPosition)
        {
            var origin = GetOriginPosition();
            var relativePos = worldPosition - origin;
            
            // Note: world Y increases up, but rows increase down.
            // relativePos.y is positive above origin.
            // row 0 is at origin.y. row 1 is at origin.y - tileSize.
            // so row = -relativePos.y / tileSize.
            
            // Check formula in TryGetGridPosition:
            // int row = Mathf.RoundToInt(-relativePos.y / _tileSize);
            
            int col = Mathf.RoundToInt(relativePos.x / _tileSize);
            int row = Mathf.RoundToInt(-relativePos.y / _tileSize);
            
            return (row, col);
        }

        /// <summary>
        /// Dünya koordinatından grid pozisyonuna çevir
        /// </summary>
        public bool TryGetGridPosition(Vector3 worldPosition, out (int row, int col) gridPosition)
        {
            var raw = GetGridPosition(worldPosition);
            
            if (raw.row >= 0 && raw.row < _rowCount && raw.col >= 0 && raw.col < _columnCount)
            {
                gridPosition = raw;
                return true;
            }
            
            gridPosition = (-1, -1);
            return false;
        }

        /// <summary>
        /// Belirli bir satırın dolu olup olmadığını kontrol et
        /// </summary>
        public bool IsRowFull(bool[,] occupiedCells, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _rowCount)
                return false;
            
            for (int col = 0; col < _columnCount; col++)
            {
                if (!occupiedCells[rowIndex, col])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Belirli bir sütunun dolu olup olmadığını kontrol et
        /// </summary>
        public bool IsColumnFull(bool[,] occupiedCells, int colIndex)
        {
            if (colIndex < 0 || colIndex >= _columnCount)
                return false;
            
            for (int row = 0; row < _rowCount; row++)
            {
                if (!occupiedCells[row, colIndex])
                    return false;
            }
            return true;
        }
    }
}
