using System;
using System.Collections;
using System.Collections.Generic;
using Common.Shapes;
using Common.UI;
using Match3.Core.Structs;
using UnityEngine;
using Common.Interfaces;
using System.Linq;
using Common.Juice;

namespace Common.GameModes
{
    using UnityEngine.SceneManagement;

    public class BlockBlastGameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardConfig _boardConfig;
        [SerializeField] private UnityGameBoardRenderer _boardRenderer;
        [SerializeField] private BlockBlastInputManager _inputManager;
        [SerializeField] private Transform _shapesContainer;
        [SerializeField] private ShapeData[] _shapeDatabase;
        [SerializeField] private ShapeSlot[] _shapeSlots; // 3 slots
        [SerializeField] private LineClearSequencer _sequencer;

        [Header("UI Elements (Runtime Created)")]
        [SerializeField] private UnityEngine.UI.Text _scoreText;
        [SerializeField] private UnityEngine.UI.Text _bestScoreText;
        [SerializeField] private GameObject _gameOverPanel;

        private ShapeGenerator _shapeGenerator;
        private bool[,] _occupiedCells;
        // Premium checkerboard colors
        private static readonly Color _cellDark  = new Color(0.12f, 0.16f, 0.22f, 1f); // Deep navy
        private static readonly Color _cellLight = new Color(0.16f, 0.21f, 0.28f, 1f); // Slate blue
        
        private const string BEST_SCORE_KEY = "BEST_SCORE";
        
        private int _currentScore;
        private int _bestScore;
        private bool _isGameOver;

        private void Start()
        {
            // _bestScore loaded in InitializeGame
            Bootstrap();
            InitializeGame();
        }

        private void Bootstrap()
        {
            // Juice Manager
            if (GameJuiceManager.Instance == null)
            {
                var juiceGO = new GameObject("GameJuiceManager");
                juiceGO.AddComponent<GameJuiceManager>();
            }

            // Sequencer
            if (_sequencer == null)
            {
                _sequencer = GetComponent<LineClearSequencer>();
                if (_sequencer == null)
                {
                    _sequencer = gameObject.AddComponent<LineClearSequencer>();
                }
            }

            if (_boardConfig == null)
            {
                _boardConfig = Resources.Load<BoardConfig>("BoardConfig");
                if (_boardConfig == null)
                {
                    Debug.LogWarning("BoardConfig not found in Resources! Creating default instance.");
                    _boardConfig = ScriptableObject.CreateInstance<BoardConfig>();
                }
            }

            if (_boardRenderer == null)
            {
                _boardRenderer = FindFirstObjectByType<UnityGameBoardRenderer>();
            }
            
            if (_boardRenderer != null)
            {
                // Ensure Renderer has config and is initialized
                _boardRenderer.SetBoardConfig(_boardConfig);
                // Initialize board with empty data if not already done (assuming 10x10 Empty)
                // We pass a dummy array, the renderer logic usually uses BoardConfig dims anyway or the array dims
                // Renderer.CreateGridTiles uses Row/Col from properties which come from BoardConfig
                // But it also takes int[,] data. match3 uses it for level layout.
                // We just want empty board.
                // Let's call CreateGridTiles via reflection or just public method if available.
                // It is public.
                 _boardRenderer.CreateGridTiles(new int[_boardConfig.RowCount, _boardConfig.ColumnCount]);

                // Juice Sequencer'a board renderer'ı bildir
                if (_sequencer != null)
                    _sequencer.SetBoardRenderer(_boardRenderer);
            }

            if (_inputManager == null)
            {
                _inputManager = gameObject.AddComponent<BlockBlastInputManager>();
                // Inject dependencies to InputManager via reflection or public fields if needed
                 // InputManager needs Camera and Renderer
                 var cam = Camera.main; // simplified
                 _inputManager.Setup(_boardRenderer, cam);
            }
            
            if (_shapeSlots == null || _shapeSlots.Length == 0)
            {
                CreateGameUI();
            }
            
            // Assign db if missing (load all from Resources?)
            if (_shapeDatabase == null || _shapeDatabase.Length == 0)
            {
                 _shapeDatabase = Resources.LoadAll<ShapeData>("Shapes");
            }

            // Fallback: Generate if still empty
            if (_shapeDatabase == null || _shapeDatabase.Length == 0)
            {
                Debug.LogWarning("No shapes found in Resources! Generating default shapes in memory.");
                _shapeDatabase = GenerateDefaultShapes();
            }
        }

