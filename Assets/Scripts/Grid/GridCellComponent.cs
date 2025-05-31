using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kaan Çakar 2025 - GridCellComponent.cs
/// Represents a single cell in the grid with visual and interaction capabilities.
/// </summary>

public class GridCellComponent : MonoBehaviour
{
    [Header("References")]
    public GridCell gridCell;

    [Header("Events")]
    public UnityEvent<GridCell> OnCellClicked;
    public UnityEvent<GridCell> OnCellHovered;

    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color blockedColor = Color.red;
    public Color occupiedColor = Color.blue;

    private Renderer cellRenderer;
    private Collider cellCollider;
    private bool isHovered = false;

    void Awake()
    {
        cellRenderer = GetComponent<Renderer>();
        cellCollider = GetComponent<Collider>();

        // Eğer collider yoksa ekle
        if (cellCollider == null)
        {
            cellCollider = gameObject.AddComponent<BoxCollider>();
        }
    }

    public void Initialize(GridCell cell)
    {
        gridCell = cell;
        UpdateVisual();
    }

    void OnMouseDown()
    {
        if (gridCell != null)
        {
            OnCellClicked?.Invoke(gridCell);

            // Grid Manager'a bildir
            if (GridInputHandler.Instance != null)
            {
                GridInputHandler.Instance.OnCellClicked(gridCell);
            }
        }
    }

    void OnMouseEnter()
    {
        if (gridCell != null && !isHovered)
        {
            isHovered = true;
            OnCellHovered?.Invoke(gridCell);

            if (cellRenderer != null && gridCell.CanMoveTo())
            {
                cellRenderer.material.color = hoverColor;
            }
        }
    }

    void OnMouseExit()
    {
        if (isHovered)
        {
            isHovered = false;
            UpdateVisual();
        }
    }

    public void UpdateVisual()
    {
        if (cellRenderer == null || gridCell == null) return;

        Color targetColor = normalColor;

        if (!gridCell.IsWalkable)
        {
            targetColor = blockedColor;
        }
        else if (gridCell.IsOccupied)
        {
            targetColor = occupiedColor;
        }
        else if (isHovered)
        {
            targetColor = hoverColor;
        }

        cellRenderer.material.color = targetColor;
    }

    public void SetHighlight(bool highlight, Color color = default)
    {
        if (cellRenderer == null) return;

        if (highlight)
        {
            Color highlightColor = color == default ? hoverColor : color;
            cellRenderer.material.color = highlightColor;
        }
        else
        {
            UpdateVisual();
        }
    }
}