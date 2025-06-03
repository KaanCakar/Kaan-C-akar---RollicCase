using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kaan Çakar 2025 - MainMenuUI.cs
/// Main menu interface with level button and lives display
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public Button levelButton;
    public TextMeshProUGUI levelButtonText;

    [Header("Lives Display (Manual)")]
    public TextMeshProUGUI livesCountText;
    public TextMeshProUGUI livesTimerText;
    public Image heartIcon;

    [Header("Level Button Settings")]
    public string levelButtonFormat = "Level {0}";
    public Color unlockedButtonColor = Color.white;
    public Color lockedButtonColor = Color.gray;

    [Header("Optional Elements")]
    public GameObject settingsButton;
    public GameObject creditsButton;

    void Start()
    {
        // Initialize UI
        SetupMainMenuUI();

        // Subscribe to events
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelChanged.AddListener(OnLevelChanged);
            LevelManager.Instance.OnLevelCompleted.AddListener(OnLevelCompleted);
        }

        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.OnLivesChanged.AddListener(OnLivesChanged);
        }

        // Setup button listener
        if (levelButton != null)
        {
            levelButton.onClick.AddListener(OnLevelButtonClicked);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelChanged.RemoveListener(OnLevelChanged);
            LevelManager.Instance.OnLevelCompleted.RemoveListener(OnLevelCompleted);
        }

        if (LivesManager.Instance != null)
        {
            LivesManager.Instance.OnLivesChanged.RemoveListener(OnLivesChanged);
        }
    }

    void SetupMainMenuUI()
    {
        // Update level button
        UpdateLevelButton();

        // Update lives display - metod adını değiştirin
        RefreshLivesDisplay();
    }

    #region Level Button

    void UpdateLevelButton()
    {
        if (LevelManager.Instance == null || levelButton == null) return;

        // Get next level to play
        int nextLevel = GetNextLevelToPlay();

        // Update button text
        if (levelButtonText != null)
        {
            levelButtonText.text = string.Format(levelButtonFormat, nextLevel);
        }

        // Update button state
        bool canPlay = CanPlayLevel(nextLevel);
        levelButton.interactable = canPlay;

        // Update button color
        var buttonImage = levelButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = canPlay ? unlockedButtonColor : lockedButtonColor;
        }

        Debug.Log($"Level button updated: Level {nextLevel} (Playable: {canPlay})");
    }

    void RefreshLivesDisplay()
    {
        if (LivesManager.Instance == null) return;

        if (livesCountText != null)
        {
            int currentLives = LivesManager.Instance.GetCurrentLives();
            livesCountText.text = currentLives.ToString();
        }

        if (livesTimerText != null)
        {
            if (LivesManager.Instance.IsLivesFull())
            {
                livesTimerText.text = "FULL";
            }
            else
            {
                string timeLeft = LivesManager.Instance.GetTimeToNextLifeFormatted();
                livesTimerText.text = timeLeft;
            }
        }
    }

    int GetNextLevelToPlay()
    {
        if (LevelManager.Instance == null) return 1;

        // Find first incomplete level
        for (int i = 1; i <= LevelManager.Instance.GetTotalLevels(); i++)
        {
            if (LevelManager.Instance.IsLevelUnlocked(i) && !LevelManager.Instance.IsLevelCompleted(i))
            {
                return i;
            }
        }

        // All levels completed - return last level
        return LevelManager.Instance.GetTotalLevels();
    }

    bool CanPlayLevel(int levelNumber)
    {
        if (LevelManager.Instance == null) return false;
        if (LivesManager.Instance == null) return true;

        // Check if level is unlocked and player has lives
        bool levelUnlocked = LevelManager.Instance.IsLevelUnlocked(levelNumber);
        bool hasLives = LivesManager.Instance.CanPlayLevel();
        bool notCompleted = !LevelManager.Instance.IsLevelCompleted(levelNumber);

        return levelUnlocked && hasLives && notCompleted;
    }

    void OnLevelButtonClicked()
    {
        // Audio feedback
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        // Lives check
        if (LivesManager.Instance != null && !LivesManager.Instance.CanPlayLevel())
        {
            ShowNoLivesMessage();
            return;
        }

        // Get level to play
        int levelToPlay = GetNextLevelToPlay();

        // Additional checks
        if (LevelManager.Instance == null)
        {
            Debug.LogError("LevelManager not found!");
            return;
        }

        if (!LevelManager.Instance.IsLevelUnlocked(levelToPlay))
        {
            Debug.LogWarning($"Level {levelToPlay} is not unlocked!");
            return;
        }

        if (LevelManager.Instance.IsLevelCompleted(levelToPlay))
        {
            Debug.Log($"Level {levelToPlay} already completed - showing completion message");
            ShowLevelCompletedMessage(levelToPlay);
            return;
        }

        // Load level
        Debug.Log($"Loading level {levelToPlay}");
        LevelManager.Instance.LoadLevel(levelToPlay);
    }

    #endregion

    #region Event Handlers

    void OnLevelChanged(int newLevel)
    {
        UpdateLevelButton();
    }

    void OnLevelCompleted(int completedLevel)
    {
        UpdateLevelButton();
    }

    void OnLivesChanged(int newLivesCount)
    {
        UpdateLevelButton(); // Lives affect playability
        RefreshLivesDisplay();
    }
    #endregion

    #region Messages & Feedback

    void ShowNoLivesMessage()
    {
        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.ShowNoLivesPopup();
        }
        else
        {
            Debug.Log("No lives - please wait or buy lives");
        }
    }

    void ShowLevelCompletedMessage(int levelNumber)
    {
        Debug.Log($"Level {levelNumber} already completed!");
    }

    #endregion

    #region Optional Buttons

    public void OnSettingsButtonClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }

    public void OnCreditsButtonClicked()
    {

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Refresh entire UI (external call)
    /// </summary>
    public void RefreshUI()
    {
        UpdateLevelButton();
        RefreshLivesDisplay();
    }

    /// <summary>
    /// Force show specific level on button
    /// </summary>
    public void SetLevelButtonText(int levelNumber)
    {
        if (levelButtonText != null)
        {
            levelButtonText.text = string.Format(levelButtonFormat, levelNumber);
        }
    }

    #endregion

    #region Debug

    [ContextMenu("Test Level Button")]
    void TestLevelButton()
    {
        OnLevelButtonClicked();
    }

    [ContextMenu("Refresh UI")]
    void TestRefreshUI()
    {
        RefreshUI();
    }

    [ContextMenu("Debug Next Level")]
    void DebugNextLevel()
    {
        int nextLevel = GetNextLevelToPlay();
        bool canPlay = CanPlayLevel(nextLevel);

        Debug.Log($"=== NEXT LEVEL DEBUG ===");
        Debug.Log($"Next Level: {nextLevel}");
        Debug.Log($"Can Play: {canPlay}");
        Debug.Log($"Level Unlocked: {LevelManager.Instance?.IsLevelUnlocked(nextLevel)}");
        Debug.Log($"Level Completed: {LevelManager.Instance?.IsLevelCompleted(nextLevel)}");
        Debug.Log($"Has Lives: {LivesManager.Instance?.CanPlayLevel()}");
    }

    #endregion
}