        private ShapeData[] GenerateDefaultShapes()
        {
            var shapes = new List<ShapeData>();
            
            void Add(ShapeType type, Color color, List<ShapeBlock> blocks)
            {
                var sd = ScriptableObject.CreateInstance<ShapeData>();
                sd.Init(type, color, blocks);
                shapes.Add(sd);
            }

            // Premium jewel-tone palette
            var sapphire  = new Color(0.20f, 0.40f, 0.85f); // Blue
            var emerald   = new Color(0.18f, 0.75f, 0.45f); // Green
            var amber     = new Color(0.95f, 0.75f, 0.15f); // Yellow
            var ruby      = new Color(0.90f, 0.22f, 0.30f); // Red
            var amethyst  = new Color(0.60f, 0.30f, 0.85f); // Purple
            var coral     = new Color(1.00f, 0.45f, 0.35f); // Orange-coral
            var teal      = new Color(0.15f, 0.80f, 0.78f); // Cyan-teal
            var rose      = new Color(0.90f, 0.35f, 0.55f); // Pink

            Add(ShapeType.Single, ruby, new List<ShapeBlock> { new(0,0) });
            Add(ShapeType.Line2, teal, new List<ShapeBlock> { new(0,0), new(0,1) });
            Add(ShapeType.Line2, teal, new List<ShapeBlock> { new(0,0), new(1,0) });
            Add(ShapeType.Line3, sapphire, new List<ShapeBlock> { new(0,0), new(0,1), new(0,2) });
            Add(ShapeType.Line3, sapphire, new List<ShapeBlock> { new(0,0), new(1,0), new(2,0) });
            Add(ShapeType.Line4, amber, new List<ShapeBlock> { new(0,0), new(0,1), new(0,2), new(0,3) });
            Add(ShapeType.Line4, amber, new List<ShapeBlock> { new(0,0), new(1,0), new(2,0), new(3,0) });
            Add(ShapeType.Square2x2, emerald, new List<ShapeBlock> { new(0,0), new(0,1), new(1,0), new(1,1) });
            Add(ShapeType.L3, coral, new List<ShapeBlock> { new(0,0), new(1,0), new(1,1) });
            Add(ShapeType.L4, amethyst, new List<ShapeBlock> { new(0,0), new(1,0), new(2,0), new(2,1) });
            Add(ShapeType.T4, rose, new List<ShapeBlock> { new(0,0), new(0,1), new(0,2), new(1,1) });

            return shapes.ToArray();
        }

        private void CreateGameUI() // Renamed for clarity
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Create SafeArea Container — FULLSCREEN (child'lar buna göre yerleştiriliyor)
            var safeAreaObj = new GameObject("SafeAreaContainer");
            var safeRect = safeAreaObj.AddComponent<RectTransform>();
            safeRect.SetParent(canvas.transform, false);
            safeRect.anchorMin = Vector2.zero;    // Bottom-left
            safeRect.anchorMax = Vector2.one;     // Top-right
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;

            // SafeAreaFitter: safe area'yı her frame kontrol eder, değişince günceller
            safeAreaObj.AddComponent<SafeAreaFitter>();

            // JUICE: Shake target = SafeAreaContainer
            GameJuiceManager.Instance?.AutoSetShakeTarget(safeRect);

            // 1. Shapes Panel (Safe Area içinde)
            CreateShapeSlotsUI(safeAreaObj.transform);

            // 2. Score Panel (Safe Area içinde)
            CreateTitleUI(safeAreaObj.transform);
            CreateScoreUI(safeAreaObj.transform);

            // 3. Game Over Panel
            CreateGameOverUI(safeAreaObj.transform);
        }

        // ApplySafeArea artık kullanılmıyor — SafeAreaFitter component'i üstlendi.
        // Eski tek-seferlik uygulama yerine her frame kontrol eden component kullanılıyor.

        private void CreateTitleUI(Transform parent)
        {
            var titleObj = new GameObject("TitlePanel");
            titleObj.transform.SetParent(parent, false);

            var rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -50); // Top padding
            rect.sizeDelta = new Vector2(0, 100);

