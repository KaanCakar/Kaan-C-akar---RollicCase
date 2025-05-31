using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Kaan Çakar 2025 - GridLevelDesignTool.cs
/// Unity Editor tool for designing grid-based puzzle levels with bus system
/// </summary>

#if UNITY_EDITOR
[CustomEditor(typeof(GridManager))]
public class GridLevelDesignTool : Editor
{
    private GridManager gridManager;
    private bool isEditingMode = false;
    private PlacementMode currentPlacementMode = PlacementMode.Person;
    private PersonColor selectedPersonColor = PersonColor.Red;
    private bool showToolbar = true;
    
    // Bus system additions
    private bool showBusSettings = true;
    private List<BusData> busSequence = new List<BusData>();
    private Vector2 busScrollPos;
    private PersonColor selectedBusColor = PersonColor.Red;
    private int selectedBusCapacity = 3;
    
    // Prefab references
    private GameObject wallPrefab;
    private GameObject[] personPrefabs = new GameObject[10];
    
    // Tool settings
    private Vector2 toolbarScrollPos;
    private Color[] personColors = {
        Color.red, Color.blue, Color.green, Color.yellow, Color.magenta,
        Color.cyan, Color.white, new Color(1f, 0.4f, 0.7f), new Color(1f, 0.5f, 0f), // Pink, Orange
        new Color(0.5f, 0f, 1f) // Purple
    };
    
    private string[] colorNames = {
        "Red", "Blue", "Green", "Yellow", "Magenta",
        "Cyan", "White", "Pink", "Orange", "Purple"
    };
    
