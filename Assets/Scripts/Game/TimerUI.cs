using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kaan Çakar 2025 - TimerUI.cs
/// Timer UI for displaying countdown, fill bar, and handling game timer logic.
/// </summary>
public class TimerUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public Image timerFillBar;

    // Internal state
    private float currentTime = 0f;
    private float totalTime = 0f;
    private bool isActive = false;
    private TimerDisplayFormat displayFormat = TimerDisplayFormat.MinutesSeconds;

    public static TimerUI Instance { get; private set; }

    // Events
    public System.Action OnTimerFinished;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (timerText == null)
            timerText = GetComponentInChildren<TextMeshProUGUI>();

        InitializeFromGameManager();
    }

    /// <summary>
    /// Initialize timer from GameManager settings
    /// </summary>
    void InitializeFromGameManager()
    {
        if (GameManager.Instance != null)
        {
            var levelData = GameManager.Instance.currentLevelData;
            if (levelData != null && levelData.timerEnabled)
            {
                totalTime = levelData.levelTimeInSeconds;
                currentTime = levelData.levelTimeInSeconds;
                displayFormat = levelData.timerFormat;
                isActive = false;

                SetVisible(true);
                UpdateDisplay();

                Debug.Log($"Timer initialized: {totalTime}s (waiting for first click)");
            }
            else
            {
                SetVisible(false);
            }
        }
        else
        {
            totalTime = 60f;
            currentTime = 60f;
            displayFormat = TimerDisplayFormat.MinutesSeconds;
            isActive = false;

            SetVisible(true);
            UpdateDisplay();
        }
    }

    void Update()
    {
        if (isActive && currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                FinishTimer();
            }

            UpdateDisplay();
        }
    }

    #region Public API

    /// <summary>
    /// Start the timer (only once)
    /// </summary>
    public void StartTimer()
    {
        if (!isActive)
        {
            isActive = true;
            Debug.Log($"Timer STARTED: {currentTime}s");
        }
    }

    /// <summary>
    /// Update timer settings (time, format, enabled state)
    /// </summary>
    public void UpdateTimerSettings(float timeInSeconds, TimerDisplayFormat format, bool enabled)
    {
        if (enabled)
        {
            totalTime = timeInSeconds;
            currentTime = timeInSeconds;
            displayFormat = format;
            isActive = false;

            SetVisible(true);
            UpdateDisplay();

            Debug.Log($"Timer settings updated: {timeInSeconds}s, Format: {format}");
        }
        else
        {
            SetVisible(false);
        }
    }

    /// <summary>
    /// Stop the timer
    /// </summary>
    public void StopTimer()
    {
        isActive = false;
        Debug.Log("Timer stopped");
    }

    /// <summary>
    /// Pause the timer
    /// </summary>
    public void PauseTimer()
    {
        isActive = false;
    }

    /// <summary>
    /// Timer'ı resume et
    /// </summary>
    public void ResumeTimer()
    {
        isActive = true;
    }

    /// <summary>
    /// Set timer visibility
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>
    /// Get the remaining time in seconds
    /// </summary>
    public float GetRemainingTime()
    {
        return currentTime;
    }

    public bool IsActive()
    {
        return isActive;
    }

    #endregion

    #region Display Updates

    void UpdateDisplay()
    {
        if (timerText != null)
        {
            string timeString = FormatTime(currentTime, displayFormat);
            timerText.text = timeString;
        }

        if (timerFillBar != null)
        {
            float fillAmount = totalTime > 0 ? currentTime / totalTime : 0f;
            timerFillBar.fillAmount = fillAmount;
        }
    }

    string FormatTime(float timeInSeconds, TimerDisplayFormat format)
    {
        int totalSeconds = Mathf.CeilToInt(timeInSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        int milliseconds = Mathf.FloorToInt((timeInSeconds - Mathf.FloorToInt(timeInSeconds)) * 100);

        switch (format)
        {
            case TimerDisplayFormat.SecondsOnly:
                return totalSeconds.ToString();

            case TimerDisplayFormat.MinutesSeconds:
                return $"{minutes}:{seconds:00}";

            case TimerDisplayFormat.MinutesSecondsMs:
                return $"{minutes}:{seconds:00}:{milliseconds:00}";

            default:
                return $"{minutes}:{seconds:00}";
        }
    }

    #endregion

    void FinishTimer()
    {
        isActive = false;
        OnTimerFinished?.Invoke();

        if (GameManager.Instance != null && !GameManager.Instance.isWinTriggered)
        {
            GameManager.Instance.LoseGame("Time's up!");
        }
        else
        {
            Debug.Log("Timer finished but win already triggered - no lose");
        }
    }

    #region Debug

    [ContextMenu("Test Timer 10s")]
    void TestTimer10()
    {
        totalTime = 10f;
        currentTime = 10f;
        displayFormat = TimerDisplayFormat.MinutesSeconds;
        isActive = false;
        UpdateDisplay();
        Debug.Log("Timer set to 10s - click StartTimer() to begin");
    }

    [ContextMenu("Start Timer Now")]
    void StartTimerNow()
    {
        StartTimer();
    }

    #endregion
}