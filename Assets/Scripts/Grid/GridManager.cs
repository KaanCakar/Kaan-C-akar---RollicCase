using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Kaan Çakar 2025 - GridManager.cs
/// GridManager is responsible for managing a grid of cells in the game.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public Vector3 gridOffset = Vector3.zero;

    [Header("Visualization")]
    public bool showGrid = true;
    public Color gridColor = Color.green;
    public Material gridMaterial;

    [Header("Prefabs")]
    public GameObject gridCellPrefab;

    [Header("Editor Tools")]
    [SerializeField] private bool autoCreateGrid = true;
    [SerializeField] private bool recreateGridOnChange = true;

    [Header("Runtime Play Area Data")]
    [SerializeField] public List<PlayAreaCellData> runtimePlayAreaData = new List<PlayAreaCellData>();

    private GridCell[,] grid;
    private Dictionary<Vector2Int, GridCell> gridDictionary;
    private Camera mainCamera;
    public bool gridInitialized = false;

    public static GridManager Instance { get; private set; }

    // Public Properties
    public bool IsGridInitialized => gridInitialized;
    public GridCell[,] Grid => grid;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Runtime'da grid'i oluştur
            if (Application.isPlaying)
            {
                // ÖNCE mevcut grid objelerini kontrol et
                if (HasExistingGridObjects())
                {
                    Debug.Log("Found existing grid objects from Editor, using them for runtime");
                    InitializeGridFromExistingObjects();
                }
                else
                {
                    Debug.Log("No existing grid objects found, creating new grid");
                    InitializeGrid();
                }
            }
        }
        else if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Editor'da grid ayarları değiştiğinde yeniden oluştur
        if (!Application.isPlaying && recreateGridOnChange && autoCreateGrid)
        {
            // OnValidate çok sık çağrıldığı için bir frame gecikmeli çağır
            EditorApplication.delayCall += () =>
            {
                if (this != null) // Obje hala varsa
                {
                    CreateGridInEditor();
                }
            };
        }
    }

    // Editor modunda grid oluşturma metodu
    [ContextMenu("Create Grid in Editor")]
    public void CreateGridInEditor()
    {
        if (Application.isPlaying) return;

        // Eski grid objelerini temizle
        ClearExistingGrid();

        InitializeGrid();

        Debug.Log($"Grid created in Editor: {gridWidth}x{gridHeight}");

        // Scene'i dirty yap
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(gameObject);
    }

    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        if (Application.isPlaying) return;

        ClearExistingGrid();
        gridInitialized = false;

        Debug.Log("Grid cleared");

        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(gameObject);
    }

    [ContextMenu("Save Current Play Area State")]
    public void SaveCurrentPlayAreaState()
    {
        if (!gridInitialized)
        {
            Debug.LogWarning("Grid not initialized, cannot save play area state");
            return;
        }

        runtimePlayAreaData.Clear();

        int visibleCount = 0;
        int invisibleCount = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                GridCell cell = GetCell(x, z);
                if (cell != null)
                {
                    runtimePlayAreaData.Add(new PlayAreaCellData
                    {
                        x = x,
                        z = z,
                        isPlayArea = cell.isPlayArea,
                        isVisible = cell.isVisible
                    });

                    if (cell.isVisible && cell.isPlayArea)
                        visibleCount++;
                    else
                        invisibleCount++;
                }
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"Saved play area state: {runtimePlayAreaData.Count} total cells");
        Debug.Log($"Visible play areas: {visibleCount}, Hidden/Erased: {invisibleCount}");
    }

    private void ClearExistingGrid()
    {
        // Eski grid objelerini bul ve sil
        List<Transform> childrenToDelete = new List<Transform>();

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("GridCell_"))
            {
                childrenToDelete.Add(child);
            }
        }

        Debug.Log($"Clearing {childrenToDelete.Count} existing grid objects");

        foreach (Transform child in childrenToDelete)
        {
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
#endif

    // Mevcut grid objelerinin varlığını kontrol et
    private bool HasExistingGridObjects()
    {
        int gridObjectCount = 0;
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("GridCell_"))
            {
                gridObjectCount++;
            }
        }

        Debug.Log($"Found {gridObjectCount} existing grid objects");
        return gridObjectCount > 0;
    }

    // Mevcut objelerden grid'i oluştur
    private void InitializeGridFromExistingObjects()
    {
        Debug.Log($"=== InitializeGridFromExistingObjects Started ===");

        // Grid array'ini oluştur
        grid = new GridCell[gridWidth, gridHeight];
        gridDictionary = new Dictionary<Vector2Int, GridCell>();

        // Önce tüm hücreleri oluştur (obje olmadan)
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPos = GetWorldPosition(x, z);
                GridCell cell = new GridCell(x, z, worldPos, null);
                grid[x, z] = cell;
                gridDictionary[new Vector2Int(x, z)] = cell;
            }
        }

        // Play area data'yı yükle
        LoadRuntimePlayAreaData();

        // Mevcut grid objelerini grid cell'lere bağla
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("GridCell_"))
            {
                // Obje adından koordinatları çıkar (GridCell_x_z formatında)
                string[] parts = child.name.Split('_');
                if (parts.Length == 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int z))
                {
                    if (IsValidPosition(x, z))
                    {
                        GridCell cell = grid[x, z];

                        // Eğer bu hücre görünür olmalıysa objeyi kullan
                        if (cell.isVisible && cell.isPlayArea)
                        {
                            cell.cellObject = child.gameObject;

                            // GridCellComponent'i kontrol et/ekle
                            var cellComponent = child.GetComponent<GridCellComponent>();
                            if (cellComponent == null)
                                cellComponent = child.gameObject.AddComponent<GridCellComponent>();
                            cellComponent.Initialize(cell);

                            child.gameObject.SetActive(true);
                            Debug.Log($"Connected existing object to cell ({x}, {z}) - Visible");
                        }
                        else
                        {
                            // Bu hücre görünmez olmalı - objeyi deaktive et
                            child.gameObject.SetActive(false);
                            Debug.Log($"Deactivated existing object at ({x}, {z}) - Not in play area");
                        }
                    }
                }
            }
        }

        gridInitialized = true;

        // Mevcut GridObject'ları grid cell'lere otomatik bağla
        AssignExistingObjectsToGridCells();

        Debug.Log($"=== Grid initialization from existing objects completed ===");
    }

    public void InitializeGrid()
    {
        Debug.Log($"=== InitializeGrid Started ===");
        Debug.Log($"Current state: gridInitialized={gridInitialized}, gridWidth={gridWidth}, gridHeight={gridHeight}");

        // Force clear previous grid if exists
        if (gridInitialized)
        {
            Debug.Log("Clearing existing grid...");
            gridInitialized = false;
            grid = null;
            gridDictionary = null;
        }

        // Boyut kontrolü
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogError($"Invalid grid dimensions: {gridWidth}x{gridHeight}");
            return;
        }

        Debug.Log($"Creating new grid array: {gridWidth}x{gridHeight}");

        // Yeni grid oluştur
        grid = new GridCell[gridWidth, gridHeight];
        gridDictionary = new Dictionary<Vector2Int, GridCell>();

        // Grid hücrelerini oluştur
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPos = GetWorldPosition(x, z);

                // GridCell component'ini her durumda oluştur
                GridCell cell = new GridCell(x, z, worldPos, null);

                // Array'e ata
                grid[x, z] = cell;
                gridDictionary[new Vector2Int(x, z)] = cell;
            }
        }

        // ÖNCE play area data'yı yükle (cell state'lerini ayarlamak için)
        if (Application.isPlaying)
        {
            LoadRuntimePlayAreaData();
        }

        // SONRA sadece visible olan hücreler için 3D obje oluştur
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                GridCell cell = grid[x, z];

                // Sadece visible olan hücreler için 3D obje oluştur
                bool shouldCreateObject = true;

                if (Application.isPlaying)
                {
                    // Runtime'da sadece visible hücreler için obje oluştur
                    shouldCreateObject = cell.isVisible && cell.isPlayArea;
                }

                if (shouldCreateObject && gridCellPrefab != null)
                {
                    GameObject cellObj = null;
                    Vector3 worldPos = GetWorldPosition(x, z);

                    try
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            // Editor modunda PrefabUtility kullan
                            cellObj = UnityEditor.PrefabUtility.InstantiatePrefab(gridCellPrefab, transform) as GameObject;
                        }
                        else
