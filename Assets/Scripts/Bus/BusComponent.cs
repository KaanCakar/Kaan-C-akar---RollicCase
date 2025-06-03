using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Kaan Çakar 2025 - BusComponent.cs
/// Enhanced version for dual bus system
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
    private bool isActiveBus = false;

    void Awake()
    {
        // Component'leri al - Enhanced search
        if (busRenderer == null)
        {
            // Önce child'larda ara
            busRenderer = GetComponentInChildren<Renderer>();
            Debug.Log($"Found renderer in children: {(busRenderer != null ? busRenderer.name : "NULL")}");
        }

        if (busRenderer == null)
        {
            // Child'larda bulamazsa kendisinde ara
            busRenderer = GetComponent<Renderer>();
            Debug.Log($"Found renderer in self: {(busRenderer != null ? busRenderer.name : "NULL")}");
        }

        if (busRenderer == null)
        {
            // Son çare: FindObjectOfType ile ara (aynı GameObject'te)
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
            Debug.Log($"Found {allRenderers.Length} renderers in hierarchy:");

            for (int i = 0; i < allRenderers.Length; i++)
            {
                Debug.Log($"  Renderer {i}: {allRenderers[i].name} (Tag: {allRenderers[i].tag})");

                // İlk renderer'ı al (veya tag'e göre filtrele)
                if (i == 0 || allRenderers[i].name.Contains("Bus") || allRenderers[i].name.Contains("Mesh"))
                {
                    busRenderer = allRenderers[i];
                    Debug.Log($"Selected renderer: {busRenderer.name}");
                    break;
                }
            }
        }

        if (busAnimator == null)
            busAnimator = GetComponent<Animator>();

        Debug.Log($"FINAL: busRenderer = {(busRenderer != null ? busRenderer.name : "NULL")}");
    }

    public void Initialize(BusData data)
    {
        Debug.Log($"=== BUS COMPONENT INITIALIZE (PREFAB SYSTEM) ===");
        Debug.Log($"Bus: {data.color}, Prefab: {gameObject.name}");

        busData = data;

        // Prefab sisteminde material zaten doğru, sadece passenger'ları setup et
        SetupPassengerIndicators();

        Debug.Log($"✅ Bus component initialized for prefab");
    }

    public void SetAsActiveBus(bool active)
    {
        isActiveBus = active;
        Debug.Log($"Bus {busData.color} set as active: {active}");
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

                // Yolcu materyalini otobüsün renk materyali ile aynı yap
                var passengerRenderer = passengerIndicators[i].GetComponent<Renderer>();
                if (passengerRenderer != null && busColorMaterials != null)
                {
                    // Bus color index'ine göre aynı materyali ata
                    int colorIndex = (int)busData.color;
                    if (colorIndex < busColorMaterials.Length && busColorMaterials[colorIndex] != null)
                    {
                        passengerRenderer.material = busColorMaterials[colorIndex];
                        Debug.Log($"Applied {busData.color} material to passenger {i}");
                    }
                    else
                    {
                        // Fallback: otobüsün mevcut materyalini kullan
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
                case BusState.Boarding:
                    busAnimator.SetTrigger("Boarding");
                    break;
                case BusState.Departing:
                    busAnimator.SetTrigger("Departing");
                    break;
            }
        }

        Debug.Log($"Bus {busData.color} state changed to: {newState}");
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

    // Bus data getter
    public BusData GetBusData()
    {
        return busData;
    }

    // Yolcu bindiğinde çağrılacak metod
    public void OnPassengerBoarded()
    {
        if (busData != null)
        {
            UpdatePassengerCount(busData.currentPassengers, busData.capacity);
        }
    }

    // Bus hareket animasyonu için
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



    // Debug için gizmo çizimi - Güvenli versiyon
    void OnDrawGizmosSelected()
    {
        // Basit ve güvenli gizmo
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);

        // Bus durumunu göster
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