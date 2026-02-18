using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// Shape slot panelini Android safe area içinde pinler (21:9/20:9 notch/home bar uyumu).
    /// Start'ta bir kez DEV log basar: safeArea rect + tray rect.
    /// </summary>
    public class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform _trayPanel; // Alt 3 shape slot paneli
        [SerializeField] private RectTransform _canvas;   // Canvas (Screen.safeArea hesabı için)

        private RectTransform _safeAreaRect;
        private RectTransform _canvasRect;

        private void Start()
        {
            if (_trayPanel == null || _canvas == null)
            {
                Debug.LogWarning("[SafeAreaFitter] TrayPanel veya Canvas atanmadı.");
                return;
            }

            _canvasRect = _canvas.GetComponent<RectTransform>();
            _safeAreaRect = GetComponent<RectTransform>();

            if (_canvasRect == null || _safeAreaRect == null)
            {
                Debug.LogWarning("[SafeAreaFitter] Canvas veya SafeArea RectTransform bulunamadı.");
                return;
            }

            ApplySafeArea();
            LogValidation();
        }

        private void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            // Safe area'yi canvas space'e çevir (0-1 aralığı)
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Tray panel'i bottom-center anchor'la, safe area içinde kalacak şekilde ayarla
            // Pivot = (0.5, 0) → bottom'dan büyüme
            _trayPanel.anchorMin = new Vector2(0f, anchorMin.y);
            _trayPanel.anchorMax = new Vector2(1f, 1f); // Top yeterli yukarı (safeArea.min'den)
            _trayPanel.pivot = new Vector2(0.5f, 0f);

            // Bottom padding (safe area altına yapışık)
            _trayPanel.anchoredPosition = new Vector2(0f, 0f);
            _trayPanel.sizeDelta = new Vector2(0f, anchorMin.y * Screen.height); // Yüksekliği safeArea'den düşür
        }

        private void LogValidation()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Rect safeArea = Screen.safeArea;
            Rect trayRect = GetScreenRect(_trayPanel);
            Rect canvasRect = GetScreenRect(_canvasRect);

            string log = $"[SafeAreaFitter]\n" +
                       $"  SafeArea: {safeArea}\n" +
                       $"  Tray:     {trayRect}\n" +
                       $"  Canvas:   {canvasRect}\n" +
                       $"  Overlap:   {RectOverlap(safeArea, trayRect)}";
            Debug.Log(log);
#endif
        }

        private Rect GetScreenRect(RectTransform rt)
        {
            if (rt == null) return new Rect(0, 0, 0, 0);

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector2 min = corners[0];
            Vector2 max = corners[0];

            for (int i = 1; i < 4; i++)
            {
                min = Vector2.Min(min, (Vector2)corners[i]);
                max = Vector2.Max(max, (Vector2)corners[i]);
            }

            // Canvas space (0,0 bottom-left) → Screen space (0,0 top-left)
            float screenHeight = Screen.height;
            return new Rect(min.x, screenHeight - max.y, max.x - min.x, max.y - min.y);
        }

        private bool RectOverlap(Rect a, Rect b)
        {
            return a.x < b.x + b.width && a.x + a.width > b.x &&
                   a.y < b.y + b.height && a.y + a.height > b.y;
        }
    }
}
