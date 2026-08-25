using UnityEngine;

namespace VN
{
    public class VNAudioManager : MonoBehaviour
    {
        public static VNAudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Default Audio Clips")]
        [SerializeField] private AudioClip _defaultBGM;
        [SerializeField] private AudioClip _defaultButtonClickSFX;

        [Header("Volume Controls (0.0 to 1.0)")]
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _bgmVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;

        public float MasterVolume
        {
            get => _masterVolume;
            set { _masterVolume = Mathf.Clamp01(value); UpdateVolumes(); }
        }

        public float BGMVolume
        {
            get => _bgmVolume;
            set { _bgmVolume = Mathf.Clamp01(value); UpdateVolumes(); }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = Mathf.Clamp01(value); UpdateVolumes(); }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_bgmSource == null) _bgmSource = gameObject.AddComponent<AudioSource>();
            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();

            _bgmSource.loop = true;
            UpdateVolumes();
        }

        private void Start()
        {
            if (_defaultBGM != null)
            {
                PlayBGM(_defaultBGM);
            }
        }

        public void PlayDefaultClickSFX()
        {
            if (_defaultButtonClickSFX != null)
            {
                PlaySFX(_defaultButtonClickSFX);
            }
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null)
            {
                _bgmSource.Stop();
                return;
            }

            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            Debug.Log($"[VNAudioManager] Playing BGM: '{clip.name}'");
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null)
            {
                Debug.Log($"[VNAudioManager] Playing SFX: '{clip.name}'");
                _sfxSource.PlayOneShot(clip, _sfxVolume * _masterVolume);
            }
        }

        private void UpdateVolumes()
        {
            if (_bgmSource != null) _bgmSource.volume = _bgmVolume * _masterVolume;
            if (_sfxSource != null) _sfxSource.volume = _sfxVolume * _masterVolume;
        }
    }
}