#endif
                        {
                            // Runtime'da normal Instantiate kullan
                            cellObj = Instantiate(gridCellPrefab, worldPos, Quaternion.identity, transform);
                        }

                        if (cellObj != null)
                        {
                            cellObj.transform.position = worldPos;
                            cellObj.name = $"GridCell_{x}_{z}";

                            // Cell objesini GridCell'e bağla
                            cell.cellObject = cellObj;

                            // GridCellComponent ekle
                            var cellComponent = cellObj.GetComponent<GridCellComponent>();
                            if (cellComponent == null)
                                cellComponent = cellObj.AddComponent<GridCellComponent>();
                            cellComponent.Initialize(cell);

                            Debug.Log($"Cell object created for ({x}, {z}) - Visible: {cell.isVisible}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to create cell object at ({x}, {z}): {e.Message}");
                    }
                }
                else if (!shouldCreateObject)
                {
                    Debug.Log($"Skipped creating cell object for ({x}, {z}) - Not visible in play area");
                }
            }
        }

        gridInitialized = true;

        // ÖNEMLİ: Mevcut GridObject'ları grid cell'lere otomatik bağla
        AssignExistingObjectsToGridCells();

        Debug.Log($"=== Grid initialization completed ===");
        Debug.Log($"Final state: gridInitialized={gridInitialized}, Total cells: {gridWidth * gridHeight}");

        // Editor'da dirty yap
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>
    /// Enhanced debug version of assignment method
    /// </summary>
    private void AssignExistingObjectsToGridCells()
    {
        Debug.Log("=== DEBUGGING GRID ASSIGNMENT ===");

        // Sahnedeki tüm GridObject'ları bul
        GridObject[] allGridObjects = FindObjectsOfType<GridObject>();
        Debug.Log($"Found {allGridObjects.Length} GridObjects in scene");

        if (allGridObjects.Length == 0)
        {
            Debug.LogWarning("No GridObjects found in scene!");
            return;
        }

        int assignedCount = 0;
        int skippedCount = 0;

        foreach (GridObject gridObj in allGridObjects)
        {
            Debug.Log($"Processing GridObject: {gridObj.name} ({gridObj.personColor})");
            Debug.Log($"  Current position: {gridObj.transform.position}");
            Debug.Log($"  Current gridCell: {(gridObj.gridCell != null ? "ASSIGNED" : "NULL")}");

            // Zaten cell'e atanmışsa geç
            if (gridObj.gridCell != null)
            {
                Debug.Log($"  ✅ {gridObj.personColor} already has gridCell assigned");
                continue;
            }

            // Objenin dünya pozisyonunu grid pozisyonuna çevir
            Vector2Int gridPos = GetGridPosition(gridObj.transform.position);
            Debug.Log($"  Calculated grid position: ({gridPos.x}, {gridPos.y})");

            // Geçerli pozisyon mu kontrol et
            if (IsValidPosition(gridPos))
            {
                GridCell cell = GetCell(gridPos);
                Debug.Log($"  Cell found: {(cell != null ? "YES" : "NO")}");

                if (cell != null)
                {
                    Debug.Log($"  Cell isPlayArea: {cell.isPlayArea}");
                    Debug.Log($"  Cell isVisible: {cell.isVisible}");
                    Debug.Log($"  Cell IsOccupied: {cell.IsOccupied}");
                }

                if (cell != null && cell.isPlayArea)
                {
                    // GridObject'ı cell'e ata
                    gridObj.gridCell = cell;

                    // Cell'i işgal edilmiş olarak işaretle
                    cell.SetOccupied(gridObj.gameObject);

                    assignedCount++;
                    Debug.Log($"  ✅ ASSIGNED {gridObj.personColor} to grid cell ({gridPos.x}, {gridPos.y})");
                }
                else
                {
                    skippedCount++;
                    string reason = cell == null ? "CELL IS NULL" :
                                   !cell.isPlayArea ? "NOT IN PLAY AREA" : "UNKNOWN";
                    Debug.LogWarning($"  ⚠️ SKIPPED {gridObj.personColor} at ({gridPos.x}, {gridPos.y}) - Reason: {reason}");
                }
            }
            else
            {
                skippedCount++;
                Debug.LogWarning($"  ❌ INVALID POSITION {gridObj.personColor} at ({gridPos.x}, {gridPos.y}) - Outside grid bounds");
            }

            Debug.Log($"  --- End processing {gridObj.name} ---");
        }

        Debug.Log($"=== ASSIGNMENT COMPLETED: {assignedCount} assigned, {skippedCount} skipped ===");
    }

    /// <summary>
    /// Manual assignment method for editor or debugging
    /// </summary>
    [ContextMenu("Assign All Objects to Grid Cells")]
    public void ManualAssignObjectsToGridCells()
    {
        if (!gridInitialized)
        {
            Debug.LogWarning("Grid not initialized! Initialize grid first.");
            return;
        }

        AssignExistingObjectsToGridCells();
    }

    // Runtime'da play area data'yı yükle
    private void LoadRuntimePlayAreaData()
    {
        if (runtimePlayAreaData == null || runtimePlayAreaData.Count == 0)
        {
            Debug.Log("No runtime play area data found, using default (all visible)");
            // DEFAULT: Tüm hücreleri play area yap
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    GridCell cell = GetCell(x, z);
                    if (cell != null)
                    {
                        cell.SetPlayArea(true);
                        cell.SetVisible(true);
                    }
                }
            }
            return;
        }

        Debug.Log($"Loading runtime play area data: {runtimePlayAreaData.Count} entries");

        // Grid cell'lerin state'ini ayarla (3D obje oluşturmadan önce)
        foreach (var areaData in runtimePlayAreaData)
        {
            if (areaData.x >= 0 && areaData.x < gridWidth && areaData.z >= 0 && areaData.z < gridHeight)
            {
                GridCell cell = grid[areaData.x, areaData.z]; // Direkt array access
                if (cell != null)
                {
                    cell.isPlayArea = areaData.isPlayArea;
                    cell.isVisible = areaData.isVisible;

                    Debug.Log($"Set cell ({areaData.x}, {areaData.z}): playArea={areaData.isPlayArea}, visible={areaData.isVisible}");
                }
            }
        }

        Debug.Log("Runtime play area data loaded successfully");
    }

    // ForceGridRecreation metodunu da güncelle:
    public void ForceGridRecreation()
    {
        Debug.Log("Force recreating grid...");

#if UNITY_EDITOR
        // Editor'da mevcut play area state'ini kaydet
        if (!Application.isPlaying && gridInitialized)
        {
            SaveCurrentPlayAreaState();
        }
#endif

        gridInitialized = false;

#if UNITY_EDITOR
        // Eski cell objelerini temizle
        if (!Application.isPlaying)
        {
            ClearExistingGrid();
        }
#endif

        InitializeGrid();
    }

    // Level data yükleme metodu - GameManager için
    public void LoadLevelPlayAreaData(List<PlayAreaCellData> levelPlayAreaData)
    {
        if (levelPlayAreaData == null)
        {
            Debug.LogWarning("Level play area data is null");
            return;
        }

        // Runtime data'yı güncelle
        runtimePlayAreaData = new List<PlayAreaCellData>(levelPlayAreaData);

        Debug.Log($"Loaded level play area data: {runtimePlayAreaData.Count} entries");

        // Eğer grid zaten initialize edilmişse, data'yı uygula
        if (gridInitialized)
        {
            LoadRuntimePlayAreaData();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }

    // === PATHFINDING SYSTEM ===

    /// <summary>
    /// Finds shortest path from start position to any exit point
    /// FIXED: Special handling for people already in exit row
    /// </summary>
    /// <param name="startPos">Starting position</param>
    /// <returns>List of grid positions forming the path, or null if no path exists</returns>
    public List<Vector2Int> FindPathToExit(Vector2Int startPos)
    {
        if (!IsValidPosition(startPos))
        {
            Debug.LogWarning($"Invalid start position: {startPos}");
            return null;
        }

        // SPECIAL CASE: If person is already in the exit row (front row)
        int frontRowZ = gridHeight - 1;
        if (startPos.y == frontRowZ)
        {
            // Check if there's at least one empty exit point in the same row
            List<Vector2Int> exitPoints = GetExitPoints();

            if (exitPoints.Count > 0)
            {
                // Person is in exit row and there are empty exits -> can move to bus
                Vector2Int nearestExit = GetNearestExitPoint(startPos, exitPoints);

                // Create simple path: current position -> nearest exit
                List<Vector2Int> exitRowPath = new List<Vector2Int> { startPos, nearestExit };
                Debug.Log($"Exit row person at {startPos} can move to {nearestExit}");
                return exitRowPath;
            }
            else
            {
                Debug.Log($"Exit row person at {startPos} has no available exits");
                return null;
            }
        }

        // NORMAL CASE: Person is not in exit row, use normal pathfinding
        List<Vector2Int> allExitPoints = GetExitPoints();

        if (allExitPoints.Count == 0)
        {
            Debug.LogWarning("No exit points found!");
            return null;
        }

        // Use BFS to find shortest path to any exit
        return BFS_FindPath(startPos, allExitPoints);
    }

    /// <summary>
    /// Gets nearest exit point to a given position
    /// </summary>
    /// <param name="fromPos">Starting position</param>
    /// <param name="exitPoints">Available exit points</param>
    /// <returns>Nearest exit point</returns>
    private Vector2Int GetNearestExitPoint(Vector2Int fromPos, List<Vector2Int> exitPoints)
    {
        if (exitPoints.Count == 0)
            return Vector2Int.zero;

        if (exitPoints.Count == 1)
            return exitPoints[0];

        // Find the closest exit point (Manhattan distance)
        Vector2Int nearest = exitPoints[0];
        int minDistance = Mathf.Abs(fromPos.x - nearest.x) + Mathf.Abs(fromPos.y - nearest.y);

        for (int i = 1; i < exitPoints.Count; i++)
        {
            Vector2Int exit = exitPoints[i];
            int distance = Mathf.Abs(fromPos.x - exit.x) + Mathf.Abs(fromPos.y - exit.y);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = exit;
            }
        }

        Debug.Log($"Nearest exit to {fromPos} is {nearest} (distance: {minDistance})");
        return nearest;
    }

    /// <summary>
    /// Enhanced exit point detection - excludes occupied cells in exit row
    /// </summary>
    /// <returns>List of exit positions</returns>
    private List<Vector2Int> GetExitPoints()
    {
        List<Vector2Int> exitPoints = new List<Vector2Int>();

        // Front row is the one with highest Z coordinate (closest to bus)
        int frontRowZ = gridHeight - 1;

        for (int x = 0; x < gridWidth; x++)
        {
            Vector2Int pos = new Vector2Int(x, frontRowZ);
            GridCell cell = GetCell(pos);

            // Exit point must be: valid, play area, walkable, and NOT OCCUPIED
            if (cell != null && cell.isPlayArea && cell.IsWalkable && !cell.IsOccupied)
            {
                exitPoints.Add(pos);
            }
        }

        Debug.Log($"Found {exitPoints.Count} exit points at row {frontRowZ}");
        return exitPoints;
    }

    /// <summary>
    /// BFS pathfinding to find shortest path to any of the target positions
    /// </summary>
    /// <param name="start">Start position</param>
    /// <param name="targets">Target positions (exit points)</param>
    /// <returns>Path as list of positions, or null if no path exists</returns>
    private List<Vector2Int> BFS_FindPath(Vector2Int start, List<Vector2Int> targets)
    {
        // BFS data structures
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Initialize BFS
        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = start; // Start came from itself

        // 4-directional movement (no diagonals)
        Vector2Int[] directions = {
            Vector2Int.up,    // North (z+1)
            Vector2Int.down,  // South (z-1)  
            Vector2Int.left,  // West (x-1)
            Vector2Int.right  // East (x+1)
        };

        // BFS main loop
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Check if we reached any target
            if (targets.Contains(current))
            {
                Debug.Log($"Path found! Reached exit at {current}");
                return ReconstructPath(cameFrom, start, current);
            }

            // Explore neighbors
            foreach (var direction in directions)
            {
                Vector2Int neighbor = current + direction;

                // Skip if already visited
                if (visited.Contains(neighbor))
                    continue;

                // Check if neighbor is walkable
                if (IsWalkableForPathfinding(neighbor))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        // No path found
        Debug.Log($"No path found from {start} to any exit point");
        return null;
    }

    /// <summary>
    /// Checks if a position is walkable for pathfinding purposes
    /// </summary>
    /// <param name="pos">Position to check</param>
    /// <returns>True if walkable</returns>
    private bool IsWalkableForPathfinding(Vector2Int pos)
    {
        // Must be valid position
        if (!IsValidPosition(pos))
            return false;

        GridCell cell = GetCell(pos);
        if (cell == null)
            return false;

        // Must be in play area
        if (!cell.isPlayArea || !cell.isVisible)
            return false;

        // Must be walkable and not occupied
        return cell.IsWalkable && !cell.IsOccupied;
    }

    /// <summary>
    /// Reconstructs the path from BFS result
    /// </summary>
    /// <param name="cameFrom">BFS parent tracking</param>
    /// <param name="start">Start position</param>
    /// <param name="end">End position</param>
    /// <returns>Path from start to end</returns>
    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = end;

        // Trace back from end to start
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Add(start);
        path.Reverse(); // Reverse to get start -> end

        Debug.Log($"Reconstructed path with {path.Count} steps: {string.Join(" -> ", path)}");
        return path;
    }

    /// <summary>
    /// Quick check if a person can move (has path to exit)
    /// ENHANCED: Better logging and edge case handling
    /// </summary>
    /// <param name="personPos">Person's grid position</param>
    /// <returns>True if person can reach an exit</returns>
    public bool CanPersonReachExit(Vector2Int personPos)
    {
        var path = FindPathToExit(personPos);
        bool canReach = path != null && path.Count >= 1; // At least the person's current position

        Debug.Log($"Person at ({personPos.x}, {personPos.y}) can reach exit: {canReach} (path length: {path?.Count ?? 0})");
        return canReach;
    }

    /// <summary>
    /// Debug visualization of pathfinding
    /// </summary>
    /// <param name="path">Path to visualize</param>
    public void DebugDrawPath(List<Vector2Int> path)
    {
        if (path == null || path.Count < 2) return;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 from = GetWorldPosition(path[i].x, path[i].y) + Vector3.up * 0.5f;
            Vector3 to = GetWorldPosition(path[i + 1].x, path[i + 1].y) + Vector3.up * 0.5f;

            Debug.DrawLine(from, to, Color.yellow, 2f);
        }
    }

    // Grid koordinatlarını dünya pozisyonuna çevir
    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x * cellSize, 0, z * cellSize) + gridOffset;
    }

    // Dünya pozisyonunu grid koordinatlarına çevir
    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - gridOffset;
        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int z = Mathf.RoundToInt(localPos.z / cellSize);
        return new Vector2Int(x, z);
    }

    // Grid cell'ini al
    public GridCell GetCell(int x, int z)
    {
        // NULL CHECK: Grid initialize edilmiş mi?
        if (grid == null || !gridInitialized)
        {
            Debug.LogWarning("Grid is not initialized yet. Call InitializeGrid() first.");
            return null;
        }

        if (IsValidPosition(x, z))
            return grid[x, z];
        return null;
    }

    public GridCell GetCell(Vector2Int gridPos)
    {
        return GetCell(gridPos.x, gridPos.y);
    }

    // Pozisyon geçerli mi kontrol et
    public bool IsValidPosition(int x, int z)
    {
        // NULL CHECK: Grid boyutları geçerli mi?
        if (grid == null)
            return false;

        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }

    public bool IsValidPosition(Vector2Int gridPos)
    {
        return IsValidPosition(gridPos.x, gridPos.y);
    }

    // Cell boş mu kontrol et
    public bool IsCellEmpty(int x, int z)
    {
        GridCell cell = GetCell(x, z);
        return cell != null && !cell.IsOccupied && cell.IsWalkable;
    }

    public bool IsCellEmpty(Vector2Int gridPos)
    {
        return IsCellEmpty(gridPos.x, gridPos.y);
    }

    // Raycast ile grid pozisyonu al (mobil dokunma için)
    public Vector2Int? GetGridPositionFromScreenPoint(Vector2 screenPoint)
    {
        if (mainCamera == null) return null;

        Ray ray = mainCamera.ScreenPointToRay(screenPoint);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector2Int gridPos = GetGridPosition(hit.point);
            if (IsValidPosition(gridPos))
                return gridPos;
        }

        return null;
    }

    // Komşu hücreleri al (hareket için)
    public List<GridCell> GetNeighbors(int x, int z, bool includeDiagonals = false)
    {
        List<GridCell> neighbors = new List<GridCell>();

        // 4 yönlü komşular
        Vector2Int[] directions = {
            Vector2Int.up,    // Kuzey
            Vector2Int.right, // Doğu
            Vector2Int.down,  // Güney
            Vector2Int.left   // Batı
        };

        foreach (var dir in directions)
        {
            int newX = x + dir.x;
            int newZ = z + dir.y;

            GridCell neighbor = GetCell(newX, newZ);
            if (neighbor != null)
                neighbors.Add(neighbor);
        }

        // Çapraz komşular (isteğe bağlı)
        if (includeDiagonals)
        {
            Vector2Int[] diagonalDirections = {
                new Vector2Int(1, 1),   // Kuzeydoğu
                new Vector2Int(-1, 1),  // Kuzeybatı
                new Vector2Int(1, -1),  // Güneydoğu
                new Vector2Int(-1, -1)  // Güneybatı
            };

            foreach (var dir in diagonalDirections)
            {
                int newX = x + dir.x;
                int newZ = z + dir.y;

                GridCell neighbor = GetCell(newX, newZ);
                if (neighbor != null)
                    neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public List<GridCell> GetNeighbors(Vector2Int gridPos, bool includeDiagonals = false)
    {
        return GetNeighbors(gridPos.x, gridPos.y, includeDiagonals);
    }

    // Grid'i görselleştir
    void OnDrawGizmos()
    {
        if (!showGrid) return;

        Gizmos.color = gridColor;

        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = GetWorldPosition(x, 0);
            Vector3 end = GetWorldPosition(x, gridHeight);
            Gizmos.DrawLine(start, end);
        }

        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = GetWorldPosition(0, z);
            Vector3 end = GetWorldPosition(gridWidth, z);
            Gizmos.DrawLine(start, end);
        }

        // Grid initialized değilse wireframe göster
        if (!gridInitialized)
        {
            Gizmos.color = Color.red;
            Vector3 center = GetWorldPosition(gridWidth / 2, gridHeight / 2);
            Gizmos.DrawWireCube(center, new Vector3(gridWidth * cellSize, 0.1f, gridHeight * cellSize));
        }
    }

    // Grid durumunu kontrol etme metodu
    public bool ValidateGrid()
    {
        if (!gridInitialized) return false;
        if (grid == null) return false;
        if (grid.GetLength(0) != gridWidth || grid.GetLength(1) != gridHeight) return false;

        return true;
    }
}