    void OnEnable()
    {
        gridManager = (GridManager)target;
        LoadPrefabReferences();
        InitializeBusSequence();
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    void LoadPrefabReferences()
    {
        // Prefabları Resources klasöründen yükle
        wallPrefab = Resources.Load<GameObject>("Prefabs/Wall");
        
        for (int i = 0; i < 10; i++)
        {
            personPrefabs[i] = Resources.Load<GameObject>($"Prefabs/Person_{i}");
        }
    }
    
    void InitializeBusSequence()
    {
        if (busSequence.Count == 0)
        {
            // Default bus sequence - sadece bir otobüs
            busSequence.Add(new BusData { color = PersonColor.Red, capacity = 3 });
        }
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Design Tool", EditorStyles.boldLabel);
        
        // Editing mode toggle
        bool newEditingMode = EditorGUILayout.Toggle("Enable Editing Mode", isEditingMode);
        if (newEditingMode != isEditingMode)
        {
            isEditingMode = newEditingMode;
            SceneView.RepaintAll();
        }
        
        if (!isEditingMode) return;
        
        // === BUS SETTINGS SECTION ===
        EditorGUILayout.Space(10);
        showBusSettings = EditorGUILayout.Foldout(showBusSettings, "Bus Sequence Settings", true, EditorStyles.foldoutHeader);
        
        if (showBusSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Configure Bus Order and Properties:", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space(5);
            
            // Bus addition controls
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add Bus:", GUILayout.Width(60));
            selectedBusColor = (PersonColor)EditorGUILayout.EnumPopup(selectedBusColor, GUILayout.Width(80));
            
            EditorGUILayout.LabelField("Capacity:", GUILayout.Width(55));
            selectedBusCapacity = EditorGUILayout.IntSlider(selectedBusCapacity, 1, 6, GUILayout.Width(100));
            
            if (GUILayout.Button("Add Bus", GUILayout.Width(70)))
            {
                AddBusToSequence();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Bus sequence list
            if (busSequence.Count > 0)
            {
                EditorGUILayout.LabelField($"Bus Sequence ({busSequence.Count} buses):", EditorStyles.miniBoldLabel);
                
                busScrollPos = EditorGUILayout.BeginScrollView(busScrollPos, GUILayout.Height(150));
                
                for (int i = 0; i < busSequence.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    // Bus order number
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));
                    
                    // Color preview
                    Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
                    EditorGUI.DrawRect(colorRect, personColors[(int)busSequence[i].color]);
                    
                    // Bus info
                    EditorGUILayout.LabelField($"{colorNames[(int)busSequence[i].color]} Bus", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Cap: {busSequence[i].capacity}", GUILayout.Width(45));
                    
                    // Edit capacity
                    int newCapacity = EditorGUILayout.IntSlider(busSequence[i].capacity, 1, 6, GUILayout.Width(80));
                    if (newCapacity != busSequence[i].capacity)
                    {
                        var busData = busSequence[i];
                        busData.capacity = newCapacity;
                        busSequence[i] = busData;
                        EditorUtility.SetDirty(gridManager);
                    }
                    
                    // Move up/down buttons
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("↑", GUILayout.Width(25)))
                    {
                        SwapBuses(i, i - 1);
                    }
                    
                    GUI.enabled = i < busSequence.Count - 1;
                    if (GUILayout.Button("↓", GUILayout.Width(25)))
                    {
                        SwapBuses(i, i + 1);
                    }
                    
                    GUI.enabled = true;
                    
                    // Remove button
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        RemoveBusFromSequence(i);
                        break; // Exit loop since we modified the collection
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No buses in sequence. Add at least one bus to start the level.", MessageType.Warning);
            }
            
            // Action buttons
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All Buses"))
            {
                ClearBusSequence();
            }
            if (GUILayout.Button("Auto-Generate from Grid"))
            {
                GenerateBusesFromGrid();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space(5);
        
        // === GRID PLACEMENT SECTION ===
        EditorGUILayout.LabelField("Grid Placement", EditorStyles.boldLabel);
        
        // Placement mode selection
        EditorGUILayout.LabelField("Placement Mode:", EditorStyles.miniBoldLabel);
        currentPlacementMode = (PlacementMode)EditorGUILayout.EnumPopup(currentPlacementMode);
        
        // Person color selection (only for person mode)
        if (currentPlacementMode == PlacementMode.Person)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Person Color:", EditorStyles.miniBoldLabel);
            selectedPersonColor = (PersonColor)EditorGUILayout.EnumPopup(selectedPersonColor);
            
            // Color preview
            Rect colorRect = GUILayoutUtility.GetRect(50, 20);
            EditorGUI.DrawRect(colorRect, personColors[(int)selectedPersonColor]);
        }
        
        EditorGUILayout.Space(10);
        
        // Action buttons
        if (GUILayout.Button("Clear All Grid Objects"))
        {
            ClearAllObjects();
        }
        
        if (GUILayout.Button("Save Level Layout"))
        {
            SaveLevelLayout();
        }
        
        if (GUILayout.Button("Load Level Layout"))
        {
            LoadLevelLayout();
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Configure bus sequence above, then click on grid cells in Scene view to place objects.\n" +
            "Hold Shift to remove objects.\n" +
            "Use 'Auto-Generate from Grid' to create buses based on people colors.",
            MessageType.Info
        );
    }
    
    // === BUS SEQUENCE METHODS ===
    void AddBusToSequence()
    {
        busSequence.Add(new BusData { color = selectedBusColor, capacity = selectedBusCapacity });
        EditorUtility.SetDirty(gridManager);
    }
    
    void RemoveBusFromSequence(int index)
    {
        if (index >= 0 && index < busSequence.Count)
        {
            busSequence.RemoveAt(index);
            EditorUtility.SetDirty(gridManager);
        }
    }
    
    void SwapBuses(int indexA, int indexB)
    {
        if (indexA >= 0 && indexA < busSequence.Count && indexB >= 0 && indexB < busSequence.Count)
        {
            var temp = busSequence[indexA];
            busSequence[indexA] = busSequence[indexB];
            busSequence[indexB] = temp;
            EditorUtility.SetDirty(gridManager);
        }
    }
    
    void ClearBusSequence()
    {
        if (EditorUtility.DisplayDialog("Clear Bus Sequence", 
            "Are you sure you want to clear all buses from the sequence?", 
            "Yes", "Cancel"))
        {
            busSequence.Clear();
            EditorUtility.SetDirty(gridManager);
        }
    }
    
    void GenerateBusesFromGrid()
    {
        // Grid'deki mevcut renkleri analiz et ve onlara göre otobüs ekle
        var usedColors = new HashSet<PersonColor>();
        
        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            for (int z = 0; z < gridManager.gridHeight; z++)
            {
                GridCell cell = gridManager.GetCell(x, z);
                if (cell != null && cell.occupyingObject != null)
                {
                    var gridObject = cell.occupyingObject.GetComponent<GridObject>();
                    if (gridObject != null && gridObject.objectType == GridObjectType.Person)
                    {
                        usedColors.Add(gridObject.personColor);
                    }
                }
            }
        }
        
        // Mevcut otobüsleri temizle
        busSequence.Clear();
        
        // Her renk için otobüs ekle
        foreach (var color in usedColors)
        {
            busSequence.Add(new BusData { color = color, capacity = 3 });
        }
        
        if (usedColors.Count == 0)
        {
            EditorUtility.DisplayDialog("No People Found", 
                "Please place some people on the grid first before using Auto-Generate.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Buses Generated", 
                $"Generated {usedColors.Count} buses based on people colors in the grid.", "OK");
        }
        
        EditorUtility.SetDirty(gridManager);
    }
    
    // === SCENE GUI METHODS ===
    static void OnSceneGUI(SceneView sceneView)
    {
        foreach (var editor in Resources.FindObjectsOfTypeAll<GridLevelDesignTool>())
        {
            editor.InstanceOnSceneGUI(sceneView);
        }
    }

    void InstanceOnSceneGUI(SceneView sceneView)
    {
        if (!isEditingMode || gridManager == null) return;

        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            HandleSceneClick(e);
        }

        if (showToolbar)
        {
            DrawSceneToolbar();
        }
    }
    
