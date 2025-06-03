using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Kaan Çakar 2025 - GridInputHandler.cs
/// Handles user input for selecting and interacting with grid cells.
/// </summary>
public class GridInputHandler : MonoBehaviour
{
    [Header("Input Settings")]
    public LayerMask gridLayerMask = -1;
    public float touchSensitivity = 1f;

    [Header("Events")]
    public UnityEvent<GridCell> OnGridCellSelected;
    public UnityEvent<Vector2Int> OnGridPositionSelected;

    private Camera mainCamera;
    private GridManager gridManager;
    private GridCell lastSelectedCell;
    private List<GridCell> highlightedCells = new List<GridCell>();

    public static GridInputHandler Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();

        gridManager = GridManager.Instance;
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameActive)
        {
            return;
        }

        // Mobile touch control
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                ProcessTouch(touch.position);
            }
        }

        // Mouse control for editor and PC
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            ProcessTouch(Input.mousePosition);
        }
#endif
    }

    void ProcessTouch(Vector2 screenPosition)
    {
        if (mainCamera == null || gridManager == null) return;

        // Detect grid position with raycast
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayerMask))
        {
            // Get grid position
            Vector2Int gridPos = gridManager.GetGridPosition(hit.point);

            if (gridManager.IsValidPosition(gridPos))
            {
                GridCell selectedCell = gridManager.GetCell(gridPos);

                if (selectedCell != null)
                {
                    SelectCell(selectedCell);
                }
            }
        }
    }

    public void SelectCell(GridCell cell)
    {
        if (cell == null) return;

        // Clear previous selection
        ClearHighlights();

        // Select new cell
        lastSelectedCell = cell;

        // Trigger events
        OnGridCellSelected?.Invoke(cell);
        OnGridPositionSelected?.Invoke(cell.GetGridPosition());

        // Highlight selected cell
        HighlightCell(cell, Color.green);

        Debug.Log($"Selected grid cell: ({cell.x}, {cell.z})");
    }

    public void OnCellClicked(GridCell cell)
    {
        SelectCell(cell);
    }

    public void HighlightCell(GridCell cell, Color color)
    {
        if (cell == null) return;

        cell.HighlightCell(true, color);

        if (!highlightedCells.Contains(cell))
        {
            highlightedCells.Add(cell);
        }
    }

    public void HighlightCells(List<GridCell> cells, Color color)
    {
        foreach (var cell in cells)
        {
            HighlightCell(cell, color);
        }
    }

    public void ClearHighlights()
    {
        foreach (var cell in highlightedCells)
        {
            if (cell != null)
            {
                cell.HighlightCell(false);
            }
        }
        highlightedCells.Clear();
    }

    public void ShowPossibleMoves(GridCell fromCell, bool includeDiagonals = false)
    {
        if (fromCell == null || gridManager == null) return;

        ClearHighlights();

        // Get neighbors and highlight movable ones
        var neighbors = gridManager.GetNeighbors(fromCell.x, fromCell.z, includeDiagonals);

        foreach (var neighbor in neighbors)
        {
            if (neighbor.CanMoveTo())
            {
                HighlightCell(neighbor, Color.cyan);
            }
        }
    }

    public GridCell GetLastSelectedCell()
    {
        return lastSelectedCell;
    }

    // Programmatically select a specific grid position
    public void SelectGridPosition(Vector2Int gridPos)
    {
        if (gridManager != null)
        {
            GridCell cell = gridManager.GetCell(gridPos);
            if (cell != null)
            {
                SelectCell(cell);
            }
        }
    }

    // Show path on grid
    public void ShowPath(List<GridCell> path, Color pathColor)
    {
        ClearHighlights();

        for (int i = 0; i < path.Count; i++)
        {
            Color cellColor = pathColor;

            // Show start and end points in different colors
            if (i == 0)
                cellColor = Color.green; // Start
            else if (i == path.Count - 1)
                cellColor = Color.red;   // End

            HighlightCell(path[i], cellColor);
        }
    }
}