            var text = CreateText(titleObj, "BLOCK BLAST", 60, Color.white);
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            
            // Add shadow for better visibility
            var shadow = titleObj.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0,0,0,0.5f);
            shadow.effectDistance = new Vector2(2, -2);
        }

        private void CreateShapeSlotsUI(Transform parent) // Helper
        {
            var panelObj = new GameObject("ShapeSlotsPanel");
            var rect = panelObj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            // KRİTİK FIX: Alt kısımdan taşma → ScorePanel'dan AŞAĞI (üst panel %15, alt panel %25)
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0.25f); // Alt %25 (tam güvenli)
            rect.pivot = new Vector2(0.5f, 0.5f);      // Pivot center
            rect.anchoredPosition = Vector2.zero;        // Ortalanmış
            rect.sizeDelta = Vector2.zero;                 // Anchor'la genişlet

            var layout = panelObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 20, 20, 20); // Eşit padding (hem üst hem alt)
            layout.childControlSize = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            var layout = panelObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 20, 10, 10); // Eşit üst/alt padding
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            _shapeSlots = new ShapeSlot[3];
            for (int i = 0; i < 3; i++)
            {
                var slotObj = new GameObject($"Slot_{i}");
                slotObj.transform.SetParent(panelObj.transform, false);
                
                var img = slotObj.AddComponent<UnityEngine.UI.Image>();
                img.color = Color.clear; 
                img.raycastTarget = true; 

                var le = slotObj.AddComponent<UnityEngine.UI.LayoutElement>();
                le.preferredWidth = 150;
                le.preferredHeight = 150;
                
                var slot = slotObj.AddComponent<ShapeSlot>();
                
                // Preview Root (Container for blocks)
                var previewRootObj = new GameObject("PreviewRoot");
                previewRootObj.transform.SetParent(slotObj.transform, false);
                
                var previewRect = previewRootObj.AddComponent<RectTransform>();
                // Center in slot
                previewRect.anchorMin = new Vector2(0.5f, 0.5f);
                previewRect.anchorMax = new Vector2(0.5f, 0.5f);
                previewRect.pivot = new Vector2(0.5f, 0.5f);
                previewRect.sizeDelta = Vector2.zero;

                slot.Setup(previewRect, slotObj.GetComponent<RectTransform>(), slotObj.AddComponent<CanvasGroup>());

                _shapeSlots[i] = slot;
            }
        }

        private void CreateScoreUI(Transform parent)
        {
            var panelObj = new GameObject("ScorePanel");
            var rect = panelObj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0, 0.85f);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var hLayout = panelObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.spacing = 100;

            _scoreText = CreateText(panelObj, "Score: 0", 40);
            _bestScoreText = CreateText(panelObj, $"Best: {_bestScore}", 40);
        }

        private void CreateGameOverUI(Transform parent)
        {
            _gameOverPanel = new GameObject("GameOverPanel");
            var rect = _gameOverPanel.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = _gameOverPanel.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0,0,0, 0.8f);

            var vLayout = _gameOverPanel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.spacing = 30;

            CreateText(_gameOverPanel, "GAME OVER", 60);

            var restartBtnObj = new GameObject("RestartButton");
            restartBtnObj.transform.SetParent(_gameOverPanel.transform, false);
            var btnImg = restartBtnObj.AddComponent<UnityEngine.UI.Image>();
            btnImg.color = Color.white;
            var btn = restartBtnObj.AddComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(RestartGame);
            var btnLayout = restartBtnObj.AddComponent<UnityEngine.UI.LayoutElement>();
            btnLayout.preferredWidth = 200;
            btnLayout.preferredHeight = 60;
            
            var btnText = CreateText(restartBtnObj, "RESTART", 30, Color.black);

            _gameOverPanel.SetActive(false);
        }

        private UnityEngine.UI.Text CreateText(GameObject parent, string content, int fontSize, Color color = default)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent.transform, false);
            var text = obj.AddComponent<UnityEngine.UI.Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = (color == default) ? Color.white : color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void InitializeGame()
        {
            if (_boardConfig == null)
            {
                Debug.LogError("BoardConfig is missing!");
                return;
            }

            _occupiedCells = new bool[_boardConfig.RowCount, _boardConfig.ColumnCount];
            _shapeGenerator = new ShapeGenerator(_boardConfig, _shapeDatabase);
            
            _currentScore = 0;
            _bestScore = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
            UpdateScoreUI();

            // Setup Input 
            _inputManager.OnShapeHover += OnShapeHover;
            _inputManager.OnShapeDropped += OnShapeDropped;

            // Setup Board Visuals
            // Assuming BoardRenderer is already initialized by its own Awake/Start or we trigger it
            // _boardRenderer.CreateGridTiles(); // If needed to be called manually

            RefillShapeSlots();
            SetupCamera();
            InitBoardColors();
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            // Calculate Board Dimensions
            float boardWidth = _boardConfig.ColumnCount * _boardConfig.TileSize;
            float boardHeight = _boardConfig.RowCount * _boardConfig.TileSize;

            // Add Margin (e.g. 1 tile on each side, + extra for UI at bottom/top)
            float margin = _boardConfig.TileSize * 1.5f; 
            float targetWidth = boardWidth + margin;
            // Height needs more margin for UI (Score top, Shapes bottom)
            // Shapes panel is ~20% of screen. Top is ~15%.
            // Let's ensure Width fits first (Priority for Portrait).
            
            float screenAspect = cam.aspect;
            
            // Calculate size based on Width
            float sizeBasedOnWidth = (targetWidth / screenAspect) / 2f;
            
            // Calculate size based on Height (just in case, Board + 40% padding for UI)
            float targetHeight = boardHeight * 1.5f; 
            float sizeBasedOnHeight = targetHeight / 2f;

            // Use the larger one to ensure fit
            cam.orthographicSize = Mathf.Max(sizeBasedOnWidth, sizeBasedOnHeight);
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[DEV] BoardConfig: Rows={_boardConfig.RowCount}, Cols={_boardConfig.ColumnCount}, TileSize={_boardConfig.TileSize}");
            Debug.Log($"[DEV] Camera Fit: OrthoSize={cam.orthographicSize}, Aspect={cam.aspect}, TargetWidth={targetWidth}");
#endif
            // Debug.Log($"Camera Setup: Board {boardWidth}x{boardHeight}, Aspect {screenAspect}, Size {cam.orthographicSize}");
            
            // Also center the camera on the board
            Vector3 center = _boardConfig.GetOriginPosition();
            center.x += (boardWidth / 2f) - (_boardConfig.TileSize / 2f); // Adjust center?
            // BoardConfig.GetOriginPosition returns Top-Left or Center?
            // GetOriginPosition: -offset, +offset. 
            // It seems it returns the Top-Left of the grid relative to (0,0)?
            // Let's check BoardConfig.GetOriginPosition logic again.
            // RowCount/2 * TileSize.
            // If Row=10, Tile=0.6. Offset=3.0. Origin=(-3.0, 3.0).
            // Grid goes: (0,0) -> origin. (0,1) -> origin + (0.6, 0).
            // Center of grid:
            // X = Origin.x + (Cols * Tile / 2) - (Tile/2)?
            // X = -3.0 + (6.0 / 2) = 0.
            // Y = Origin.y - (Rows * Tile / 2) = 3.0 - 3.0 = 0.
            // So Board is centered at (0,0).
            // Camera at (0,0,-10) is correct.
            // Just need to set Size.
        }

        private void RefillShapeSlots()
        {
            var nextPieces = _shapeGenerator.NextPieces;
            for (int i = 0; i < _shapeSlots.Length; i++)
            {
                if (i < nextPieces.Count)
                {
                    _shapeSlots[i].Initialize(nextPieces[i]);
                    _inputManager.RegisterSlot(_shapeSlots[i]);
                }
                else
                {
                    _shapeSlots[i].Clear();
                }
            }
            
            // Check Game Over
            if (_shapeGenerator.IsGameOver(_occupiedCells))
            {
                OnGameOver();
            }
        }

        private void OnGameOver()
        {
            if (_isGameOver) return;
            _isGameOver = true;
            Debug.Log("Game Over!");
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
        }

        private void UpdateScore(int points)
        {
            _currentScore += points;
            if (_currentScore > _bestScore)
            {
                _bestScore = _currentScore;
                PlayerPrefs.SetInt(BEST_SCORE_KEY, _bestScore);
                PlayerPrefs.Save();
            }
            UpdateScoreUI();
        }

        private void UpdateScoreUI()
        {
            if (_scoreText) _scoreText.text = $"Score: {_currentScore}";
            if (_bestScoreText) _bestScoreText.text = $"Best: {_bestScore}";
        }

        // --- Smart Snap Logic (Sub-Grid Precision) ---
        
        private bool TryFindBestPlacement(GridPosition mouseGridPos, Vector3 mouseWorldPos, ShapeData shape, out GridPosition bestOrigin)
        {
            if(shape == null) 
            {
                bestOrigin = GridPosition.Zero;
                return false;
            }

            var centerOffset = GetShapeCenterOffset(shape);
            int exactRow = mouseGridPos.RowIndex - centerOffset.RowIndex;
            int exactCol = mouseGridPos.ColumnIndex - centerOffset.ColumnIndex;

            // 1. Check Exact Position First (User Preference: Strict)
            if (_shapeGenerator.CanPlaceAt(shape, _occupiedCells, exactRow, exactCol))
            {
                bestOrigin = new GridPosition(exactRow, exactCol);
                return true;
            }

            // 2. If Exact is Invalid, Check Neighbors with HIGH PRECISION (Forgiveness)
            // This solves "I am 1 pixel off and it won't place".
            
            float minDistance = float.MaxValue;
            bool foundValid = false;
            GridPosition bestPos = new GridPosition(exactRow, exactCol);
            
            // Only snap if VERY close (e.g. within 70% of a tile). 
            // If user is far away, don't snap.
            float snapThreshold = _boardConfig.TileSize * 0.7f; 

            // Radius 1 check (3x3)
            for (int rOffset = -1; rOffset <= 1; rOffset++)
            {
                for (int cOffset = -1; cOffset <= 1; cOffset++)
                {
                    if (rOffset == 0 && cOffset == 0) continue; // Already checked exact

                    int testR = exactRow + rOffset;
                    int testC = exactCol + cOffset;

                    if (_shapeGenerator.CanPlaceAt(shape, _occupiedCells, testR, testC))
                    {
                        // Calculate World Position of where the "Center Block" would be if we placed it here.
                        // Shape Center Offset is relative to Top-Left (testR, testC).
                        // So Center Block is at (testR + centerOffset.row, testC + centerOffset.col).
                        
                        Vector3 targetCenterPos = _boardConfig.GetWorldPosition(testR + centerOffset.RowIndex, testC + centerOffset.ColumnIndex);
                        targetCenterPos.z = 0; // Ensure 2D distance
                        
                        float dist = Vector3.Distance(mouseWorldPos, targetCenterPos);
                        
                        if (dist < minDistance && dist < snapThreshold)
                        {
                            minDistance = dist;
                            bestPos = new GridPosition(testR, testC);
                            foundValid = true;
                        }
                    }
                }
            }

            if (foundValid)
            {
                bestOrigin = bestPos;
                return true;
            }

            // Fallback: Return exact (invalid) for Red Ghost
            bestOrigin = new GridPosition(exactRow, exactCol);
            return false;
        }

        private void OnShapeHover(GridPosition gridPos, ShapeData shape, bool isValid, Vector3 worldPos)
        {
            if (_isGameOver) return;
            ClearGhost();
            if (shape == null) return;

            // REVERTED: Direct Snap (Classic Feel)
            // User Feedback: "Smart Snap feels like losing control / hiding shape"
            GridPosition bestOrigin = gridPos;
            bool isPlaceable = _shapeGenerator.CanPlaceAt(shape, _occupiedCells, gridPos.RowIndex, gridPos.ColumnIndex);
            
            // Draw ghost at exact position
            bool nearBoard = bestOrigin.RowIndex >= -2 && bestOrigin.RowIndex < _boardConfig.RowCount + 2 &&
                             bestOrigin.ColumnIndex >= -2 && bestOrigin.ColumnIndex < _boardConfig.ColumnCount + 2;

             if (nearBoard)
             {
                 DrawGhost(bestOrigin, shape, isPlaceable);
             }
        }

        private void OnShapeDropped(ShapeSlot slot, ShapeData shape, GridPosition gridPos, Vector3 worldPos)
        {
            if (_isGameOver) return;
            ClearGhost();

            // REVERTED: Direct Snap
            GridPosition bestOrigin = gridPos;
            bool canPlace = _shapeGenerator.CanPlaceAt(shape, _occupiedCells, gridPos.RowIndex, gridPos.ColumnIndex);
            
            bool onBoard = gridPos.RowIndex >= 0 && gridPos.RowIndex < _boardConfig.RowCount &&
                           gridPos.ColumnIndex >= 0 && gridPos.ColumnIndex < _boardConfig.ColumnCount;
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[DEV] Drop onBoard={onBoard} cell=({gridPos.RowIndex},{gridPos.ColumnIndex}) canPlace={canPlace}");
#endif

            if (canPlace)
            {
                PlaceShape(slot, shape, bestOrigin);
            }
            else
            {
                // Geçersiz drop — ses + shake
                GameJuiceManager.Instance?.OnInvalid();
            }
        }

        // Helper removed or unused
        private GridPosition GetShapeCenterOffset(ShapeData shape)
        {
             // Keep for now to avoid compilation errors if used elsewhere, 
             // but logic above ignores it.
            if (shape == null || shape.Blocks == null || shape.Blocks.Count == 0) return new GridPosition(0, 0);
            return new GridPosition(0,0); // Dummy
        }

        // --- Ghost System (Object Pooling) ---

        private GameObject _ghostRoot;
        private readonly List<SpriteRenderer> _ghostPool = new List<SpriteRenderer>();
        private Sprite _ghostSprite; // Shared sprite for all ghost blocks

        private Sprite GetOrCreateGhostSprite()
        {
            if (_ghostSprite != null) return _ghostSprite;
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int p = 0; p < 16; p++) pixels[p] = Color.white;
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            _ghostSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _ghostSprite;
        }

        private void InitializeGhost()
        {
            if (_boardConfig == null) return;
            if (_ghostRoot == null)
            {
                _ghostRoot = new GameObject("GhostRoot");
            }
            // Pre-warm 9 blocks (covers most shapes)
            for (int i = 0; i < 9; i++) GetGhostBlock(i);
            _ghostRoot.SetActive(false);
        }

        private SpriteRenderer GetGhostBlock(int index)
        {
            while (index >= _ghostPool.Count)
            {
                // Plain empty GameObject — no CreatePrimitive, no MeshCollider issues
                var obj = new GameObject($"Ghost_{_ghostPool.Count}");
                if (_ghostRoot != null) obj.transform.SetParent(_ghostRoot.transform);

                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = GetOrCreateGhostSprite();
                sr.sortingOrder = 10; // Above board tiles

                float size = _boardConfig != null ? _boardConfig.TileSize : 0.6f;
                obj.transform.localScale = Vector3.one * (size * 0.9f);

                _ghostPool.Add(sr);
            }
            return _ghostPool[index];
        }

        private void DrawGhost(GridPosition origin, ShapeData shape, bool isPlaceable)
        {
            if (_boardConfig == null || shape == null) return;
            if (_ghostRoot == null) InitializeGhost();
            if (_ghostRoot == null) return;

            _ghostRoot.SetActive(true);

            // Ghost Colors: Gray (Valid) / Red (Invalid) - User Request
            Color ghostColor = isPlaceable
                ? new Color(0.5f, 0.5f, 0.5f, 0.5f)    // Gray transparent (Standard Block Blast style)
                : new Color(1f, 0f, 0f, 0.45f);        // Red transparent

            // Hide all first
            for (int g = 0; g < _ghostPool.Count; g++)
                if (_ghostPool[g] != null) _ghostPool[g].gameObject.SetActive(false);

            int i = 0;
            foreach (var block in shape.Blocks)
            {
                var sr = GetGhostBlock(i++);
                if (sr == null) continue;

                sr.gameObject.SetActive(true);
                sr.color = ghostColor;

                int r = origin.RowIndex + block.localRow;
                int c = origin.ColumnIndex + block.localCol;

                Vector3 pos = _boardConfig.GetWorldPosition(r, c);
                pos.z = -1f; // In front of board
                sr.transform.position = pos;
                sr.transform.localScale = Vector3.one * (_boardConfig.TileSize * 0.92f);
            }
        }

        private void ClearGhost()
        {
            if (_ghostRoot != null) _ghostRoot.SetActive(false);
        }

        private void PlaceShape(ShapeSlot slot, ShapeData shape, GridPosition origin)
        {
            // JUICE: Başarılı yerleştirme sesi
            GameJuiceManager.Instance?.OnPlace();

            // 1. Mark as occupied and set color + punch-scale animation
            foreach (var block in shape.Blocks)
            {
                int r = origin.RowIndex + block.localRow;
                int c = origin.ColumnIndex + block.localCol;
                
                _occupiedCells[r, c] = true;
                _boardRenderer.SetTileColor(new GridPosition(r, c), shape.ShapeColor);
                
                // Punch-scale: 1.0 → 1.08 → 1.0 (~0.12s)
                var tileTransform = _boardRenderer.GetTileTransform(r, c);
                if (tileTransform != null)
                    StartCoroutine(PunchScaleTile(tileTransform));
            }
            
            // Score per block
            UpdateScore(10 * shape.BlockCount);

            // 2. Consume shape from slot & Refill
            slot.Clear();
            
            // Consume from generator (advance queue)
            // Note: Current GenerateDefaultShapes / ShapeGenerator logic is infinite queue. 
            // We just need to refill the empty slot with next piece?
            // OR waits until all 3 are empty?
            // User requested: "Slot consume + replenish". 
            // In standard Block Blast, you use 3 shapes, THEN you get 3 new ones.
            // But for this task, I will stick to "Refill only when all used" OR "Refill immediately".
            // Let's implement "Refill immediately" as it is simpler and satisfies "replenish".
            // Actually, wait. If I refill immediately, the user never sees an empty slot.
            // Let's check if there are other filled slots.
            


            // If we want "Batch" style, we only refill when !anySlotFilled.
            // But ShapeGenerator right now is a Queue.
            // Let's just do: GetNextPiece() for the slot we just used.
            // RefillShapeSlots is iterating 0..3 and taking from Queue.
            // If I call RefillShapeSlots(), it will refill ALL slots from the Queue.
            // This will SHIFT existing shapes if I'm not careful.
            // Current RefillShapeSlots:
            // loops i=0..2. if i < nextPieces.Count -> Initialize.
            // It blindly overwrites.
            
            // Fix: We should only fill EMPTY slots?
            // But ShapeGenerator.NextPieces is a read-only list of the queue.
            // I will just call GetNextPiece() essentially consuming one from queue, and put it in the slot?
            // No, RefillShapeSlots logic is:
            // shapeSlots[0] gets nextPieces[0]
            // shapeSlots[1] gets nextPieces[1]
            // ...
            // If I consume one, `GetNextPiece()` is called inside PlaceShape in the old code.
            // `_shapeGenerator.GetNextPiece()` removes head, adds tail.
            // So `NextPieces` shifts. 
            // [A, B, C] -> GetNextPiece -> Returns A. Queue becomes [B, C, D].
            // RefillShapeSlots: Slot0=B, Slot1=C, Slot2=D.
            // This IS the shifting behavior.
            
            // I will strictly follow "Slot consume + replenish" = Immediate refill with shift.
            _shapeGenerator.GetNextPiece(); 
            RefillShapeSlots();

            // 3. Check Lines
            CheckLines();
        }

        private void CheckLines()
        {
            List<int> fullRows = new List<int>();
            List<int> fullCols = new List<int>();

            // Check Rows
            for (int r = 0; r < _boardConfig.RowCount; r++)
            {
                if (_boardConfig.IsRowFull(_occupiedCells, r)) fullRows.Add(r);
            }

            // Check Cols
            for (int c = 0; c < _boardConfig.ColumnCount; c++)
            {
                if (_boardConfig.IsColumnFull(_occupiedCells, c)) fullCols.Add(c);
            }

            if (fullRows.Count > 0 || fullCols.Count > 0)
            {
                ClearLines(fullRows, fullCols);
            }
            else
            {
                // Çizgi yoksa hemen game over kontrolü
                CheckGameOver();
            }
        }

        private void ClearLines(List<int> rows, List<int> cols)
        {
            // JUICE: Sequencer (flash + ses + particle) — ardışık temizleme
            // Sequencer biterken CheckGameOver'ı tetikle
            if (_sequencer != null)
            {
                _sequencer.StartClearSequence(rows, cols, () => CheckGameOver());
            }

            // Skor hesabı
            int totalLines = rows.Count + cols.Count;
            if (totalLines > 0)
            {
                int comboMultiplier = totalLines;
                int points = 10 * totalLines * comboMultiplier;
                UpdateScore(points);
            }

            // 1. Internal state güncelle (senkron — önemli)
            foreach (var r in rows)
            {
                for (int c = 0; c < _boardConfig.ColumnCount; c++) _occupiedCells[r, c] = false;
            }
            foreach (var c in cols)
            {
                for (int r = 0; r < _boardConfig.RowCount; r++) _occupiedCells[r, c] = false;
            }

            // 2. Görselleri güncelle (senkron — flash sequencer SONRA kendi rengine döner)
            for (int r = 0; r < _boardConfig.RowCount; r++)
            {
                for (int c = 0; c < _boardConfig.ColumnCount; c++)
                {
                    if (!_occupiedCells[r, c])
                    {
                        _boardRenderer.SetTileColor(new GridPosition(r, c), GetCellColor(r, c));
                    }
                }
            }
        }

        /// <summary>Returns checkerboard cell color for the given row/column.</summary>
        private Color GetCellColor(int row, int col)
        {
            return (row + col) % 2 == 0 ? _cellDark : _cellLight;
        }

        /// <summary>Paints the entire board with checkerboard pattern.</summary>
        private void InitBoardColors()
        {
            if (_boardConfig == null || _boardRenderer == null) return;
            for (int r = 0; r < _boardConfig.RowCount; r++)
            {
                for (int c = 0; c < _boardConfig.ColumnCount; c++)
                {
                    _boardRenderer.SetTileColor(new GridPosition(r, c), GetCellColor(r, c));
                }
            }
        }

        private bool IsInternalValid(int r, int c)
        {
            return r >= 0 && r < _boardConfig.RowCount && c >= 0 && c < _boardConfig.ColumnCount;
        }

        private void CheckGameOver()
        {
            if (_isGameOver) return;

            // Check if ANY shape in the current slots can be placed ANYWHERE
            bool anyMovePossible = false;
            foreach (var slot in _shapeSlots)
            {
                if (slot.CurrentShape == null) continue;
                if (CanPlaceShapeAnywhere(slot.CurrentShape))
                {
                    anyMovePossible = true;
                    break;
                }
            }

            if (!anyMovePossible)
            {
                TriggerGameOver();
            }
        }

        private bool CanPlaceShapeAnywhere(ShapeData shape)
        {
            // Brute force: Check every cell as a potential origin
            for (int r = 0; r < _boardConfig.RowCount; r++)
            {
                for (int c = 0; c < _boardConfig.ColumnCount; c++)
                {
                    if (CanPlaceAt(new GridPosition(r, c), shape))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CanPlaceAt(GridPosition origin, ShapeData shape)
        {
            foreach (var block in shape.Blocks)
            {
                int r = origin.RowIndex + block.localRow;
                int c = origin.ColumnIndex + block.localCol;

                if (!IsInternalValid(r, c)) return false;
                if (_occupiedCells[r, c]) return false;
            }
            return true;
        }

        private void TriggerGameOver()
        {
            if (_isGameOver) return;
            _isGameOver = true;
            
            // JUICE: Game over ses + shake
            GameJuiceManager.Instance?.OnGameOver();

            Debug.Log("GAME OVER: no moves");

            // Show UI
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
                // Ensure it's the last sibling to be on top
                _gameOverPanel.transform.SetAsLastSibling();
            }
        }

        // ── Juice v2: Place Punch-Scale ─────────────────────────────────────────────
        // Allocation minimal: yield return null loop (~0.12s), WaitForSeconds kaçınılır.
        private IEnumerator PunchScaleTile(Transform t)
        {
            if (t == null) yield break;

            const float punchDuration = 0.12f;
            const float peakScale     = 1.08f;
            float elapsed = 0f;
            Vector3 baseScale = t.localScale;

            while (elapsed < punchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float n  = elapsed / punchDuration;
                // Üçgen dalga: 0→peak→0
                float s  = (n < 0.5f) ? (peakScale - 1f) * (n * 2f) + 1f
                                       : (peakScale - 1f) * ((1f - n) * 2f) + 1f;
                t.localScale = baseScale * s;
                yield return null;
            }

            t.localScale = baseScale; // Reset
        }
    }
}
