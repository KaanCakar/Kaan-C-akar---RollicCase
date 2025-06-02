using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Kaan Çakar 2025 - GridLevelDesignTool.cs
/// Complete final version - Simplified with erase session system
/// </summary>

#if UNITY_EDITOR
[CustomEditor(typeof(GridManager))]
public class GridLevelDesignTool : Editor
{
    private GridManager gridManager;
    private bool isEditingMode = false;
    private EditMode currentEditMode = EditMode.ErasePlayArea;
    private PersonColor selectedPersonColor = PersonColor.Red;
    private bool showToolbar = true;

    // Erase session system - ONLY UNDO SYSTEM
    private GridSnapshot eraseUndoSnapshot = null;
    private bool isInEraseSession = false;
    private float lastEraseTime = 0f;
    private const float eraseSessionTimeout = 3f;

    // Brush settings
    private bool isDragging = false;
    private Vector2Int lastPaintedCell = Vector2Int.one * -1;
    private int brushSize = 1;

    // Bus system
    private bool showBusSettings = true;
    private List<BusData> busSequence = new List<BusData>();
    private Vector2 busScrollPos;
    private PersonColor selectedBusColor = PersonColor.Red;
    private int selectedBusCapacity = 3;

    // Prefab references
    private GameObject[] personPrefabs = new GameObject[10];

    // Tool settings
    private Vector2 toolbarScrollPos;
    private Color[] personColors = {
        Color.red, Color.blue, Color.green, Color.yellow, Color.magenta,
        Color.cyan, Color.white, new Color(1f, 0.4f, 0.7f), new Color(1f, 0.5f, 0f),
        new Color(0.5f, 0f, 1f)
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
        for (int i = 0; i < 10; i++)
        {
            personPrefabs[i] = Resources.Load<GameObject>($"Prefabs/Person_{i}");
        }
    }

    void InitializeBusSequence()
    {
        if (busSequence.Count == 0)
        {
            busSequence.Add(new BusData { color = PersonColor.Red, capacity = 3 });
        }
    }

    // === ERASE SESSION UNDO SYSTEM ===
    [System.Serializable]
    public class GridSnapshot
    {
        public List<PlayAreaCellData> playAreaData;
        public List<GridObjectData> objectData;
        public string actionName;

