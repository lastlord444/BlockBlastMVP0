using UnityEngine;
using UnityEngine.UI;

namespace Common.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Source")]
        [SerializeField] private AudioSource _sfxSource;

        [Header("Clips")]
        [SerializeField] private AudioClip _placeClip;
        [SerializeField] private AudioClip _clearClip;
        [SerializeField] private AudioClip _clickClip;
        [SerializeField] private AudioClip _gameOverClip;

        // PlayerPrefs Keys
        private const string KEY_SFX = "opt_sfx";
        private const string KEY_VIB = "opt_vib";

        public bool IsSfxEnabled { get; private set; }
        public bool IsVibEnabled { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Initialize Settings
                IsSfxEnabled = PlayerPrefs.GetInt(KEY_SFX, 1) == 1;
                IsVibEnabled = PlayerPrefs.GetInt(KEY_VIB, 1) == 1;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ToggleSfx(bool isOn)
        {
            IsSfxEnabled = isOn;
            PlayerPrefs.SetInt(KEY_SFX, isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ToggleVibration(bool isOn)
        {
            IsVibEnabled = isOn;
            PlayerPrefs.SetInt(KEY_VIB, isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void PlayClick() => PlaySound(_clickClip);
        public void PlayPlace()
        {
            PlaySound(_placeClip);
            Vibrate();
        }
        public void PlayClear()
        {
            PlaySound(_clearClip);
            Vibrate();
        }
        public void PlayGameOver()
        {
            PlaySound(_gameOverClip);
            Vibrate();
        }

        private void PlaySound(AudioClip clip)
        {
            if (IsSfxEnabled && clip != null && _sfxSource != null)
            {
                _sfxSource.PlayOneShot(clip);
            }
        }

        private void Vibrate()
        {
            if (IsVibEnabled)
            {
                Handheld.Vibrate();
            }
        }
    }
}
