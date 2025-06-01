using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kaan Çakar 2025 - GameManager.cs
/// Main game logic controller for the bus puzzle game - Updated for shape system
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
    public List<BusData> allBuses = new List<BusData>();
    public BusData currentBus;
    public int currentBusIndex = 0;
    public Transform busPosition; // Otobüsün bulunduğu pozisyon
    public GameObject currentBusObject; // Mevcut otobüs objesi

    [Header("3D Waiting Grid System")]
    public WaitingGrid waitingGrid; // WaitingGrid referansı

    [Header("Level Data")]
    public LevelData currentLevelData; // Inspector'dan atanacak

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

    private GridManager gridManager;
    private List<GridObject> allPeople = new List<GridObject>();
    private GridObject selectedPerson;

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

        // Waiting Grid'i bul
        if (waitingGrid == null)
            waitingGrid = FindObjectOfType<WaitingGrid>();

        if (waitingGrid == null)
        {
            Debug.LogError("WaitingGrid not found in scene!");
        }

        InitializeLevel();
        StartLevel();
    }

    void Update()
    {
        if (!isGameActive) return;

        CheckWinCondition();
        CheckLoseCondition();
    }

    void InitializeLevel()
    {
        CollectGridObjects();
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
            if (obj.objectType == GridObjectType.Person && obj.gridCell != null && obj.gridCell.isPlayArea)
            {
                allPeople.Add(obj);
            }
        }
    }

    void SetupBuses()
    {
        allBuses.Clear();

        // Önce level data'dan bus sequence'ı al
        if (currentLevelData != null && currentLevelData.busSequence != null && currentLevelData.busSequence.Count > 0)
        {
            // Level editor'dan gelen bus sequence'ı kullan
            allBuses = new List<BusData>(currentLevelData.busSequence);
            Debug.Log($"Loaded {allBuses.Count} buses from level data");
        }
        else
        {
            // Fallback: Grid'deki renklere göre otomatik oluştur
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
                    capacity = 3,
                    currentPassengers = 0
                });
            }

            Debug.Log($"Auto-generated {allBuses.Count} buses from grid colors");
        }

        // Reset passenger counts
        foreach (var bus in allBuses)
        {
            bus.currentPassengers = 0;
        }
    }

    void StartLevel()
    {
        isGameActive = true;
        currentBusIndex = 0;
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
        currentBus.currentPassengers = 0;

        OnBusArrived?.Invoke(currentBus);
        OnGameMessage?.Invoke($"{currentBus.color} bus has arrived! ({currentBus.capacity} seats)");

        // Waiting grid'deki uygun renkteki insanları otobüse bindir
        CheckAndBoardFromWaitingGrid();

        Debug.Log($"Bus arrived: {currentBus.color} with {currentBus.capacity} capacity");
    }

    /// <summary>
    /// Waiting grid'de otobüs rengine uygun insanları kontrol et ve bindir
    /// </summary>
    void CheckAndBoardFromWaitingGrid()
    {
        if (waitingGrid == null || currentBus == null) return;

        // Waiting grid'de bu renkte kaç kişi var?
        int availablePeople = waitingGrid.GetPeopleCountByColor(currentBus.color);

        if (availablePeople > 0)
        {
            OnGameMessage?.Invoke($"Found {availablePeople} {currentBus.color} people in waiting area!");

            // Bu renkteki insanları al
            List<GridObject> peopleToBoard = waitingGrid.GetPeopleByColor(currentBus.color);

            // Otobüs kapasitesi kadar bindir
            int boarded = 0;
            for (int i = 0; i < peopleToBoard.Count && boarded < currentBus.capacity; i++)
            {
                GridObject person = peopleToBoard[i];

                // Waiting grid'den çıkar
                waitingGrid.RemovePersonByObject(person);

                // Otobüse bindir
                StartCoroutine(BoardPersonFromWaitingGrid(person));

                boarded++;
            }

            OnGameMessage?.Invoke($"{boarded} people boarded from waiting area!");
        }
    }

    /// <summary>
    /// Waiting grid'den otobüse binme animasyonu
    /// </summary>
    System.Collections.IEnumerator BoardPersonFromWaitingGrid(GridObject person)
    {
        Vector3 startPos = person.transform.position;
        Vector3 busPos = GetBusPosition();
        Vector3 busEntryPoint = busPos + Vector3.back * 2f;

        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            person.transform.position = Vector3.Lerp(startPos, busEntryPoint, t);
            yield return null;
        }

        // Otobüse bindir
        person.BoardBus();
        currentBus.currentPassengers++;
        peopleInBuses++;

        OnGameMessage?.Invoke($"{person.personColor} boarded from waiting area! ({currentBus.currentPassengers}/{currentBus.capacity})");

        // Otobüs doldu mu?
        if (currentBus.IsFull())
        {
            yield return new WaitForSeconds(0.5f); // Kısa bekleme
            DepartCurrentBus();
        }
    }

    // Kişi seçildiğinde çağrılır - ANA OYUN MEKANİĞİ
    public void OnPersonSelected(GridObject person)
    {
        if (!person.IsPlayable())
        {
            OnGameMessage?.Invoke("This person cannot move!");
            return;
        }

        // Sadece oynanabilir alandaki insanları kontrol et
        if (person.gridCell == null || !person.gridCell.isPlayArea)
        {
            OnGameMessage?.Invoke("Person is not in playable area!");
            return;
        }

        // Renk kontrolü yap ve hedefe gönder
        ProcessPersonMovement(person);

        OnPersonSelectedEvent?.Invoke(person);
    }

    void ProcessPersonMovement(GridObject person)
    {
        if (currentBus == null)
        {
            OnGameMessage?.Invoke("No bus available!");
            return;
        }

        // Renk kontrolü
        if (person.personColor == currentBus.color)
        {
            // Doğru renk - otobüse gönder
            SendPersonToBus(person);
        }
        else
        {
            // Yanlış renk - bekleme gridine gönder
            SendPersonToWaitingGrid(person);
        }
    }

    void SendPersonToBus(GridObject person)
    {
        if (currentBus.IsFull())
        {
            OnGameMessage?.Invoke("Bus is full!");
            return;
        }

        // Otobüs pozisyonunu al
        Vector3 targetBusPosition = GetBusPosition();

        // Kişiyi otobüse hareket ettir
        StartCoroutine(MovePersonToBus(person, targetBusPosition));
    }

    Vector3 GetBusPosition()
    {
        if (busPosition != null)
            return busPosition.position;

        if (currentBusObject != null)
            return currentBusObject.transform.position;

        // Fallback - ekranın ortası
        return new Vector3(0, 1, 0);
    }

    System.Collections.IEnumerator MovePersonToBus(GridObject person, Vector3 busPos)
    {
        Vector3 startPos = person.transform.position;
        Vector3 busEntryPoint = busPos + Vector3.back * 2f; // Otobüsün arkasına

        float duration = 1.5f;
        float elapsed = 0f;

        // Grid'den kaldır
        RemovePersonFromGrid(person);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            person.transform.position = Vector3.Lerp(startPos, busEntryPoint, t);
            yield return null;
        }

        // Otobüse bindir
        person.BoardBus();
        currentBus.currentPassengers++;
        peopleInBuses++;

        OnGameMessage?.Invoke($"{person.personColor} boarded! ({currentBus.currentPassengers}/{currentBus.capacity})");

        // Otobüs doldu mu?
        if (currentBus.IsFull())
        {
            DepartCurrentBus();
        }
    }

    void SendPersonToWaitingGrid(GridObject person)
    {
        // Waiting grid dolu mu?
        if (waitingGrid != null && waitingGrid.IsFull())
        {
            LoseGame("Waiting grid is full!");
            return;
        }

        // Kişiyi waiting grid'e ekle (hareket WaitingGrid tarafından yapılacak)
        if (waitingGrid != null && waitingGrid.AddPersonToWaiting(person))
        {
            // Başarıyla eklendi
            RemovePersonFromGrid(person);

            OnGameMessage?.Invoke($"Wrong color! ({waitingGrid.GetOccupiedCount()}/{waitingGrid.capacity})");
        }
        else
        {
            LoseGame("Failed to add person to waiting area!");
        }
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
            person.gridCell.SetEmpty();
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

        // Play area kontrolü ekle
        if (!person.gridCell.isPlayArea) return false;

        // 4 taraf kontrolü - komşu hücrelerin en az biri boş olmalı veya play area dışı olmalı
        var neighbors = gridManager.GetNeighbors(person.gridCell.x, person.gridCell.z);

        foreach (var neighbor in neighbors)
        {
            if (!neighbor.IsOccupied || !neighbor.isPlayArea)
            {
                return true; // En az bir boş komşu var veya play area dışı
            }
        }

        return false; // Tüm taraflar dolu
    }

    public List<GridObject> GetPlayablePersons()
    {
        List<GridObject> playable = new List<GridObject>();

        foreach (var person in allPeople)
        {
            if (CanPersonMove(person) && person.gridCell != null && person.gridCell.isPlayArea)
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
        // Waiting Grid dolu mu?
        if (waitingGrid != null && waitingGrid.IsFull())
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

    // Level data yükleme metodu
    public void LoadLevelData(LevelData levelData)
    {
        currentLevelData = levelData;

        // Grid'i yeniden oluştur
        if (gridManager != null)
        {
            gridManager.gridWidth = levelData.gridWidth;
            gridManager.gridHeight = levelData.gridHeight;
            gridManager.ForceGridRecreation();

            // Play area verilerini uygula
            if (levelData.playAreaData != null)
            {
                foreach (var areaData in levelData.playAreaData)
                {
                    GridCell cell = gridManager.GetCell(areaData.x, areaData.z);
                    if (cell != null)
                    {
                        cell.SetPlayArea(areaData.isPlayArea);
                        cell.SetVisible(areaData.isVisible);
                    }
                }
            }
        }

        // Level'ı yeniden başlat
        InitializeLevel();
        StartLevel();
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

    // Getter methods
    public BusData GetCurrentBus()
    {
        return currentBus;
    }

    public bool IsWaitingGridFull()
    {
        return waitingGrid != null ? waitingGrid.IsFull() : false;
    }

    public int GetWaitingGridCount()
    {
        return waitingGrid != null ? waitingGrid.GetOccupiedCount() : 0;
    }

    // Shape system helper methods
    public int GetPlayAreaCellCount()
    {
        if (gridManager == null) return 0;

        int count = 0;
        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            for (int z = 0; z < gridManager.gridHeight; z++)
            {
                GridCell cell = gridManager.GetCell(x, z);
                if (cell != null && cell.isPlayArea)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public List<Vector2Int> GetPlayAreaPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        if (gridManager == null) return positions;

        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            for (int z = 0; z < gridManager.gridHeight; z++)
            {
                GridCell cell = gridManager.GetCell(x, z);
                if (cell != null && cell.isPlayArea)
                {
                    positions.Add(new Vector2Int(x, z));
                }
            }
        }

        return positions;
    }
}