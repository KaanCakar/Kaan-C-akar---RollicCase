using UnityEngine;

/// <summary>
/// Kaan Çakar 2025 - AudioManager.cs
/// Simple audio system for game events
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip personMoveSound;
    public AudioClip busMovementSound;
    public AudioClip buttonClickSound;
    public AudioClip levelCompleteSound;
    public AudioClip gameOverSound;

    [Header("Audio Source")]
    public AudioSource sfxSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float sfxVolume = 0.1f;

    public static AudioManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SaveInspectorVolume();

            LoadVolumeSettings();

            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        SubscribeToEvents();
    }

    /// <summary>
    /// Setup audio sources and initial volume
    /// </summary>
    void SetupAudioSources()
    {
        Debug.Log($"🔊 SetupAudioSources - sfxVolume is: {sfxVolume}");

        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SFX Source");
            sfxObject.transform.SetParent(transform);
            sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;

            Debug.Log($"🔊 SetupAudioSources - Created AudioSource, setting volume to: {sfxVolume}");
            sfxSource.volume = sfxVolume;
            Debug.Log($"🔊 SetupAudioSources - AudioSource.volume is now: {sfxSource.volume}");
        }
    }
    void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPersonSelectedEvent.AddListener(OnPersonMoved);
            GameManager.Instance.OnBusArrived.AddListener(OnBusMovement);
            GameManager.Instance.OnBusDeparted.AddListener(OnBusMovement);
            GameManager.Instance.OnLevelComplete.AddListener(OnLevelComplete);
            GameManager.Instance.OnGameLost.AddListener(OnGameOver);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPersonSelectedEvent.RemoveListener(OnPersonMoved);
            GameManager.Instance.OnBusArrived.RemoveListener(OnBusMovement);
            GameManager.Instance.OnBusDeparted.RemoveListener(OnBusMovement);
            GameManager.Instance.OnLevelComplete.RemoveListener(OnLevelComplete);
            GameManager.Instance.OnGameLost.RemoveListener(OnGameOver);
        }
    }

    #region Event Handlers

    void OnPersonMoved(GridObject person)
    {
        PlaySFX(personMoveSound);
    }

    void OnBusMovement(BusData busData)
    {
        PlaySFX(busMovementSound);
    }

    void OnLevelComplete()
    {
        PlaySFX(levelCompleteSound);
    }

    void OnGameOver()
    {
        PlaySFX(gameOverSound);
    }

    #endregion

    #region Public Audio Methods

    /// <summary>
    /// Play a sound effect
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    /// <summary>
    /// Play sound with custom volume
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume * sfxVolume);
        }
    }

    /// <summary>
    /// Play button click sound
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }

    #endregion

    #region Volume Control

    /// <summary>
    /// Set SFX volume
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
        SaveVolumeSettings();
    }

    /// <summary>
    /// Toggle SFX on/off
    /// </summary>
    public void ToggleSFX()
    {
        sfxVolume = sfxVolume > 0 ? 0 : 1;
        SetSFXVolume(sfxVolume);
    }

    void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    void LoadVolumeSettings()
    {
        float savedVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        sfxVolume = savedVolume;

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }
    #endregion

    #region Debug Methods

    [ContextMenu("Test Person Move Sound")]
    void TestPersonMoveSound()
    {
        PlaySFX(personMoveSound);
    }

    [ContextMenu("Test Bus Movement Sound")]
    void TestBusMovementSound()
    {
        PlaySFX(busMovementSound);
    }

    [ContextMenu("Test All Sounds")]
    void TestAllSounds()
    {
        StartCoroutine(TestSoundsSequence());
    }

    System.Collections.IEnumerator TestSoundsSequence()
    {
        Debug.Log("🔊 Testing all sounds...");

        PlaySFX(personMoveSound);
        yield return new WaitForSeconds(1f);

        PlaySFX(busMovementSound);
        yield return new WaitForSeconds(1f);

        PlaySFX(buttonClickSound);
        yield return new WaitForSeconds(1f);

        PlaySFX(levelCompleteSound);
        yield return new WaitForSeconds(1f);

        PlaySFX(gameOverSound);

        Debug.Log("🔊 Sound test completed!");
    }

    #endregion

    [ContextMenu("Save Current Inspector Volume")]
    void SaveInspectorVolume()
    {
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }
}