using System;
using System.Collections.Generic;
using Common.Enums;
using Common.Interfaces;
using Common.Models;
using Match3.App.Interfaces;
using Match3.Core;
using Match3.Core.Structs;
using UnityEngine;

namespace Common
{
    public class UnityGameBoardRenderer : MonoBehaviour, IUnityGameBoardRenderer, IGameBoardDataProvider<IUnityGridSlot>
    {
        [Header("Block Blast Configuration")]
        [SerializeField] private BoardConfig _boardConfig;

        [Space]
        [SerializeField] private TileModel[] _gridTiles;

        private IGridTile[,] _gridSlotTiles;
        private IUnityGridSlot[,] _gameBoardSlots;

        private Vector3 _originPosition;
        private TileItemsPool _tileItemsPool;

        // Board config properties (cached)
        private int RowCount => _boardConfig.RowCount;
        private int ColumnCount => _boardConfig.ColumnCount;
        private float TileSize => _boardConfig.TileSize;

        private void Awake()
        {
            // Auto-Healing: Load Tiles from Resources if missing
            if (_gridTiles == null || _gridTiles.Length == 0)
            {
                Debug.LogWarning("GridTiles missing. Attempting to load from Resources/Tiles...");
                var loadedTiles = new List<TileModel>();
                
                var available = Resources.Load<GameObject>("Tiles/AvailableTilePrefab");
                if(available) loadedTiles.Add(new TileModel(TileGroup.Available, available));

                var unavailable = Resources.Load<GameObject>("Tiles/UnavailableTilePrefab");
                if(unavailable) loadedTiles.Add(new TileModel(TileGroup.Unavailable, unavailable));
                
                var ice = Resources.Load<GameObject>("Tiles/IceTilePrefab");
                if(ice) loadedTiles.Add(new TileModel(TileGroup.Ice, ice));
                
                var stone = Resources.Load<GameObject>("Tiles/StoneTilePrefab");
                if(stone) loadedTiles.Add(new TileModel(TileGroup.Stone, stone));

                if (loadedTiles.Count > 0)
                {
                    _gridTiles = loadedTiles.ToArray();
                    Debug.Log($"Restored {_gridTiles.Length} tiles from Resources.");
                }
                else
                {
                    Debug.LogError("CRITICAL: Could not load tiles from Resources/Tiles! Ensure prefabs are in Assets/Resources/Tiles.");
                    _gridTiles = new TileModel[0]; // Avoid crash in pool constructor
                }
            }
            
            _tileItemsPool = new TileItemsPool(_gridTiles, transform);
            
            // BoardConfig kontrolü ve otomatik yükleme
            if (_boardConfig == null)
            {
                _boardConfig = Resources.Load<BoardConfig>("BoardConfig");
                if (_boardConfig == null)
                {
                    Debug.LogError("BoardConfig atanmamış ve Resources'da bulunamadı! Assets/Resources/BoardConfig.asset oluştur ve assign et.");
                }
            }
        }

        public IUnityGridSlot[,] GetGameBoardSlots(int level)
        {
            return _gameBoardSlots;
        }

        public void SetBoardConfig(BoardConfig config)
        {
            _boardConfig = config;
        }

        public BoardConfig GetBoardConfig()
        {
            return _boardConfig;
        }

        public void CreateGridTiles(int[,] data)
        {
            _gridSlotTiles = new IGridTile[RowCount, ColumnCount];
            _gameBoardSlots = new IUnityGridSlot[RowCount, ColumnCount];
            _originPosition = _boardConfig.GetOriginPosition();

            CreateGridTiles(TileGroup.Available);
        }

        public bool IsTileActive(GridPosition gridPosition)
        {
            return GetTileGroup(gridPosition) != TileGroup.Unavailable;
        }

        public void ActivateTile(GridPosition gridPosition)
        {
            SetTile(gridPosition.RowIndex, gridPosition.ColumnIndex, TileGroup.Available);
        }

        public void DeactivateTile(GridPosition gridPosition)
        {
            SetTile(gridPosition.RowIndex, gridPosition.ColumnIndex, TileGroup.Unavailable);
        }

        public void SetNextGridTileGroup(GridPosition gridPosition)
        {
            var tileGroup = GetTileGroup(gridPosition);
            SetTile(gridPosition.RowIndex, gridPosition.ColumnIndex, GetNextAvailableGroup(tileGroup));
        }

        public bool IsPointerOnGrid(Vector3 worldPointerPosition, out GridPosition gridPosition)
        {
            gridPosition = GetGridPositionByPointer(worldPointerPosition);
            return IsPositionOnGrid(gridPosition);
        }

        public bool IsPointerOnBoard(Vector3 worldPointerPosition, out GridPosition gridPosition)
        {
            gridPosition = GetGridPositionByPointer(worldPointerPosition);
            return IsPositionOnBoard(gridPosition);
        }

