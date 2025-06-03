using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kaan Çakar 2025 - WaitingGrid.cs
/// WaitingGrid is a grid system that manages waiting slots for people.
/// </summary>
public class WaitingGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int capacity = 5;
    public float slotSpacing = 1.2f;
    public Vector3 gridStartPosition = Vector3.zero;
    public bool arrangeHorizontally = true;

    [Header("Slot Visuals")]
    public GameObject slotPrefab;
    public Material emptySlotMaterial;
    public Material occupiedSlotMaterial;

    [Header("Debug")]
    public bool showGizmos = true;
    public Color emptySlotColor = Color.gray;
    public Color occupiedSlotColor = Color.red;

    private List<WaitingSlot> waitingSlots = new List<WaitingSlot>();
    private int currentOccupiedCount = 0;

    public static WaitingGrid Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeSlots();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeSlots()
    {
        waitingSlots.Clear();

        for (int i = 0; i < capacity; i++)
        {
            Vector3 slotPosition = CalculateSlotPosition(i);
        
            GameObject slotObject = null;
            if (slotPrefab != null)
            {
                slotObject = Instantiate(slotPrefab, slotPosition, Quaternion.identity, transform);
                slotObject.name = $"WaitingSlot_{i}";
            }

            WaitingSlot slot = new WaitingSlot(i, slotPosition, slotObject);
            waitingSlots.Add(slot);
        }

        Debug.Log($"Waiting Grid initialized with {capacity} slots");
    }

    Vector3 CalculateSlotPosition(int index)
    {
        Vector3 position = gridStartPosition;

        if (arrangeHorizontally)
        {
            position += Vector3.right * (index * slotSpacing);
        }
        else
        {
            position += Vector3.forward * (index * slotSpacing);
        }

        return transform.position + position;
    }

    public bool AddPersonToWaiting(GridObject person)
    {
        if (IsFull()) return false;

        for (int i = 0; i < waitingSlots.Count; i++)
        {
            if (waitingSlots[i].IsEmpty())
            {
                waitingSlots[i].SetOccupyingPerson(person);
                currentOccupiedCount++;

                StartCoroutine(MovePersonToSlot(person, waitingSlots[i]));

                Debug.Log($"Person {person.personColor} added to waiting slot {i}");
                return true;
            }
        }

        return false;
    }

    System.Collections.IEnumerator MovePersonToSlot(GridObject person, WaitingSlot slot)
    {
        Vector3 startPos = person.transform.position;
        Vector3 targetPos = slot.worldPosition;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            person.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        person.transform.position = targetPos;
        person.isInWaitingGrid = true;

        Debug.Log($"Person {person.personColor} reached waiting slot");
    }

    public bool RemovePersonFromWaiting(GridObject person)
    {
        for (int i = 0; i < waitingSlots.Count; i++)
        {
            if (waitingSlots[i].GetOccupyingPerson() == person)
            {
                waitingSlots[i].SetOccupyingPerson(null);
                currentOccupiedCount--;

                Debug.Log($"Person {person.personColor} removed from waiting slot {i}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get people in waiting grid by their color
    /// </summary>
    public List<GridObject> GetPeopleByColor(PersonColor color)
    {
        List<GridObject> peopleOfColor = new List<GridObject>();
        
        for (int i = 0; i < waitingSlots.Count; i++)
        {
            GridObject person = waitingSlots[i].GetOccupyingPerson();
            if (person != null && person.personColor == color)
            {
                peopleOfColor.Add(person);
            }
        }
        
        return peopleOfColor;
    }

    /// <summary>
    /// Remove a person from waiting grid by their GridObject reference
    /// </summary>
    public bool RemovePersonByObject(GridObject person)
    {
        for (int i = 0; i < waitingSlots.Count; i++)
        {
            if (waitingSlots[i].GetOccupyingPerson() == person)
            {
                waitingSlots[i].SetOccupyingPerson(null);
                currentOccupiedCount--;
                
                Debug.Log($"Person {person.personColor} removed from waiting slot {i}");
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Remove all people of a specific color from the waiting grid
    /// </summary>
    public List<GridObject> RemoveAllPeopleByColor(PersonColor color)
    {
        List<GridObject> removedPeople = new List<GridObject>();
        
        for (int i = 0; i < waitingSlots.Count; i++)
        {
            GridObject person = waitingSlots[i].GetOccupyingPerson();
            if (person != null && person.personColor == color)
            {
                waitingSlots[i].SetOccupyingPerson(null);
                currentOccupiedCount--;
                removedPeople.Add(person);
                
                Debug.Log($"Person {person.personColor} removed from waiting slot {i}");
            }
        }
        
        return removedPeople;
    }

    /// <summary>
    /// Get the count of people in waiting grid by their color
    /// </summary>
    public int GetPeopleCountByColor(PersonColor color)
    {
        int count = 0;
        
        for (int i = 0; i < waitingSlots.Count; i++)
        {
            GridObject person = waitingSlots[i].GetOccupyingPerson();
            if (person != null && person.personColor == color)
            {
                count++;
            }
        }
        
        return count;
    }

    public bool IsFull()
    {
        return currentOccupiedCount >= capacity;
    }

    public bool IsEmpty()
    {
        return currentOccupiedCount == 0;
    }

    public int GetOccupiedCount()
    {
        return currentOccupiedCount;
    }

    public int GetAvailableSlots()
    {
        return capacity - currentOccupiedCount;
    }

    public void SetPositionRelativeToBus(Transform busTransform)
    {
        if (busTransform != null)
        {
            Vector3 busPosition = busTransform.position;
            transform.position = busPosition + Vector3.left * 3f;
            
            UpdateSlotPositions();
        }
    }

    void UpdateSlotPositions()
    {
        for (int i = 0; i < waitingSlots.Count; i++)
        {
            Vector3 newPosition = CalculateSlotPosition(i);
            waitingSlots[i].worldPosition = newPosition;

            if (waitingSlots[i].slotObject != null)
            {
                waitingSlots[i].slotObject.transform.position = newPosition;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        for (int i = 0; i < capacity; i++)
        {
            Vector3 slotPos = CalculateSlotPosition(i);

            if (Application.isPlaying && i < waitingSlots.Count)
            {
                Gizmos.color = waitingSlots[i].IsEmpty() ? emptySlotColor : occupiedSlotColor;
            }
            else
            {
                Gizmos.color = emptySlotColor;
            }

            Gizmos.DrawWireCube(slotPos, Vector3.one * 0.8f);
            Gizmos.DrawSphere(slotPos + Vector3.up * 0.5f, 0.2f);
        }
        // Draw lines between slots
        Gizmos.color = Color.yellow;
        for (int i = 0; i < capacity - 1; i++)
        {
            Vector3 pos1 = CalculateSlotPosition(i);
            Vector3 pos2 = CalculateSlotPosition(i + 1);
            Gizmos.DrawLine(pos1, pos2);
        }
    }
}

[System.Serializable]
public class WaitingSlot
{
    public int index;
    public Vector3 worldPosition;
    public GameObject slotObject;
    private GridObject occupyingPerson;

    public WaitingSlot(int index, Vector3 position, GameObject slotObj = null)
    {
        this.index = index;
        this.worldPosition = position;
        this.slotObject = slotObj;
        this.occupyingPerson = null;
    }

    public bool IsEmpty()
    {
        return occupyingPerson == null;
    }

    public GridObject GetOccupyingPerson()
    {
        return occupyingPerson;
    }

    public void SetOccupyingPerson(GridObject person)
    {
        occupyingPerson = person;

        UpdateSlotVisual();
    }

    void UpdateSlotVisual()
    {
        if (slotObject == null) return;

        Renderer renderer = slotObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (occupyingPerson != null)
            {
                renderer.material.color = GridObject.GetPersonColorValue(occupyingPerson.personColor);
            }
            else
            {
                renderer.material.color = Color.gray;
            }
        }
    }
}