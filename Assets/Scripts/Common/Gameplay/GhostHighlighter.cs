using System;
using System.Collections.Generic;
using Common.Shapes;
using UnityEngine;
using Match3.Core.Structs;

namespace Common.Gameplay
{
    public class GhostHighlighter : MonoBehaviour
    {
        private GameObject _ghostRoot;
        private readonly List<SpriteRenderer> _ghostPool = new List<SpriteRenderer>();
        private Sprite _ghostSprite;
        private BoardConfig _boardConfig;

        // Custom Colors for Valid (Green) / Invalid (Red)
        private static readonly Color _validColor = new Color(0.2f, 0.9f, 0.2f, 0.5f);   // Bright Green
        private static readonly Color _invalidColor = new Color(0.9f, 0.2f, 0.2f, 0.5f); // Bright Red

        public void Initialize(BoardConfig config)
        {
            _boardConfig = config;
            
            if (_ghostRoot == null)
            {
                _ghostRoot = new GameObject("GhostRoot");
                _ghostRoot.transform.SetParent(transform);
                _ghostRoot.transform.localPosition = Vector3.zero;
            }
            
            _ghostRoot.SetActive(false);
        }

        public void Show(GridPosition origin, ShapeData shape, bool isValid)
        {
            if (_ghostRoot == null || shape == null || _boardConfig == null) return;

            _ghostRoot.SetActive(true);
            
            // Hide all first
            foreach (var sr in _ghostPool)
            {
                if(sr != null) sr.gameObject.SetActive(false);
            }

            Color targetColor = isValid ? _validColor : _invalidColor;
            
            int i = 0;
            foreach (var block in shape.Blocks)
            {
                var sr = GetGhostBlock(i++);
                sr.gameObject.SetActive(true);
                sr.color = targetColor;

                int r = origin.RowIndex + block.localRow;
                int c = origin.ColumnIndex + block.localCol;

                // Move sprite to world position
                Vector3 pos = _boardConfig.GetWorldPosition(r, c);
                pos.z = -1f; // In front of board
                sr.transform.position = pos;
                
                // Adjust scale slightly smaller than tile
                float size = _boardConfig.TileSize * 0.92f;
                sr.transform.localScale = new Vector3(size, size, 1f);
            }
        }

        public void Hide()
        {
            if (_ghostRoot != null) _ghostRoot.SetActive(false);
        }

        private SpriteRenderer GetGhostBlock(int index)
        {
            // Expand pool if necessary
            while (index >= _ghostPool.Count)
            {
                var obj = new GameObject($"GhostBlock_{_ghostPool.Count}");
                obj.transform.SetParent(_ghostRoot.transform);
                
                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = GetOrCreateGhostSprite();
                sr.sortingOrder = 15; // Above board (order 10 usually)

                _ghostPool.Add(sr);
            }
            
            return _ghostPool[index];
        }

        private Sprite GetOrCreateGhostSprite()
        {
            if (_ghostSprite != null) return _ghostSprite;
            
            // Create 4x4 white texture
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int p = 0; p < 16; p++) pixels[p] = Color.white;
            
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            
            // Create sprite
            _ghostSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _ghostSprite;
        }
    }
}
