using UnityEngine;
using System;

/// <summary>
/// Kaan Çakar 2025 - LivesManager.cs
/// Classic mobile game lives system with 30-minute regeneration
/// </summary>
public class LivesManager : MonoBehaviour
{
    [Header("Lives Settings")]
    public int maxLives = 5;
    public float lifeRegenTimeMinutes = 30f;
    
    [Header("Current Status")]
    [SerializeField] private int currentLives;
    [SerializeField] private DateTime lastLifeLostTime;
    [SerializeField] private DateTime nextLifeRegenTime;
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent<int> OnLivesChanged;
    public UnityEngine.Events.UnityEvent OnLifeLost;
    public UnityEngine.Events.UnityEvent OnLifeRegained;
    public UnityEngine.Events.UnityEvent OnLivesFull;
    
    // PlayerPrefs Keys
    private const string CURRENT_LIVES_KEY = "CurrentLives";
    private const string LAST_LIFE_LOST_KEY = "LastLifeLostTime";
    private const string NEXT_LIFE_REGEN_KEY = "NextLifeRegenTime";
    private const string FIRST_LAUNCH_KEY = "FirstLaunch";
    
    public static LivesManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLives();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        CheckLifeRegeneration();
        InvokeRepeating(nameof(CheckLifeRegeneration), 1f, 1f);
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            CheckLifeRegeneration();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            CheckLifeRegeneration();
        }
    }
    
    #region Initialization
    
    void InitializeLives()
    {
        if (!PlayerPrefs.HasKey(FIRST_LAUNCH_KEY))
        {
            currentLives = maxLives;
            lastLifeLostTime = DateTime.MinValue;
            nextLifeRegenTime = DateTime.MinValue;
            
            PlayerPrefs.SetInt(FIRST_LAUNCH_KEY, 1);
            SaveLivesData();
        }
        else
        {
            LoadLivesData();
        }
    }
    
    #endregion
    
    #region Save/Load System
    
    void SaveLivesData()
    {
        PlayerPrefs.SetInt(CURRENT_LIVES_KEY, currentLives);
        PlayerPrefs.SetString(LAST_LIFE_LOST_KEY, lastLifeLostTime.ToBinary().ToString());
        PlayerPrefs.SetString(NEXT_LIFE_REGEN_KEY, nextLifeRegenTime.ToBinary().ToString());
        PlayerPrefs.Save();
    }
    
    void LoadLivesData()
    {
        currentLives = PlayerPrefs.GetInt(CURRENT_LIVES_KEY, maxLives);
        
        string lastLifeLostStr = PlayerPrefs.GetString(LAST_LIFE_LOST_KEY, "");
        if (!string.IsNullOrEmpty(lastLifeLostStr) && long.TryParse(lastLifeLostStr, out long lastLifeBinary))
        {
            lastLifeLostTime = DateTime.FromBinary(lastLifeBinary);
        }
        else
        {
            lastLifeLostTime = DateTime.MinValue;
        }
        
        string nextLifeRegenStr = PlayerPrefs.GetString(NEXT_LIFE_REGEN_KEY, "");
        if (!string.IsNullOrEmpty(nextLifeRegenStr) && long.TryParse(nextLifeRegenStr, out long nextLifeBinary))
        {
            nextLifeRegenTime = DateTime.FromBinary(nextLifeBinary);
        }
        else
        {
            nextLifeRegenTime = DateTime.MinValue;
        }
    }
    
    #endregion
    
    #region Life Management
    
    /// <summary>
    /// Life loss when level is failed
    /// </summary>
    public bool LoseLife()
    {
        if (currentLives <= 0)
        {
            return false;
        }
        
        currentLives--;
        lastLifeLostTime = DateTime.Now;
        
        if (currentLives < maxLives)
        {
            nextLifeRegenTime = DateTime.Now.AddMinutes(lifeRegenTimeMinutes);
        }
        
        SaveLivesData();
        
        OnLifeLost?.Invoke();
        OnLivesChanged?.Invoke(currentLives);
        
        return true;
    }
    
    /// <summary>
    /// Life gain through regeneration or purchase
    /// </summary>
    public bool GainLife()
    {
        if (currentLives >= maxLives)
        {
            return false;
        }
        
        currentLives++;
        
        if (currentLives < maxLives)
        {
            nextLifeRegenTime = DateTime.Now.AddMinutes(lifeRegenTimeMinutes);
        }
        else
        {
            nextLifeRegenTime = DateTime.MinValue;
            OnLivesFull?.Invoke();
        }
        
        SaveLivesData();
        
        OnLifeRegained?.Invoke();
        OnLivesChanged?.Invoke(currentLives);
        
        return true;
    }
    
    /// <summary>
    /// Check and process life regeneration
    /// </summary>
    void CheckLifeRegeneration()
    {
        if (currentLives >= maxLives) return;
        if (nextLifeRegenTime == DateTime.MinValue) return;
        
        DateTime now = DateTime.Now;
        
        if (now >= nextLifeRegenTime)
        {
            int livesToAdd = 1;
            
            TimeSpan timePassed = now - nextLifeRegenTime;
            if (timePassed.TotalMinutes > lifeRegenTimeMinutes)
            {
                int extraLives = Mathf.FloorToInt((float)(timePassed.TotalMinutes / lifeRegenTimeMinutes));
                livesToAdd += extraLives;
            }
            
            livesToAdd = Mathf.Min(livesToAdd, maxLives - currentLives);
            
            for (int i = 0; i < livesToAdd; i++)
            {
                GainLife();
            }
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Get current lives count
    /// </summary>
    public int GetCurrentLives()
    {
        return currentLives;
    }
    
    /// <summary>
    /// Get maximum lives count
    /// </summary>
    public int GetMaxLives()
    {
        return maxLives;
    }
    
    /// <summary>
    /// Get time remaining to next life in seconds
    /// </summary>
    public float GetTimeToNextLife()
    {
        if (currentLives >= maxLives) return 0f;
        if (nextLifeRegenTime == DateTime.MinValue) return 0f;
        
        TimeSpan timeRemaining = nextLifeRegenTime - DateTime.Now;
        return Mathf.Max(0f, (float)timeRemaining.TotalSeconds);
    }
    
    /// <summary>
    /// Get formatted time remaining for UI display
    /// </summary>
    public string GetTimeToNextLifeFormatted()
    {
        float timeToNext = GetTimeToNextLife();
        
        if (timeToNext <= 0) return "Ready!";
        
        int hours = Mathf.FloorToInt(timeToNext / 3600);
        int minutes = Mathf.FloorToInt((timeToNext % 3600) / 60);
        int seconds = Mathf.FloorToInt(timeToNext % 60);
        
        if (hours > 0)
            return $"{hours:00}h {minutes:00}m {seconds:00}s";
        else
            return $"{minutes:00}m {seconds:00}s";
    }
    
    /// <summary>
    /// Check if lives are at maximum
    /// </summary>
    public bool IsLivesFull()
    {
        return currentLives >= maxLives;
    }
    
    /// <summary>
    /// Check if no lives remaining
    /// </summary>
    public bool IsLivesEmpty()
    {
        return currentLives <= 0;
    }
    
    /// <summary>
    /// Check if player can start a level
    /// </summary>
    public bool CanPlayLevel()
    {
        return currentLives > 0;
    }
    
    #endregion
    
    #region Admin/Debug Methods
    
    /// <summary>
    /// Reset lives to full (debug)
    /// </summary>
    [ContextMenu("Reset Lives to Full")]
    public void ResetLivesToFull()
    {
        currentLives = maxLives;
        nextLifeRegenTime = DateTime.MinValue;
        SaveLivesData();
        OnLivesChanged?.Invoke(currentLives);
    }
    
    /// <summary>
    /// Set lives to zero (debug)
    /// </summary>
    [ContextMenu("Set Lives to Zero")]
    public void SetLivesToZero()
    {
        currentLives = 0;
        lastLifeLostTime = DateTime.Now;
        nextLifeRegenTime = DateTime.Now.AddMinutes(lifeRegenTimeMinutes);
        SaveLivesData();
        OnLivesChanged?.Invoke(currentLives);
    }
    
    /// <summary>
    /// Add one life (debug)
    /// </summary>
    [ContextMenu("Add One Life")]
    public void AddOneLife()
    {
        GainLife();
    }
    
    /// <summary>
    /// Remove one life (debug)
    /// </summary>
    [ContextMenu("Remove One Life")]
    public void RemoveOneLife()
    {
        LoseLife();
    }
    
    /// <summary>
    /// Fast regeneration for testing (debug)
    /// </summary>
    [ContextMenu("Fast Regen (10 seconds)")]
    public void FastRegen()
    {
        if (currentLives < maxLives)
        {
            nextLifeRegenTime = DateTime.Now.AddSeconds(10);
            SaveLivesData();
        }
    }
    
    /// <summary>
    /// Clear all lives data and reset (debug)
    /// </summary>
    [ContextMenu("Clear All Lives Data")]
    public void ClearAllLivesData()
    {
        PlayerPrefs.DeleteKey(CURRENT_LIVES_KEY);
        PlayerPrefs.DeleteKey(LAST_LIFE_LOST_KEY);
        PlayerPrefs.DeleteKey(NEXT_LIFE_REGEN_KEY);
        PlayerPrefs.DeleteKey(FIRST_LAUNCH_KEY);
        PlayerPrefs.Save();
        
        InitializeLives();
    }
    
    /// <summary>
    /// Debug lives status (debug)
    /// </summary>
    [ContextMenu("Debug Lives Status")]
    public void DebugLivesStatus()
    {
        Debug.Log("=== LIVES STATUS DEBUG ===");
        Debug.Log($"Current Lives: {currentLives}/{maxLives}");
        Debug.Log($"Last Life Lost: {lastLifeLostTime}");
        Debug.Log($"Next Regen: {nextLifeRegenTime}");
        Debug.Log($"Time to Next: {GetTimeToNextLifeFormatted()}");
        Debug.Log($"Can Play: {CanPlayLevel()}");
        Debug.Log($"Is Full: {IsLivesFull()}");
        Debug.Log($"Is Empty: {IsLivesEmpty()}");
    }
    
    #endregion
}