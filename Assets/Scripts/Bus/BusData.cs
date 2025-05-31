using UnityEngine;

/// <summary>
/// Kaan Çakar 2025 - BusData.cs
/// Data structure for bus information
/// </summary>
[System.Serializable]
public class BusData
{
    [Header("Bus Properties")]
    public PersonColor color;
    public int capacity = 3;
    public int currentPassengers = 0;
    
    [Header("Bus State")]
    public BusState state = BusState.Waiting;
    
    // Constructor
    public BusData()
    {
        color = PersonColor.Red;
        capacity = 3;
        currentPassengers = 0;
        state = BusState.Waiting;
    }
    
    public BusData(PersonColor busColor, int busCapacity)
    {
        color = busColor;
        capacity = busCapacity;
        currentPassengers = 0;
        state = BusState.Waiting;
    }
    
    // Helper methods
    public bool IsFull()
    {
        return currentPassengers >= capacity;
    }
    
    public bool HasSpace()
    {
        return currentPassengers < capacity;
    }
    
    public bool IsEmpty()
    {
        return currentPassengers == 0;
    }
    
    public int GetAvailableSeats()
    {
        return capacity - currentPassengers;
    }
    
    public float GetOccupancyPercentage()
    {
        return (float)currentPassengers / capacity * 100f;
    }
    
    // Add passenger
    public bool AddPassenger()
    {
        if (HasSpace())
        {
            currentPassengers++;
            return true;
        }
        return false;
    }
    
    // Remove passenger
    public bool RemovePassenger()
    {
        if (currentPassengers > 0)
        {
            currentPassengers--;
            return true;
        }
        return false;
    }
    
    // Reset bus
    public void Reset()
    {
        currentPassengers = 0;
        state = BusState.Waiting;
    }
    
    // Debug info
    public override string ToString()
    {
        return $"{color} Bus: {currentPassengers}/{capacity} ({state})";
    }
}