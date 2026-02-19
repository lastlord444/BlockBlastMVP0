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
using Common.Gameplay;
using Common.Levels;
using Common.Audio;

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
        [Header("Adventure Mode")]
        [SerializeField] private LevelPatternDatabaseSO _adventureDatabase;


        [Header("UI Elements (Runtime Created)")]
        [SerializeField] private UnityEngine.UI.Text _scoreText;
        [SerializeField] private UnityEngine.UI.Text _bestScoreText;
        [SerializeField] private GameObject _gameOverPanel;

        private ShapeGenerator _shapeGenerator;
        private bool[,] _occupiedCells;
        // Premium checkerboard colors
        private static readonly Color _cellDark  = new Color(0.12f, 0.16f, 0.22f, 1f); // Deep navy
        private static readonly Color _cellLight = new Color(0.16f, 0.21f, 0.28f, 1f); // Slate blue
        
        private GhostHighlighter _ghostHighlighter;
        
        private const string BEST_SCORE_KEY = "BEST_SCORE";
        
        private int _currentScore;
        private int _bestScore;
        private bool _isGameOver;
        
        private void Awake()
        {
            // EMERGENCY AUTO-WIRE
            // If references are lost due to revert/git issues, find them automatically.
            
            if (_boardConfig == null)
            {
                _boardConfig = Resources.Load<BoardConfig>("BoardConfig");
                if (_boardConfig == null) Debug.LogError("BoardConfig not found in Resources!");
            }

            if (_boardRenderer == null)
                _boardRenderer = FindObjectOfType<UnityGameBoardRenderer>();

            if (_inputManager == null)
            {
                _inputManager = FindObjectOfType<BlockBlastInputManager>();
                if (_inputManager == null)
                {
                    Debug.LogWarning("InputManager missing. Creating one...");
                    var go = new GameObject("BlockBlastInputManager");
                    _inputManager = go.AddComponent<BlockBlastInputManager>();
                    // It might need references (Renderer, Camera). Let's try to assign them.
                    var mainCam = Camera.main;
                    if (mainCam == null) mainCam = FindObjectOfType<Camera>();
                    
                    _inputManager.Setup(_boardRenderer, mainCam);
                }
            }
                
            if (_shapeDatabase == null || _shapeDatabase.Length == 0)
            {
                _shapeDatabase = Resources.LoadAll<ShapeData>("");
            }
            
            // Auto-Wire ShapeSlots (CRITICAL)
            if (_shapeSlots == null || _shapeSlots.Length == 0 || _shapeSlots.Any(s => s == null))
            {
                _shapeSlots = FindObjectsOfType<ShapeSlot>()
                              .OrderBy(s => s.transform.GetSiblingIndex()) // Order by layout
                              .ToArray();
                              
                Debug.Log($"Auto-Wired {_shapeSlots.Length} Shape Slots.");
            }

            // Also find UI if possible
            if (_gameOverPanel == null)
            {
                // Try to find by name or tag if needed, but for now just log warning
                // Debug.LogWarning("UI Panels not assigned.");
            }
        }

        private void Start()
        {
            // Fallback (Original Logic)
            InitializeGame();
        }
        
        private void OnUIStartGame()
        {
            GameUiCanvas.Instance.ShowGame();
            StartNewGame();
        }

        private void OnUIRestartGame()
        {
            GameUiCanvas.Instance.ShowGame();
            StartNewGame();
        }

        private void OnUIGoHome()
        {
            GameUiCanvas.Instance.ShowMainMenu();
        }

        public void StartNewGame()
        {
            // Reset state if needed
            _isGameOver = false;
            // Clear board visuals (if not handled by InitializeGame correctly, but InitGame creates new references)
            // Ideally we should clear the board renderer.
            _boardRenderer.ResetGridTiles(); 
            
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
            Add(ShapeType.Z4, ruby, new List<ShapeBlock> { new(0,0), new(0,1), new(1,1), new(1,2) }); // Z horizontal
            Add(ShapeType.S4, emerald, new List<ShapeBlock> { new(0,1), new(0,2), new(1,0), new(1,1) }); // S horizontal

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

            // Pinned-to-bottom: sabit yükseklik (200 canvas unit), alt kenardan 16 unit boşluk.
            // Bu yaklaşım CanvasScaler ile doğru ölçeklenir; yüzde-anchor + sabit padding gibi
            // aspect ratio'ya göre değişmez.
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 16f);  // Alt kenardan 16 canvas-unit
            rect.sizeDelta = new Vector2(0f, 200f);        // Sabit 200 canvas-unit yükseklik

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
            
            if (_shapeDatabase == null || _shapeDatabase.Length == 0)
            {
                 // Desperate attempt to load anything
                 _shapeDatabase = Resources.LoadAll<ShapeData>("");
            }
            
            _shapeGenerator = new ShapeGenerator(_boardConfig, _shapeDatabase);
            
            _currentScore = 0;
            _bestScore = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
            UpdateScoreUI();

            // Setup Input 
            if (_inputManager != null)
            {
                _inputManager.OnShapeHover += OnShapeHover;
                _inputManager.OnShapeDropped += OnShapeDropped;
            }
            else
            {
                Debug.LogError("CRITICAL: InputManager is NULL in InitializeGame!");
            }

            // Setup Board Visuals
            // Assuming BoardRenderer is already initialized by its own Awake/Start or we trigger it
            // _boardRenderer.CreateGridTiles(); // If needed to be called manually

            RefillShapeSlots();
            
            // Adventure Mode Logic: 30% chance + No Repeat
            bool isAdventure = UnityEngine.Random.value < 0.3f; // 30%
            if (isAdventure && _adventureDatabase != null && _adventureDatabase.patterns.Count > 0)
            {
                string lastId = PlayerPrefs.GetString("last_pattern_id", "");
                var candidates = _adventureDatabase.patterns.FindAll(p => p.id != lastId);
                
                if (candidates.Count == 0) candidates = _adventureDatabase.patterns; // Fallback if only 1 pattern exists
                
                if (candidates.Count > 0)
                {
                    var level = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    LoadLevel(level);
                    PlayerPrefs.SetString("last_pattern_id", level.id);
                }
            }

            SetupCamera();
            InitBoardColors();

            // Setup Ghost
            if (_ghostHighlighter == null)
            {
                var ghostObj = new GameObject("GhostHighlighter");
                _ghostHighlighter = ghostObj.AddComponent<GhostHighlighter>();
            }
            _ghostHighlighter.Initialize(_boardConfig);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[DEV] BoardConfig: Rows={_boardConfig.RowCount}, Cols={_boardConfig.ColumnCount}, TileSize={_boardConfig.TileSize}");
            Debug.Log($"[DEV] Camera Fit: OrthoSize={cam.orthographicSize}, Aspect={cam.aspect}, TargetWidth={targetWidth}");
