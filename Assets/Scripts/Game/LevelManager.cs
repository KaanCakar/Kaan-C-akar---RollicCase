using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Kaan Çakar 2025 - LevelManager.cs
/// Level progression and scene management system
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level Settings")]
    public int totalLevels = 7;
    public string levelScenePrefix = "Level_";
    public string mainMenuSceneName = "MainMenu";
    
    [Header("Current Level")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int highestUnlockedLevel = 1;
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent<int> OnLevelChanged;
    public UnityEngine.Events.UnityEvent<int> OnLevelCompleted;
    public UnityEngine.Events.UnityEvent<int> OnLevelUnlocked;
    
    // PlayerPrefs Keys
    private const string CURRENT_LEVEL_KEY = "CurrentLevel";
    private const string HIGHEST_UNLOCKED_LEVEL_KEY = "HighestUnlockedLevel";
    private const string LEVEL_COMPLETED_KEY = "LevelCompleted_";
    
    public static LevelManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgressData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        DetectCurrentLevelFromScene();
    }
    
    #region Save/Load Progress
    
    void LoadProgressData()
    {
        currentLevel = PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 1);
        highestUnlockedLevel = PlayerPrefs.GetInt(HIGHEST_UNLOCKED_LEVEL_KEY, 1);
        
        Debug.Log($"Progress loaded: Current={currentLevel}, Highest={highestUnlockedLevel}");
    }
    
    void SaveProgressData()
    {
        PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, currentLevel);
        PlayerPrefs.SetInt(HIGHEST_UNLOCKED_LEVEL_KEY, highestUnlockedLevel);
        PlayerPrefs.Save();
        
        Debug.Log($"Progress saved: Current={currentLevel}, Highest={highestUnlockedLevel}");
    }
    
    #endregion
    
    #region Level Detection
    
    void DetectCurrentLevelFromScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        
        if (sceneName.StartsWith(levelScenePrefix))
        {
            // "Level_01" -> extract "01" -> convert to 1
            string levelNumberStr = sceneName.Substring(levelScenePrefix.Length);
            
            if (int.TryParse(levelNumberStr, out int detectedLevel))
            {
                currentLevel = detectedLevel;
                Debug.Log($"Detected current level from scene: {currentLevel}");
                
                OnLevelChanged?.Invoke(currentLevel);
            }
        }
        else if (sceneName == mainMenuSceneName)
        {
            Debug.Log($"In main menu scene");
        }
        else
        {
            Debug.Log($"Unknown scene: {sceneName}");
        }
    }
    
    #endregion
    
    #region Level Completion & Progression
    
    /// <summary>
    /// Current level completed - unlock next level
    /// </summary>
    public void CompleteCurrentLevel()
    {
        Debug.Log($"Level {currentLevel} completed!");
        
        // Mark level as completed
        SetLevelCompleted(currentLevel, true);
        
        // Unlock next level
        if (currentLevel >= highestUnlockedLevel && currentLevel < totalLevels)
        {
            highestUnlockedLevel = currentLevel + 1;
            OnLevelUnlocked?.Invoke(highestUnlockedLevel);
            Debug.Log($"Level {highestUnlockedLevel} unlocked!");
        }
        
        // Update current level to next
        if (currentLevel < totalLevels)
        {
            currentLevel++;
            OnLevelChanged?.Invoke(currentLevel);
        }
        else
        {
            Debug.Log("All levels completed!");
        }
        
        SaveProgressData();
        OnLevelCompleted?.Invoke(currentLevel - 1); // Previous level completed
    }
    
    /// <summary>
    /// Load specific level (for menu selection)
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        if (!IsLevelUnlocked(levelNumber))
        {
            Debug.LogWarning($"Level {levelNumber} is not unlocked!");
            return;
        }
        
        if (IsLevelCompleted(levelNumber))
        {
            Debug.LogWarning($"Level {levelNumber} already completed!");
            return;
        }
        
        currentLevel = levelNumber;
        
        string sceneName = GetLevelSceneName(levelNumber);
        Debug.Log($"🎮 Loading level {levelNumber}: {sceneName}");
        
        OnLevelChanged?.Invoke(currentLevel);
        SceneManager.LoadScene(sceneName);
    }
    
    /// <summary>
    /// Load next level in sequence
    /// </summary>
    public void LoadNextLevel()
    {
        if (currentLevel < totalLevels)
        {
            LoadLevel(currentLevel + 1);
        }
        else
        {
            LoadMainMenu();
        }
    }
    
    /// <summary>
    /// Restart current level
    /// </summary>
    public void RestartCurrentLevel()
    {
        Debug.Log($"Restarting level {currentLevel}");
        
        string sceneName = GetLevelSceneName(currentLevel);
        SceneManager.LoadScene(sceneName);
    }
    
    /// <summary>
    /// Load main menu
    /// </summary>
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    #endregion
    
    #region Level Status Queries
    
    /// <summary>
    /// Check if level is unlocked
    /// </summary>
    public bool IsLevelUnlocked(int levelNumber)
    {
        return levelNumber <= highestUnlockedLevel && levelNumber >= 1 && levelNumber <= totalLevels;
    }
    
    /// <summary>
    /// Check if level is completed
    /// </summary>
    public bool IsLevelCompleted(int levelNumber)
    {
        return PlayerPrefs.GetInt(LEVEL_COMPLETED_KEY + levelNumber, 0) == 1;
    }
    
    /// <summary>
    /// Set level completion status
    /// </summary>
    public void SetLevelCompleted(int levelNumber, bool completed)
    {
        PlayerPrefs.SetInt(LEVEL_COMPLETED_KEY + levelNumber, completed ? 1 : 0);
        PlayerPrefs.Save();
        
        Debug.Log($"Level {levelNumber} marked as {(completed ? "completed" : "not completed")}");
    }
    
    /// <summary>
    /// Get level scene name
    /// </summary>
    public string GetLevelSceneName(int levelNumber)
    {
        return $"{levelScenePrefix}{levelNumber:00}";
    }
    
    /// <summary>
    /// Get current level number
    /// </summary>
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
    
    /// <summary>
    /// Get highest unlocked level
    /// </summary>
    public int GetHighestUnlockedLevel()
    {
        return highestUnlockedLevel;
    }
    
    /// <summary>
    /// Get total levels count
    /// </summary>
    public int GetTotalLevels()
    {
        return totalLevels;
    }
    
    /// <summary>
    /// Get unlocked levels list
    /// </summary>
    public List<int> GetUnlockedLevels()
    {
        List<int> unlockedLevels = new List<int>();
        
        for (int i = 1; i <= highestUnlockedLevel && i <= totalLevels; i++)
        {
            unlockedLevels.Add(i);
        }
        
        return unlockedLevels;
    }
    
    /// <summary>
    /// Get completed levels list
    /// </summary>
    public List<int> GetCompletedLevels()
    {
        List<int> completedLevels = new List<int>();
        
        for (int i = 1; i <= totalLevels; i++)
        {
            if (IsLevelCompleted(i))
            {
                completedLevels.Add(i);
            }
        }
        
        return completedLevels;
    }
    
    #endregion
    
    #region GameManager Integration
    
    /// <summary>
    /// Called by GameManager when level is won
    /// </summary>
    public void OnGameManagerLevelWon()
    {
        CompleteCurrentLevel();
    }
    
    /// <summary>
    /// Called by GameManager when level is lost
    /// </summary>
    public void OnGameManagerLevelLost()
    {
        // Level lost - no progression, player can retry
    }
    
    #endregion
    
    #region Debug & Admin
    
    /// <summary>
    /// Reset all progress (debug)
    /// </summary>
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        for (int i = 1; i <= totalLevels; i++)
        {
            PlayerPrefs.DeleteKey(LEVEL_COMPLETED_KEY + i);
        }
        
        PlayerPrefs.DeleteKey(CURRENT_LEVEL_KEY);
        PlayerPrefs.DeleteKey(HIGHEST_UNLOCKED_LEVEL_KEY);
        PlayerPrefs.Save();
        
        currentLevel = 1;
        highestUnlockedLevel = 1;
        
        Debug.Log("🗑️ All progress reset!");
    }
    
    /// <summary>
    /// Unlock all levels (debug)
    /// </summary>
    [ContextMenu("Unlock All Levels")]
    public void UnlockAllLevels()
    {
        highestUnlockedLevel = totalLevels;
        SaveProgressData();
        
        Debug.Log($"All {totalLevels} levels unlocked!");
    }
    
    /// <summary>
    /// Complete all levels (debug)
    /// </summary>
    [ContextMenu("Complete All Levels")]
    public void CompleteAllLevels()
    {
        for (int i = 1; i <= totalLevels; i++)
        {
            SetLevelCompleted(i, true);
        }
        
        highestUnlockedLevel = totalLevels;
        SaveProgressData();
        
        Debug.Log($"All {totalLevels} levels completed!");
    }
    
    /// <summary>
    /// Debug current status
    /// </summary>
    [ContextMenu("Debug Level Status")]
    public void DebugLevelStatus()
    {
        Debug.Log("=== LEVEL MANAGER STATUS ===");
        Debug.Log($"Current Level: {currentLevel}");
        Debug.Log($"Highest Unlocked: {highestUnlockedLevel}");
        Debug.Log($"Total Levels: {totalLevels}");
        
        Debug.Log("Unlocked Levels:");
        var unlockedLevels = GetUnlockedLevels();
        foreach (int level in unlockedLevels)
        {
            bool completed = IsLevelCompleted(level);
            Debug.Log($"  Level {level}: {(completed ? "Completed" : "Unlocked")}");
        }
        
        Debug.Log("=== END STATUS ===");
    }
    
    #endregion
}