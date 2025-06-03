using UnityEngine;

/// <summary>
/// Kaan Çakar 2025 - GridCell.cs
/// Represents a single cell in the grid with shape drawing support.
/// </summary>

[System.Serializable]
public class GridCell
{
    [Header("Position")]
    public int x;
    public int z;
    public Vector3 worldPosition;

    [Header("State")]
    public bool IsWalkable = true;
    public bool IsOccupied = false;

    [Header("Play Area System")]
    public bool isPlayArea = true;
    public bool isVisible = true;

    [Header("References")]
    public GameObject cellObject;
    public GameObject occupyingObject;

    [Header("Properties")]
    public float movementCost = 1f;

    public GridCell(int x, int z, Vector3 worldPos, GameObject cellObj = null)
    {
        this.x = x;
        this.z = z;
        this.worldPosition = worldPos;
        this.cellObject = cellObj;
        this.isPlayArea = true;
        this.isVisible = true;
    }

    public Vector2Int GetGridPosition()
    {
        return new Vector2Int(x, z);
    }

    public void SetOccupied(GameObject obj)
    {
        Debug.Log($"GridCell ({x}, {z}) - SetOccupied called with {(obj != null ? obj.name : "NULL")}");

        IsOccupied = true;
        occupyingObject = obj;

        if (obj != null && obj.GetComponent<GridObject>()?.objectType == GridObjectType.Person)
        {
            IsWalkable = false;
            Debug.Log($"GridCell ({x}, {z}) - Set to NOT WALKABLE (occupied by person)");
        }

        Debug.Log($"GridCell ({x}, {z}) - Final state: Occupied={IsOccupied}, Walkable={IsWalkable}");
    }

    public void SetEmpty()
    {
        Debug.Log($"GridCell ({x}, {z}) - SetEmpty called. Was occupied by: {(occupyingObject != null ? occupyingObject.name : "nothing")}");

        IsOccupied = false;
        occupyingObject = null;
        IsWalkable = true;

        Debug.Log($"GridCell ({x}, {z}) - Final state: Occupied={IsOccupied}, Walkable={IsWalkable}");
    }

    public bool CanMoveTo()
    {
        return IsWalkable && !IsOccupied && isPlayArea && isVisible;
    }

    public void SetWalkable(bool walkable)
    {
        IsWalkable = walkable;
        UpdateVisual();
    }

    public void SetPlayArea(bool playArea)
    {
        isPlayArea = playArea;
        UpdateVisual();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (cellObject != null)
        {
            cellObject.SetActive(visible);
        }

        UpdateVisual();
    }

    public void HighlightCell(bool highlight, Color color = default)
    {
        if (!isVisible || cellObject == null) return;

        var renderer = cellObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (highlight)
            {
                Color highlightColor = color == default ? Color.yellow : color;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // In editor mode
                    if (renderer.sharedMaterial != null)
                    {
                        var material = new Material(renderer.sharedMaterial);
                        material.color = highlightColor;
                        renderer.sharedMaterial = material;
                    }
                }
                else
#endif
                {
                    // In runtime
                    renderer.material.color = highlightColor;
                }
            }
            else
            {
                UpdateVisual();
            }
        }
    }

    private void UpdateVisual()
    {
        if (!isVisible || cellObject == null) return;

        var renderer = cellObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color targetColor = Color.white;

            if (!isPlayArea)
            {
                targetColor = Color.gray;  // Non-playable area
            }
            else if (!IsWalkable)
            {
                targetColor = Color.red;   // Non-walkable area
            }
            else if (IsOccupied)
            {
                targetColor = Color.blue;  // Occupied area
            }

            // IN EDITOR MODE: use sharedMaterial, prevent material leaks
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // In editor mode to prevent material leaks
                if (renderer.sharedMaterial != null)
                {
                    var material = new Material(renderer.sharedMaterial);
                    material.color = targetColor;
                    renderer.sharedMaterial = material;
                }
            }
            else
#endif
            {
                // Use normal material in runtime
                renderer.material.color = targetColor;
            }
        }
    }

    // Helper methods for level design
    public bool HasPerson()
    {
        return IsOccupied && occupyingObject != null &&
               occupyingObject.GetComponent<GridObject>()?.objectType == GridObjectType.Person;
    }

    // Check if in playable area
    public bool IsInPlayArea()
    {
        return isPlayArea && isVisible;
    }
}