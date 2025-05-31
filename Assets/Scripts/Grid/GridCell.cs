using UnityEngine;

/// <summary>
/// Kaan Çakar 2025 - GridCell.cs
/// Represents a single cell in the grid.
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
    }
    
    public Vector2Int GetGridPosition()
    {
        return new Vector2Int(x, z);
    }
    
    public void SetOccupied(GameObject obj)
    {
        IsOccupied = true;
        occupyingObject = obj;
        IsWalkable = false; // Hem duvar hem insan için yürünemez
    }
    
    public void SetEmpty()
    {
        IsOccupied = false;
        occupyingObject = null;
        IsWalkable = true; // Boş hücre her zaman yürünebilir
    }
    
    public bool CanMoveTo()
    {
        return IsWalkable && !IsOccupied;
    }
    
    public void SetWalkable(bool walkable)
    {
        IsWalkable = walkable;
        
        // Görsel feedback
        if (cellObject != null)
        {
            var renderer = cellObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = walkable ? Color.white : Color.red;
            }
        }
    }
    
    public void HighlightCell(bool highlight, Color color = default)
    {
        if (cellObject != null)
        {
            var renderer = cellObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (highlight)
                {
                    Color highlightColor = color == default ? Color.yellow : color;
                    renderer.material.color = highlightColor;
                }
                else
                {
                    // Normal renge dön
                    renderer.material.color = IsWalkable ? Color.white : Color.red;
                }
            }
        }
    }
    
    // Level design için yardımcı metodlar
    public bool HasWall()
    {
        return IsOccupied && occupyingObject != null && occupyingObject.CompareTag("Wall");
    }
    
    public bool HasPerson()
    {
        return IsOccupied && occupyingObject != null && occupyingObject.CompareTag("Player");
    }
}