using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Kaan Çakar 2025 - BusComponent.cs
/// Bus component for managing bus state, passengers, and animations.
/// </summary>
public class BusComponent : MonoBehaviour
{
    [Header("Bus Visual")]
    public Renderer busRenderer;
    public Material[] busColorMaterials;

    [Header("Passenger Indicators")]
    public Transform passengerContainer;
    public GameObject passengerIndicatorPrefab;
    public List<GameObject> passengerIndicators = new List<GameObject>();

    [Header("Animation")]
    public Animator busAnimator;



    private BusData busData;
    private BusState currentState = BusState.Approaching;
    private bool isActiveBus = false;

    void Awake()
    {
        if (busRenderer == null)
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>();

            for (int i = 0; i < allRenderers.Length; i++)
            {
                Debug.Log($"  Renderer {i}: {allRenderers[i].name} (Tag: {allRenderers[i].tag})");

                if (i == 0 || allRenderers[i].name.Contains("Bus") || allRenderers[i].name.Contains("Mesh"))
                {
                    busRenderer = allRenderers[i];
                    Debug.Log($"Selected renderer: {busRenderer.name}");
                    break;
                }
            }
        }

        if (busAnimator == null) busAnimator = GetComponent<Animator>();
    }

    public void Initialize(BusData data)
    {
        Debug.Log($"=== BUS COMPONENT INITIALIZE (PREFAB SYSTEM) ===");
        Debug.Log($"Bus: {data.color}, Prefab: {gameObject.name}");
        busData = data;
        SetupPassengerIndicators();
    }

    public void SetAsActiveBus(bool active)
    {
        isActiveBus = active;
    }

    /// <summary>
    /// Setup passenger indicators based on bus capacity.
    /// </summary>
    void SetupPassengerIndicators()
    {
        if (passengerContainer == null || passengerIndicatorPrefab == null) return;

        foreach (var indicator in passengerIndicators)
        {
            if (indicator != null)
                Destroy(indicator);
        }
        passengerIndicators.Clear();

        for (int i = 0; i < busData.capacity; i++)
        {
            GameObject indicator = Instantiate(passengerIndicatorPrefab, passengerContainer);
            indicator.SetActive(false);
            passengerIndicators.Add(indicator);
        }
    }

    /// <summary>
    /// Update the passenger count and indicators.
    /// </summary>
    /// <param name="currentPassengers">Current number of passengers.</param>
    /// <param name="capacity">Total capacity of the bus.</param>
    public void UpdatePassengerCount(int currentPassengers, int capacity)
    {
        busData.currentPassengers = currentPassengers;

        for (int i = 0; i < passengerIndicators.Count; i++)
        {
            if (i < currentPassengers)
            {
                passengerIndicators[i].SetActive(true);

                var passengerRenderer = passengerIndicators[i].GetComponent<Renderer>();
                if (passengerRenderer != null && busColorMaterials != null)
                {
                    int colorIndex = (int)busData.color;
                    if (colorIndex < busColorMaterials.Length && busColorMaterials[colorIndex] != null)
                    {
                        passengerRenderer.material = busColorMaterials[colorIndex];
                        Debug.Log($"Applied {busData.color} material to passenger {i}");
                    }
                    else
                    {
                        if (busRenderer != null)
                        {
                            passengerRenderer.material = busRenderer.material;
                        }
                    }
                }
            }
            else
            {
                passengerIndicators[i].SetActive(false);
            }
        }
        if (busAnimator != null)
        {
            busAnimator.SetTrigger("PassengerBoarded");
        }
    }

    /// <summary>
    /// Set the current state of the bus and trigger the appropriate animation.
    /// </summary>
    /// <param name="newState"></param>
    public void SetState(BusState newState)
    {
        currentState = newState;

        if (busAnimator != null)
        {
            try
            {
                switch (newState)
                {
                    case BusState.Approaching:
                        busAnimator.SetTrigger("Approaching");
                        break;
                    case BusState.Waiting:
                        busAnimator.SetTrigger("Waiting");
                        break;
                    case BusState.Boarding:
                        busAnimator.SetTrigger("Boarding");
                        break;
                    case BusState.Departing:
                        busAnimator.SetTrigger("Departing");
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Animator trigger failed: {e.Message}");
            }
        }
    }

    public bool IsFull()
    {
        return busData.currentPassengers >= busData.capacity;
    }
    public bool HasSpace()
    {
        return busData.currentPassengers < busData.capacity;
    }
    public BusData GetBusData()
    {
        return busData;
    }
    public void OnPassengerBoarded()
    {
        if (busData != null)
        {
            UpdatePassengerCount(busData.currentPassengers, busData.capacity);
        }
    }
    public void PlayMoveAnimation(Vector3 targetPosition, float duration, System.Action onComplete = null)
    {
        StartCoroutine(MoveToPositionCoroutine(targetPosition, duration, onComplete));
    }

    System.Collections.IEnumerator MoveToPositionCoroutine(Vector3 targetPosition, float duration, System.Action onComplete)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        onComplete?.Invoke();
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);

        if (isActiveBus)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.3f);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
        }
    }
}

[System.Serializable]
public enum BusState
{
    Approaching,
    Waiting,
    Boarding,
    Departing,
    Gone
}