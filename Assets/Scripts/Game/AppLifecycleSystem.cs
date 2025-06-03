using UnityEngine;
using System;

/// <summary>
/// Kaan Çakar 2025 - AppLifecycleManager.cs
/// App lifecycle management - pause/resume/quit handling
/// </summary>
public class AppLifecycleManager : MonoBehaviour
{
    [Header("Lifecycle Settings")]
    public bool enablePauseOnBackground = true;
    public bool enableLifeLossOnQuit = true;
    public float quitDetectionTimeSeconds = 5f;


    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnGamePaused;
    public UnityEngine.Events.UnityEvent OnGameResumed;
    public UnityEngine.Events.UnityEvent OnGameQuit;
    public UnityEngine.Events.UnityEvent OnAppBackgrounded;
    public UnityEngine.Events.UnityEvent OnAppForegrounded;

    // Internal state
    private bool isGamePaused = false;
    private bool wasInLevel = false;
    private DateTime backgroundTime;
    private string lastSceneName = "";

    // PlayerPrefs Keys
    private const string LAST_SCENE_KEY = "LastActiveScene";
    private const string BACKGROUND_TIME_KEY = "BackgroundTime";
    private const string WAS_IN_LEVEL_KEY = "WasInLevel";

    public static AppLifecycleManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLifecycle();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CheckForUnexpectedQuit();
    }

    #region Unity Lifecycle Events

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            OnAppGoingToBackground();
        }
        else
        {
            OnAppReturningFromBackground();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            OnAppGoingToBackground();
        }
        else
        {
            OnAppReturningFromBackground();
        }
    }

    void OnApplicationQuit()
    {
        HandleAppQuit();
    }

    #endregion

    #region Lifecycle Management

    void InitializeLifecycle()
    {
        Debug.Log("=== APP LIFECYCLE MANAGER INITIALIZATION ===");

        lastSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        wasInLevel = IsInLevel(lastSceneName);

        Debug.Log($"App started in scene: {lastSceneName}");
        Debug.Log($"Is in level: {wasInLevel}");
    }

    void OnAppGoingToBackground()
    {
        backgroundTime = DateTime.Now;
        lastSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        wasInLevel = IsInLevel(lastSceneName);

        SaveLifecycleState();

        if (wasInLevel && enablePauseOnBackground)
        {
            PauseGame();
        }

        OnAppBackgrounded?.Invoke();

        Debug.Log($"Background time saved: {backgroundTime}");
        Debug.Log($"Was in level: {wasInLevel}");
    }

    void OnAppReturningFromBackground()
    {
        Debug.Log("App returning from background");

        DateTime returnTime = DateTime.Now;
        TimeSpan timeAway = returnTime - backgroundTime;

        Debug.Log($"Time away: {timeAway.TotalSeconds:F1} seconds");

        if (timeAway.TotalSeconds > quitDetectionTimeSeconds && wasInLevel && enableLifeLossOnQuit)
        {
            HandleLifeLossOnReturn();
        }
        else
        {
            Debug.Log("Short absence - resuming normally");
        }

        if (isGamePaused)
        {
            ResumeGame();
        }

        ClearLifecycleState();

        OnAppForegrounded?.Invoke();

        if (LivesManager.Instance != null)
        {
            // LivesManager will handle regeneration automatically
        }
    }

    void CheckForUnexpectedQuit()
    {
        Debug.Log("Checking for unexpected quit...");

        if (PlayerPrefs.HasKey(BACKGROUND_TIME_KEY))
        {
            string backgroundTimeStr = PlayerPrefs.GetString(BACKGROUND_TIME_KEY);
            bool wasInLevelSaved = PlayerPrefs.GetInt(WAS_IN_LEVEL_KEY, 0) == 1;
            string lastScene = PlayerPrefs.GetString(LAST_SCENE_KEY, "");

            if (long.TryParse(backgroundTimeStr, out long backgroundTimeBinary))
            {
                DateTime savedBackgroundTime = DateTime.FromBinary(backgroundTimeBinary);
                TimeSpan timeSinceBackground = DateTime.Now - savedBackgroundTime;

                Debug.Log($"Found saved state: Scene={lastScene}, InLevel={wasInLevelSaved}, TimeAway={timeSinceBackground.TotalSeconds:F1}s");

                if (timeSinceBackground.TotalSeconds > quitDetectionTimeSeconds && wasInLevelSaved && enableLifeLossOnQuit)
                {
                    HandleLifeLossOnAppStart();
                }
                else
                {
                    Debug.Log("No life loss needed");
                }
            }

            ClearLifecycleState();
        }
        else
        {
            Debug.Log("No saved lifecycle state found - clean app start");
        }
    }

    #endregion

    #region Pause/Resume System

    public void PauseGame()
    {
        if (isGamePaused) return;

        isGamePaused = true;

        // Pause game time
        Time.timeScale = 0f;

        // Pause audio
        if (AudioManager.Instance != null)
        {
            AudioListener.pause = true;
        }

        // Pause timer
        if (TimerUI.Instance != null)
        {
            TimerUI.Instance.PauseTimer();
        }

        OnGamePaused?.Invoke();
    }

    public void ResumeGame()
    {
        if (!isGamePaused) return;

        isGamePaused = false;

        // Resume game time
        Time.timeScale = 1f;

        // Resume audio
        if (AudioManager.Instance != null)
        {
            AudioListener.pause = false;
        }

        // Resume timer
        if (TimerUI.Instance != null)
        {
            TimerUI.Instance.ResumeTimer();
        }

        OnGameResumed?.Invoke();
    }

    public void TogglePause()
    {
        if (isGamePaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    #endregion

    #region Life Loss Handling

    void HandleAppQuit()
    {

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool inLevel = IsInLevel(currentScene);

        if (inLevel && enableLifeLossOnQuit)
        {
            LoseLifeForQuit();
        }

        OnGameQuit?.Invoke();
    }

    void HandleLifeLossOnReturn()
    {
        LoseLifeForQuit();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseGame("You left the level!");
        }
    }

    void HandleLifeLossOnAppStart()
    {
        LoseLifeForQuit();

        // Return to main menu
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadMainMenu();
        }
    }

    void LoseLifeForQuit()
    {
        if (LivesManager.Instance != null)
        {
            bool lifeLost = LivesManager.Instance.LoseLife();
        }
    }

    #endregion

    #region Utility Methods

    bool IsInLevel(string sceneName)
    {
        // Check if scene is a level scene
        if (string.IsNullOrEmpty(sceneName)) return false;

        // Level scenes start with "Level_"
        return sceneName.StartsWith("Level_");
    }

    void SaveLifecycleState()
    {
        PlayerPrefs.SetString(BACKGROUND_TIME_KEY, backgroundTime.ToBinary().ToString());
        PlayerPrefs.SetString(LAST_SCENE_KEY, lastSceneName);
        PlayerPrefs.SetInt(WAS_IN_LEVEL_KEY, wasInLevel ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ClearLifecycleState()
    {
        PlayerPrefs.DeleteKey(BACKGROUND_TIME_KEY);
        PlayerPrefs.DeleteKey(LAST_SCENE_KEY);
        PlayerPrefs.DeleteKey(WAS_IN_LEVEL_KEY);
        PlayerPrefs.Save();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get current pause state
    /// </summary>
    public bool IsGamePaused()
    {
        return isGamePaused;
    }

    /// <summary>
    /// Manual pause (for pause button)
    /// </summary>
    public void ManualPause()
    {
        PauseGame();
    }

    /// <summary>
    /// Manual resume (for resume button)
    /// </summary>
    public void ManualResume()
    {
        ResumeGame();
    }

    /// <summary>
    /// Force quit handling (for testing)
    /// </summary>
    public void SimulateAppQuit()
    {
        HandleAppQuit();
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Test Pause")]
    void TestPause()
    {
        PauseGame();
    }

    [ContextMenu("Test Resume")]
    void TestResume()
    {
        ResumeGame();
    }

    [ContextMenu("Test Toggle Pause")]
    void TestTogglePause()
    {
        TogglePause();
    }

    [ContextMenu("Simulate App Quit")]
    void TestAppQuit()
    {
        SimulateAppQuit();
    }

    [ContextMenu("Simulate Long Background")]
    void TestLongBackground()
    {
        backgroundTime = DateTime.Now.AddMinutes(-10);
        wasInLevel = true;
        SaveLifecycleState();

        Debug.Log("📱 Simulated long background - restart app to test");
    }

    [ContextMenu("Debug Lifecycle State")]
    void DebugLifecycleState()
    {
        Debug.Log("=== APP LIFECYCLE STATE ===");
        Debug.Log($"Is Paused: {isGamePaused}");
        Debug.Log($"Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"Is In Level: {IsInLevel(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)}");
        Debug.Log($"Enable Pause On Background: {enablePauseOnBackground}");
        Debug.Log($"Enable Life Loss On Quit: {enableLifeLossOnQuit}");
        Debug.Log($"Quit Detection Time: {quitDetectionTimeSeconds}s");
    }

    #endregion
}