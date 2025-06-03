using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kaan Çakar 2025 - GridObject.cs
/// Represents an object on the grid, such as a person.
/// </summary>
public class GridObject : MonoBehaviour
{
    [Header("Grid Object Info")]
    public GridCell gridCell;
    public GridObjectType objectType = GridObjectType.Person;
    public PersonColor personColor = PersonColor.Red;

    [Header("Person Settings")]
    public bool isInBus = false;
    public bool isInWaitingGrid = false;

    [Header("Events")]
    public UnityEvent OnPersonClicked;
    public UnityEvent OnPersonBoarded;

    [Header("Visual Feedback - Quick Outline")]
    public bool useQuickOutline = true;
    [SerializeField] private Outline outlineComponent;
    [Header("Outline Colors")]
    public Color playableOutlineColor = Color.black;
    public Color nonPlayableFlashColor = Color.red;
    public Color selectedOutlineColor = Color.white;
    public float outlineWidth = 3f;

    [Header("Visual Settings")]
    public Material[] personMaterials;
    public GameObject selectionIndicator;

    [Header("Opacity System")]
    public bool useOpacityForNonPlayable = true;
    private Material originalMaterial;
    private Material transparentMaterial;

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

        if (useQuickOutline)
        {
            SetupQuickOutline();
        }
        if (objRenderer != null && useOpacityForNonPlayable)
        {
            originalMaterial = objRenderer.material;
            CreateTransparentMaterial();
        }
    }

    void Start()
    {
        UpdateVisuals();
        StartCoroutine(DelayedPlayableStatusUpdate());
    }

    void Update()
    {
        HandleMovement();
    }

    #region Quick Outline Setup

    void SetupQuickOutline()
    {
        outlineComponent = GetComponent<Outline>();
        if (outlineComponent == null)
        {
            outlineComponent = gameObject.AddComponent<Outline>();
        }

        outlineComponent.OutlineMode = Outline.Mode.OutlineAll;
        outlineComponent.OutlineColor = playableOutlineColor; 
        outlineComponent.OutlineWidth = outlineWidth;

        outlineComponent.enabled = false;

    }

    public void SetOutlineColor(Color color)
    {
        playableOutlineColor = color;
        if (outlineComponent != null)
        {
            outlineComponent.OutlineColor = color;
        }
    }

    public void SetOutlineWidth(float width)
    {
        outlineWidth = width;
        if (outlineComponent != null)
        {
            outlineComponent.OutlineWidth = width;
        }
    }

    public void SetOutlineToBlack()
    {
        playableOutlineColor = Color.black;
        if (outlineComponent != null && outlineComponent.enabled)
        {
            outlineComponent.OutlineColor = Color.black;
        }
    }

    public void SetOutlineToCustomColor(Color color)
    {
        playableOutlineColor = color;
        if (outlineComponent != null && outlineComponent.enabled)
        {
            outlineComponent.OutlineColor = color;
        }
    }

    #endregion

    #region Visual Feedback System

    // Event-based playable state sistem
    public void SetPlayableState(bool playable)
    {
        if (isPlayable != playable)
        {
            isPlayable = playable;
            UpdatePlayableVisual();
            Debug.Log($"Person {personColor} playable state changed to: {isPlayable}");
        }
    }

    void UpdatePlayableVisual()
    {
        Debug.Log($"🎨 UpdatePlayableVisual - Person: {personColor}, Playable: {isPlayable}");

        if (useQuickOutline && outlineComponent != null)
        {
            outlineComponent.enabled = isPlayable;

            if (isPlayable)
            {
                outlineComponent.OutlineColor = playableOutlineColor; // Siyah
                outlineComponent.OutlineWidth = outlineWidth;
                Debug.Log($"{personColor} - BLACK Outline AÇILDI");
            }
            else
            {
                Debug.Log($"{personColor} - Outline KAPANDI");
            }
        }

        if (useOpacityForNonPlayable && objRenderer != null)
        {
            if (isPlayable)
            {
                RestoreOriginalMaterial();
                Debug.Log($"{personColor} - Normal material restored");
            }
            else
            {
                ApplyNonPlayableMaterial();
                Debug.Log($"{personColor} - Non-playable material applied");
            }
        }
    }

    void CreateTransparentMaterial()
    {
        if (originalMaterial != null)
        {
            transparentMaterial = new Material(originalMaterial);

            Color color = transparentMaterial.color;
            color *= 0.6f;
            color.a = 0.8f;
            transparentMaterial.color = color;

            Debug.Log($"Non-playable material created for {personColor}");
        }
    }

    void RestoreOriginalMaterial()
    {
        if (objRenderer != null && originalMaterial != null)
        {
            objRenderer.material = originalMaterial;
        }
    }

    void ApplyNonPlayableMaterial()
    {
        if (objRenderer != null && transparentMaterial != null)
        {
            objRenderer.material = transparentMaterial;
        }
        else if (transparentMaterial == null && originalMaterial != null)
        {
            Color color = GetPersonColorValue(personColor);
            color *= 0.6f;
            objRenderer.material.color = color;
        }
    }

    #endregion

    #region Public API
    public void CheckPlayableStatus()
    {
        if (GameManager.Instance != null && gridCell != null && !isInBus && !isInWaitingGrid)
        {
            bool newPlayable = GameManager.Instance.IsPersonPlayable(this);
            SetPlayableState(newPlayable);
        }
    }

    // Cache-friendly IsPlayable check
    public bool IsPlayable()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.IsPersonPlayable(this);
        }

        return isPlayable && !isInBus && !isInWaitingGrid;
    }

    public void ToggleOutline(bool enable)
    {
        if (outlineComponent != null)
        {
            outlineComponent.enabled = enable;
        }
    }

    public void SetTemporaryOutlineColor(Color color)
    {
        if (outlineComponent != null && outlineComponent.enabled)
        {
            outlineComponent.OutlineColor = color;
        }
    }

    #endregion

    #region Initialization & Setup

    System.Collections.IEnumerator DelayedPlayableStatusUpdate()
    {
        while (GameManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        while (gridCell == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.2f);

        if (objectType == GridObjectType.Person && !isInBus && !isInWaitingGrid)
        {
            CheckPlayableStatus();
        }
    }

    public void Initialize(GridCell cell, GridObjectType type)
    {
        gridCell = cell;
        objectType = type;

        UpdateVisuals();

        if (objectType == GridObjectType.Person)
        {
            MakeClickable();


            CheckPlayableStatus();
        }
    }

    void MakeClickable()
    {
        if (GetComponent<Collider>() == null)
        {
            var collider = gameObject.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.3f;
        }
    }

    #endregion

    #region Input Handling

    void OnMouseDown()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameActive)
        {
            return;
        }

        if (objectType == GridObjectType.Person && !isInBus && !isInWaitingGrid)
        {
            HandlePersonClick();
        }
    }

    void HandlePersonClick()
    {
        if (!isPlayable)
        {
            Debug.Log($"Person {personColor} is not playable - blocked on all sides");

            if (outlineComponent != null)
            {
                StartCoroutine(FlashOutline(nonPlayableFlashColor, 0.5f));
            }
            return;
        }

        if (outlineComponent != null)
        {
            StartCoroutine(SelectionFlash());
        }

        OnPersonClicked?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPersonSelected(this);
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
        }

        Debug.Log($"Person clicked: {personColor} at ({gridCell?.x}, {gridCell?.z})");
    }

    System.Collections.IEnumerator FlashOutline(Color flashColor, float duration)
    {
        if (outlineComponent == null) yield break;

        Color originalColor = outlineComponent.OutlineColor;
        bool wasEnabled = outlineComponent.enabled;

        // Flash
        outlineComponent.enabled = true;
        outlineComponent.OutlineColor = flashColor;

        yield return new WaitForSeconds(duration);

        // Restore
        outlineComponent.OutlineColor = originalColor;
        outlineComponent.enabled = wasEnabled;
    }

    System.Collections.IEnumerator SelectionFlash()
    {
        if (outlineComponent == null) yield break;

        Color originalColor = outlineComponent.OutlineColor;

        outlineComponent.OutlineColor = selectedOutlineColor;
        yield return new WaitForSeconds(0.2f);

        outlineComponent.OutlineColor = originalColor;
    }

    #endregion

    #region Visual Updates

    void UpdateVisuals()
    {
        if (objRenderer == null) return;

        if (objectType == GridObjectType.Person)
        {
            UpdatePersonVisual();
            UpdatePlayableVisual();
        }
    }

    void UpdatePersonVisual()
    {
        if (objRenderer != null)
        {
            if (personMaterials != null && (int)personColor < personMaterials.Length && personMaterials[(int)personColor] != null)
            {
                objRenderer.material = personMaterials[(int)personColor];
                originalMaterial = objRenderer.material;
                Debug.Log($"Material kullanıldı: {personColor}");
            }
            else
            {
                Color targetColor = GetPersonColorValue(personColor);
                objRenderer.material.color = targetColor;
                Debug.Log($"Fallback renk kullanıldı: {personColor}");
            }

            Debug.Log($"UpdatePersonVisual tamamlandı - {personColor}");
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(selected);
        }

        if (useQuickOutline && outlineComponent != null && selected)
        {
            StartCoroutine(SelectionFlash());
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

    #endregion

    #region Movement System

    public void MoveTo(Vector3 newPosition, GridCell newCell = null)
    {
        if (objectType != GridObjectType.Person) return;

        targetPosition = newPosition;
        isMoving = true;

        if (gridCell != null)
        {
            gridCell.SetEmpty();
        }

        if (newCell != null)
        {
            gridCell = newCell;
            newCell.SetOccupied(gameObject);
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

            CheckPlayableStatus();
        }
    }

    #endregion

    #region Bus System

    public void BoardBus()
    {
        if (objectType != GridObjectType.Person) return;

        isInBus = true;

        if (gridCell != null)
        {
            gridCell.SetEmpty();
            gridCell = null;
        }

        OnPersonBoarded?.Invoke();

        StartCoroutine(BoardBusAnimation());
    }

    System.Collections.IEnumerator BoardBusAnimation()
    {
        Vector3 startPos = transform.position;
        Vector3 busDirection = Vector3.up * 3f; // Yukarı hareket

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, startPos + busDirection, t);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);

            yield return null;
        }

        gameObject.SetActive(false);
        Debug.Log($"{personColor} person boarded the bus");
    }

    public void SendToWaitingGrid()
    {
        if (objectType != GridObjectType.Person) return;

        isInWaitingGrid = true;

        if (gridCell != null)
        {
            gridCell.SetEmpty();
            gridCell = null;
        }

        Debug.Log($"{personColor} person marked for waiting grid");
    }

    #endregion

    #region Cleanup

    void OnDestroy()
    {
        if (transparentMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(transparentMaterial);
            else
                DestroyImmediate(transparentMaterial);
        }
    }

    #endregion

    #region Debug & Test Methods

    [ContextMenu("Test Outline States")]
    void TestOutlineStates()
    {
        StartCoroutine(TestOutlineSequence());
    }

    System.Collections.IEnumerator TestOutlineSequence()
    {
        Debug.Log("Testing BLACK Quick Outline states...");

        SetPlayableState(true);
        Debug.Log("   → Playable: BLACK outline should appear");
        yield return new WaitForSeconds(2f);

        SetPlayableState(false);
        Debug.Log("   → Non-playable: Outline should disappear, material should darken");
        yield return new WaitForSeconds(2f);

        Debug.Log("   → Testing RED flash effect...");
        StartCoroutine(FlashOutline(Color.red, 1f));
        yield return new WaitForSeconds(1.5f);

        SetPlayableState(true);
        Debug.Log("   → Testing selection flash (WHITE → BLACK)...");
        StartCoroutine(SelectionFlash());
        yield return new WaitForSeconds(1f);

        Debug.Log("BLACK Quick Outline test completed!");
    }

    [ContextMenu("Force Update Playable State")]
    void ForceUpdatePlayableState()
    {
        CheckPlayableStatus();
        Debug.Log($"Forced playable update: {isPlayable}");
    }

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
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.2f);
        }
    }

    #endregion
}