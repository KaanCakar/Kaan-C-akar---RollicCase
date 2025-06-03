using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kaan Çakar 2025 - TimerUI.cs
/// Simplified timer display - only text and fill bar
/// </summary>
public class TimerUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public Image timerFillBar; // Progress bar

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
        // UI component'lerini otomatik bul
        if (timerText == null)
            timerText = GetComponentInChildren<TextMeshProUGUI>();
        
        // Başlangıçta GameManager'dan ayarları al ama başlatma
        InitializeFromGameManager();
    }
    
    /// <summary>
    /// GameManager'dan timer ayarlarını al ve göster
    /// </summary>
    void InitializeFromGameManager()
    {
        if (GameManager.Instance != null)
        {
            var levelData = GameManager.Instance.currentLevelData;
            if (levelData != null && levelData.timerEnabled)
            {
                // Timer'ı ayarla ama başlatma
                totalTime = levelData.levelTimeInSeconds;
                currentTime = levelData.levelTimeInSeconds;
                displayFormat = levelData.timerFormat;
                isActive = false; // ✅ Henüz başlatma
                
                SetVisible(true);
                UpdateDisplay();
                
                Debug.Log($"🕐 Timer initialized: {totalTime}s (waiting for first click)");
            }
            else
            {
                // Timer disabled - gizle
                SetVisible(false);
                Debug.Log("🚫 Timer disabled - UI hidden");
            }
        }
        else
        {
            // GameManager yok - default görünümü göster
            totalTime = 60f;
            currentTime = 60f;
            displayFormat = TimerDisplayFormat.MinutesSeconds;
            isActive = false; // ✅ Başlatma
            
            SetVisible(true);
            UpdateDisplay();
            
            Debug.Log("⚠️ GameManager not found - showing default timer");
        }
    }
    
    void Update()
    {
        // ✅ Sadece aktifse countdown yap
        if (isActive && currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            
            // Timer bitti mi kontrol et
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
    /// Timer'ı başlat (ilk person tıklamasında çağrılacak)
    /// </summary>
    public void StartTimer()
    {
        if (!isActive) // Sadece ilk kez başlatılacak
        {
            isActive = true;
            Debug.Log($"🕐 Timer STARTED: {currentTime}s");
        }
    }
    
    /// <summary>
    /// Timer'ı güncelle (Tool'dan çağrılacak)
    /// </summary>
    public void UpdateTimerSettings(float timeInSeconds, TimerDisplayFormat format, bool enabled)
    {
        if (enabled)
        {
            totalTime = timeInSeconds;
            currentTime = timeInSeconds;
            displayFormat = format;
            isActive = false; // ✅ Reset - sadece güncelle, başlatma
            
            SetVisible(true);
            UpdateDisplay();
            
            Debug.Log($"🔄 Timer settings updated: {timeInSeconds}s, Format: {format}");
        }
        else
        {
            SetVisible(false);
            Debug.Log("🚫 Timer disabled");
        }
    }
    
    /// <summary>
    /// Timer'ı durdur
    /// </summary>
    public void StopTimer()
    {
        isActive = false;
        Debug.Log("🛑 Timer stopped");
    }
    
    /// <summary>
    /// Timer'ı pause et
    /// </summary>
    public void PauseTimer()
    {
        isActive = false;
        Debug.Log("⏸️ Timer paused");
    }
    
    /// <summary>
    /// Timer'ı resume et
    /// </summary>
    public void ResumeTimer()
    {
        isActive = true;
        Debug.Log("▶️ Timer resumed");
    }
    
    /// <summary>
    /// Timer'ı gizle/göster
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// Kalan süreyi al
    /// </summary>
    public float GetRemainingTime()
    {
        return currentTime;
    }
    
    /// <summary>
    /// Timer aktif mi?
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
    
    #endregion
    
    #region Display Updates
    
    void UpdateDisplay()
    {
        // Text güncelle
        if (timerText != null)
        {
            string timeString = FormatTime(currentTime, displayFormat);
            timerText.text = timeString;
        }
        
        // ✅ Fill bar güncelle (sadece bu kalacak)
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
        
        Debug.Log("⏰ Timer FINISHED!");
        
        // GameManager'a timer bittiğini bildir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseGame("Time's up!");
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