        public GridSnapshot(GridManager gridManager, string action)
        {
            actionName = action;
            playAreaData = new List<PlayAreaCellData>();
            objectData = new List<GridObjectData>();

            if (gridManager.IsGridInitialized)
            {
                for (int x = 0; x < gridManager.gridWidth; x++)
                {
                    for (int z = 0; z < gridManager.gridHeight; z++)
                    {
                        GridCell cell = gridManager.GetCell(x, z);
                        if (cell != null)
                        {
                            playAreaData.Add(new PlayAreaCellData
                            {
                                x = x,
                                z = z,
                                isPlayArea = cell.isPlayArea,
                                isVisible = cell.isVisible
                            });

                            if (cell.occupyingObject != null)
                            {
                                var gridObject = cell.occupyingObject.GetComponent<GridObject>();
                                if (gridObject != null)
                                {
                                    objectData.Add(new GridObjectData
                                    {
                                        x = x,
                                        z = z,
                                        objectType = gridObject.objectType,
                                        personColor = gridObject.personColor
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void SaveEraseUndoSnapshot(string actionName)
    {
        if (gridManager == null || !gridManager.IsGridInitialized)
        {
            Debug.LogError("Cannot save undo snapshot: Grid not ready!");
            return;
        }

        try
        {
            eraseUndoSnapshot = new GridSnapshot(gridManager, actionName);
            isInEraseSession = false;
            Debug.Log($"Saved erase undo: {actionName} with {eraseUndoSnapshot.playAreaData?.Count ?? 0} cells");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving undo snapshot: {e.Message}");
            eraseUndoSnapshot = null;
        }
    }

    void StartEraseSessionIfNeeded(string actionName)
    {
        float currentTime = (float)EditorApplication.timeSinceStartup;

        if (!isInEraseSession || (currentTime - lastEraseTime) > eraseSessionTimeout)
        {
            SaveEraseUndoSnapshot(actionName);
            isInEraseSession = true;
            Debug.Log($"Started new erase session: {actionName}");
        }

        lastEraseTime = currentTime;
    }

    void PerformEraseUndo()
    {
        if (eraseUndoSnapshot == null)
        {
            EditorUtility.DisplayDialog("Undo", "No erase action to undo!", "OK");
            return;
        }

        Debug.Log($"Performing erase undo: {eraseUndoSnapshot.actionName}");

        try
        {
            ApplySnapshot(eraseUndoSnapshot);
            EditorUtility.DisplayDialog("Undo", $"Undid: {eraseUndoSnapshot.actionName}", "OK");
            eraseUndoSnapshot = null;
            isInEraseSession = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in PerformEraseUndo: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Undo failed: {e.Message}", "OK");
        }
    }

    void ApplySnapshot(GridSnapshot snapshot)
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager is null in ApplySnapshot!");
            EditorUtility.DisplayDialog("Error", "GridManager is null!", "OK");
            return;
        }

        if (snapshot == null)
        {
            Debug.LogError("Snapshot is null in ApplySnapshot!");
            EditorUtility.DisplayDialog("Error", "Snapshot is null!", "OK");
            return;
        }

        if (!gridManager.IsGridInitialized)
        {
            Debug.LogError("Grid is not initialized in ApplySnapshot!");
            EditorUtility.DisplayDialog("Error", "Grid is not initialized!", "OK");
            return;
        }

        Debug.Log($"Applying snapshot: {snapshot.actionName}");

        ClearAllObjectsQuiet();

        if (snapshot.playAreaData != null)
        {
            foreach (var areaData in snapshot.playAreaData)
            {
                if (areaData == null) continue;

                GridCell cell = gridManager.GetCell(areaData.x, areaData.z);
                if (cell != null)
                {
                    cell.SetPlayArea(areaData.isPlayArea);
                    cell.SetVisible(areaData.isVisible);
                }
            }
        }

        if (snapshot.objectData != null)
        {
            foreach (var objData in snapshot.objectData)
            {
                if (objData == null) continue;

                GridCell cell = gridManager.GetCell(objData.x, objData.z);
                if (cell != null && cell.isPlayArea)
                {
                    var oldColor = selectedPersonColor;
                    selectedPersonColor = objData.personColor;
                    PlaceObjectInCell(cell);
                    selectedPersonColor = oldColor;
                }
            }
        }

        EditorUtility.SetDirty(gridManager);
        SceneView.RepaintAll();
        Debug.Log("ApplySnapshot completed successfully");
    }

    void DrawBusSettings()
    {
        EditorGUILayout.Space(10);
        showBusSettings = EditorGUILayout.Foldout(showBusSettings, "Bus Sequence Settings", true, EditorStyles.foldoutHeader);

        if (showBusSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Configure Bus Order and Properties:", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space(5);

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

            if (busSequence.Count > 0)
            {
                EditorGUILayout.LabelField($"Bus Sequence ({busSequence.Count} buses):", EditorStyles.miniBoldLabel);

                busScrollPos = EditorGUILayout.BeginScrollView(busScrollPos, GUILayout.Height(150));

                for (int i = 0; i < busSequence.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));

                    Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
                    EditorGUI.DrawRect(colorRect, personColors[(int)busSequence[i].color]);

                    EditorGUILayout.LabelField($"{colorNames[(int)busSequence[i].color]} Bus", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Cap: {busSequence[i].capacity}", GUILayout.Width(45));

                    int newCapacity = EditorGUILayout.IntSlider(busSequence[i].capacity, 1, 6, GUILayout.Width(80));
                    if (newCapacity != busSequence[i].capacity)
                    {
                        var busData = busSequence[i];
                        busData.capacity = newCapacity;
                        busSequence[i] = busData;
                        EditorUtility.SetDirty(gridManager);
                    }

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

                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        RemoveBusFromSequence(i);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No buses in sequence. Add at least one bus to start the level.", MessageType.Warning);
            }

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
    }

    void DrawShapeSettings()
    {
        EditorGUILayout.LabelField("Grid Editing", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentEditMode == EditMode.ErasePlayArea, "Erase Play Area", EditorStyles.miniButtonLeft))
            currentEditMode = EditMode.ErasePlayArea;
        if (GUILayout.Toggle(currentEditMode == EditMode.PlacePerson, "Place Person", EditorStyles.miniButtonRight))
            currentEditMode = EditMode.PlacePerson;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (currentEditMode == EditMode.ErasePlayArea)
        {
            EditorGUILayout.LabelField("Click cells to remove from play area", EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Brush Size:", GUILayout.Width(70));
            brushSize = EditorGUILayout.IntSlider(brushSize, 1, 5);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.LabelField("Click on play area cells to place people", EditorStyles.helpBox);
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill All (12x12)"))
        {
            SaveEraseUndoSnapshot("Fill All 12x12");
            CreateFullGrid();
        }
        if (GUILayout.Button("Clear All"))
        {
            SaveEraseUndoSnapshot("Clear All");
            FillAllPlayArea(false);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = eraseUndoSnapshot != null;

        string undoButtonText = "Undo Erase Action";
        if (eraseUndoSnapshot != null)
        {
            undoButtonText = $"Undo: {eraseUndoSnapshot.actionName}";
        }

        if (GUILayout.Button(undoButtonText, GUILayout.Height(25)))
        {
            PerformEraseUndo();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Clear Undo"))
        {
            eraseUndoSnapshot = null;
            isInEraseSession = false;
            Debug.Log("Erase undo cleared");
        }
        EditorGUILayout.EndHorizontal();

        if (isInEraseSession)
        {
            EditorGUILayout.LabelField("✏️ Erase session active - multiple erases will be undone together", EditorStyles.helpBox);
        }
    }

    void CreateFullGrid()
    {
        if (gridManager == null) return;

        gridManager.gridWidth = 12;
        gridManager.gridHeight = 12;
        gridManager.ForceGridRecreation();

        FillAllPlayArea(true);
    }

    void DrawPersonSettings()
    {
        EditorGUILayout.LabelField("Person Placement", EditorStyles.boldLabel);

        if (GUILayout.Toggle(currentEditMode == EditMode.PlacePerson, "Place Person", EditorStyles.miniButton))
            currentEditMode = EditMode.PlacePerson;

        if (currentEditMode == EditMode.PlacePerson)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Person Color:", EditorStyles.miniBoldLabel);
            selectedPersonColor = (PersonColor)EditorGUILayout.EnumPopup(selectedPersonColor);

            Rect colorRect = GUILayoutUtility.GetRect(50, 20);
            EditorGUI.DrawRect(colorRect, personColors[(int)selectedPersonColor]);
        }
    }

    void DrawActionButtons()
    {
        if (GUILayout.Button("Clear All Objects"))
        {
            SaveEraseUndoSnapshot("Clear All Objects");
            ClearAllObjects();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Level Layout"))
        {
            SaveLevelLayout();
        }

        if (GUILayout.Button("Load Level Layout"))
        {
            LoadLevelLayout();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Play Area Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Save Current Play Area State", GUILayout.Height(25)))
        {
            gridManager.SaveCurrentPlayAreaState();
            EditorUtility.DisplayDialog("Play Area Saved",
                $"Saved {gridManager.runtimePlayAreaData.Count} play area entries", "OK");
        }

        if (gridManager.runtimePlayAreaData != null)
        {
            EditorGUILayout.LabelField($"Saved Play Area Entries: {gridManager.runtimePlayAreaData.Count}", EditorStyles.helpBox);
        }
    }

    void DrawHelpBox()
    {
        EditorGUILayout.HelpBox(
            "SIMPLIFIED WORKFLOW:\n" +
            "1. Click 'Fill All (12x12)' to create full grid\n" +
            "2. Use 'Erase Play Area' mode to remove unwanted cells\n" +
            "3. Switch to 'Place Person' to add people\n" +
            "4. Use Undo if you make mistakes\n" +
            "5. Save Play Area State before testing\n" +
            "Hold Shift to remove objects.",
            MessageType.Info
        );
    }

    void FillAllPlayArea(bool playArea)
    {
        if (gridManager == null)
        {
            EditorUtility.DisplayDialog("Error", "GridManager not found!", "OK");
            return;
        }

        if (!gridManager.IsGridInitialized)
        {
            if (gridManager.gridWidth <= 0 || gridManager.gridHeight <= 0)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Invalid grid dimensions: {gridManager.gridWidth}x{gridManager.gridHeight}", "OK");
                return;
            }

            gridManager.InitializeGrid();

            if (!gridManager.IsGridInitialized)
            {
                EditorUtility.DisplayDialog("Error", "Failed to initialize grid!", "OK");
                return;
            }
        }

        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            for (int z = 0; z < gridManager.gridHeight; z++)
            {
                GridCell cell = gridManager.GetCell(x, z);
                if (cell != null)
                {
                    cell.SetPlayArea(playArea);
                    cell.SetVisible(playArea);
                }
            }
        }

        EditorUtility.SetDirty(gridManager);
        SceneView.RepaintAll();
    }

    void PaintPlayArea(Vector2Int gridPos, bool playArea)
    {
        if (gridManager == null || !gridManager.IsGridInitialized)
            return;

        for (int x = -brushSize / 2; x <= brushSize / 2; x++)
        {
            for (int z = -brushSize / 2; z <= brushSize / 2; z++)
            {
                Vector2Int targetPos = gridPos + new Vector2Int(x, z);

                if (gridManager.IsValidPosition(targetPos))
                {
                    GridCell cell = gridManager.GetCell(targetPos);
                    if (cell != null)
                    {
                        cell.SetPlayArea(playArea);
                        cell.SetVisible(playArea);
                    }
                }
            }
        }
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

        busSequence.Clear();

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
        if (!isEditingMode || gridManager == null || !gridManager.IsGridInitialized) return;

        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            isDragging = true;
            HandleSceneClick(e);
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && isDragging)
        {
            HandleSceneClick(e);
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            isDragging = false;
            lastPaintedCell = Vector2Int.one * -1;
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
                if (gridPos == lastPaintedCell) return;
                lastPaintedCell = gridPos;

                GridCell cell = gridManager.GetCell(gridPos);

                if (cell != null)
                {
                    bool isRemoving = e.shift;

                    if (!isDragging && currentEditMode == EditMode.ErasePlayArea)
                    {
                        StartEraseSessionIfNeeded("Manual Erase Session");
                    }

                    if (currentEditMode == EditMode.ErasePlayArea)
                    {
                        PaintPlayArea(gridPos, false);
                    }
                    else if (currentEditMode == EditMode.PlacePerson)
                    {
                        if (cell.isPlayArea)
                        {
                            if (isRemoving)
                            {
                                RemoveObjectFromCell(cell);
                            }
                            else
                            {
                                PlaceObjectInCell(cell);
                            }
                        }
                    }

                    e.Use();
                    EditorUtility.SetDirty(gridManager);
                    SceneView.RepaintAll();
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

            gridObject.Initialize(cell, GridObjectType.Person);
            gridObject.personColor = selectedPersonColor;

            cell.SetOccupied(newObj);
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
        if (currentEditMode == EditMode.PlacePerson)
        {
            return personPrefabs[(int)selectedPersonColor];
        }
        return null;
    }

    void DrawSceneToolbar()
    {
        Handles.BeginGUI();

        GUILayout.BeginArea(new Rect(10, 10, 400, 260));
        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label("Grid Level Design Tool", EditorStyles.boldLabel);

        if (busSequence.Count > 0)
        {
            GUILayout.Label($"Bus Sequence ({busSequence.Count}):", EditorStyles.miniBoldLabel);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Mathf.Min(busSequence.Count, 8); i++)
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

        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentEditMode == EditMode.ErasePlayArea, "Erase", EditorStyles.miniButtonLeft))
            currentEditMode = EditMode.ErasePlayArea;
        if (GUILayout.Toggle(currentEditMode == EditMode.PlacePerson, "Person", EditorStyles.miniButtonRight))
            currentEditMode = EditMode.PlacePerson;
        GUILayout.EndHorizontal();

        if (currentEditMode == EditMode.ErasePlayArea)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Brush:", GUILayout.Width(40));
            brushSize = EditorGUILayout.IntSlider(brushSize, 1, 5);
            GUILayout.EndHorizontal();
        }

        if (currentEditMode == EditMode.PlacePerson)
        {
            GUILayout.Label("Color:", EditorStyles.miniBoldLabel);
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

        GUILayout.Space(3);

        GUI.enabled = eraseUndoSnapshot != null;

        string sceneUndoText = "Undo Erase";
        if (eraseUndoSnapshot != null)
        {
            sceneUndoText = $"Undo: {eraseUndoSnapshot.actionName}";
        }

        if (GUILayout.Button(sceneUndoText, GUILayout.Height(22)))
        {
            PerformEraseUndo();
        }
        GUI.enabled = true;

        if (isInEraseSession)
        {
            GUILayout.Label("✏️ Erase session", EditorStyles.miniLabel);
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
            ClearAllObjectsQuiet();
        }
    }

    void ClearAllObjectsQuiet()
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
        levelData.busSequence = new List<BusData>(busSequence);
        levelData.playAreaData = new List<PlayAreaCellData>();

        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            for (int z = 0; z < gridManager.gridHeight; z++)
            {
                GridCell cell = gridManager.GetCell(x, z);
                if (cell != null)
                {
                    levelData.playAreaData.Add(new PlayAreaCellData
                    {
                        x = x,
                        z = z,
                        isPlayArea = cell.isPlayArea,
                        isVisible = cell.isVisible
                    });

                    if (cell.occupyingObject != null)
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
        }

        return levelData;
    }

    void ApplyLevelData(LevelData levelData)
    {
        SaveEraseUndoSnapshot("Load Level Data");
        ClearAllObjectsQuiet();

        gridManager.gridWidth = levelData.gridWidth;
        gridManager.gridHeight = levelData.gridHeight;
        gridManager.ForceGridRecreation();

        if (levelData.busSequence != null && levelData.busSequence.Count > 0)
        {
            busSequence = new List<BusData>(levelData.busSequence);
        }

        if (levelData.playAreaData != null)
        {
            foreach (var areaData in levelData.playAreaData)
            {
                GridCell cell = gridManager.GetCell(areaData.x, areaData.z);
                if (cell != null)
                {
                    cell.SetPlayArea(areaData.isPlayArea);
                    cell.SetVisible(areaData.isVisible);
                }
            }
        }

        foreach (var objData in levelData.objects)
        {
            GridCell cell = gridManager.GetCell(objData.x, objData.z);
            if (cell != null && cell.isPlayArea)
            {
                var oldColor = selectedPersonColor;
                selectedPersonColor = objData.personColor;
                PlaceObjectInCell(cell);
                selectedPersonColor = oldColor;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Design Tool", EditorStyles.boldLabel);

        if (gridManager != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Grid Status:", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"• Initialized: {gridManager.IsGridInitialized}");
            EditorGUILayout.LabelField($"• Dimensions: {gridManager.gridWidth}x{gridManager.gridHeight}");

            if (gridManager.IsGridInitialized)
            {
                int nullCells = 0;
                for (int x = 0; x < Mathf.Min(5, gridManager.gridWidth); x++)
                {
                    for (int z = 0; z < Mathf.Min(5, gridManager.gridHeight); z++)
                    {
                        if (gridManager.GetCell(x, z) == null) nullCells++;
                    }
                }

                if (nullCells > 0)
                {
                    EditorGUILayout.LabelField($"• Problem: {nullCells} null cells detected!", EditorStyles.miniLabel);
                    GUI.color = Color.red;
                }
                else
                {
                    EditorGUILayout.LabelField("• Status: Grid cells OK", EditorStyles.miniLabel);
                    GUI.color = Color.green;
                }
            }

            GUI.color = Color.white;

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("FORCE INITIALIZE", GUILayout.Height(25)))
            {
                Debug.Log("=== FORCE INITIALIZE REQUESTED ===");
                gridManager.gridInitialized = false;
                gridManager.InitializeGrid();
                EditorUtility.SetDirty(gridManager);
            }

            if (GUILayout.Button("RECREATE GRID", GUILayout.Height(25)))
            {
                Debug.Log("=== RECREATE GRID REQUESTED ===");
                gridManager.ForceGridRecreation();
                EditorUtility.SetDirty(gridManager);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("GridManager not found!", MessageType.Error);
            return;
        }

        EditorGUILayout.Space(5);

        bool newEditingMode = EditorGUILayout.Toggle("Enable Editing Mode", isEditingMode);
        if (newEditingMode != isEditingMode)
        {
            isEditingMode = newEditingMode;
            SceneView.RepaintAll();
        }

        if (!isEditingMode) return;

        DrawBusSettings();
        EditorGUILayout.Space(5);
        DrawShapeSettings();
        EditorGUILayout.Space(5);
        DrawPersonSettings();
        EditorGUILayout.Space(10);
        DrawActionButtons();
        EditorGUILayout.Space(5);
        DrawHelpBox();
    }

    public List<BusData> GetBusSequence()
    {
        return new List<BusData>(busSequence);
    }
}

public enum EditMode
{
    ErasePlayArea,
    PlacePerson
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
    Person
}

[System.Serializable]
public class LevelData
{
    public int gridWidth;
    public int gridHeight;
    public List<GridObjectData> objects;
    public List<BusData> busSequence;
    public List<PlayAreaCellData> playAreaData;
}

[System.Serializable]
public class GridObjectData
{
    public int x;
    public int z;
    public GridObjectType objectType;
    public PersonColor personColor;
}

[System.Serializable]
public class PlayAreaCellData
{
    public int x;
    public int z;
    public bool isPlayArea;
    public bool isVisible;
}

#endif