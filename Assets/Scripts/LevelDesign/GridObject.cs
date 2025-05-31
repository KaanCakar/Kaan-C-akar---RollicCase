using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kaan Çakar 2025 - GridObject.cs
/// Component for objects placed on the grid (walls, people,etc.)
/// </summary>
public class GridObject : MonoBehaviour
{
    [Header("Grid Object Info")]
    public GridCell gridCell;
    public GridObjectType objectType;
    public PersonColor personColor = PersonColor.Red;

    [Header("Person Settings")]
    public bool isInBus = false;
    public bool isInWaitingGrid = false;

    [Header("Events")]
    public UnityEvent OnPersonClicked;
    public UnityEvent OnPersonBoarded;

    [Header("Visual Feedback")]
    public GameObject selectionIndicator;
    public Material[] personMaterials;
    public GameObject playableOutline; // Oynanabilir durumu göstermek için

    private Renderer objRenderer;
    private bool isSelected = false;
    private bool isPlayable = false;

    // Person movement
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float moveSpeed = 5f;

    void Awake()
    {
        objRenderer = GetComponent<Renderer>();
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
        if (playableOutline != null)
            playableOutline.SetActive(false);
    }

    void Start()
    {
        UpdateVisuals();
        UpdatePlayableStatus();
    }

    void Update()
    {
        HandleMovement();
        
        // Oynanabilir durumu sürekli güncelle
        if (objectType == GridObjectType.Person && !isInBus && !isInWaitingGrid)
        {
            UpdatePlayableStatus();
        }
    }

    public void Initialize(GridCell cell, GridObjectType type)
    {
        gridCell = cell;
        objectType = type;

        UpdateVisuals();

        // Person ise tıklanabilir yap
        if (objectType == GridObjectType.Person)
        {
            MakeClickable();
        }
    }

    void MakeClickable()
    {
        // Collider yoksa ekle
        if (GetComponent<Collider>() == null)
        {
            var collider = gameObject.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.3f;
        }
    }

    void OnMouseDown()
    {
        if (objectType == GridObjectType.Person && !isInBus && !isInWaitingGrid)
        {
            HandlePersonClick();
        }
    }

    void HandlePersonClick()
    {
        // Oynanabilir mi kontrol et
        if (!isPlayable)
        {
            Debug.Log($"Person {personColor} is not playable - blocked on all sides");
            return;
        }

        // Event'i tetikle
        OnPersonClicked?.Invoke();

        // GameManager'a bildir - DÜZELTME: OnPersonSelected metodu var
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPersonSelected(this);
        }

