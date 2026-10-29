using System.Collections;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DPSMainMenuController : MonoBehaviour
{
    [Header("UI References")] 
    [SerializeField] private GameObject _menuPanel; 
    [SerializeField] private CanvasGroup _menuCanvasGroup; // Add a CanvasGroup to your _menuPanel
    [SerializeField] private TMP_Text _startResumeButtonText; 
    [SerializeField] private string _startText = "Start";
    [SerializeField] private string _resumeText = "Resume";

    [Header("Neon & Background Pulse Effect")]
    [SerializeField] private Graphic[] _neonElements; // Assign UI elements (images/texts) you want to pulse with neon color
    [SerializeField] private TMP_Text[] _neonTextMeshPros; // Assign TextMeshPro components for neon glow styling
    [SerializeField] private Image _backgroundPiece; // The background image element whose opacity you want to animate
    [SerializeField, Range(0f, 1f)] private float _backgroundAlphaMin = 0.2f; // Value X (min opacity)
    [SerializeField, Range(0f, 1f)] private float _backgroundAlphaMax = 0.8f; // Value Y (max opacity)
    [SerializeField] private Color _neonColorA = new Color(0f, 1f, 0.8f, 1f); // e.g., Cyan neon
    [SerializeField] private Color _neonColorB = new Color(1f, 0.08f, 0.58f, 1f); // e.g., Hot pink neon
    [SerializeField, Min(0.1f)] private float _pulseSpeed = 2f; // Speed of the neon/background pulse rhythm

    [Header("Audio Settings")] 
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private string _audioMixerParameter = "MasterVolume";
    [SerializeField] private Slider _volumeSlider;

    [Header("Scene Names")] 
    [SerializeField] private string _gameSceneName = "DoomFps";

    private void Awake()
    {
        if (DPSGameManager.Instance != null)
        {
            DPSGameManager.Instance.RegisterMenu(this);
        }
    }

    private void OnDestroy()
    {
        if (DPSGameManager.Instance != null)
        {
            DPSGameManager.Instance.UnregisterMenu(this);
        }
    }

    private void Start()
    {
        if (_volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
            _volumeSlider.value = savedVolume;
            SetVolume(savedVolume);
            _volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        SetPausedState(DPSGameManager.Instance != null && DPSGameManager.Instance.IsPaused);
    }

    private void Update()
    {
        // Calculate a shared pulse rhythm (0 to 1 loop using sine wave scaled by unscaledTime so it works when paused)
        float pulse = (Mathf.Sin(Time.unscaledTime * _pulseSpeed * Mathf.PI) + 1f) * 0.5f;

        // 1. Update background opacity between min (X) and max (Y)
        if (_backgroundPiece != null)
        {
            Color bgCol = _backgroundPiece.color;
            bgCol.a = Mathf.Lerp(_backgroundAlphaMin, _backgroundAlphaMax, pulse);
            _backgroundPiece.color = bgCol;
        }

        // 2. Animate Standard UI Neon Elements (Image/Text tint color shifting or glowing)
        Color currentNeonColor = Color.Lerp(_neonColorA, _neonColorB, pulse);
        
        if (_neonElements != null)
        {
            foreach (var graphic in _neonElements)
            {
                if (graphic != null)
                {
                    graphic.color = currentNeonColor;
                }
            }
        }

        // 3. Animate TextMeshPro elements (supports face color and glowing setup)
        if (_neonTextMeshPros != null)
        {
            foreach (var tmpText in _neonTextMeshPros)
            {
                if (tmpText != null)
                {
                    tmpText.color = currentNeonColor;
                    
                    // Optional: If you enable material glow properties on your TMP shader, 
                    // you can sync the glow intensity here as well:
                    // tmpText.fontSharedMaterial.SetFloat(ShaderUtilities.ID_GlowPower, pulse * 0.5f);
                }
            }
        }
    }

    public void SetPausedState(bool isPaused)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == _gameSceneName)
        {
            if (_menuPanel != null) _menuPanel.SetActive(isPaused);

            if (_startResumeButtonText != null)
            {
                _startResumeButtonText.text = isPaused ? _resumeText : _startText;
            }

            if (_menuCanvasGroup != null)
            {
                _menuCanvasGroup.alpha = isPaused ? 1f : 0f;
                _menuCanvasGroup.interactable = isPaused;
                _menuCanvasGroup.blocksRaycasts = isPaused;
            }
        }
        else
        {
            // In main menu scene: Always active, show "Start"
            if (_menuPanel != null) _menuPanel.SetActive(true);
            if (_startResumeButtonText != null) _startResumeButtonText.text = _startText;

            if (_menuCanvasGroup != null)
            {
                _menuCanvasGroup.alpha = 1f;
                _menuCanvasGroup.interactable = true;
                _menuCanvasGroup.blocksRaycasts = true;
            }
        }
    }

    public void OnStartResumePressed()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene != _gameSceneName)
        {
            // From Main Menu: Start the Game
            if (DPSGameManager.Instance != null)
            {
                DPSGameManager.Instance.StartGame();
            }
            else
            {
                SceneManager.LoadScene(_gameSceneName);
            }
        }
        else
        {
            // In Game Pause Menu: Fade out UI & Resume Game
            if (_menuCanvasGroup != null)
            {
                StartCoroutine(FadeOutMenuAndResume(0.4f));
            }
            else
            {
                if (DPSGameManager.Instance != null)
                {
                    DPSGameManager.Instance.ResumeGame();
                }
            }
        }
    }

    private IEnumerator FadeOutMenuAndResume(float duration)
    {
        if (_menuCanvasGroup != null)
        {
            _menuCanvasGroup.interactable = false;
            _menuCanvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaledDeltaTime because Time.timeScale may be 0
                _menuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
        }

        if (DPSGameManager.Instance != null)
        {
            DPSGameManager.Instance.ResumeGame();
        }
    }

    public void SetVolume(float sliderValue)
    {
        if (_audioMixer != null)
        {
            float dB = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20f : -80f;
            _audioMixer.SetFloat(_audioMixerParameter, dB);
        }
        else
        {
            AudioListener.volume = sliderValue;
        }

        PlayerPrefs.SetFloat("GameVolume", sliderValue);
    }

    public void OnExitPressed()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}