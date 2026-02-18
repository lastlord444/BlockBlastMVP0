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
        // _blockSize dinamik hesaplanır (SlotSize / maxDimension * 0.85)
        private float _spacing = 2f;
        private const float _slotSize = 150f; // Layout'ta preferredWidth/Height ile uyumlu
        private const float _maxScaleFactor = 0.85f; // Slot içinde doluluk oranı (padding)

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
            // 1. Calculate Bounds
            int minR = int.MaxValue, maxR = int.MinValue;
            int minC = int.MaxValue, maxC = int.MinValue;

            foreach (var block in shape.Blocks)
            {
                if (block.localRow < minR) minR = block.localRow;
                if (block.localRow > maxR) maxR = block.localRow;
                if (block.localCol < minC) minC = block.localCol;
                if (block.localCol > maxC) maxC = block.localCol;
            }

            int widthInBlocks = maxC - minC + 1;
            int heightInBlocks = maxR - minR + 1;

            // 2. Dinamik block size hesapla: slot içine tam sığsın, taşmasın
            float availableSize = _slotSize * _maxScaleFactor;
            float blockSizeByWidth  = (availableSize - (widthInBlocks  - 1) * _spacing) / widthInBlocks;
            float blockSizeByHeight = (availableSize - (heightInBlocks - 1) * _spacing) / heightInBlocks;
            
            // İkisinden küçüğünü al: 150px slot, 4 blok yüksekliğinde şekil sığmalı
            float dynamicBlockSize = Mathf.Min(blockSizeByWidth, blockSizeByHeight);
            // Minimum 8px, maksimum 40px (orijinal değer)
            dynamicBlockSize = Mathf.Clamp(dynamicBlockSize, 8f, 40f);

            float totalWidth  = widthInBlocks  * dynamicBlockSize + (widthInBlocks  - 1) * _spacing;
            float totalHeight = heightInBlocks * dynamicBlockSize + (heightInBlocks - 1) * _spacing;

            // ÖNEMLI: Preview root'un boyutunu shape bounds'una ayarla
            if (_previewRoot != null)
            {
                _previewRoot.sizeDelta = new Vector2(totalWidth, totalHeight);
            }

            // 3. Instantiate Blocks
            
            // LOGICAL CENTER (orta blok (0,0)'da olsun)
            int centerR = Mathf.RoundToInt((minR + maxR) / 2f);
            int centerC = Mathf.RoundToInt((minC + maxC) / 2f);

            foreach (var block in shape.Blocks)
            {
                var blockObj = new GameObject("Block");
                blockObj.transform.SetParent(_previewRoot, false);

                var img = blockObj.AddComponent<Image>();
                img.color = shape.ShapeColor;
                
                var rect = blockObj.GetComponent<RectTransform>();
                if (rect == null) rect = blockObj.AddComponent<RectTransform>(); 
                rect.sizeDelta = new Vector2(dynamicBlockSize, dynamicBlockSize);

                // Merkeze hizala
                int relR = block.localRow - centerR;
                int relC = block.localCol - centerC;

                float xPos = relC * (dynamicBlockSize + _spacing);
                float yPos = -relR * (dynamicBlockSize + _spacing);

                rect.anchoredPosition = new Vector2(xPos, yPos);
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
