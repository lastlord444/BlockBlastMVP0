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
            // Board: new Vector3(col, -row) * tileSize (Y-down)
            // UI: anchoredPosition.y = -(row - centerY) * step (Y-down match)
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
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_currentShape == null) return;

            _initialPosition = _rectTransform.anchoredPosition;
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();

            // Canvas root'u bul (drag sırasında reparent için)
            if (_rootCanvas == null)
                _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

            OnDragStarted?.Invoke(this, eventData);
            
            // Premium UX: Scale up for "lifted" feel (industry standard: 1.1-1.2x)
            _rectTransform.localScale = Vector3.one * 1.15f;
            _canvasGroup.alpha = 1.0f;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Debug.Log("Drag Started on ShapeSlot");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_currentShape == null) return;

            // İlk drag hareketinde canvas root'a reparent et (panel clipping'i önler)
            if (_rootCanvas != null && transform.parent != _rootCanvas.transform)
            {
                // Dünya pozisyonunu koru, canvas root'a taşı
                transform.SetParent(_rootCanvas.transform, true);
                transform.SetAsLastSibling(); // Her şeyin üzerinde görünsün
            }

            _canvasGroup.alpha = 1.0f;

            // Delta tabanlı hareket: parmağı smooth takip et
            Vector2 delta = eventData.delta / transform.lossyScale.x;
            _rectTransform.anchoredPosition += delta;

            // Canvas sınırlarına clamp (canvas root'a göre)
            if (_rootCanvas != null)
            {
                RectTransform canvasRect = _rootCanvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    Vector2 halfSize = _rectTransform.rect.size * 0.5f;
                    Vector2 canvasHalf = canvasRect.rect.size * 0.5f;
                    Vector2 pos = _rectTransform.anchoredPosition;
                    pos.x = Mathf.Clamp(pos.x, -canvasHalf.x + halfSize.x, canvasHalf.x - halfSize.x);
                    pos.y = Mathf.Clamp(pos.y, -canvasHalf.y + halfSize.y, canvasHalf.y - halfSize.y);
                    _rectTransform.anchoredPosition = pos;
                }
            }

            OnDragUpdated?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Debug.Log("Drag Ended on ShapeSlot");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_currentShape == null) return;

            // Orijinal parent'a geri dön (worldPositionStays=false → anchoredPosition doğru hesaplanır)
            if (_originalParent != null && transform.parent != _originalParent)
            {
                transform.SetParent(_originalParent, false);
                transform.SetSiblingIndex(_originalSiblingIndex);
            }

            // Scale ve pozisyonu sıfırla
            _rectTransform.localScale = Vector3.one;
            _canvasGroup.alpha = 1f;
            _rectTransform.anchoredPosition = _initialPosition;

            OnDragEnded?.Invoke(this, eventData);
        }
    }
}