        public bool IsPositionOnGrid(GridPosition gridPosition)
        {
            return GridMath.IsPositionOnGrid(gridPosition, RowCount, ColumnCount);
        }

        public Vector3 GetWorldPosition(GridPosition gridPosition)
        {
            return _boardConfig.GetWorldPosition(gridPosition.RowIndex, gridPosition.ColumnIndex);
        }

        public TileGroup GetTileGroup(GridPosition gridPosition)
        {
            return (TileGroup) _gridSlotTiles[gridPosition.RowIndex, gridPosition.ColumnIndex].GroupId;
        }

        public void SetTileColor(GridPosition gridPosition, Color color)
        {
            if (_gridSlotTiles == null) return;
            
            if(IsPositionOnGrid(gridPosition))
            {
                var tile = _gridSlotTiles[gridPosition.RowIndex, gridPosition.ColumnIndex];
                if (tile != null)
                {
                    tile.SetColor(color);
                }
            }
        }

        public Transform GetTileTransform(int r, int c)
        {
            var gp = new GridPosition(r, c);
            if (_gridSlotTiles == null || !IsPositionOnGrid(gp)) return null;
            var tile = _gridSlotTiles[r, c];
            if (tile is MonoBehaviour mb) return mb.transform;
            return null;
        }

        public void ResetGridTiles()
        {
            SetTilesGroup(TileGroup.Available);
        }

        public void Dispose()
        {
            DisposeGridTiles();
            DisposeGameBoardData();
        }

        private bool IsPositionOnBoard(GridPosition gridPosition)
        {
            return IsPositionOnGrid(gridPosition) && IsTileActive(gridPosition);
        }

        public GridPosition GetRawGridPosition(Vector3 worldPointerPosition)
        {
            var raw = _boardConfig.GetGridPosition(worldPointerPosition);
            return new GridPosition(raw.row, raw.col);
        }

        private GridPosition GetGridPositionByPointer(Vector3 worldPointerPosition)
        {
            if (_boardConfig.TryGetGridPosition(worldPointerPosition, out var gridPos))
            {
                return new GridPosition(gridPos.row, gridPos.col);
            }
            return new GridPosition(-1, -1);
        }

        private void CreateGridTiles(TileGroup defaultTileGroup)
        {
            for (var rowIndex = 0; rowIndex < RowCount; rowIndex++)
            {
                for (var columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
                {
                    var gridTile = GetTile(rowIndex, columnIndex, defaultTileGroup);

                    _gridSlotTiles[rowIndex, columnIndex] = gridTile;
                    _gameBoardSlots[rowIndex, columnIndex] =
                        new UnityGridSlot(gridTile, new GridPosition(rowIndex, columnIndex));
                }
            }
        }

        private void SetTilesGroup(TileGroup group)
        {
            for (var rowIndex = 0; rowIndex < RowCount; rowIndex++)
            {
                for (var columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
                {
                    SetTile(rowIndex, columnIndex, group);
                }
            }
        }

        private void SetTile(int rowIndex, int columnIndex, TileGroup group)
        {
            var currentTile = _gridSlotTiles[rowIndex, columnIndex];
            if (currentTile != null)
            {
                _tileItemsPool.ReturnGridTile(currentTile);
            }

            var gridTile = GetTile(rowIndex, columnIndex, group);

            _gridSlotTiles[rowIndex, columnIndex] = gridTile;
            _gameBoardSlots[rowIndex, columnIndex].SetState(gridTile);
        }

        private IGridTile GetTile(int rowIndex, int columnIndex, TileGroup group)
        {
            var gridTile = _tileItemsPool.GetGridTile(group);
            gridTile.SetWorldPosition(_boardConfig.GetWorldPosition(rowIndex, columnIndex));

            return gridTile;
        }

        private TileGroup GetNextAvailableGroup(TileGroup group)
        {
            var index = (int) group + 1;
            var resultGroup = TileGroup.Available;
            var groupValues = (TileGroup[]) Enum.GetValues(typeof(TileGroup));

            if (index < groupValues.Length)
            {
                resultGroup = groupValues[index];
            }

            return resultGroup;
        }

        private void DisposeGridTiles()
        {
            if (_gridSlotTiles == null)
            {
                return;
            }

            foreach (var gridSlotTile in _gridSlotTiles)
            {
                gridSlotTile.Dispose();
            }

            Array.Clear(_gridSlotTiles, 0, _gridSlotTiles.Length);
            _gridSlotTiles = null;
        }

        private void DisposeGameBoardData()
        {
            if (_gameBoardSlots == null)
            {
                return;
            }

            Array.Clear(_gameBoardSlots, 0, _gameBoardSlots.Length);
            _gameBoardSlots = null;
        }
    }
}