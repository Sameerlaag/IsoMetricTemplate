using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DPSGameManager : MonoBehaviour
{
    public static DPSGameManager Instance { get; private set; }

    [Header("Scene Settings")] 
    [SerializeField] private string _gameSceneName = "DoomFps";
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    [Header("Input")]
    [SerializeField] private InputReader _inputReader;

    public event Action<bool> OnPauseStateChanged;
    // Kept for backward compatibility
    public event Action<bool> ResumedGame;

    public bool IsPaused { get; private set; }
    private DPSMainMenuController _activeMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Whenever a new scene loads, ensure we are completely unpaused
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (scene.name == _mainMenuSceneName)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_inputReader != null)
            {
                _inputReader.DisablePlayerActions();
            }
        }
        else if (scene.name == _gameSceneName)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_inputReader != null)
            {
                _inputReader.EnablePlayerActions();
            }
        }
    }

    private void Update()
    {
        // Only allow ESC toggle in the game scene and use wasPressedThisFrame
        if (SceneManager.GetActiveScene().name == _gameSceneName)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void StartGame()
    {
        ResumeGame(); // Reset pause state before loading
        SceneManager.LoadScene(_gameSceneName);
    }

    public void ReturnToMainMenu()
    {
        ResumeGame(); // Reset pause state before loading
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    public void PauseGame()
    {
        if (IsPaused) return;

        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true; // Stop sounds while paused

        if (_inputReader != null)
        {
            _inputReader.DisablePlayerActions();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnPauseStateChanged?.Invoke(true);
        UpdateMenuState();
    }

    public void ResumeGame()
    {
        if (!IsPaused && Time.timeScale == 1f) return;

        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false; // Restore sounds

        if (SceneManager.GetActiveScene().name == _gameSceneName)
        {
            if (_inputReader != null)
            {
                _inputReader.EnablePlayerActions();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        OnPauseStateChanged?.Invoke(false);
        ResumedGame?.Invoke(true);

        UpdateMenuState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && SceneManager.GetActiveScene().name == _gameSceneName && !IsPaused)
        {
            PauseGame();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && SceneManager.GetActiveScene().name == _gameSceneName && !IsPaused)
        {
            PauseGame();
        }
    }

    public void RegisterMenu(DPSMainMenuController menu)
    {
        _activeMenu = menu;
        UpdateMenuState();
    }

    public void UnregisterMenu(DPSMainMenuController menu)
    {
        if (_activeMenu == menu) _activeMenu = null;
    }

    private void UpdateMenuState()
    {
        if (_activeMenu != null)
        {
            _activeMenu.SetPausedState(IsPaused);
        }
    }
}
