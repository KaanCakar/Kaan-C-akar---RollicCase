using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Kaan Çakar 2025 - BusComponent.cs
/// Component that represents a bus in the game world
/// </summary>
public class BusComponent : MonoBehaviour
{
    [Header("Bus Visual")]
    public Renderer busRenderer;
    public Material[] busColorMaterials; // Renk başına farklı materyaller

    [Header("Passenger Indicators")]
    public Transform passengerContainer;
    public GameObject passengerIndicatorPrefab;
    public List<GameObject> passengerIndicators = new List<GameObject>();

    [Header("Animation")]
    public Animator busAnimator;

    private BusData busData;
    private BusState currentState = BusState.Approaching;

    void Awake()
    {
        // Component'leri al
        if (busRenderer == null)
            busRenderer = GetComponentInChildren<Renderer>();

        if (busAnimator == null)
            busAnimator = GetComponent<Animator>();
    }

    public void Initialize(BusData data)
    {
        busData = data;
        SetupVisuals();
        SetupPassengerIndicators();
    }

    void SetupVisuals()
    {
        if (busRenderer == null) return;

        // Renk materyalini ayarla
        if (busColorMaterials != null && (int)busData.color < busColorMaterials.Length)
        {
            busRenderer.material = busColorMaterials[(int)busData.color];
        }
        else
        {
            // Fallback olarak rengi değiştir
            busRenderer.material.color = GridObject.GetPersonColorValue(busData.color);
        }
    }

    void SetupPassengerIndicators()
    {
        if (passengerContainer == null || passengerIndicatorPrefab == null) return;

        // Mevcut indicator'ları temizle
        foreach (var indicator in passengerIndicators)
        {
            if (indicator != null)
                Destroy(indicator);
        }
        passengerIndicators.Clear();

        // Kapasite kadar indicator oluştur
        for (int i = 0; i < busData.capacity; i++)
        {
            GameObject indicator = Instantiate(passengerIndicatorPrefab, passengerContainer);
            indicator.SetActive(false); // Başlangıçta gizli
            passengerIndicators.Add(indicator);
        }
    }

    public void UpdatePassengerCount(int currentPassengers, int capacity)
    {
        busData.currentPassengers = currentPassengers;

        // Yolcu indicator'larını güncelle
        for (int i = 0; i < passengerIndicators.Count; i++)
        {
            if (i < currentPassengers)
            {
                passengerIndicators[i].SetActive(true);

                // Yolcu rengini ayarla (son binen yolcunun rengi)
                var renderer = passengerIndicators[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = GridObject.GetPersonColorValue(busData.color);
                }
            }
            else
            {
                passengerIndicators[i].SetActive(false);
            }
        }

        // Animasyon tetikle
        if (busAnimator != null)
        {
            busAnimator.SetTrigger("PassengerBoarded");
        }
    }


    public void SetState(BusState newState)
    {
        currentState = newState;

        // State'e göre animasyon
        if (busAnimator != null)
        {
            switch (newState)
            {
                case BusState.Approaching:
                    busAnimator.SetTrigger("Approaching");
                    break;
                case BusState.Waiting:
                    busAnimator.SetTrigger("Waiting");
                    break;
                case BusState.Departing:
                    busAnimator.SetTrigger("Departing");
                    break;
            }
        }
    }


    // Otobüs doldu mu kontrol et
    public bool IsFull()
    {
        return busData.currentPassengers >= busData.capacity;
    }

    // Otobüse yer var mı?
    public bool HasSpace()
    {
        return busData.currentPassengers < busData.capacity;
    }

    // Debug için
    void OnDrawGizmosSelected()
    {
        // Otobüs bilgilerini göster
        Gizmos.color = GridObject.GetPersonColorValue(busData.color);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one);

        // Kapasite göstergesi
        for (int i = 0; i < busData.capacity; i++)
        {
            Vector3 pos = transform.position + Vector3.right * (i - busData.capacity / 2f);

            if (i < busData.currentPassengers)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(pos, 0.2f);
            }
            else
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawWireSphere(pos, 0.2f);
            }
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