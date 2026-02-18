using System.Collections;
using UnityEngine;

namespace Common.Juice
{
    /// <summary>
    /// JUICE PASS v1 — Tek merkezi ses ve titreşim yöneticisi.
    /// Singleton pattern: Sahneye bir kere eklenir, tekrar eklenmez.
    /// AudioClip'ler Inspector'dan atanır; atanmamışsa sessizce geçilir.
    /// </summary>
    public class GameJuiceManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────────
        public static GameJuiceManager Instance { get; private set; }

        // ── Inspector Alanları ─────────────────────────────────────────────────────
        [Header("Audio Clips")]
        [SerializeField] private AudioClip _clipPlace;
        [SerializeField] private AudioClip _clipInvalid;
        [SerializeField] private AudioClip _clipLineClear;
        [SerializeField] private AudioClip _clipGameOver;

        [Header("Ses Ayarları")]
        [SerializeField] [Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField] [Range(0.5f, 2f)] private float _basePitch  = 1f;
        /// <summary>Her combo adımında pitch ne kadar artar (0.07 = +7% per line)</summary>
        [SerializeField] [Range(0f, 0.2f)] private float _comboPitchStep = 0.07f;

        [Header("UI Shake Ayarları")]
        [SerializeField] private RectTransform _shakeTarget;   // Genellikle SafeAreaContainer
        [SerializeField] [Range(0f, 40f)]  private float _shakeAmplitudeNormal  = 10f;
        [SerializeField] [Range(0f, 80f)]  private float _shakeAmplitudeStrong  = 26f;
        [SerializeField] [Range(0f, 0.5f)] private float _shakeDuration         = 0.22f;
        [SerializeField] [Range(0f, 0.5f)] private float _shakeInvalidDuration  = 0.14f;

        // ── Private Durum ──────────────────────────────────────────────────────────
        private AudioSource _audioSource;
        private Coroutine   _shakeRoutine;
        private Vector2     _shakeOrigin;

        // ──────────────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Genel Yardımcı — Tek clip çal
        // ──────────────────────────────────────────────────────────────────────────
        private void Play(AudioClip clip, float pitch = 1f, float volumeScale = 1f)
        {
            if (clip == null) return;
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip, _masterVolume * volumeScale);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Genel API — Dışarıdan çağrılan metodlar
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Blok başarıyla yerleştirildi.</summary>
        public void OnPlace()
        {
            Play(_clipPlace, _basePitch);
            TriggerHapticLight();
        }

        /// <summary>Geçersiz drop — blok yerleşemedi.</summary>
        public void OnInvalid()
        {
            Play(_clipInvalid, _basePitch * 0.9f);
            TriggerHapticLight();
            ShakeUI(_shakeAmplitudeNormal * 0.6f, _shakeInvalidDuration);
        }

        /// <summary>
        /// Çizgi temizlendi.
        /// <param name="comboIndex">Kaçıncı çizgi (0-tabanlı). Pitch artar.</param>
        /// </summary>
        public void OnLineClear(int comboIndex = 0)
        {
            float pitch = _basePitch + comboIndex * _comboPitchStep;
            Play(_clipLineClear, pitch);
            TriggerHapticMedium();
            ShakeUI(_shakeAmplitudeNormal, _shakeDuration);
        }

        /// <summary>Game Over.</summary>
        public void OnGameOver()
        {
            Play(_clipGameOver, _basePitch * 0.85f, 0.9f);
            TriggerHapticStrong();
            ShakeUI(_shakeAmplitudeStrong, _shakeDuration * 1.5f);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Haptic (Android — Handheld.Vibrate())
        // ──────────────────────────────────────────────────────────────────────────
        private static void TriggerHapticLight()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private static void TriggerHapticMedium()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private static void TriggerHapticStrong()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
            Handheld.Vibrate();
#endif
        }

        // ──────────────────────────────────────────────────────────────────────────
        // UI Shake — RectTransform random offset coroutine
        // ──────────────────────────────────────────────────────────────────────────
        public void ShakeUI(float amplitude, float duration)
        {
            if (_shakeTarget == null) return;
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeOrigin  = _shakeTarget.anchoredPosition;
            _shakeRoutine = StartCoroutine(ShakeRoutine(amplitude, duration));
        }

        private IEnumerator ShakeRoutine(float amplitude, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float decay = 1f - (elapsed / duration);        // Amplitude zamanla azalır
                float ox = Random.Range(-1f, 1f) * amplitude * decay;
                float oy = Random.Range(-1f, 1f) * amplitude * decay;
                _shakeTarget.anchoredPosition = _shakeOrigin + new Vector2(ox, oy);
                yield return null;
            }
            _shakeTarget.anchoredPosition = _shakeOrigin;
            _shakeRoutine = null;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Inspector Yardımcısı — Sahne başlangıcında shake hedefini oto-bul
        // ──────────────────────────────────────────────────────────────────────────
        public void AutoSetShakeTarget(RectTransform target)
        {
            if (_shakeTarget == null)
                _shakeTarget = target;
        }
    }
}