#endif
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
                 // Check if valid using exact position
                 bool isPlaceableExact = _shapeGenerator.CanPlaceAt(shape, _occupiedCells, bestOrigin.RowIndex, bestOrigin.ColumnIndex);
                 _ghostHighlighter.Show(bestOrigin, shape, isPlaceableExact);
             }
        }

        private void OnShapeDropped(ShapeSlot slot, ShapeData shape, GridPosition gridPos, Vector3 worldPos)
        {
            if (_isGameOver) return;
            _ghostHighlighter.Hide();

            // REVERTED: Direct Snap
            GridPosition bestOrigin = gridPos;
            bool canPlace = _shapeGenerator.CanPlaceAt(shape, _occupiedCells, gridPos.RowIndex, gridPos.ColumnIndex);
            
            bool onBoard = gridPos.RowIndex >= 0 && gridPos.RowIndex < _boardConfig.RowCount &&
                           gridPos.ColumnIndex >= 0 && gridPos.ColumnIndex < _boardConfig.ColumnCount;
            
#if UNITY_EDITOR || DEVELOPMENT_BUILD
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[DEV] Drop onBoard={onBoard} cell=({gridPos.RowIndex},{gridPos.ColumnIndex}) canPlace={canPlace}");
#endif
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

        private void ClearGhost()
        {
            _ghostHighlighter?.Hide();
        }

        private void PlaceShape(ShapeSlot slot, ShapeData shape, GridPosition origin)
        {
            // JUICE: Removed direct call here. Audio logic moved to CheckLines callback
            // GameJuiceManager.Instance?.OnPlace();

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

            // 2. Consume shape from slot
            slot.Clear();
            
            // Check if all slots are empty
            bool allEmpty = true;
            foreach (var s in _shapeSlots)
            {
                if (s.CurrentShape != null)
                {
                    allEmpty = false;
                    break;
                }
            }

            if (allEmpty)
            {
                // Advance generator 3 times
                _shapeGenerator.GetNextPiece();
                _shapeGenerator.GetNextPiece();
                _shapeGenerator.GetNextPiece();
                RefillShapeSlots();
            }

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
                CheckGameOver();
            }
        }

        private void LoadLevel(LevelPatternSO level)
        {
            if (level == null || level.PreFilledCells == null) return;

            foreach (var cell in level.PreFilledCells)
            {
                // Safety check
                if (cell.x >= 0 && cell.x < _boardConfig.RowCount &&
                    cell.y >= 0 && cell.y < _boardConfig.ColumnCount)
                {
                    _occupiedCells[cell.x, cell.y] = true;
                    _boardRenderer.SetTileColor(new GridPosition(cell.x, cell.y), level.FillColor);
                }
            }
            
            Debug.Log($"Loaded Adventure Level with {level.PreFilledCells.Count} pre-filled cells.");
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
            GameUiCanvas.Instance?.ShowGameOver();
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
