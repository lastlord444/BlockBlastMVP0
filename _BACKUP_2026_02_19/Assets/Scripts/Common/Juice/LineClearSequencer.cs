using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common.Juice
{
    /// <summary>
    /// Çizgi temizleme VFX ve sequencing.
    /// - Juice (SFX/Haptic/Shake) = line başına 1 kez
    /// - Flash/Particle = hücre başına (dedupe kesişimler)
    /// - Çoklu çizgi: Sırayla (0.06s delay) temizlenir
    /// </summary>
    public class LineClearSequencer : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Prefabs")]
        [SerializeField] private GameObject _particlePrefab; // Spark/toka efekti prefab

        [Header("Alternatif: Yerel sprite-based particle (Prefab yoksa)")]
        [SerializeField] private bool _useFallbackParticle = true;
        [SerializeField] private Color _particleColor = new Color(1f, 0.9f, 0.4f);

        [Header("Flash Ayarları")]
        [SerializeField] private Color _flashColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private float _flashDuration = 0.12f;

        [Header("Sequence Ayarları")]
        [SerializeField] private float _lineDelay = 0.06f;   // Line arası gecikme

        [Header("Board Renderer Referansı")]
        [SerializeField] private UnityGameBoardRenderer _boardRenderer;

        // ── Public API ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Çoklu çizgi temizlemeyi başlatır (line-based juice).
        /// </summary>
        public void StartClearSequence(List<int> rows, List<int> cols, Action onComplete)
        {
            StartCoroutine(DoClearSequence(rows, cols, onComplete));
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Private Implementation
        // ──────────────────────────────────────────────────────────────────────────
        private IEnumerator DoClearSequence(List<int> rows, List<int> cols, Action onComplete)
        {
            if (_boardRenderer == null) yield break;

            var boardConfig = _boardRenderer.GetBoardConfig();
            if (boardConfig == null) yield break;

            // Hücreleri dedupe ile topla (kesişim=1 kez render)
            var tilesToClear = new HashSet<(int, int)>();
            foreach (int r in rows)
            {
                for (int c = 0; c < boardConfig.ColumnCount; c++)
                    tilesToClear.Add((r, c));
            }
            foreach (int c in cols)
            {
                for (int r = 0; r < boardConfig.RowCount; r++)
                    tilesToClear.Add((r, c));
            }

            // LINE BAŞINA JUICE: Kombinasyon satırları + sütunları process et
            int lineIndex = 0;

            // Satırları işle (her satır = 1 line = 1 juice)
            foreach (int r in rows)
            {
                // 1. Line-based juice (satır başına 1 kez)
                GameJuiceManager.Instance?.OnLineClear(lineIndex);
                lineIndex++;

                // 2. Flash + Particle (satırdaki tüm hücreler)
                for (int c = 0; c < boardConfig.ColumnCount; c++)
                {
                    FlashTile(r, c);
                    if (tilesToClear.Contains((r, c)))
                        SpawnParticle(r, c);
                }

                // 3. Line arası gecikme
                yield return new WaitForSeconds(_lineDelay);
            }

            // Sütunları işle (her sütun = 1 line = 1 juice)
            foreach (int c in cols)
            {
                // 1. Line-based juice (sütun başına 1 kez)
                GameJuiceManager.Instance?.OnLineClear(lineIndex);
                lineIndex++;

                // 2. Flash + Particle (sütundaki hücreler, kesişim hariç)
                for (int r = 0; r < boardConfig.RowCount; r++)
                {
                    // Kesişim hücrelerinde 2 kez render etmemek için satırda yoksa işle
                    if (!rows.Contains(r))
                    {
                        FlashTile(r, c);
                        if (tilesToClear.Contains((r, c)))
                            SpawnParticle(r, c);
                    }
                }

                // 3. Line arası gecikme
                yield return new WaitForSeconds(_lineDelay);
            }

            onComplete?.Invoke();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Yardımcılar
        // ──────────────────────────────────────────────────────────────────────────
        private void FlashTile(int r, int c)
        {
            StartCoroutine(FlashTileCoroutine(r, c));
        }

        private IEnumerator FlashTileCoroutine(int r, int c)
        {
            if (_boardRenderer == null) yield break;

            // Flash renkini uygula
            _boardRenderer.SetTileColor(new Match3.Core.Structs.GridPosition(r, c), _flashColor);

            yield return new WaitForSeconds(_flashDuration);

            // Checkerboard varsayılan rengine dön
            Color defaultColor = GetDefaultCheckerColor(r, c);
            _boardRenderer.SetTileColor(new Match3.Core.Structs.GridPosition(r, c), defaultColor);
        }

        private Color GetDefaultCheckerColor(int r, int c)
        {
            // Premium checkerboard renkleri (BlockBlastGameManager ile aynı)
            Color cellDark  = new Color(0.12f, 0.16f, 0.22f, 1f);
            Color cellLight = new Color(0.16f, 0.21f, 0.28f, 1f);
            return (r + c) % 2 == 0 ? cellDark : cellLight;
        }

        // ── Particle Fallback Cache (NO per-spawn Texture2D) ─────────────────────────
        // Her renk değiştiğinde cache'i güncellemek için rengi takip et
        private static Sprite _cachedSprite;
        private static Color  _cachedSpriteColor = Color.clear;
        private static Texture2D _cachedTexture;

        private Sprite GetOrCreateFallbackSprite()
        {
            // Renk değişmediyse cache'i kullan
            if (_cachedSprite != null && _cachedSpriteColor == _particleColor)
                return _cachedSprite;

            // Eski texture'ı temizle (domain reload güvenliği)
            if (_cachedTexture != null)
            {
                Destroy(_cachedTexture);
                _cachedTexture = null;
                _cachedSprite  = null;
            }

            // Tek seferlik texture oluştur
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.name = "SparkFallback_Cache";
            Color[] pixels = new Color[256];
            for (int i = 0; i < 256; i++) pixels[i] = _particleColor;
            tex.SetPixels(pixels);
            tex.Apply(false, true); // readWrite=false → VRAM'e yükle, daha az bellek
            tex.hideFlags = HideFlags.DontSaveInBuild; // Build'e sızmasın

            _cachedTexture    = tex;
            _cachedSprite     = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            _cachedSpriteColor = _particleColor;
            return _cachedSprite;
        }

        private void OnDestroy()
        {
            // Domain reload / scene unload sırasında cache'i temizle
            if (_cachedTexture != null)
            {
                Destroy(_cachedTexture);
                _cachedTexture = null;
                _cachedSprite  = null;
            }
        }

        private void SpawnParticle(int r, int c)
        {
            var boardConfig = _boardRenderer?.GetBoardConfig();
            if (boardConfig == null) return;

            Vector3 worldPos = boardConfig.GetWorldPosition(r, c);
            worldPos.z = -2f;

            // Prefab varsa kullan
            if (_particlePrefab != null)
            {
                Instantiate(_particlePrefab, worldPos, Quaternion.identity);
                return;
            }

            // Prefab yoksa fallback — CACHED sprite kullan (per-spawn Texture2D YASAK)
            if (!_useFallbackParticle) return;

            var go = new GameObject($"Spark_{r}_{c}");
            go.transform.position = worldPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateFallbackSprite(); // Cache'den al
            sr.sortingOrder = 20;

            // Küçük scale
            go.transform.localScale = Vector3.one * (boardConfig.TileSize * 0.4f);

            // Coroutine ile animasyon ve destroy
            StartCoroutine(AnimateAndDestroyParticle(go, sr));
        }

        private IEnumerator AnimateAndDestroyParticle(GameObject go, SpriteRenderer sr)
        {
            float elapsed = 0f;
            float duration = 0.4f;
            Vector3 startScale = go.transform.localScale;
            Color startColor = sr.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Scale artışı + fade out
                go.transform.localScale = startScale * (1f + t * 0.5f);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

                yield return null;
            }

            Destroy(go);
        }

        // ── Inspector Helper ───────────────────────────────────────────────────────
        public void SetBoardRenderer(UnityGameBoardRenderer renderer)
        {
            _boardRenderer = renderer;
        }
    }
}
