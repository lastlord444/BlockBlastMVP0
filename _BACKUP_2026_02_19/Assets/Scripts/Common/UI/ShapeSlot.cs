using System;
using Common.Shapes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Common.UI
{
    public class ShapeSlot : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform _previewRoot;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private CanvasGroup _canvasGroup;

        // Configuration
        private const float PADDING = 8f;
        private const float MAX_TILE_RATIO = 0.45f; // Slot boyutunun %45'i max tile
        private const float MIN_TILE = 18f;
        private const float MAX_TILE = 40f;

        // Drag Configuration
        private float _dragLiftHeight = 100f; // Visual lift amount (approx 1.5-2 cells)

        public void Setup(RectTransform previewRoot, RectTransform rectTransform, CanvasGroup canvasGroup)
        {
            _previewRoot = previewRoot;
            _rectTransform = rectTransform;
            _canvasGroup = canvasGroup;
        }

        private ShapeData _currentShape;
        private Vector2 _initialPosition;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private Canvas _rootCanvas;
        private RectTransform _rootCanvasRect;

        // Drag State
        private Vector2 _dragOffset;
        private bool _isDragging;

        public event Action<ShapeSlot, PointerEventData> OnDragStarted;
        public event Action<ShapeSlot, PointerEventData> OnDragUpdated;
        public event Action<ShapeSlot, PointerEventData> OnDragEnded;

        public ShapeData CurrentShape => _currentShape;

        public void Initialize(ShapeData shape)
        {
            _currentShape = shape;
            ClearVisuals();

            if (shape != null)
            {
                CreateBlockVisuals(shape);
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
            else
            {
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void Clear()
        {
            _currentShape = null;
            ClearVisuals();
            _canvasGroup.blocksRaycasts = false;
        }

        private void ClearVisuals()
        {
            if (_previewRoot == null) return;
            foreach (Transform child in _previewRoot)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreateBlockVisuals(ShapeData shape)
        {
            if (_previewRoot == null || shape == null || shape.Blocks.Count == 0) return;

            // LayoutGroup/ContentSizeFitter devre dışı bırak (override engeli)
            var lg = _previewRoot.GetComponent<UnityEngine.UI.LayoutGroup>();
            if (lg != null) lg.enabled = false;
            var csf = _previewRoot.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (csf != null) csf.enabled = false;

            // 1. Bounds hesapla
            int minR = int.MaxValue, maxR = int.MinValue;
            int minC = int.MaxValue, maxC = int.MinValue;

            foreach (var block in shape.Blocks)
            {
                if (block.localRow < minR) minR = block.localRow;
                if (block.localRow > maxR) maxR = block.localRow;
                if (block.localCol < minC) minC = block.localCol;
                if (block.localCol > maxC) maxC = block.localCol;
            }

            int widthCells  = maxC - minC + 1;
            int heightCells = maxR - minR + 1;
            int maxDim = Mathf.Max(widthCells, heightCells);

            // 2. Slot boyutu
            Vector2 slotSize = (_rectTransform != null && _rectTransform.rect.width > 1f)
                ? _rectTransform.rect.size
                : new Vector2(150f, 150f);

            float innerW = Mathf.Max(1f, slotSize.x - PADDING * 2f);
            float innerH = Mathf.Max(1f, slotSize.y - PADDING * 2f);

            // 3. Shape-fit (step)
            float stepFit = Mathf.Min(innerW / maxDim, innerH / maxDim);

            // 4. Max tile sınır (1x1 dev olmasın)
            float maxTile = Mathf.Min(innerW, innerH) * MAX_TILE_RATIO;

            // 5. Final step: clamp
            float step = Mathf.Clamp(Mathf.Min(stepFit, maxTile), MIN_TILE, MAX_TILE);
            step = Mathf.Floor(step); // Pixel snap

            // 6. Gap hesapla
            float gap = Mathf.Clamp(Mathf.Round(step * 0.12f), 2f, 6f);
            float visual = Mathf.Max(1f, step - gap);

            // 7. PreviewRoot ayarla
            _previewRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _previewRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _previewRoot.pivot = new Vector2(0.5f, 0.5f);
            _previewRoot.anchoredPosition = Vector2.zero;
            _previewRoot.sizeDelta = Vector2.zero;

            // 8. Merkez hesapla
            float centerX = (minC + maxC) * 0.5f;
            float centerY = (minR + maxR) * 0.5f;

            // 9. Block'ları yerleştir — board ile aynı yön
            foreach (var block in shape.Blocks)
            {
                var blockObj = new GameObject("Block");
                blockObj.transform.SetParent(_previewRoot, false);

                // LayoutGroup temizliği
                var blockLg = blockObj.GetComponent<UnityEngine.UI.LayoutGroup>();
                if (blockLg != null) blockLg.enabled = false;
                var blockCsf = blockObj.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (blockCsf != null) blockCsf.enabled = false;

                // Ana Image
                var img = blockObj.AddComponent<Image>();
                img.color = shape.ShapeColor;

                // Kenarlık (border)
                var outlineObj = new GameObject("Outline");
                outlineObj.transform.SetParent(blockObj.transform, false);
                var outlineImg = outlineObj.AddComponent<Image>();

                Color outlineColor = new Color(
                    Mathf.Max(0f, shape.ShapeColor.r - 0.3f),
                    Mathf.Max(0f, shape.ShapeColor.g - 0.3f),
                    Mathf.Max(0f, shape.ShapeColor.b - 0.3f),
                    1f
                );
                outlineImg.color = outlineColor;

                var outlineRect = outlineObj.GetComponent<RectTransform>();
                if (outlineRect == null) outlineRect = outlineObj.AddComponent<RectTransform>();
                outlineRect.anchorMin = Vector2.zero;
                outlineRect.anchorMax = Vector2.one;
                outlineRect.pivot = new Vector2(0.5f, 0.5f);
                outlineRect.anchoredPosition = Vector2.zero;

                float borderThickness = Mathf.Max(2f, visual * 0.1f);
                outlineRect.sizeDelta = new Vector2(-borderThickness * 2f, -borderThickness * 2f);

                // RectTransform
                var rect = blockObj.GetComponent<RectTransform>();
                if (rect == null) rect = blockObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(visual, visual);

                // Board ile TAM AYNI YÖN: row arttıkça aşağı
                float visRowOffset = -(block.localRow - centerY) * step;
                float visColOffset = (block.localCol - centerX) * step;
                rect.anchoredPosition = new Vector2(visColOffset, visRowOffset);
            }
            
            // Set dynamic lift height based on tile size (approx 1.5 tiles above)
            _dragLiftHeight = step * 2.5f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
           // Handled in OnBeginDrag for cleaner logic, but we need to capture initial state here if needed.
           // Leaving empty as OnBeginDrag handles initialization.
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_currentShape == null) return;
            
            _isDragging = true;
            _initialPosition = _rectTransform.anchoredPosition;
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();

            // 1. Find Canvas Root
            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
                if (_rootCanvas != null)
                {
                    _rootCanvasRect = _rootCanvas.GetComponent<RectTransform>();
                }
            }

            // 2. Reparent to Root (Keep World Position)
            if (_rootCanvas != null)
            {
                transform.SetParent(_rootCanvas.transform, true);
                transform.SetAsLastSibling(); // Render on top
            }

            // 3. Calculate Drag Offset
            // We want the shape to stick to the finger relative to where we grabbed it, 
            // BUT with a vertical lift so the user can see under their finger.
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvasRect, 
                eventData.position, 
                _rootCanvas.worldCamera, 
                out Vector2 localPointerPos
            );
            
            // Calculate current anchored position in Root Canvas space
            Vector2 currentPos = _rectTransform.anchoredPosition;
            
            // Offset = ShapePos - PointerPos
            _dragOffset = currentPos - localPointerPos;

            // 4. Visual Feedback
            _rectTransform.localScale = Vector3.one * 1.1f; // Slight scale up
            _canvasGroup.blocksRaycasts = false; // Allow raycast to pass through to board


            OnDragStarted?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _currentShape == null || _rootCanvasRect == null) return;

            // 1. Convert Screen Pointer to Local Canvas Space
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvasRect,
                eventData.position,
                _rootCanvas.worldCamera,
                out Vector2 localPointerPos))
            {
                // 2. Apply Offset + Lift
                Vector2 targetPos = localPointerPos + _dragOffset;
                
                // Lift the shape UPwards (Y+) so it sits above the finger
                targetPos.y += _dragLiftHeight;

                // 3. Apply to RectTransform
                _rectTransform.anchoredPosition = targetPos;
                
                // 4. Clamp Removed (User feedback: Shape gets stuck at top)
                // ClampToWindow(); 
            }

            OnDragUpdated?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Handled in OnPointerUp usually to cover both drag-drop and click-release
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_currentShape == null) return;
            
            _isDragging = false;

            // Logic handled by GameManager via OnDragEnded event (checking drop zone)
            // If drop was handled (shape placed), GameManager will consume/reset.
            // If not placed, we revert here.
            
            // Wait for one frame? No, event invocation is synchronous. 
            // If placed, `Clear()` is called which resets state.
            // If NOT placed, we revert visually.
            
            OnDragEnded?.Invoke(this, eventData);
            
            // If logic didn't clear the shape (i.e. invalid drop), return to slot
            if (_currentShape != null) 
            {
                ReturnToSlot();
            }
        }
        
        private void ReturnToSlot()
        {
            if (_originalParent != null)
            {
                transform.SetParent(_originalParent, false); // false = resets local position automatically to some degree, but better explicit
                transform.SetSiblingIndex(_originalSiblingIndex);
            }

            _rectTransform.anchoredPosition = _initialPosition;
            _rectTransform.localScale = Vector3.one;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
        }
    }
}