    void HandleSceneClick(Event e)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            Vector2Int gridPos = gridManager.GetGridPosition(hit.point);
            
            if (gridManager.IsValidPosition(gridPos))
            {
                GridCell cell = gridManager.GetCell(gridPos);
                
                if (cell != null)
                {
                    bool isRemoving = e.shift;
                    
                    if (isRemoving)
                    {
                        RemoveObjectFromCell(cell);
                    }
                    else
                    {
                        PlaceObjectInCell(cell);
                    }
                    
                    e.Use();
                    EditorUtility.SetDirty(gridManager);
                }
            }
        }
    }
    
    void PlaceObjectInCell(GridCell cell)
    {
        RemoveObjectFromCell(cell);
        
        GameObject prefabToPlace = GetSelectedPrefab();
        
        if (prefabToPlace != null)
        {
            GameObject newObj = PrefabUtility.InstantiatePrefab(prefabToPlace) as GameObject;
            newObj.transform.position = cell.worldPosition;
            newObj.transform.parent = gridManager.transform;
            
            var gridObject = newObj.GetComponent<GridObject>();
            if (gridObject == null)
                gridObject = newObj.AddComponent<GridObject>();
            
            gridObject.Initialize(cell, GetObjectType());
            
            cell.SetOccupied(newObj);
            cell.SetWalkable(false);
        }
    }
    
    void RemoveObjectFromCell(GridCell cell)
    {
        if (cell.occupyingObject != null)
        {
            DestroyImmediate(cell.occupyingObject);
            cell.SetEmpty();
        }
    }
    
    GameObject GetSelectedPrefab()
    {
        switch (currentPlacementMode)
        {
            case PlacementMode.Wall:
                return wallPrefab;
            case PlacementMode.Person:
                return personPrefabs[(int)selectedPersonColor];
            default:
                return null;
        }
    }
    
    GridObjectType GetObjectType()
    {
        switch (currentPlacementMode)
        {
            case PlacementMode.Wall:
                return GridObjectType.Wall;
            case PlacementMode.Person:
                return GridObjectType.Person;
            default:
                return GridObjectType.Wall;
        }
    }
    
    void DrawSceneToolbar()
    {
        Handles.BeginGUI();
        
        GUILayout.BeginArea(new Rect(10, 10, 350, 200));
        GUILayout.BeginVertical(GUI.skin.box);
        
        GUILayout.Label("Grid Level Design Tool", EditorStyles.boldLabel);
        
        // Bus sequence preview in scene
        if (busSequence.Count > 0)
        {
            GUILayout.Label($"Bus Sequence ({busSequence.Count}):", EditorStyles.miniBoldLabel);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Mathf.Min(busSequence.Count, 8); i++) // Show max 8 buses
            {
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = personColors[(int)busSequence[i].color];
                GUILayout.Button($"{busSequence[i].capacity}", GUILayout.Width(25), GUILayout.Height(20));
                GUI.backgroundColor = oldColor;
            }
            if (busSequence.Count > 8)
            {
                GUILayout.Label("...", EditorStyles.miniLabel);
            }
            GUILayout.EndHorizontal();
        }
        
        GUILayout.Space(5);
        
        // Quick mode selection
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentPlacementMode == PlacementMode.Wall, "Wall", EditorStyles.miniButton))
            currentPlacementMode = PlacementMode.Wall;
        if (GUILayout.Toggle(currentPlacementMode == PlacementMode.Person, "Person", EditorStyles.miniButton))
            currentPlacementMode = PlacementMode.Person;
        GUILayout.EndHorizontal();
        
        // Person color selection for scene toolbar
        if (currentPlacementMode == PlacementMode.Person)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 5; i++)
            {
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = personColors[i];
                if (GUILayout.Button("", GUILayout.Width(25), GUILayout.Height(20)))
                {
                    selectedPersonColor = (PersonColor)i;
                }
                GUI.backgroundColor = oldColor;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            for (int i = 5; i < 10; i++)
            {
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = personColors[i];
                if (GUILayout.Button("", GUILayout.Width(25), GUILayout.Height(20)))
                {
                    selectedPersonColor = (PersonColor)i;
                }
                GUI.backgroundColor = oldColor;
            }
            GUILayout.EndHorizontal();
        }
        
        GUILayout.Label("Shift + Click to remove", EditorStyles.miniLabel);
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
        
        Handles.EndGUI();
    }
    
    // === FILE OPERATIONS ===
    void ClearAllObjects()
    {
        if (EditorUtility.DisplayDialog("Clear All Objects", 
            "Are you sure you want to clear all placed objects from the grid?", 
            "Yes", "Cancel"))
        {
            for (int x = 0; x < gridManager.gridWidth; x++)
            {
                for (int z = 0; z < gridManager.gridHeight; z++)
                {
                    GridCell cell = gridManager.GetCell(x, z);
                    if (cell != null)
                    {
                        RemoveObjectFromCell(cell);
                    }
                }
            }
            
            EditorUtility.SetDirty(gridManager);
        }
    }
    
    void SaveLevelLayout()
    {
        string path = EditorUtility.SaveFilePanel("Save Level Layout", 
            "Assets/Levels", "Level_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"), "json");
        
        if (!string.IsNullOrEmpty(path))
        {
            LevelData levelData = CreateLevelData();
            string json = JsonUtility.ToJson(levelData, true);
            System.IO.File.WriteAllText(path, json);
            
            Debug.Log($"Level saved to: {path}");
            AssetDatabase.Refresh();
        }
    }
    
    void LoadLevelLayout()
    {
        string path = EditorUtility.OpenFilePanel("Load Level Layout", "Assets/Levels", "json");
        
        if (!string.IsNullOrEmpty(path))
        {
            string json = System.IO.File.ReadAllText(path);
            LevelData levelData = JsonUtility.FromJson<LevelData>(json);
            ApplyLevelData(levelData);
            
            Debug.Log($"Level loaded from: {path}");
            EditorUtility.SetDirty(gridManager);
        }
    }
    
    LevelData CreateLevelData()
    {
        LevelData levelData = new LevelData();
        levelData.gridWidth = gridManager.gridWidth;
        levelData.gridHeight = gridManager.gridHeight;
        levelData.objects = new List<GridObjectData>();
        levelData.busSequence = new List<BusData>(busSequence); // Save bus sequence
        
        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            for (int z = 0; z < gridManager.gridHeight; z++)
            {
                GridCell cell = gridManager.GetCell(x, z);
                if (cell != null && cell.occupyingObject != null)
                {
                    var gridObject = cell.occupyingObject.GetComponent<GridObject>();
                    if (gridObject != null)
                    {
                        GridObjectData objData = new GridObjectData();
                        objData.x = x;
                        objData.z = z;
                        objData.objectType = gridObject.objectType;
                        objData.personColor = gridObject.personColor;
                        levelData.objects.Add(objData);
                    }
                }
            }
        }
        
        return levelData;
    }
    
    void ApplyLevelData(LevelData levelData)
    {
        ClearAllObjects();
        
        gridManager.gridWidth = levelData.gridWidth;
        gridManager.gridHeight = levelData.gridHeight;
        
        // Load bus sequence if available
        if (levelData.busSequence != null && levelData.busSequence.Count > 0)
        {
            busSequence = new List<BusData>(levelData.busSequence);
        }
        
        foreach (var objData in levelData.objects)
        {
            GridCell cell = gridManager.GetCell(objData.x, objData.z);
            if (cell != null)
            {
                var oldMode = currentPlacementMode;
                var oldColor = selectedPersonColor;
                
                switch (objData.objectType)
                {
                    case GridObjectType.Wall:
                        currentPlacementMode = PlacementMode.Wall;
                        break;
                    case GridObjectType.Person:
                        currentPlacementMode = PlacementMode.Person;
                        selectedPersonColor = objData.personColor;
                        break;
                }
                
                PlaceObjectInCell(cell);
                
                currentPlacementMode = oldMode;
                selectedPersonColor = oldColor;
            }
        }
    }
    
    // Public method to get bus sequence for GameManager
    public List<BusData> GetBusSequence()
    {
        return new List<BusData>(busSequence);
    }
}

// === ENUMS AND DATA CLASSES ===
public enum PlacementMode
{
    Wall,
    Person
}

public enum PersonColor
{
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3,
    Magenta = 4,
    Cyan = 5,
    White = 6,
    Pink = 7,
    Orange = 8,
    Purple = 9
}

public enum GridObjectType
{
    Wall,
    Person
}

[System.Serializable]
public class LevelData
{
    public int gridWidth;
    public int gridHeight;
    public List<GridObjectData> objects;
    public List<BusData> busSequence; // Bus sequence data
}

[System.Serializable]
public class GridObjectData
{
    public int x;
    public int z;
    public GridObjectType objectType;
    public PersonColor personColor;
}

#endif