        Debug.Log($"Person clicked: {personColor} at ({gridCell.x}, {gridCell.z})");
    }

    void UpdatePlayableStatus()
    {
        if (GameManager.Instance != null)
        {
            bool newPlayable = GameManager.Instance.CanPersonMove(this);
            
            if (newPlayable != isPlayable)
            {
                isPlayable = newPlayable;
                UpdatePlayableVisual();
            }
        }
    }

    void UpdatePlayableVisual()
    {
        if (playableOutline != null)
        {
            playableOutline.SetActive(isPlayable);
        }
        
        // Alternatif olarak material'i değiştir
        if (objRenderer != null && objectType == GridObjectType.Person)
        {
            if (isPlayable)
            {
                // Oynanabilir - normal renk
                UpdatePersonVisual();
            }
            else
            {
                // Oynanamaz - soluk renk
                Color normalColor = GetPersonColorValue(personColor);
                objRenderer.material.color = new Color(normalColor.r, normalColor.g, normalColor.b, 0.5f);
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(selected);
        }

        // Materyali değiştir (seçili durumu göstermek için)
        if (objRenderer != null && selected)
        {
            objRenderer.material.color = Color.white;
        }
        else if (!selected)
        {
            UpdateVisuals();
        }
    }

    void UpdateVisuals()
    {
        if (objRenderer == null) return;

        switch (objectType)
        {
            case GridObjectType.Wall:
                objRenderer.material.color = Color.gray;
                break;

            case GridObjectType.Person:
                UpdatePersonVisual();
                UpdatePlayableVisual();
                break;
        }
    }

    void UpdatePersonVisual()
    {
        if (objRenderer != null)
        {
            // Material dizisinden doğru materyali kullan
            if (personMaterials != null && (int)personColor < personMaterials.Length && personMaterials[(int)personColor] != null)
            {
                objRenderer.material = personMaterials[(int)personColor];
            }
            else
            {
                // Fallback olarak rengi değiştir
                objRenderer.material.color = GetPersonColorValue(personColor);
            }
        }
    }

    public static Color GetPersonColorValue(PersonColor color)
    {
        switch (color)
        {
            case PersonColor.Red: return Color.red;
            case PersonColor.Blue: return Color.blue;
            case PersonColor.Green: return Color.green;
            case PersonColor.Yellow: return Color.yellow;
            case PersonColor.Magenta: return Color.magenta;
            case PersonColor.Cyan: return Color.cyan;
            case PersonColor.White: return Color.white;
            case PersonColor.Pink: return new Color(1f, 0.4f, 0.7f);
            case PersonColor.Orange: return new Color(1f, 0.5f, 0f);
            case PersonColor.Purple: return new Color(0.5f, 0f, 1f);
            default: return Color.white;
        }
    }

    // Person'ı belirli bir pozisyona hareket ettir
    public void MoveTo(Vector3 newPosition, GridCell newCell = null)
    {
        if (objectType != GridObjectType.Person) return;

        targetPosition = newPosition;
        isMoving = true;

        // Eski cell'i boşalt - DÜZELTME: SetEmpty() kullan
        if (gridCell != null)
        {
            gridCell.SetEmpty();
        }

        // Yeni cell'i işgal et
        if (newCell != null)
        {
            gridCell = newCell;
            newCell.SetOccupied(gameObject); // DÜZELTME: GameObject parametre olmalı
        }
    }

    void HandleMovement()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            transform.position = targetPosition;
            isMoving = false;
            
            // Hareket tamamlandığında oynanabilir durumu güncelle
            UpdatePlayableStatus();
        }
    }

    // Otobüse binme metodu (yeni sistem için)
    public void BoardBus()
    {
        if (objectType != GridObjectType.Person) return;

        isInBus = true;
        
        // Grid cell'i boşalt - DÜZELTME: SetEmpty() kullan
        if (gridCell != null)
        {
            gridCell.SetEmpty();
            gridCell = null;
        }

        OnPersonBoarded?.Invoke();
        
        // Görsel efekt eklenebilir (otobüse doğru hareket vs.)
        StartCoroutine(MoveTowardsBus());
    }

    System.Collections.IEnumerator MoveTowardsBus()
    {
        // Otobüse doğru hareket animasyonu
        Vector3 startPos = transform.position;
        Vector3 busDirection = Vector3.up * 5f; // Otobüs yukarıda varsayılıyor
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.position = Vector3.Lerp(startPos, startPos + busDirection, t);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            
            yield return null;
        }
        
        // Animasyon tamamlandı, objeyi yok et
        gameObject.SetActive(false);
    }

    // Bekleme gridine gönderme metodu
    public void SendToWaitingGrid()
    {
        if (objectType != GridObjectType.Person) return;

        isInWaitingGrid = true;
        
        // Grid cell'i boşalt - DÜZELTME: SetEmpty() kullan  
        if (gridCell != null)
        {
            gridCell.SetEmpty();
            gridCell = null;
        }

        // Bekleme gridi pozisyonuna taşı
        StartCoroutine(MoveToWaitingGrid());
    }

    System.Collections.IEnumerator MoveToWaitingGrid()
    {
        // Bekleme gridine hareket animasyonu
        Vector3 startPos = transform.position;
        Vector3 waitingGridPos = new Vector3(0, 2f, -5f); // Bekleme gridi pozisyonu (ayarlanacak)
        
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.position = Vector3.Lerp(startPos, waitingGridPos, t);
            
            yield return null;
        }
        
        // Bekleme gridinde bekliyor
        Debug.Log($"{personColor} person is now in waiting grid");
    }

    public bool IsPlayable()
    {
        return isPlayable && !isInBus && !isInWaitingGrid;
    }

    // Debug için
    void OnDrawGizmosSelected()
    {
        if (gridCell != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(gridCell.worldPosition, Vector3.one * 0.8f);
        }

        if (isMoving)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        
        if (isPlayable)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
        }
    }
}