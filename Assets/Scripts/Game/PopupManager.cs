using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Kaan Çakar 2025 - PopupManager.cs
/// Classic Unity UI popup system for Win/Lose scenarios
/// </summary>
public class PopupManager : MonoBehaviour
{
    [Header("Popup Canvases")]
    public GameObject winPopup;
    public GameObject losePopup;
    public GameObject noLivesPopup;
    
    [Header("Win Popup Elements")]
    public TextMeshProUGUI winTitleText;
    public TextMeshProUGUI winMessageText;
    public Button winMainMenuButton;
    
    [Header("Lose Popup Elements")]
    public TextMeshProUGUI loseTitleText;
    public TextMeshProUGUI loseMessageText;
    public Button loseTryAgainButton;
    
    [Header("No Lives Popup Elements")]
    public TextMeshProUGUI noLivesTitleText;
    public TextMeshProUGUI waitTimerText;
    public Button buyLivesButton;
    public Button noLivesMainMenuButton;
    
    [Header("Background Overlay")]
    public GameObject popupOverlay;
    
    [Header("Animation Settings")]
    public float popupAnimationSpeed = 0.3f;
    public AnimationCurve popupScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    public static PopupManager Instance { get; private set; }
    
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
        HideAllPopups();
        
        SetupButtonListeners();
        
        if (GameManager.Instance != null)
        {
            // GameManager.Instance.OnLevelComplete.AddListener(ShowWinPopup);
            GameManager.Instance.OnGameLost.AddListener(ShowLosePopup);
        }
    }
    
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameLost.RemoveListener(ShowLosePopup);
        }
    }
    
    void SetupButtonListeners()
    {
        // Win popup buttons
        if (winMainMenuButton != null)
            winMainMenuButton.onClick.AddListener(OnMainMenuClicked);
            
        // Lose popup buttons
        if (loseTryAgainButton != null)
            loseTryAgainButton.onClick.AddListener(OnTryAgainClicked);
            
        // No lives popup buttons
        if (buyLivesButton != null)
            buyLivesButton.onClick.AddListener(OnBuyLivesClicked);
        if (noLivesMainMenuButton != null)
            noLivesMainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }
    
    #region Show Popup Methods
    
    public void ShowWinPopup()
    { 
        HideAllPopups();
        
        if (winPopup != null)
        {
            SetupWinPopupContent();
            
            winPopup.SetActive(true);
            ShowOverlay();
            StartCoroutine(AnimatePopupIn(winPopup));
            
            // Audio feedback
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.levelCompleteSound);
            }
        }
    }
    
    public void ShowLosePopup()
    {
        if (LivesManager.Instance != null && LivesManager.Instance.GetCurrentLives() <= 0)
        {
            ShowNoLivesPopup();
            return;
        }
        
        HideAllPopups();
        
        if (losePopup != null)
        {
            if (LivesManager.Instance != null)
            {
                LivesManager.Instance.LoseLife();
            }
            
            SetupLosePopupContent();
            
            losePopup.SetActive(true);
            ShowOverlay();    
            StartCoroutine(AnimatePopupIn(losePopup));
            
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.gameOverSound);
            }
        }
    }
    
    public void ShowNoLivesPopup()
    {
        Debug.Log("💀 Showing NO LIVES popup");
        
        HideAllPopups();
        
        if (noLivesPopup != null)
        {
            SetupNoLivesPopupContent();
            
            noLivesPopup.SetActive(true);
            ShowOverlay();
            
            StartCoroutine(AnimatePopupIn(noLivesPopup));
            
            StartCoroutine(UpdateWaitTimer());
        }
    }
    
    #endregion
    
    #region Setup Popup Content
    
    void SetupWinPopupContent()
    {
        if (winTitleText != null)
            winTitleText.text = "Level Complete!";
            
        if (winMessageText != null)
            winMessageText.text = "Great job! Ready for the next challenge?";
    }
    
    void SetupLosePopupContent()
    {
        if (loseTitleText != null)
            loseTitleText.text = "Give Up -1";
            
        if (loseMessageText != null)
            loseMessageText.text = "Don't give up! Try again and show them who's boss!";
    }
    
    void SetupNoLivesPopupContent()
    {
        if (noLivesTitleText != null)
            noLivesTitleText.text = "No Lives Left!";
            
        // DISABLE
        if (buyLivesButton != null)
            buyLivesButton.interactable = false;
    }
    
    #endregion
    
    #region Button Click Handlers
    
    void OnTryAgainClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
            
        if (LivesManager.Instance != null && LivesManager.Instance.GetCurrentLives() <= 0)
        {
            ShowNoLivesPopup();
            return;
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
        
        HideAllPopups();
    }
    
    void OnMainMenuClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
            
        HideAllPopups();
        
        SceneManager.LoadScene("MainMenu");
    }
    
    void OnBuyLivesClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }
    
    #endregion
    
    #region Popup Animation & Management
    
    void HideAllPopups()
    {
        if (winPopup != null) winPopup.SetActive(false);
        if (losePopup != null) losePopup.SetActive(false);
        if (noLivesPopup != null) noLivesPopup.SetActive(false);
        HideOverlay();
    }
    
    void ShowOverlay()
    {
        if (popupOverlay != null)
            popupOverlay.SetActive(true);
    }
    
    void HideOverlay()
    {
        if (popupOverlay != null)
            popupOverlay.SetActive(false);
    }
    
    System.Collections.IEnumerator AnimatePopupIn(GameObject popup)
    {
        if (popup == null) yield break;
        
        Transform popupTransform = popup.transform;
        Vector3 originalScale = popupTransform.localScale;

        popupTransform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        
        while (elapsed < popupAnimationSpeed)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = popupScaleCurve.Evaluate(elapsed / popupAnimationSpeed);
            
            popupTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            
            yield return null;
        }
        
        popupTransform.localScale = originalScale;
    }
    
    System.Collections.IEnumerator UpdateWaitTimer()
    {
        if (waitTimerText == null || LivesManager.Instance == null) yield break;
        
        while (noLivesPopup != null && noLivesPopup.activeInHierarchy)
        {
            float timeToNextLife = LivesManager.Instance.GetTimeToNextLife();
            
            if (timeToNextLife <= 0)
            {
                HideAllPopups();
                yield break;
            }
            
            int hours = Mathf.FloorToInt(timeToNextLife / 3600);
            int minutes = Mathf.FloorToInt((timeToNextLife % 3600) / 60);
            int seconds = Mathf.FloorToInt(timeToNextLife % 60);
            
            waitTimerText.text = $"Wait {hours:00}h {minutes:00}m {seconds:00}s";
            
            yield return new WaitForSecondsRealtime(1f);
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Manuel olarak popup gösterme (test için)
    /// </summary>
    [ContextMenu("Test Win Popup")]
    public void TestWinPopup()
    {
        ShowWinPopup();
    }
    
    [ContextMenu("Test Lose Popup")]
    public void TestLosePopup()
    {
        ShowLosePopup();
    }
    
    [ContextMenu("Test No Lives Popup")]
    public void TestNoLivesPopup()
    {
        ShowNoLivesPopup();
    }
    
    #endregion
}