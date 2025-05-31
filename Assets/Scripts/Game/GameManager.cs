using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kaan Çakar 2025 - GameManager.cs
/// Main game logic controller for the bus puzzle game
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int levelNumber = 1;
    
    [Header("Game State")]
    public bool isGameActive = true;
    public int totalPeople = 0;
    public int peopleInBuses = 0;
    
    [Header("Bus System")]
    public List<BusData> allBuses = new List<BusData>(); // Tüm otobüsler
    public BusData currentBus; // Şu an bekleyen otobüs
    public int currentBusIndex = 0;
    
    [Header("Waiting Grid System")]
    public int waitingGridCapacity = 5;
    public int waitingGridCount = 0;
    public List<GridObject> waitingPeople = new List<GridObject>();
    
    [Header("Events")]
    public UnityEvent OnLevelComplete;
    public UnityEvent OnGameLost;
    public UnityEvent<string> OnGameMessage;
    public UnityEvent<BusData> OnBusArrived;
    public UnityEvent<BusData> OnBusDeparted;
    public UnityEvent<GridObject> OnPersonSelectedEvent;
    
    [Header("UI References")]
    public GameObject gameUI;
    public TMPro.TextMeshProUGUI levelText;
    public TMPro.TextMeshProUGUI statusText;
    public TMPro.TextMeshProUGUI busInfoText;
    
    private GridManager gridManager;
    private List<GridObject> allPeople = new List<GridObject>();
    private GridObject selectedPerson; // Seçili kişi
    
    public static GameManager Instance { get; private set; }
    
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
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GridManager not found in scene!");
        }
        
        InitializeLevel();
        StartLevel();
    }
    
    void Update()
    {
        if (!isGameActive) return;
        
        UpdateUI();
        CheckWinCondition();
        CheckLoseCondition();
    }
    
    void InitializeLevel()
    {
        // Grid'deki tüm objeleri topla
        CollectGridObjects();
        
        // Otobüs verilerini hazırla
        SetupBuses();
        
        totalPeople = allPeople.Count;
        
        if (levelText != null)
            levelText.text = $"Level {levelNumber}";
    }
    
    void CollectGridObjects()
    {
        allPeople.Clear();
        
        var gridObjects = FindObjectsOfType<GridObject>();
        
        foreach (var obj in gridObjects)
        {
            if (obj.objectType == GridObjectType.Person)
            {
                allPeople.Add(obj);
            }
        }
    }
    
    void SetupBuses()
    {
        allBuses.Clear();
        
        // Her renk için otobüs oluştur (sadece grid'de o renkte insan varsa)
        var usedColors = new HashSet<PersonColor>();
        
        foreach (var person in allPeople)
        {
            usedColors.Add(person.personColor);
        }
        
        foreach (var color in usedColors)
        {
            allBuses.Add(new BusData
            {
                color = color,
                capacity = 3, // Her otobüs 3 kişilik
                currentPassengers = 0
            });
        }
        
        // Otobüsleri karıştır (zorluk için)
        for (int i = 0; i < allBuses.Count; i++)
        {
            var temp = allBuses[i];
            int randomIndex = Random.Range(i, allBuses.Count);
            allBuses[i] = allBuses[randomIndex];
            allBuses[randomIndex] = temp;
        }
    }
    
    void StartLevel()
    {
        isGameActive = true;
        currentBusIndex = 0;
        waitingGridCount = 0;
        waitingPeople.Clear();
        selectedPerson = null;
        
        // İlk otobüsü getir
        if (allBuses.Count > 0)
        {
            ArriveNextBus();
        }
        
        OnGameMessage?.Invoke($"Level {levelNumber} Started! Click people to move them.");
    }
    
    void ArriveNextBus()
    {
        if (currentBusIndex >= allBuses.Count)
        {
            // Tüm otobüsler geldi, level tamamlandı
            if (peopleInBuses >= totalPeople)
            {
                WinLevel();
            }
            return;
        }
        
        currentBus = allBuses[currentBusIndex];
        currentBus.currentPassengers = 0; // Reset passenger count
        
        OnBusArrived?.Invoke(currentBus);
        OnGameMessage?.Invoke($"{currentBus.color} bus has arrived! ({currentBus.capacity} seats)");
        
        Debug.Log($"Bus arrived: {currentBus.color} with {currentBus.capacity} capacity");
    }
    
    // Kişi seçildiğinde çağrılır
    public void OnPersonSelected(GridObject person)
    {
        if (selectedPerson != null)
        {
            selectedPerson.SetSelected(false);
        }
        
        selectedPerson = person;
        person.SetSelected(true);
        
        // Kişiyi otobüse bindirmeye çalış
        TryBoardPerson(person);
        
        OnPersonSelectedEvent?.Invoke(person); // Event'i tetikle
    }
    
    public bool TryBoardPerson(GridObject person)
    {
        if (currentBus == null) return false;
        
        // Renk kontrolü
        if (person.personColor == currentBus.color)
        {
            // Doğru renk - otobüse bindir
            if (currentBus.currentPassengers < currentBus.capacity)
            {
                currentBus.currentPassengers++;
                peopleInBuses++;
                
                // Kişiyi otobüse bindir
                person.BoardBus();
                
                // Kişiyi grid'den kaldır
                RemovePersonFromGrid(person);
                
                OnGameMessage?.Invoke($"{person.personColor} person boarded the bus! ({currentBus.currentPassengers}/{currentBus.capacity})");
                
                // Otobüs doldu mu?
                if (currentBus.currentPassengers >= currentBus.capacity)
                {
                    DepartCurrentBus();
                }
                
                return true;
            }
        }
        else
        {
            // Yanlış renk - bekleme gridine gönder
            return SendToWaitingGrid(person);
        }
        
        return false;
    }
    
    bool SendToWaitingGrid(GridObject person)
    {
        if (waitingGridCount >= waitingGridCapacity)
        {
            // Bekleme gridi dolu - oyun kaybedildi
            LoseGame("Waiting grid is full!");
            return false;
        }
        
        waitingGridCount++;
        waitingPeople.Add(person);
        
        // Kişiyi bekleme gridine taşı
        person.SendToWaitingGrid();
        
        // Kişiyi grid'den kaldır
        RemovePersonFromGrid(person);
        
        OnGameMessage?.Invoke($"Wrong color! Person sent to waiting area. ({waitingGridCount}/{waitingGridCapacity})");
        
        return true;
    }
    
    void DepartCurrentBus()
    {
        if (currentBus == null) return;
        
        OnBusDeparted?.Invoke(currentBus);
        OnGameMessage?.Invoke($"{currentBus.color} bus departed with {currentBus.currentPassengers} passengers!");
        
        currentBusIndex++;
        
        // Sıradaki otobüsü getir
        ArriveNextBus();
    }
    
    void RemovePersonFromGrid(GridObject person)
    {
        if (person.gridCell != null)
        {
            person.gridCell.SetEmpty(); // DÜZELTME: SetOccupied(false) yerine SetEmpty()
            person.gridCell = null;
        }
        
        // Kişiyi aktif listeden çıkar
        if (allPeople.Contains(person))
        {
            allPeople.Remove(person);
        }
    }
    
    public bool CanPersonMove(GridObject person)
    {
        if (person == null || person.gridCell == null) return false;
        
        // 4 taraf kontrolü - komşu hücrelerin en az biri boş olmalı
        var neighbors = gridManager.GetNeighbors(person.gridCell.x, person.gridCell.z);
        
        foreach (var neighbor in neighbors)
        {
            if (!neighbor.IsOccupied) // DÜZELTME: isOccupied yerine IsOccupied
            {
                return true; // En az bir boş komşu var
            }
        }
        
        return false; // Tüm taraflar dolu
    }
    
    public List<GridObject> GetPlayablePersons()
    {
        List<GridObject> playable = new List<GridObject>();
        
        foreach (var person in allPeople)
        {
            if (CanPersonMove(person))
            {
                playable.Add(person);
            }
        }
        
        return playable;
    }
    
    void CheckWinCondition()
    {
        // Tüm insanlar otobüslere bindi mi?
        if (peopleInBuses >= totalPeople)
        {
            WinLevel();
        }
    }
    
    void CheckLoseCondition()
    {
        // Bekleme gridi dolu mu?
        if (waitingGridCount >= waitingGridCapacity)
        {
            LoseGame("Waiting grid is full!");
            return;
        }
        
        // Hareket edilebilir kişi kalmadı mı ama hala level tamamlanmadı mı?
        var playablePeople = GetPlayablePersons();
        if (playablePeople.Count == 0 && peopleInBuses < totalPeople && currentBusIndex >= allBuses.Count)
        {
            LoseGame("No more moves available!");
        }
    }
    
    void WinLevel()
    {
        isGameActive = false;
        OnLevelComplete?.Invoke();
        OnGameMessage?.Invoke("Level Complete! All people are on buses!");
    }
    
    public void LoseGame(string reason)
    {
        isGameActive = false;
        OnGameLost?.Invoke();
        OnGameMessage?.Invoke($"Game Over: {reason}");
    }
    
    void UpdateUI()
    {
        if (statusText != null)
        {
            statusText.text = $"People: {peopleInBuses}/{totalPeople} | Waiting: {waitingGridCount}/{waitingGridCapacity}";
        }
        
        if (busInfoText != null && currentBus != null)
        {
            busInfoText.text = $"Current Bus: {currentBus.color} ({currentBus.currentPassengers}/{currentBus.capacity})";
        }
    }
    
    // Public methods for UI buttons
    public void RestartLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    public void NextLevel()
    {
        levelNumber++;
        RestartLevel();
    }
    
    // Getter methods for other scripts
    public BusData GetCurrentBus()
    {
        return currentBus;
    }
    
    public bool IsWaitingGridFull()
    {
        return waitingGridCount >= waitingGridCapacity;
    }
}

[System.Serializable]
public class BusData
{
    public PersonColor color;
    public int capacity = 3;
    public int currentPassengers = 0;
    
    public bool IsFull()
    {
        return currentPassengers >= capacity;
    }
    
    public bool HasSpace()
    {
        return currentPassengers < capacity;
    }
}