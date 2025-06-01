using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Kaan Çakar 2025 - GridManager.cs
/// Manages a grid of cells for pathfinding and movement.
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
                InitializeGrid();
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

                Debug.Log($"Creating cell at ({x}, {z}) - WorldPos: {worldPos}");

                // Grid cell objesini oluştur (optional)
                GameObject cellObj = null;

                // GridCell component'ini her durumda oluştur
                GridCell cell = new GridCell(x, z, worldPos, cellObj);

                // Array'e ata
                grid[x, z] = cell;
                gridDictionary[new Vector2Int(x, z)] = cell;

                // Prefab varsa 3D obje de oluştur
                if (gridCellPrefab != null)
                {
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

                            Debug.Log($"Cell object created successfully for ({x}, {z})");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to create cell object at ({x}, {z}): {e.Message}");
                        // Cell object olmasa da GridCell'i oluştur
                    }
                }
                else
                {
                    Debug.LogWarning("gridCellPrefab is null, creating cells without 3D objects");
                }
            }
        }

        gridInitialized = true;
        Debug.Log($"=== Grid initialization completed ===");
        Debug.Log($"Final state: gridInitialized={gridInitialized}, Total cells: {gridWidth * gridHeight}");

        // Test: İlk birkaç cell'i kontrol et
        for (int x = 0; x < Mathf.Min(3, gridWidth); x++)
        {
            for (int z = 0; z < Mathf.Min(3, gridHeight); z++)
            {
                GridCell testCell = grid[x, z];
                Debug.Log($"Test cell ({x}, {z}): {(testCell != null ? "OK" : "NULL")}");
            }
        }

        // Editor'da dirty yap
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    // ForceGridRecreation metodunu da güncelle:
    public void ForceGridRecreation()
    {
        Debug.Log("Force recreating grid...");
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
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(GridManager))]
    public class GridManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GridManager gridManager = (GridManager)target;

            UnityEditor.EditorGUILayout.Space(10);
            UnityEditor.EditorGUILayout.LabelField("Grid Controls", UnityEditor.EditorStyles.boldLabel);

            if (!gridManager.IsGridInitialized)
            {
                if (GUILayout.Button("Initialize Grid"))
                {
                    gridManager.InitializeGrid();
                    UnityEditor.EditorUtility.SetDirty(gridManager);
                }
            }
            else
            {
                UnityEditor.EditorGUILayout.LabelField($"Grid Status: Initialized ({gridManager.gridWidth}x{gridManager.gridHeight})");

                if (GUILayout.Button("Force Recreate Grid"))
                {
                    gridManager.ForceGridRecreation();
                    UnityEditor.EditorUtility.SetDirty(gridManager);
                }
            }
        }
    }
#endif
}