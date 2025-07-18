using UnityEngine;

/// <summary>
/// Kaan Çakar 2025 - BusData.cs
/// Data structure for managing bus state, passengers, and system info.
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

    [Header("Bus System Info")]
    public bool isActive = false;
    public bool isSpawned = false;
    public float spawnTime = 0f;

    public BusData()
    {
        color = PersonColor.Red;
        capacity = 3;
        currentPassengers = 0;
        state = BusState.Waiting;
        isActive = false;
        isSpawned = false;
        spawnTime = 0f;
    }

    public BusData(PersonColor busColor, int busCapacity)
    {
        color = busColor;
        capacity = busCapacity;
        currentPassengers = 0;
        state = BusState.Waiting;
        isActive = false;
        isSpawned = false;
        spawnTime = 0f;
    }

    // === HELPER METHODS ===
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
    public bool AddPassenger()
    {
        if (HasSpace())
        {
            currentPassengers++;
            return true;
        }
        else
        {
            return false;
        }
    }

    // Remove a passenger if possible
    public bool RemovePassenger()
    {
        if (currentPassengers > 0)
        {
            currentPassengers--;
            return true;
        }
        return false;
    }

    // Set bus state
    public void SetActive(bool active)
    {
        isActive = active;
        if (active)
        {
            state = BusState.Waiting;
        }
    }

    // Mark as spawned
    public void MarkAsSpawned()
    {
        isSpawned = true;
        spawnTime = Time.time;
    }

    // Get spawn duration
    public float GetSpawnDuration()
    {
        return isSpawned ? Time.time - spawnTime : 0f;
    }

    // Reset bus to initial state
    public void Reset()
    {
        currentPassengers = 0;
        state = BusState.Waiting;
        isActive = false;
        isSpawned = false;
        spawnTime = 0f;
    }

    // Copy bus data
    public BusData Copy()
    {
        BusData copy = new BusData(color, capacity);
        copy.currentPassengers = currentPassengers;
        copy.state = state;
        copy.isActive = isActive;
        copy.isSpawned = isSpawned;
        copy.spawnTime = spawnTime;
        return copy;
    }

    // Validate bus data
    public bool IsValid()
    {
        return capacity > 0 && currentPassengers >= 0 && currentPassengers <= capacity;
    }

    // Compare with another bus data
    public bool Equals(BusData other)
    {
        if (other == null) return false;
        return color == other.color &&
               capacity == other.capacity &&
               currentPassengers == other.currentPassengers &&
               state == other.state;
    }

    // Debug info
    public override string ToString()
    {
        string statusInfo = isActive ? " [ACTIVE]" : (isSpawned ? " [WAITING]" : " [NOT SPAWNED]");
        return $"{color} Bus: {currentPassengers}/{capacity} ({state}){statusInfo}";
    }

    // // Detailed debug info
    // public string GetDetailedInfo()
    // {
    //     return $"Bus Details:\n" +
    //            $"  Color: {color}\n" +
    //            $"  Capacity: {capacity}\n" +
    //            $"  Passengers: {currentPassengers}/{capacity} ({GetOccupancyPercentage():F1}%)\n" +
    //            $"  State: {state}\n" +
    //            $"  Is Active: {isActive}\n" +
    //            $"  Is Spawned: {isSpawned}\n" +
    //            $"  Spawn Duration: {GetSpawnDuration():F1}s\n" +
    //            $"  Available Seats: {GetAvailableSeats()}\n" +
    //            $"  Is Full: {IsFull()}\n" +
    //            $"  Is Valid: {IsValid()}";
    // }
}