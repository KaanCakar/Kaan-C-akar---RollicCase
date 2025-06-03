using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

/// <summary>
/// Kaan Çakar 2025 - GridLevelDesignTool.cs
/// Grid level design tool for Unity Editor.
/// </summary>

// Enums for various settings and configurations
public enum TimerDisplayFormat
{
    SecondsOnly,        // "45"
    MinutesSeconds,     // "1:30"
    MinutesSecondsMs    // "1:30:50"
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

#if UNITY_EDITOR
[CustomEditor(typeof(GridManager))]
public class GridLevelDesignTool : Editor
{
    private GridManager gridManager;
    private bool isEditingMode = false;
    private EditMode currentEditMode = EditMode.ErasePlayArea;
    private PersonColor selectedPersonColor = PersonColor.Red;
    private bool showToolbar = true;

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

    // Level timer settings
    [Header("Level Timer Settings")]
    public bool enableLevelTimer = true;
    public float levelTimeInSeconds = 60f;
    public TimerDisplayFormat timerFormat = TimerDisplayFormat.MinutesSeconds;

    // Scene Level Manager
    [Header("Scene Level Manager")]
    public string levelNamePrefix = "Level_";
    public string levelsFolder = "Assets/Scenes/Levels/";
    public int currentLevelNumber = 1;
    public bool autoIncrementLevel = true;

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

    #region GUI Drawing Methods

