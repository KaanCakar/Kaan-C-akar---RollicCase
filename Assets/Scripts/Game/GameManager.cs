using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kaan Çakar 2025 - GameManager.cs
/// Game manager for handling game state, bus system, and level management.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int levelNumber = 1;

    [Header("Game State")]
    public bool isGameActive = true;
    public int totalPeople = 0;
    public int peopleInBuses = 0;
    public bool isWinTriggered = false;

    [Header("Dual Bus System")]
    public Transform busSpawnPoint;
    public Transform busActivePosition;
    public Transform busWaitingPosition;
    public Transform busExitPoint;

    [Header("Bus Objects")]
    public GameObject currentActiveBus;
    public GameObject currentWaitingBus;
    public GameObject busPrefab;

    [Header("Bus Prefabs")]
    public GameObject[] busPrefabs = new GameObject[10];

    [Header("Bus Animation")]
    public float busAnimationSpeed = 2f;
    public AnimationCurve busMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Bus System")]
    public List<BusData> allBuses = new List<BusData>();
    public BusData currentBus;
    public int currentBusIndex = 0;

    [Header("Manual Bus Testing")]
    public List<BusData> manualBusSequence = new List<BusData>();

    [Header("3D Waiting Grid System")]
    public WaitingGrid waitingGrid;

    [Header("Level Data")]
    public LevelData currentLevelData;

    [Header("Optimization")]
    public UnityEvent OnGridStateChanged;

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

    // Bus states
    public enum BusSystemState
    {
        SpawningBuses,
        ActiveWaiting,
        MovingBuses,
        LevelComplete
    }

    public BusSystemState busSystemState = BusSystemState.SpawningBuses;

    private Dictionary<GridObject, bool> playableCache = new Dictionary<GridObject, bool>();
    private bool needsPlayableUpdate = true;

    private GridManager gridManager;
    private List<GridObject> allPeople = new List<GridObject>();
    private GridObject selectedPerson;
    private Coroutine busMovementCoroutine;

    private string[] busPrefabNames = {
        "Bus_0", "Bus_1", "Bus_2", "Bus_3", "Bus_4",
        "Bus_5", "Bus_6", "Bus_7", "Bus_8", "Bus_9"
    };

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

        if (waitingGrid == null)
            waitingGrid = FindObjectOfType<WaitingGrid>();

        if (waitingGrid == null)
        {
            Debug.LogError("WaitingGrid not found in scene!");
        }

        LoadBusPrefabs();

        OnGridStateChanged.AddListener(UpdateAllPlayableStates);

        InitializeLevel();
        StartLevel();
    }

    void Update()
    {
        if (!isGameActive) return;
        if (needsPlayableUpdate)
        {
            UpdateAllPlayableStates();
            needsPlayableUpdate = false;
        }

        CheckWinCondition();
        CheckLoseCondition();
    }

    void LoadBusPrefabs()
    {
        for (int i = 0; i < 10; i++)
        {
            if (busPrefabs[i] == null)
            {
                busPrefabs[i] = Resources.Load<GameObject>($"Prefabs/{busPrefabNames[i]}");

                if (busPrefabs[i] != null)
                {
                    Debug.Log($"Loaded {busPrefabNames[i]}");
                }
                else
                {
                    Debug.LogWarning($"Could not load {busPrefabNames[i]} from Resources/Prefabs/");
                }
            }
        }
    }

    void UpdateAllPlayableStates()
    {
        foreach (var person in allPeople)
        {
            if (person != null && !person.isInBus && !person.isInWaitingGrid)
            {
                bool newPlayableState = CanPersonMove(person);
                playableCache[person] = newPlayableState;

                person.SetPlayableState(newPlayableState);
            }
        }

        Debug.Log($"Updated playable states for {allPeople.Count} people");
    }

    public void TriggerGridStateUpdate()
    {
        needsPlayableUpdate = true;
        OnGridStateChanged?.Invoke();
    }

    void OnPersonMoved()
    {
        TriggerGridStateUpdate();
    }

    void OnBusStateChanged()
    {
        TriggerGridStateUpdate();
    }

    public bool IsPersonPlayable(GridObject person)
    {
        if (playableCache.ContainsKey(person))
        {
            return playableCache[person];
        }

        bool playable = CanPersonMove(person);
        playableCache[person] = playable;
        return playable;
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

        if (manualBusSequence != null && manualBusSequence.Count > 0)
        {
            allBuses = new List<BusData>(manualBusSequence);

            for (int i = 0; i < allBuses.Count; i++)
            {
                Debug.Log($"  Manual Bus {i}: {allBuses[i].color} (Capacity: {allBuses[i].capacity})");
            }
        }
        else if (allBuses.Count > 0)
        {
            for (int i = 0; i < allBuses.Count; i++)
            {
                Debug.Log($"  Existing Bus {i}: {allBuses[i].color} (Capacity: {allBuses[i].capacity})");
            }
        }
        else if (currentLevelData != null && currentLevelData.busSequence != null && currentLevelData.busSequence.Count > 0)
        {
            allBuses = new List<BusData>(currentLevelData.busSequence);

            for (int i = 0; i < allBuses.Count; i++)
            {
                Debug.Log($"  Level Bus {i}: {allBuses[i].color} (Capacity: {allBuses[i].capacity})");
            }
        }
        else
        {
            Debug.LogWarning("No bus sequence found! Using auto-generation...");
            Debug.LogWarning("Use Tool's 'Force Update GameManager' button to set bus sequence!");
            var usedColors = new HashSet<PersonColor>();

            foreach (var person in allPeople)
            {
                usedColors.Add(person.personColor);
            }

            allBuses.Clear();
            foreach (var color in usedColors)
            {
                allBuses.Add(new BusData
                {
                    color = color,
                    capacity = 3,
                    currentPassengers = 0
                });
            }
        }

        foreach (var bus in allBuses)
        {
            bus.currentPassengers = 0;
        }


        for (int i = 0; i < allBuses.Count; i++)
        {
            Debug.Log($"Bus[{i}]: {allBuses[i].color}");
        }
    }

    void StartLevel()
    {
        isGameActive = true;
        currentBusIndex = 0;
        selectedPerson = null;
        busSystemState = BusSystemState.SpawningBuses;

        if (allBuses.Count > 0)
        {
            StartCoroutine(SpawnInitialBuses());
        }

        OnGameMessage?.Invoke($"Level {levelNumber} Started! Click people to move them.");

        needsPlayableUpdate = true;
    }
    System.Collections.IEnumerator SpawnInitialBuses()
    {
        if (currentBusIndex < allBuses.Count)
        {
            yield return StartCoroutine(SpawnBusAt(currentBusIndex, busActivePosition, true));
            currentBusIndex++;
        }

        if (currentBusIndex < allBuses.Count)
        {
            yield return StartCoroutine(SpawnBusAt(currentBusIndex, busWaitingPosition, false));
        }
        busSystemState = BusSystemState.ActiveWaiting;
        CheckAndBoardFromWaitingGrid();
    }

    System.Collections.IEnumerator SpawnBusAt(int busIndex, Transform targetPosition, bool isActive)
    {
        if (busIndex >= allBuses.Count) yield break;

        BusData busData = allBuses[busIndex];
        Debug.Log($"=== SPAWNING BUS ===");
        Debug.Log($"Bus: {busData.color} (Index: {(int)busData.color})");
        Debug.Log($"Target: {targetPosition.name}, Active: {isActive}");

        busData.MarkAsSpawned();
        busData.SetActive(isActive);

        int colorIndex = (int)busData.color;
        GameObject selectedPrefab = null;

        if (colorIndex < busPrefabs.Length && busPrefabs[colorIndex] != null)
        {
            selectedPrefab = busPrefabs[colorIndex];
        }
        else
        {
            Debug.LogError($"No prefab found for {busData.color} (index: {colorIndex})");

            if (busPrefab != null)
            {
                selectedPrefab = busPrefab;
            }
            else
            {
                yield break;
            }
        }

        GameObject newBus = Instantiate(selectedPrefab, busSpawnPoint.position, busSpawnPoint.rotation);
        Debug.Log($"Instantiated bus: {newBus.name}");

        BusComponent busComponent = newBus.GetComponent<BusComponent>();
        if (busComponent != null)
        {
            busComponent.Initialize(busData);
            busComponent.SetAsActiveBus(isActive);

            if (isActive)
            {
                busComponent.SetState(BusState.Approaching);
            }
            else
            {
                busComponent.SetState(BusState.Waiting);
            }
        }
        else
        {
            Debug.LogError($"No BusComponent found on {newBus.name}!");
        }

        if (isActive)
        {
            currentActiveBus = newBus;
            currentBus = busData;
            OnBusArrived?.Invoke(currentBus);
            OnGameMessage?.Invoke($"{currentBus.color} bus is ready! ({currentBus.capacity} seats)");
        }
        else
        {
            currentWaitingBus = newBus;
            OnGameMessage?.Invoke($"Next: {busData.color} bus is waiting!");
        }

        yield return StartCoroutine(MoveBusToPosition(newBus, targetPosition.position));

        if (busComponent != null)
        {
            busComponent.SetState(isActive ? BusState.Waiting : BusState.Waiting);
        }

        if (isActive)
        {
            yield return new WaitForSeconds(0.5f);
            CheckAndBoardFromWaitingGrid();
        }
    }

    System.Collections.IEnumerator MoveBusToPosition(GameObject bus, Vector3 targetPosition)
    {
        Vector3 startPosition = bus.transform.position;
        float duration = 2f / busAnimationSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = busMoveCurve.Evaluate(elapsed / duration);
            bus.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        bus.transform.position = targetPosition;
    }

    void CheckAndBoardFromWaitingGrid()
    {
        if (waitingGrid == null || currentBus == null)
        {
            Debug.LogWarning("WaitingGrid or currentBus is not set! Cannot board people.");
            return;
        }

        int availablePeople = waitingGrid.GetPeopleCountByColor(currentBus.color);

        if (availablePeople > 0)
        {
            List<GridObject> peopleToBoard = waitingGrid.GetPeopleByColor(currentBus.color);

            int boarded = 0;
            for (int i = 0; i < peopleToBoard.Count && boarded < currentBus.capacity; i++)
            {
                GridObject person = peopleToBoard[i];

                waitingGrid.RemovePersonByObject(person);
                StartCoroutine(BoardPersonFromWaitingGrid(person));

                boarded++;
            }
        }
        else
        {
            Debug.Log("No matching people found for boarding");
        }
    }

    System.Collections.IEnumerator BoardPersonFromWaitingGrid(GridObject person)
    {

        Vector3 busPos = GetBusPosition();
        Vector3 busEntryPoint = busPos + Vector3.back * 2f;
        person.transform.position = busEntryPoint;

        Debug.Log($"🚌 Moved {person.personColor} directly to bus");

        if (currentBus.IsFull())
        {
            Debug.Log($"❌ Bus is full! {person.personColor} cannot board");
            yield break;
        }

        person.BoardBus();
        currentBus.AddPassenger();
        peopleInBuses++;

        BusComponent busComponent = currentActiveBus?.GetComponent<BusComponent>();
        if (busComponent != null)
        {
            busComponent.UpdatePassengerCount(currentBus.currentPassengers, currentBus.capacity);
            busComponent.SetState(BusState.Boarding);

            yield return new WaitForSeconds(0.3f);
            busComponent.SetState(BusState.Waiting);
        }

        OnGameMessage?.Invoke($"{person.personColor} boarded from waiting area! ({currentBus.currentPassengers}/{currentBus.capacity})");

        if (currentBus.IsFull())
        {
            yield return new WaitForSeconds(0.5f);
            DepartCurrentBus();
        }
    }

    // === PATHFINDING INTEGRATION ===
    public bool CanPersonMove(GridObject person)
    {
        if (person == null || person.gridCell == null)
        {
            return false;
        }

        if (gridManager == null)
        {
            return false;
        }

        if (!person.gridCell.isPlayArea)
        {
            return false;
        }

        int frontRowZ = gridManager.gridHeight - 1;
        bool isInFrontRow = person.gridCell.z == frontRowZ;
        // If the person is in the front row, they can always playable
        if (isInFrontRow)
        {
            return true;
        }

        Vector2Int personPos = new Vector2Int(person.gridCell.x, person.gridCell.z);
        bool hasPath = gridManager.CanPersonReachExit(personPos);

        return hasPath;
    }


    public void OnPersonSelected(GridObject person)
    {
        if (TimerUI.Instance != null && !TimerUI.Instance.IsActive())
        {
            TimerUI.Instance.StartTimer();
        }
        OnPersonSelectedEvent?.Invoke(person);

        if (!person.IsPlayable())
        {
            OnGameMessage?.Invoke("This person cannot move!");
            return;
        }

        if (person.gridCell == null || !person.gridCell.isPlayArea)
        {
            OnGameMessage?.Invoke("Person is not in playable area!");
            return;
        }

        // Front row special handling
        int frontRowZ = gridManager.gridHeight - 1;
        bool isInFrontRow = person.gridCell.z == frontRowZ;

        if (isInFrontRow)
        {
            ProcessFrontRowPerson(person);
            OnPersonSelectedEvent?.Invoke(person);
            return;
        }

        if (gridManager == null)
        {
            OnGameMessage?.Invoke("Grid system not available!");
            return;
        }

        Vector2Int personPos = new Vector2Int(person.gridCell.x, person.gridCell.z);
        List<Vector2Int> pathToExit = gridManager.FindPathToExit(personPos);

        if (pathToExit == null || pathToExit.Count < 2)
        {
            OnGameMessage?.Invoke("No path to exit available!");
            return;
        }

        ProcessPersonMovementWithPath(person, pathToExit);
        OnPersonSelectedEvent?.Invoke(person);
    }

    void ProcessFrontRowPerson(GridObject person)
    {
        if (currentBus == null)
        {
            OnGameMessage?.Invoke("No bus available!");
            return;
        }

        if (person.personColor == currentBus.color)
        {
            if (currentBus.IsFull())
            {
                OnGameMessage?.Invoke("Bus is full!");
                return;
            }

            SendFrontRowPersonToBus(person);
        }
        else
        {
            SendFrontRowPersonToWaitingGrid(person);
        }
    }

    void SendFrontRowPersonToBus(GridObject person)
    {
        if (currentBus.IsFull())
        {
            OnGameMessage?.Invoke($"{currentBus.color} bus is full! ({currentBus.currentPassengers}/{currentBus.capacity})");

            SendFrontRowPersonToWaitingGrid(person);
            return;
        }

        RemovePersonFromGrid(person);
        StartCoroutine(FrontRowToBusAnimation(person));
    }

    void SendFrontRowPersonToWaitingGrid(GridObject person)
    {
        if (waitingGrid != null && waitingGrid.IsFull())
        {
            LoseGame("Waiting grid is full!");
            return;
        }

        RemovePersonFromGrid(person);
        StartCoroutine(FrontRowToWaitingGridAnimation(person));
    }

    System.Collections.IEnumerator FrontRowToBusAnimation(GridObject person)
    {
        if (currentBus == null)
        {
            OnGameMessage?.Invoke("No bus available!");
            OnPersonMoved();
            yield break;
        }

        if (currentBus.IsFull())
        {
            OnGameMessage?.Invoke($"Bus became full during click! Redirecting to waiting grid.");
            yield return StartCoroutine(FrontRowToWaitingGridAnimation(person));
            OnPersonMoved();
            yield break;
        }

        Vector3 startPos = person.transform.position;
        Vector3 busEntryPoint = GetBusPosition() + Vector3.back * 2f;

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            person.transform.position = Vector3.Lerp(startPos, busEntryPoint, t);
            yield return null;
        }

        if (currentBus == null)
        {
            OnGameMessage?.Invoke("Bus disappeared during animation!");
            OnPersonMoved();
            yield break;
        }

        if (currentBus.IsFull())
        {
            OnGameMessage?.Invoke($"Bus became full during animation! Person sent to waiting grid.");

            if (waitingGrid != null && waitingGrid.AddPersonToWaiting(person))
            {
                OnGameMessage?.Invoke($"Redirected to waiting area ({waitingGrid.GetOccupiedCount()}/{waitingGrid.capacity})");
            }
            else
            {
                LoseGame("Failed to add person to waiting area!");
            }

            OnPersonMoved();
            yield break;
        }

        person.BoardBus();
        currentBus.currentPassengers++;
        peopleInBuses++;

        BusComponent busComponent = currentActiveBus?.GetComponent<BusComponent>();
        if (busComponent != null)
        {
            busComponent.UpdatePassengerCount(currentBus.currentPassengers, currentBus.capacity);
            busComponent.SetState(BusState.Boarding);

            yield return new WaitForSeconds(0.5f);
            busComponent.SetState(BusState.Waiting);
        }

        OnGameMessage?.Invoke($"{person.personColor} boarded directly! ({currentBus.currentPassengers}/{currentBus.capacity})");

        if (currentBus.IsFull())
        {
            DepartCurrentBus();
        }

        OnPersonMoved();
    }

    System.Collections.IEnumerator FrontRowToWaitingGridAnimation(GridObject person)
    {
        if (waitingGrid != null && waitingGrid.AddPersonToWaiting(person))
        {
            OnGameMessage?.Invoke($"Wrong color! Sent to waiting area ({waitingGrid.GetOccupiedCount()}/{waitingGrid.capacity})");
        }
        else
        {
            LoseGame("Failed to add person to waiting area!");
        }

        OnPersonMoved();
        yield return null;
    }

    void ProcessPersonMovementWithPath(GridObject person, List<Vector2Int> pathToExit)
    {
        if (currentBus == null)
        {
            OnGameMessage?.Invoke("No bus available!");
            return;
        }

        if (person.personColor == currentBus.color)
        {
            SendPersonToBusWithPath(person, pathToExit);
        }
        else
        {
            SendPersonToWaitingGridWithPath(person, pathToExit);
        }
    }

    void SendPersonToBusWithPath(GridObject person, List<Vector2Int> pathToExit)
    {
        if (currentBus.IsFull())
        {
            OnGameMessage?.Invoke($"{currentBus.color} bus is full! ({currentBus.currentPassengers}/{currentBus.capacity})");

            SendPersonToWaitingGridWithPath(person, pathToExit);
            return;
        }

        StartCoroutine(MovePersonAlongPath(person, pathToExit, MoveDestination.Bus));
    }


    void SendPersonToWaitingGridWithPath(GridObject person, List<Vector2Int> pathToExit)
    {
        if (waitingGrid != null && waitingGrid.IsFull())
        {
            LoseGame("Waiting grid is full!");
            return;
        }

        StartCoroutine(MovePersonAlongPath(person, pathToExit, MoveDestination.WaitingGrid));
    }

    public enum MoveDestination
    {
        Bus,
        WaitingGrid
    }

    System.Collections.IEnumerator MovePersonAlongPath(GridObject person, List<Vector2Int> path, MoveDestination destination)
    {
        RemovePersonFromGrid(person);

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int gridPos = path[i];
            Vector3 worldPos = gridManager.GetWorldPosition(gridPos.x, gridPos.y);
            worldPos.y = person.transform.position.y;

            yield return StartCoroutine(MovePersonToPosition(person, worldPos));
        }

        yield return StartCoroutine(HandleFinalDestination(person, destination));

        OnPersonMoved();
    }

    System.Collections.IEnumerator MovePersonToPosition(GridObject person, Vector3 targetPos)
    {
        Vector3 startPos = person.transform.position;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            person.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        person.transform.position = targetPos;
    }

    System.Collections.IEnumerator HandleFinalDestination(GridObject person, MoveDestination destination)
    {
        switch (destination)
        {
            case MoveDestination.Bus:
                yield return StartCoroutine(MoveToBusEntryPoint(person));
                break;

            case MoveDestination.WaitingGrid:
                yield return StartCoroutine(MoveToWaitingGrid(person));
                break;
        }
    }

    System.Collections.IEnumerator MoveToBusEntryPoint(GridObject person)
    {
        if (currentBus.IsFull())
        {
            OnGameMessage?.Invoke($"Bus became full during movement! Sending to waiting grid.");
            yield return StartCoroutine(MoveToWaitingGrid(person));
            yield break;
        }

        Vector3 busEntryPoint = GetBusPosition() + Vector3.back * 2f;
        yield return StartCoroutine(MovePersonToPosition(person, busEntryPoint));

        if (currentBus.IsFull())
        {
            OnGameMessage?.Invoke($"Bus became full! Person redirected to waiting grid.");
            yield return StartCoroutine(MoveToWaitingGrid(person));
            yield break;
        }

        person.BoardBus();
        currentBus.currentPassengers++;
        peopleInBuses++;

        BusComponent busComponent = currentActiveBus?.GetComponent<BusComponent>();
        if (busComponent != null)
        {
            busComponent.UpdatePassengerCount(currentBus.currentPassengers, currentBus.capacity);
            busComponent.SetState(BusState.Boarding);

            yield return new WaitForSeconds(0.3f);
            busComponent.SetState(BusState.Waiting);
        }

        OnGameMessage?.Invoke($"{person.personColor} boarded! ({currentBus.currentPassengers}/{currentBus.capacity})");

        if (currentBus.IsFull())
        {
            DepartCurrentBus();
        }
    }


    System.Collections.IEnumerator MoveToWaitingGrid(GridObject person)
    {
        if (waitingGrid != null && waitingGrid.AddPersonToWaiting(person))
        {
            OnGameMessage?.Invoke($"Wrong color! ({waitingGrid.GetOccupiedCount()}/{waitingGrid.capacity})");
        }
        else
        {
            LoseGame("Failed to add person to waiting area!");
        }

        yield return null;
    }

    void DepartCurrentBus()
    {
        if (currentBus == null || busSystemState == BusSystemState.MovingBuses) return;

        Debug.Log($"=== DEPARTING CURRENT BUS: {currentBus.color} ===");

        OnBusDeparted?.Invoke(currentBus);
        OnGameMessage?.Invoke($"{currentBus.color} bus departed with {currentBus.currentPassengers} passengers!");

        busSystemState = BusSystemState.MovingBuses;

        if (busMovementCoroutine != null)
        {
            StopCoroutine(busMovementCoroutine);
        }
        busMovementCoroutine = StartCoroutine(HandleBusMovement());
    }

    System.Collections.IEnumerator HandleBusMovement()
    {
        Debug.Log("=== HANDLING BUS MOVEMENT ===");
        BusComponent activeBusComponent = null;
        if (currentActiveBus != null)
        {
            activeBusComponent = currentActiveBus.GetComponent<BusComponent>();
            if (activeBusComponent != null)
            {
                activeBusComponent.SetState(BusState.Departing);
            }

            yield return StartCoroutine(MoveBusToPosition(currentActiveBus, busExitPoint.position));
            Destroy(currentActiveBus);
            currentActiveBus = null;
        }

        if (currentWaitingBus != null)
        {
            BusComponent waitingBusComponent = currentWaitingBus.GetComponent<BusComponent>();
            if (waitingBusComponent != null)
            {
                waitingBusComponent.SetState(BusState.Approaching);
            }

            yield return StartCoroutine(MoveBusToPosition(currentWaitingBus, busActivePosition.position));

            currentActiveBus = currentWaitingBus;
            currentWaitingBus = null;

            if (currentBusIndex < allBuses.Count)
            {
                currentBus = allBuses[currentBusIndex];
                Debug.Log($"NEW CURRENT BUS: {currentBus.color} at index {currentBusIndex}");
                currentBusIndex++;
            }
            else
            {
                currentBus = null;
                Debug.Log($"REACHED END OF BUS SEQUENCE");
            }

            if (currentBus != null)
            {
                currentBus.SetActive(true);

                if (waitingBusComponent != null)
                {
                    waitingBusComponent.Initialize(currentBus);
                    waitingBusComponent.SetAsActiveBus(true);
                    waitingBusComponent.SetState(BusState.Waiting);
                }

                OnBusArrived?.Invoke(currentBus);
                OnGameMessage?.Invoke($"{currentBus.color} bus is now ready! ({currentBus.capacity} seats)");
            }
        }

        if (currentBusIndex + 1 < allBuses.Count)
        {
            yield return StartCoroutine(SpawnBusAt(currentBusIndex + 1, busWaitingPosition, false));
        }

        busSystemState = BusSystemState.ActiveWaiting;

        if (currentBus != null)
        {
            CheckAndBoardFromWaitingGrid();
        }

        if (currentBusIndex >= allBuses.Count)
        {
            busSystemState = BusSystemState.LevelComplete;
        }

        OnBusStateChanged();

        Debug.Log("=== BUS MOVEMENT COMPLETED ===");

        if (isWinTriggered)
        {
            yield return new WaitForSeconds(0.5f);
            WinLevel();
        }
    }

    void RemovePersonFromGrid(GridObject person)
    {
        if (person.gridCell != null)
        {
            person.gridCell.SetEmpty();
            person.gridCell = null;
        }

        if (allPeople.Contains(person))
        {
            allPeople.Remove(person);
        }
    }

    Vector3 GetBusPosition()
    {
        if (busActivePosition != null)
            return busActivePosition.position;

        if (currentActiveBus != null)
            return currentActiveBus.transform.position;

        return new Vector3(0, 1, 0);
    }

    public List<GridObject> GetPlayablePersons()
    {
        List<GridObject> playable = new List<GridObject>();

        foreach (var person in allPeople)
        {
            if (person.gridCell != null && person.gridCell.isPlayArea)
            {
                if (CanPersonMove(person))
                {
                    playable.Add(person);
                }
            }
        }

        return playable;
    }

    List<GridObject> GetAllPeopleInWaitingGrid()
    {
        var waitingPeople = new List<GridObject>();

        if (waitingGrid != null)
        {
            for (int i = 0; i < 10; i++)
            {
                PersonColor color = (PersonColor)i;
                var peopleOfColor = waitingGrid.GetPeopleByColor(color);
                waitingPeople.AddRange(peopleOfColor);
            }
        }

        return waitingPeople;
    }

    void CheckWinCondition()
    {
        int totalPeopleInBuses = peopleInBuses;
        int totalPeopleInWaiting = waitingGrid != null ? waitingGrid.GetOccupiedCount() : 0;
        int totalPeopleProcessed = totalPeopleInBuses + totalPeopleInWaiting;

        if (totalPeopleProcessed >= totalPeople)
        {

            if (totalPeopleInWaiting > 0)
            {
                Debug.Log($"Total people in buses: {totalPeopleInBuses}, Waiting: {totalPeopleInWaiting}");
                if (currentBus != null)
                {
                    int waitingForCurrentBus = waitingGrid.GetPeopleCountByColor(currentBus.color);
                    Debug.Log($"People waiting for {currentBus.color}: {waitingForCurrentBus}");

                    if (waitingForCurrentBus > 0)
                    {
                        Debug.Log("WIN CONDITION MET - TRIGGERING!");
                        TriggerWinAfterFinalBus();
                    }
                    else
                    {
                        Debug.Log("Wrong bus color, waiting...");
                    }
                }
            }
            else
            {
                TriggerWinAfterFinalBus();
            }
        }
        else
        {
            Debug.Log($"Not enough people processed: {totalPeopleProcessed}/{totalPeople}");
        }
    }

    void CheckLoseCondition()
    {
        if (isWinTriggered) return;
        if (waitingGrid != null && waitingGrid.IsFull())
        {
            LoseGame("Waiting grid is full!");
            return;
        }

        var playablePeople = GetPlayablePersons();
        int waitingCount = waitingGrid != null ? waitingGrid.GetOccupiedCount() : 0;

        if (playablePeople.Count == 0 && waitingCount > 0 && currentBusIndex >= allBuses.Count)
        {
            bool hasMatchingBusesForWaiting = CheckIfWaitingPeopleHaveMatchingBuses();

            if (!hasMatchingBusesForWaiting)
            {
                LoseGame("No more moves available!");
            }
        }
        else if (playablePeople.Count == 0 && waitingCount == 0 && peopleInBuses < totalPeople)
        {
            LoseGame("No more moves available!");
        }
    }

    /// <summary>
    /// Waiting grid'deki insanlar için gelecek otobüs var mı kontrol et
    /// </summary>
    bool CheckIfWaitingPeopleHaveMatchingBuses()
    {
        if (waitingGrid == null || waitingGrid.IsEmpty()) return true;

        var waitingColors = new HashSet<PersonColor>();

        var allWaitingPeople = GetAllPeopleInWaitingGrid();
        foreach (var person in allWaitingPeople)
        {
            waitingColors.Add(person.personColor);
        }

        for (int i = currentBusIndex; i < allBuses.Count; i++)
        {
            if (waitingColors.Contains(allBuses[i].color))
            {
                waitingColors.Remove(allBuses[i].color);
            }
        }

        bool allColorsHaveBuses = waitingColors.Count == 0;
        return allColorsHaveBuses;
    }

    /// <summary>
    /// Trigger win condition after the final bus has departed
    /// </summary>
    void TriggerWinAfterFinalBus()
    {
        if (isWinTriggered) return;

        isWinTriggered = true;

        if (TimerUI.Instance != null)
        {
            TimerUI.Instance.StopTimer();
        }
    }


    void WinLevel()
    {
        if (!isGameActive) return;

        Debug.LogError("WinLevel() CALLED FROM: " + System.Environment.StackTrace);

        isGameActive = false;

        if (TimerUI.Instance != null)
        {
            TimerUI.Instance.StopTimer();
        }

        StopAllCoroutines();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.levelCompleteSound);
        }

        OnGameMessage?.Invoke("Level Complete! All people are on buses!");

        if (PopupManager.Instance != null)
        {
            PopupManager.Instance.ShowWinPopup();
        }
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnGameManagerLevelWon();
        }
    }

    public void LoseGame(string reason)
    {
        if (!isGameActive) return;

        isGameActive = false;

        if (TimerUI.Instance != null)
        {
            TimerUI.Instance.StopTimer();
        }

        StopAllCoroutines();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.gameOverSound);
        }

        OnGameLost?.Invoke();
        OnGameMessage?.Invoke($"Game Over: {reason}");
    }
    public void LoadLevelData(LevelData levelData)
    {
        currentLevelData = levelData;

        if (gridManager != null)
        {
            gridManager.gridWidth = levelData.gridWidth;
            gridManager.gridHeight = levelData.gridHeight;

            if (levelData.playAreaData != null)
            {
                gridManager.LoadLevelPlayAreaData(levelData.playAreaData);
            }

            gridManager.ForceGridRecreation();
        }

        InitializeLevel();
        StartLevel();
    }

    public void RestartLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        levelNumber++;
        RestartLevel();
    }

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

    // Cache'i temizle
    public void ClearPlayableCache()
    {
        playableCache.Clear();
        needsPlayableUpdate = true;
    }

    // Manual bus sequence metodları
    [ContextMenu("Use Manual Bus Sequence")]
    public void UseManualBusSequence()
    {
        if (manualBusSequence.Count > 0)
        {
            Debug.Log("=== USING MANUAL BUS SEQUENCE ===");
            allBuses = new List<BusData>(manualBusSequence);

            for (int i = 0; i < allBuses.Count; i++)
            {
                Debug.Log($"  Manual Bus {i}: {allBuses[i].color} (Capacity: {allBuses[i].capacity})");
            }
        }
        else
        {
            Debug.LogWarning("Manual bus sequence is empty!");
        }
    }

    [ContextMenu("Debug Current Level Data")]
    public void DebugCurrentLevelData()
    {
        Debug.Log("=== CURRENT LEVEL DATA DEBUG ===");
        Debug.Log($"currentLevelData: {(currentLevelData != null ? "EXISTS" : "NULL")}");

        if (currentLevelData != null)
        {
            Debug.Log($"Grid Size: {currentLevelData.gridWidth}x{currentLevelData.gridHeight}");
            Debug.Log($"Objects: {currentLevelData.objects?.Count ?? 0}");
            Debug.Log($"Bus Sequence: {currentLevelData.busSequence?.Count ?? 0}");
            Debug.Log($"Play Area Data: {currentLevelData.playAreaData?.Count ?? 0}");

            if (currentLevelData.busSequence != null)
            {
                for (int i = 0; i < currentLevelData.busSequence.Count; i++)
                {
                    var bus = currentLevelData.busSequence[i];
                    Debug.Log($"  Level Bus {i}: {bus.color} (Capacity: {bus.capacity})");
                }
            }
        }

        Debug.Log("=== CURRENT LEVEL DATA DEBUG END ===");
    }

    [ContextMenu("Quick Test Bus Sequence")]
    public void QuickTestBusSequence()
    {
        Debug.Log("=== QUICK TEST BUS SEQUENCE ===");

        allBuses.Clear();

        // Örnek sıra: Kırmızı → Mavi → Yeşil
        allBuses.Add(new BusData(PersonColor.Red, 3));
        allBuses.Add(new BusData(PersonColor.Blue, 4));
        allBuses.Add(new BusData(PersonColor.Green, 2));

        Debug.Log("Test bus sequence created:");
        for (int i = 0; i < allBuses.Count; i++)
        {
            Debug.Log($"  Test Bus {i}: {allBuses[i].color} (Capacity: {allBuses[i].capacity})");
        }

        Debug.Log("=== QUICK TEST COMPLETED ===");
    }

    [ContextMenu("Debug Bus Prefabs")]
    public void DebugBusPrefabs()
    {
        Debug.Log("=== BUS PREFABS DEBUG ===");

        for (int i = 0; i < busPrefabs.Length; i++)
        {
            PersonColor color = (PersonColor)i;
            string status = busPrefabs[i] != null ? "LOADED" : "MISSING";
            string prefabName = busPrefabs[i] != null ? busPrefabs[i].name : "NULL";

            Debug.Log($"  {color} ({i}): {status} - {prefabName}");
        }

        Debug.Log("=== BUS PREFABS DEBUG END ===");
    }

    [ContextMenu("Debug Bus Positions")]
    public void DebugBusPositions()
    {
        Debug.Log("=== BUS POSITIONS DEBUG ===");
        Debug.Log($"Spawn Point: {(busSpawnPoint != null ? busSpawnPoint.position : "NULL")}");
        Debug.Log($"Active Position: {(busActivePosition != null ? busActivePosition.position : "NULL")}");
        Debug.Log($"Waiting Position: {(busWaitingPosition != null ? busWaitingPosition.position : "NULL")}");
        Debug.Log($"Exit Point: {(busExitPoint != null ? busExitPoint.position : "NULL")}");
        Debug.Log($"Current Active Bus: {(currentActiveBus != null ? currentActiveBus.name : "NULL")}");
        Debug.Log($"Current Waiting Bus: {(currentWaitingBus != null ? currentWaitingBus.name : "NULL")}");
        Debug.Log($"Bus System State: {busSystemState}");
        Debug.Log($"Current Bus Index: {currentBusIndex}/{allBuses.Count}");
    }

    void OnDrawGizmosSelected()
    {
        if (busSpawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(busSpawnPoint.position, Vector3.one * 2f);
        }

        if (busActivePosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(busActivePosition.position, Vector3.one * 2f);
        }

        if (busWaitingPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(busWaitingPosition.position, Vector3.one * 2f);
        }

        if (busExitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(busExitPoint.position, Vector3.one * 2f);
        }

        if (busSpawnPoint != null && busActivePosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(busSpawnPoint.position, busActivePosition.position);
        }

        if (busActivePosition != null && busExitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(busActivePosition.position, busExitPoint.position);
        }

        if (busSpawnPoint != null && busWaitingPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(busSpawnPoint.position, busWaitingPosition.position);
        }

        if (busWaitingPosition != null && busActivePosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(busWaitingPosition.position, busActivePosition.position);
        }
    }
}