using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.Shapes
{
    /// <summary>
    /// Block Blast için şekil üretici.
    /// 3 adet "next piece" sunar ve rastgele şekil verir.
    /// </summary>
    public class ShapeGenerator
    {
        private readonly BoardConfig _config;
        private readonly List<ShapeData> _availableShapes;
        private readonly Queue<ShapeData> _nextPieces;

        public ShapeGenerator(BoardConfig config, ShapeData[] shapeDatabase)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _availableShapes = new List<ShapeData>(shapeDatabase ?? throw new ArgumentNullException(nameof(shapeDatabase)));
            _nextPieces = new Queue<ShapeData>(_config.NextPieceCount);

            // Başlangıçta 3 şekil üret
            RefillNextPieces();
        }

        /// <summary>
        /// Sırada bekleyen 3 şekli döner
        /// </summary>
        public IReadOnlyList<ShapeData> NextPieces => 
            _nextPieces.ToArray();

        /// <summary>
        /// Bir sonraki şekli al ve kuyruğu yenile
        /// </summary>
        public ShapeData GetNextPiece()
        {
            if (_nextPieces.Count == 0)
                RefillNextPieces();

            var piece = _nextPieces.Dequeue();
            
            // Yeni bir şekil ekle
            AddRandomPiece();

            return piece;
        }

        /// <summary>
        /// Şu anki 3 şekilden hiçbiri yerleşemez mi kontrol et
        /// </summary>
        public bool IsGameOver(bool[,] occupiedCells)
        {
            foreach (var piece in _nextPieces)
            {
                if (CanPlaceAnywhere(piece, occupiedCells))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Belirli bir şeklinin board üzerinde herhangi bir yere yerleşip yerleşemeyeceğini kontrol et
        /// </summary>
        public bool CanPlaceAnywhere(ShapeData piece, bool[,] occupiedCells)
        {
            var (shapeRows, shapeCols) = piece.GetBounds();
            int boardRows = _config.RowCount;
            int boardCols = _config.ColumnCount;

            // Tüm pozisyonları dene
            for (int row = 0; row <= boardRows - shapeRows; row++)
            {
                for (int col = 0; col <= boardCols - shapeCols; col++)
                {
                    if (CanPlaceAt(piece, occupiedCells, row, col))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Belirli bir şeklinin belirli bir pozisyona yerleşip yerleşemeyeceğini kontrol et
        /// </summary>
        public bool CanPlaceAt(ShapeData piece, bool[,] occupiedCells, int startRow, int startCol)
        {
            foreach (var block in piece.Blocks)
            {
                int targetRow = startRow + block.localRow;
                int targetCol = startCol + block.localCol;

                // Board dışı mı?
                if (targetRow < 0 || targetRow >= _config.RowCount ||
                    targetCol < 0 || targetCol >= _config.ColumnCount)
                    return false;

                // Zaten dolu mu?
                if (occupiedCells[targetRow, targetCol])
                    return false;
            }
            return true;
        }

        private void RefillNextPieces()
        {
            while (_nextPieces.Count < _config.NextPieceCount)
            {
                AddRandomPiece();
            }
        }

        private void AddRandomPiece()
        {
            if (_bag.Count == 0)
            {
                RefillBag();
            }

            var nextShape = _bag.Dequeue();
            _nextPieces.Enqueue(nextShape);
        }

        private Queue<ShapeData> _bag = new Queue<ShapeData>();

        private void RefillBag()
        {
            if (_availableShapes.Count == 0) return;

            // Create a temporary list to shuffle
            var temp = new List<ShapeData>(_availableShapes);
            
            // Fisher-Yates Shuffle
            int n = temp.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (temp[k], temp[n]) = (temp[n], temp[k]);
            }

            foreach (var s in temp)
            {
                _bag.Enqueue(s);
            }
        }
    }
}