    void DrawBusSettings()
    {
        showBusSettings = EditorGUILayout.Foldout(showBusSettings, "Bus Sequence Settings", true, EditorStyles.foldoutHeader);

        if (!showBusSettings) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        try
        {
            EditorGUILayout.LabelField("Configure Bus Order and Properties:", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space(5);

            // Add Bus Section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            try
            {
                EditorGUILayout.LabelField("Add New Bus:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Color:", GUILayout.Width(50));
                selectedBusColor = (PersonColor)EditorGUILayout.EnumPopup(selectedBusColor, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Capacity:", GUILayout.Width(60));
                selectedBusCapacity = EditorGUILayout.IntSlider(selectedBusCapacity, 1, 6);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);
                if (GUILayout.Button("Add Bus", GUILayout.Height(25)))
                {
                    AddBusToSequence();
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            if (busSequence.Count > 0)
            {
                EditorGUILayout.LabelField($"Bus Sequence ({busSequence.Count} buses):", EditorStyles.miniBoldLabel);

                busScrollPos = EditorGUILayout.BeginScrollView(busScrollPos, GUILayout.Height(200));
                try
                {
                    for (int i = 0; i < busSequence.Count; i++)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        try
                        {
                            // Bus info row
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"Bus {i + 1}:", EditorStyles.boldLabel, GUILayout.Width(50));

                            Rect colorRect = GUILayoutUtility.GetRect(30, 20, GUILayout.Width(30));
                            EditorGUI.DrawRect(colorRect, personColors[(int)busSequence[i].color]);

                            EditorGUILayout.LabelField($"{colorNames[(int)busSequence[i].color]}", GUILayout.Width(80));
                            EditorGUILayout.EndHorizontal();

                            // Capacity row
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Capacity:", GUILayout.Width(60));

                            int newCapacity = EditorGUILayout.IntSlider(busSequence[i].capacity, 1, 6);
                            if (newCapacity != busSequence[i].capacity)
                            {
                                var busData = busSequence[i];
                                busData.capacity = newCapacity;
                                busSequence[i] = busData;
                                EditorUtility.SetDirty(gridManager);
                            }
                            EditorGUILayout.EndHorizontal();

                            // Buttons row
                            EditorGUILayout.BeginHorizontal();
                            try
                            {
                                // Move Up button
                                bool canMoveUp = i > 0;
                                GUI.enabled = canMoveUp;
                                GUI.backgroundColor = canMoveUp ? Color.cyan : Color.gray;
                                if (GUILayout.Button("Move Up ↑", GUILayout.Height(25)))
                                {
                                    SwapBuses(i, i - 1);
                                }

                                // Move Down button  
                                bool canMoveDown = i < busSequence.Count - 1;
                                GUI.enabled = canMoveDown;
                                GUI.backgroundColor = canMoveDown ? Color.cyan : Color.gray;
                                if (GUILayout.Button("Move Down ↓", GUILayout.Height(25)))
                                {
                                    SwapBuses(i, i + 1);
                                }

                                // Delete button
                                GUI.enabled = true;
                                GUI.backgroundColor = Color.red;
                                if (GUILayout.Button("Delete ×", GUILayout.Height(25)))
                                {
                                    RemoveBusFromSequence(i);
                                    GUI.backgroundColor = Color.white;
                                    break;
                                }
                                GUI.backgroundColor = Color.white;
                            }
                            finally
                            {
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        finally
                        {
                            EditorGUILayout.EndVertical();
                        }

                        EditorGUILayout.Space(5);
                    }
                }
                finally
                {
                    EditorGUILayout.EndScrollView();
                }

                // Bus sequence preview
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Bus Order Preview:", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                try
                {
                    for (int i = 0; i < busSequence.Count; i++)
                    {
                        Color oldColor = GUI.backgroundColor;
                        GUI.backgroundColor = personColors[(int)busSequence[i].color];

                        string buttonText = $"{i + 1}\n{busSequence[i].color}\n({busSequence[i].capacity})";
                        GUILayout.Button(buttonText, GUILayout.Width(60), GUILayout.Height(50));

                        GUI.backgroundColor = oldColor;

                        if (i < busSequence.Count - 1)
                        {
                            GUILayout.Label("→", GUILayout.Width(15));
                        }
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No buses in sequence. Add at least one bus to start the level.", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            try
            {
                if (GUILayout.Button("Clear All Buses", GUILayout.Height(30)))
                {
                    ClearBusSequence();
                }
                if (GUILayout.Button("Auto-Generate from Grid", GUILayout.Height(30)))
                {
                    GenerateBusesFromGrid();
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            // Save bus sequence buttons
            EditorGUILayout.LabelField("Bus Sequence Save/Load:", EditorStyles.boldLabel);

            if (GUILayout.Button("Save Current Bus Sequence", GUILayout.Height(25)))
            {
                SaveCurrentBusSequence();
            }

            if (GUILayout.Button("Force Update GameManager", GUILayout.Height(25)))
            {
                ForceUpdateGridManagerBusData();
            }
        }
        finally
        {
            EditorGUILayout.EndVertical();
        }
    }

    void DrawTimerSettings()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Timer Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Timer enable/disable
        enableLevelTimer = EditorGUILayout.Toggle("Enable Level Timer", enableLevelTimer);

        if (enableLevelTimer)
        {
            EditorGUILayout.Space(5);

            // Timer duration ayarları
            EditorGUILayout.LabelField("Timer Duration:", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Time Limit:", GUILayout.Width(80));
            levelTimeInSeconds = EditorGUILayout.FloatField(levelTimeInSeconds, GUILayout.Width(60));
            EditorGUILayout.LabelField("seconds", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Quick Presets:", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("30s", GUILayout.Width(40)))
                levelTimeInSeconds = 30f;
            if (GUILayout.Button("1m", GUILayout.Width(40)))
                levelTimeInSeconds = 60f;
            if (GUILayout.Button("2m", GUILayout.Width(40)))
                levelTimeInSeconds = 120f;
            if (GUILayout.Button("3m", GUILayout.Width(40)))
                levelTimeInSeconds = 180f;
            if (GUILayout.Button("5m", GUILayout.Width(40)))
                levelTimeInSeconds = 300f;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Display format
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Display Format:", GUILayout.Width(100));
            timerFormat = (TimerDisplayFormat)EditorGUILayout.EnumPopup(timerFormat);
            EditorGUILayout.EndHorizontal();

            // Preview
            EditorGUILayout.Space(3);
            string previewText = FormatTime(levelTimeInSeconds, timerFormat);
            EditorGUILayout.LabelField($"Preview: {previewText}", EditorStyles.helpBox);

            EditorGUILayout.Space(5);

            // Timer info
            EditorGUILayout.HelpBox(
                "Timer Info:\n" +
                "• Starts on first person click\n" +
                "• Does NOT start in Start() function\n" +
                "• Level is lost when time runs out",
                MessageType.Info
            );

            // Save button
            if (GUILayout.Button("Save Timer Settings", GUILayout.Height(25)))
            {
                SaveTimerSettings();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Timer devre dışı - Level'da süre sınırı yok", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    void DrawSceneLevelManager()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Scene Level Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Level naming settings
        EditorGUILayout.LabelField("Level Settings:", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Level Prefix:", GUILayout.Width(80));
        levelNamePrefix = EditorGUILayout.TextField(levelNamePrefix, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Level Number:", GUILayout.Width(80));
        currentLevelNumber = EditorGUILayout.IntField(currentLevelNumber, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Save Folder:", GUILayout.Width(80));
        levelsFolder = EditorGUILayout.TextField(levelsFolder);
        EditorGUILayout.EndHorizontal();

        // Preview
        string previewName = $"{levelNamePrefix}{currentLevelNumber:00}";
        string fullPath = $"{levelsFolder}{previewName}.unity";
        EditorGUILayout.LabelField($"Preview: {fullPath}", EditorStyles.helpBox);

        EditorGUILayout.Space(5);

        // Action buttons
        EditorGUILayout.BeginHorizontal();

        // Save current scene as level
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button($"Save as {previewName}", GUILayout.Height(30)))
        {
            SaveCurrentSceneAsLevel();
        }

        // Auto-increment checkbox
        GUI.backgroundColor = Color.white;
        autoIncrementLevel = EditorGUILayout.Toggle(autoIncrementLevel, GUILayout.Width(20));
        EditorGUILayout.LabelField("Auto +1", GUILayout.Width(50));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Quick save buttons
        EditorGUILayout.LabelField("Quick Save:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();

        for (int i = 1; i <= 5; i++)
        {
            string quickName = $"{levelNamePrefix}{i:00}";
            if (GUILayout.Button(quickName, GUILayout.Width(60)))
            {
                currentLevelNumber = i;
                SaveCurrentSceneAsLevel();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Management buttons
        EditorGUILayout.LabelField("Level Management:", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Open Levels Folder", GUILayout.Height(25)))
        {
            OpenLevelsFolder();
        }

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("List All Levels", GUILayout.Height(25)))
        {
            ListAllLevels();
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Info box
        EditorGUILayout.HelpBox(
            "Scene Level System:\n" +
            "• Saves complete scene copy with all assets\n" +
            "• Includes Camera, Canvas, GameManager, etc.\n" +
            "• Easy to load/test individual levels\n" +
            "• Build Settings'e otomatik eklenir",
            MessageType.Info
        );

        EditorGUILayout.EndVertical();
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
            CreateFullGrid();
        }
        if (GUILayout.Button("Clear All"))
        {
            FillAllPlayArea(false);
        }
        EditorGUILayout.EndHorizontal();
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
            "WORKFLOW:\n" +
            "1. Click 'Fill All (12x12)' to create full grid\n" +
            "2. Use 'Erase Play Area' mode to remove unwanted cells\n" +
            "3. Switch to 'Place Person' to add people\n" +
            "4. Configure bus sequence\n" +
            "5. Save Play Area State before testing\n" +
            "Hold Shift to remove objects.",
            MessageType.Info
        );
    }

    #endregion

    #region Scene Level Manager Methods

    void SaveCurrentSceneAsLevel()
    {
        string sceneName = $"{levelNamePrefix}{currentLevelNumber:00}";
        string fullPath = $"{levelsFolder}{sceneName}.unity";

        Debug.Log($"=== SAVING SCENE AS LEVEL ===");
        Debug.Log($"Level Name: {sceneName}");
        Debug.Log($"Full Path: {fullPath}");

        try
        {
            string folderPath = levelsFolder.Replace("Assets/", "");
            if (!AssetDatabase.IsValidFolder(levelsFolder.TrimEnd('/')))
            {
                string[] folders = folderPath.Split('/');
                string currentPath = "Assets";

                foreach (string folder in folders)
                {
                    if (!string.IsNullOrEmpty(folder))
                    {
                        string newPath = $"{currentPath}/{folder}";
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folder);
                            Debug.Log($"Created folder: {newPath}");
                        }
                        currentPath = newPath;
                    }
                }
            }

            if (EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()))
            {
                Debug.Log("Current scene saved");
            }

            bool success = AssetDatabase.CopyAsset(EditorSceneManager.GetActiveScene().path, fullPath);

            if (success)
            {
                AddSceneToBuildSettings(fullPath);

                SaveLevelDataBackup(sceneName);

                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Level Saved Successfully!",
                    $"Scene saved as: {sceneName}\n\n" +
                    $"Location: {fullPath}\n" +
                    $"Added to Build Settings\n" +
                    $"JSON backup created", "OK");

                Debug.Log($"Level saved successfully: {sceneName}");

                if (autoIncrementLevel)
                {
                    currentLevelNumber++;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Save Failed",
                    $"Failed to save scene as {sceneName}\n" +
                    $"Check console for details.", "OK");
                Debug.LogError($"Failed to save scene: {fullPath}");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error",
                $"Error saving level: {e.Message}", "OK");
            Debug.LogError($"Exception saving level: {e.Message}");
        }

        Debug.Log("=== SAVE SCENE AS LEVEL COMPLETED ===");
    }

    void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool sceneExists = scenes.Any(scene => scene.path == scenePath);

        if (!sceneExists)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log($"Added to Build Settings: {scenePath}");
        }
        else
        {
            Debug.Log($"Scene already in Build Settings: {scenePath}");
        }
    }

    void SaveLevelDataBackup(string levelName)
    {
        try
        {
            LevelData levelData = CreateLevelData();
            levelData.levelName = levelName;

            string json = JsonUtility.ToJson(levelData, true);
            string jsonPath = $"{levelsFolder}{levelName}_data.json";

            System.IO.File.WriteAllText(jsonPath, json);
            AssetDatabase.Refresh();

            Debug.Log($"Level data backup saved: {jsonPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not save level data backup: {e.Message}");
        }
    }

    void OpenLevelsFolder()
    {
        string absolutePath = System.IO.Path.GetFullPath(levelsFolder);

        if (System.IO.Directory.Exists(absolutePath))
        {
            EditorUtility.RevealInFinder(absolutePath);
            Debug.Log($"Opened folder: {absolutePath}");
        }
        else
        {
            EditorUtility.DisplayDialog("Folder Not Found",
                $"Levels folder does not exist:\n{absolutePath}\n\n" +
                "Create it by saving a level first.", "OK");
        }
    }

    void ListAllLevels()
    {
        Debug.Log("=== ALL LEVELS IN PROJECT ===");

        if (!AssetDatabase.IsValidFolder(levelsFolder.TrimEnd('/')))
        {
            Debug.LogWarning("Levels folder does not exist!");
            EditorUtility.DisplayDialog("No Levels Found",
                "Levels folder does not exist. Create levels first.", "OK");
            return;
        }

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { levelsFolder.TrimEnd('/') });

        if (sceneGuids.Length == 0)
        {
            Debug.Log("No level scenes found.");
            EditorUtility.DisplayDialog("No Levels",
                "No level scenes found in the levels folder.", "OK");
            return;
        }

        Debug.Log($"Found {sceneGuids.Length} level(s):");

        List<string> levelList = new List<string>();

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            Debug.Log($"  • {fileName} ({path})");
            levelList.Add(fileName);
        }

        string levelNames = string.Join("\n", levelList);
        EditorUtility.DisplayDialog("Level List",
            $"Found {sceneGuids.Length} levels:\n\n{levelNames}", "OK");
    }

    void DrawSceneLevelManagerSafe()
    {
        try
        {
            DrawSceneLevelManager();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Scene Level Manager Error: {e.Message}", MessageType.Error);
        }
    }

    #endregion

    #region Utility Methods

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

    string FormatTime(float timeInSeconds, TimerDisplayFormat format)
    {
        int totalSeconds = Mathf.FloorToInt(timeInSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        int milliseconds = Mathf.FloorToInt((timeInSeconds - totalSeconds) * 100);

        switch (format)
        {
            case TimerDisplayFormat.SecondsOnly:
                return totalSeconds.ToString();

            case TimerDisplayFormat.MinutesSeconds:
                return $"{minutes}:{seconds:00}";

            case TimerDisplayFormat.MinutesSecondsMs:
                return $"{minutes}:{seconds:00}:{milliseconds:00}";

            default:
                return $"{minutes}:{seconds:00}";
        }
    }

    void SaveTimerSettings()
    {
        Debug.Log("=== SAVING TIMER SETTINGS ===");

        LevelData tempLevelData = CreateLevelData();

        tempLevelData.timerEnabled = enableLevelTimer;
        tempLevelData.levelTimeInSeconds = levelTimeInSeconds;
        tempLevelData.timerFormat = timerFormat;

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.currentLevelData = tempLevelData;
            if (TimerUI.Instance != null)
            {
                TimerUI.Instance.UpdateTimerSettings(levelTimeInSeconds, timerFormat, enableLevelTimer);
                Debug.Log($"TimerUI updated with new settings");
            }

            EditorUtility.SetDirty(gameManager);

            string statusMessage = enableLevelTimer ?
                $"Timer: {FormatTime(levelTimeInSeconds, timerFormat)} ({timerFormat})" :
                "Timer: Disabled";

            EditorUtility.DisplayDialog("Timer Settings Saved",
                $"Timer settings saved successfully!\n\n" +
                $"{statusMessage}\n\n" +
                "Changes will take effect immediately in Play mode.", "OK");

            Debug.Log($"Timer settings saved: Enabled={enableLevelTimer}, Time={levelTimeInSeconds}s, Format={timerFormat}");
        }
        else
        {
            EditorUtility.DisplayDialog("GameManager Not Found",
                "Could not find GameManager in the scene.\n" +
                "Make sure GameManager is in the scene.", "OK");
            Debug.LogWarning("GameManager not found!");
        }

        Debug.Log("=== SAVE TIMER SETTINGS COMPLETED ===");
    }

    #endregion

    #region Bus Sequence Methods

    void AddBusToSequence()
    {
        BusData newBus = new BusData
        {
            color = selectedBusColor,
            capacity = selectedBusCapacity
        };

        busSequence.Add(newBus);
        EditorUtility.SetDirty(gridManager);
        Repaint();
    }

    void RemoveBusFromSequence(int index)
    {
        if (index >= 0 && index < busSequence.Count)
        {
            busSequence.RemoveAt(index);
            EditorUtility.SetDirty(gridManager);
            Repaint();
        }
    }

    void SwapBuses(int indexA, int indexB)
    {
        if (indexA >= 0 && indexA < busSequence.Count && indexB >= 0 && indexB < busSequence.Count)
        {
            BusData temp = busSequence[indexA];
            busSequence[indexA] = busSequence[indexB];
            busSequence[indexB] = temp;

            EditorUtility.SetDirty(gridManager);
            Repaint();
            SceneView.RepaintAll();
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

    void SaveCurrentBusSequence()
    {
        Debug.Log("=== SAVING CURRENT BUS SEQUENCE ===");

        if (busSequence.Count == 0)
        {
            EditorUtility.DisplayDialog("No Bus Sequence",
                "There are no buses in the sequence to save.", "OK");
            return;
        }

        Debug.Log($"Current bus sequence ({busSequence.Count} buses):");
        for (int i = 0; i < busSequence.Count; i++)
        {
            Debug.Log($"  Bus {i}: {busSequence[i].color} (Capacity: {busSequence[i].capacity})");
        }

        gridManager.SaveCurrentPlayAreaState();

        LevelData tempLevelData = CreateLevelData();

        string json = JsonUtility.ToJson(tempLevelData, true);
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = $"Assets/BusSequence_Backup_{timestamp}.json";

        System.IO.File.WriteAllText(path, json);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Bus Sequence Saved",
            $"Bus sequence saved successfully!\n" +
            $"Buses: {busSequence.Count}\n" +
            $"Backup file: {path}", "OK");

        Debug.Log($"Bus sequence saved to: {path}");
        Debug.Log("=== SAVE COMPLETED ===");
    }

    void ForceUpdateGridManagerBusData()
    {
        Debug.Log("=== FORCE UPDATE GRIDMANAGER BUS DATA ===");

        if (busSequence.Count == 0)
        {
            EditorUtility.DisplayDialog("No Bus Data",
                "There are no buses to update.", "OK");
            return;
        }

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.allBuses.Clear();

            for (int i = 0; i < busSequence.Count; i++)
            {
                BusData busCopy = new BusData(busSequence[i].color, busSequence[i].capacity);
                gameManager.allBuses.Add(busCopy);
                Debug.Log($"  Added Bus {i}: {busCopy.color} (Capacity: {busCopy.capacity})");
            }

            Debug.Log($"Updated GameManager with {gameManager.allBuses.Count} buses");

            if (gameManager.manualBusSequence != null)
            {
                gameManager.manualBusSequence.Clear();
                for (int i = 0; i < busSequence.Count; i++)
                {
                    BusData busCopy = new BusData(busSequence[i].color, busSequence[i].capacity);
                    gameManager.manualBusSequence.Add(busCopy);
                }
                Debug.Log($"Also updated manualBusSequence");
            }

            LevelData tempLevelData = CreateLevelData();
            gameManager.currentLevelData = tempLevelData;
            Debug.Log($"Updated currentLevelData");

            EditorUtility.SetDirty(gameManager);

            EditorUtility.DisplayDialog("GameManager Updated",
                $"Successfully updated GameManager!\n" +
                $"Buses: {gameManager.allBuses.Count}\n" +
                "Changes will take effect immediately in Play mode.\n\n" +
                "Press Play to test the new bus sequence!", "OK");
        }
        else
        {
            Debug.LogWarning("GameManager not found in scene!");
            EditorUtility.DisplayDialog("GameManager Not Found",
                "Could not find GameManager in the scene.\n" +
                "Make sure GameManager is in the scene.", "OK");
        }

        Debug.Log("=== FORCE UPDATE COMPLETED ===");
    }

    #endregion

    #region Scene GUI Methods

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

        GUILayout.BeginArea(new Rect(10, 10, 400, 240));
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

        GUILayout.Space(5);
        GUILayout.Label("Shift + Click to remove", EditorStyles.miniLabel);

        GUILayout.EndVertical();
        GUILayout.EndArea();

        Handles.EndGUI();
    }

    #endregion

    #region File Operations

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

        levelData.timerEnabled = enableLevelTimer;
        levelData.levelTimeInSeconds = levelTimeInSeconds;
        levelData.timerFormat = timerFormat;

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
        ClearAllObjectsQuiet();

        gridManager.gridWidth = levelData.gridWidth;
        gridManager.gridHeight = levelData.gridHeight;
        gridManager.ForceGridRecreation();

        if (levelData.busSequence != null && levelData.busSequence.Count > 0)
        {
            busSequence = new List<BusData>(levelData.busSequence);
        }

        enableLevelTimer = levelData.timerEnabled;
        levelTimeInSeconds = levelData.levelTimeInSeconds;
        timerFormat = levelData.timerFormat;

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

    #endregion

    #region Inspector GUI

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
                gridManager.gridInitialized = false;
                gridManager.InitializeGrid();
                EditorUtility.SetDirty(gridManager);
            }

            if (GUILayout.Button("RECREATE GRID", GUILayout.Height(25)))
            {
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

        // SAFE GUI DRAWING WITH PROPER ERROR HANDLING
        try
        {
            // Bus Settings
            EditorGUILayout.Space(10);
            DrawBusSettingsSafe();

            // Timer Settings
            EditorGUILayout.Space(5);
            DrawTimerSettingsSafe();

            // Scene Level Manager - YENİ!
            EditorGUILayout.Space(5);
            DrawSceneLevelManagerSafe();

            // Shape Settings
            EditorGUILayout.Space(5);
            DrawShapeSettingsSafe();

            // Person Settings
            EditorGUILayout.Space(5);
            DrawPersonSettingsSafe();

            // Action Buttons
            EditorGUILayout.Space(10);
            DrawActionButtonsSafe();

            // Help Box
            EditorGUILayout.Space(5);
            DrawHelpBoxSafe();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GUI Error in Level Design Tool: {e.Message}");

            EditorGUILayout.HelpBox($"GUI Error occurred: {e.Message}\nTry disabling and re-enabling editing mode.", MessageType.Error);
        }
    }

    #endregion

    #region Safe Drawing Methods

    void DrawBusSettingsSafe()
    {
        try
        {
            DrawBusSettings();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Bus Settings Error: {e.Message}", MessageType.Error);
        }
    }

    void DrawTimerSettingsSafe()
    {
        try
        {
            DrawTimerSettings();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Timer Settings Error: {e.Message}", MessageType.Error);
        }
    }

    void DrawShapeSettingsSafe()
    {
        try
        {
            DrawShapeSettings();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Shape Settings Error: {e.Message}", MessageType.Error);
        }
    }

    void DrawPersonSettingsSafe()
    {
        try
        {
            DrawPersonSettings();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Person Settings Error: {e.Message}", MessageType.Error);
        }
    }

    void DrawActionButtonsSafe()
    {
        try
        {
            DrawActionButtons();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Action Buttons Error: {e.Message}", MessageType.Error);
        }
    }

    void DrawHelpBoxSafe()
    {
        try
        {
            DrawHelpBox();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Help Box Error: {e.Message}", MessageType.Error);
        }
    }

    #endregion

    public List<BusData> GetBusSequence()
    {
        return new List<BusData>(busSequence);
    }
}

// Data Structures
[System.Serializable]
public class LevelData
{
    public string levelName = "";
    public int gridWidth;
    public int gridHeight;
    public List<GridObjectData> objects;
    public List<BusData> busSequence;
    public List<PlayAreaCellData> playAreaData;

    [Header("Timer Settings")]
    public bool timerEnabled = false;
    public float levelTimeInSeconds = 60f;
    public TimerDisplayFormat timerFormat = TimerDisplayFormat.MinutesSeconds;
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