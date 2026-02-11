using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum EditorMode
{
    Grid,
    Pipe,
    Block
}

public enum BlockRotation
{
    Deg0 = 0,
    Deg90 = 1,
    Deg180 = 2,
    Deg270 = 3
}

public class GridEditorWindow : EditorWindow
{
    private LevelData currentMap;
    private const float cellSize = 40f;
    private EditorMode currentMode = EditorMode.Grid;

    // Reference to BlockColorData
    private BlockColorData blockColorData;

    // Editor-only pipe data storage
    private List<PipeData> editorPipes = new List<PipeData>();

    // Pipe
    private PipeData selectedPipeData = null;

    // Block
    private BlockShape? selectedShape = null;
    private BlockRotation selectedRotation = BlockRotation.Deg0;
    private Vector2Int? blockOrigin = null;
    private ColorEnum selectedColor = ColorEnum.Red; // ColorEnum thay vì int
    private MoveAxis selectedDirection = MoveAxis.Free;

    // Block properties
    private bool selectedHasStar = false;
    private int selectedIceCount = 0;
    private bool selectedIsCloseOpen = false;
    private bool selectedIsStartOpen = false;
    private int selectedCrateCount = 0;
    private int selectedKeyColorIndex = -1; // int color ID, -1 = None
    private float selectedBombTimeLimit = 0f;
    private int selectedBombMoveLimit = 0;
    private List<int> selectedRopeColors = new(); // List of int color IDs
    private int selectedScissorColor = -1; // int color ID, -1 = None
    private List<int> selectedLinkedBlockIndexes = new();
    private List<ColorBlockData> selectedMixColors = new();
    private bool selectedIsStone = false;
    private bool selectedIsMixColor = false;
    private int selectedInnerBlockIndex = -1;

    // Scroll
    private Vector2 mainScroll;
    private Vector2 blockEditorScroll;

    // Database shape offsets
    private static readonly Dictionary<BlockShape, Vector2Int[]> shapeOffsets = new()
    {
        { BlockShape.One, new[] { new Vector2Int(0, 0) } },
        { BlockShape.Two, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) } },
        // Three: origin ở ô dưới, 2 ô lên trên
        { BlockShape.Three, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) } },
        // ShortL: origin ở góc (giao nhau), 1 ô lên trên, 1 ô sang phải
        { BlockShape.ShortL, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0) } },
        { BlockShape.ShortT, new[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1) } },
        // L: origin ở góc (giao nhau), 2 ô lên trên, 1 ô sang phải
        { BlockShape.L, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0) } },
        { BlockShape.ReverseL, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(-1, 0) } },
        // TwoSquare: origin ở góc trên phải (1,1), các ô khác relative to đó
        { BlockShape.TwoSquare, new[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1) } },
        { BlockShape.Plus, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) } },
    };

    [MenuItem("Tools/Level Editor")]
    public static void Open() => GetWindow<GridEditorWindow>("Grid Editor");

    private void OnEnable()
    {
        // Load BlockColorData
        string[] guids = AssetDatabase.FindAssets("t:BlockColorData");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            blockColorData = AssetDatabase.LoadAssetAtPath<BlockColorData>(path);
        }
        else
        {
            Debug.LogWarning("BlockColorData not found in project!");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        var newMap = (LevelData)EditorGUILayout.ObjectField("Map Data", currentMap, typeof(LevelData), false);

        // Auto-load the first LevelData asset if none is assigned
        if (currentMap == null && newMap == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                newMap = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                Debug.Log($"Auto-loaded LevelData: {path}");
            }
        }

        if (newMap != currentMap)
        {
            currentMap = newMap;
            LoadEditorData(); // Load data when map changes
        }

        if (currentMap == null) { DrawMapCreationUI(); return; }

        EditorGUILayout.Space();
        DrawModeToolbar();
        EditorGUILayout.Space();

        switch (currentMode)
        {
            case EditorMode.Grid: DrawMapSettingsRealtime(); DrawResetMapButton(); break;
            case EditorMode.Pipe: DrawPipeEditorPanel(); DrawClearAllPipesButton(); break;
            case EditorMode.Block: DrawBlockEditorPanel(); DrawClearAllBlocksButton(); break;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        DrawThreeGrids();
        DrawSaveDeleteButtons();
    }

    private void DrawModeToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentMode == EditorMode.Grid, "Grid", "Button")) currentMode = EditorMode.Grid;
        if (GUILayout.Toggle(currentMode == EditorMode.Pipe, "Pipe", "Button")) currentMode = EditorMode.Pipe;
        if (GUILayout.Toggle(currentMode == EditorMode.Block, "Block", "Button")) currentMode = EditorMode.Block;
        EditorGUILayout.EndHorizontal();
    }

    // =====================================================================
    // DATA LOADING/SAVING
    // =====================================================================
    private void LoadEditorData()
    {
        if (currentMap == null) return;

        Debug.Log($"LoadEditorData: Loading data from {currentMap.name}");
        Debug.Log($"LoadEditorData: Current blocks count: {currentMap.blocks.Count}");

        // Load pipe data directly from PipeData
        editorPipes.Clear();
        selectedPipeData = null;

        // Copy PipeData to editor pipes
        if (currentMap.pipes != null)
        {
            foreach (var pipeData in currentMap.pipes)
            {
                editorPipes.Add(new PipeData(pipeData));
            }
        }

        // Initialize lists if null
        if (currentMap.emptyNodes == null) currentMap.emptyNodes = new List<NodeData>();
        if (currentMap.blocks == null) currentMap.blocks = new List<BlockData>();
        if (currentMap.colorPaths == null) currentMap.colorPaths = new List<ColorPathData>();
        if (currentMap.pipes == null) currentMap.pipes = new List<PipeData>();

        Debug.Log($"Loaded level data: {currentMap.emptyNodes.Count} empty nodes, {currentMap.blocks.Count} blocks, {currentMap.colorPaths.Count} color paths, {editorPipes.Count} editor pipes");

        Repaint();
    }

    private void SaveEditorData()
    {
        if (currentMap == null) return;

        // Copy editor pipes directly to LevelData
        currentMap.pipes.Clear();

        foreach (var editorPipe in editorPipes)
        {
            currentMap.pipes.Add(new PipeData(editorPipe));
        }

        EditorUtility.SetDirty(currentMap);

        // Force refresh the asset and inspector
        AssetDatabase.Refresh();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

        Debug.Log($"Saved level data with {currentMap.emptyNodes.Count} empty nodes, {currentMap.blocks.Count} blocks, {currentMap.pipes.Count} pipes");
    }

    // =====================================================================
    // CREATE NEW MAP
    // =====================================================================
    private int newWidth = 5, newHeight = 5;

    private void DrawMapCreationUI()
    {
        EditorGUILayout.LabelField("Create New Map", EditorStyles.boldLabel);
        newWidth = EditorGUILayout.IntField("Width", newWidth);
        newHeight = EditorGUILayout.IntField("Height", newHeight);
        if (GUILayout.Button("Create New MapData")) CreateMapData(newWidth, newHeight);
    }

    private void CreateMapData(int width, int height)
    {
        currentMap = ScriptableObject.CreateInstance<LevelData>();
        currentMap.width = width;
        currentMap.height = height;
        currentMap.emptyNodes = new List<NodeData>();
        currentMap.pipes = new List<PipeData>();
        currentMap.blocks = new List<BlockData>();
        currentMap.colorPaths = new List<ColorPathData>();

        // Không tạo emptyNodes ban đầu - để grid trống, user sẽ click để tạo empty nodes
        // Grid mặc định sẽ có tất cả cells là spawnable (không empty)

        string path = EditorUtility.SaveFilePanelInProject("Save LevelData", "NewLevelData", "asset", "Choose where to save");
        if (!string.IsNullOrEmpty(path)) { AssetDatabase.CreateAsset(currentMap, path); AssetDatabase.SaveAssets(); }
    }

    // =====================================================================
    // MAP SETTINGS
    // =====================================================================
    private void DrawMapSettingsRealtime()
    {
        EditorGUILayout.LabelField("Map Settings", EditorStyles.boldLabel);

        int width = EditorGUILayout.IntField("Width", currentMap.width);
        int height = EditorGUILayout.IntField("Height", currentMap.height);
        int level = EditorGUILayout.IntField("Level", currentMap.levelIndex);
        LevelDifficult levelDifficult = (LevelDifficult)EditorGUILayout.EnumPopup("Difficult", currentMap.levelDifficult);
        int time = EditorGUILayout.IntField("Time", currentMap.time);

        if (time != currentMap.time) { currentMap.time = time; EditorUtility.SetDirty(currentMap); }
        if (level != currentMap.levelIndex) { currentMap.levelIndex = level; EditorUtility.SetDirty(currentMap); }
        if (levelDifficult != currentMap.levelDifficult) { currentMap.levelDifficult = levelDifficult; EditorUtility.SetDirty(currentMap); }
        if (width != currentMap.width || height != currentMap.height) ResizeGrid(width, height);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Camera Data", EditorStyles.boldLabel);
        if (currentMap.cameraData == null) currentMap.cameraData = new CameraData();

        EditorGUI.BeginChangeCheck();

        // Position X, Y, Z ngang hàng với width nhỏ hơn
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Position", GUILayout.Width(60));
        EditorGUILayout.LabelField("X", GUILayout.Width(15));
        currentMap.cameraData.positionX = EditorGUILayout.FloatField(currentMap.cameraData.positionX, GUILayout.Width(50));
        EditorGUILayout.LabelField("Y", GUILayout.Width(15));
        currentMap.cameraData.positionY = EditorGUILayout.FloatField(currentMap.cameraData.positionY, GUILayout.Width(50));
        EditorGUILayout.LabelField("Z", GUILayout.Width(15));
        currentMap.cameraData.positionZ = EditorGUILayout.FloatField(currentMap.cameraData.positionZ, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        // Rotation X, Y, Z ngang hàng với width nhỏ hơn
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rotation", GUILayout.Width(60));
        EditorGUILayout.LabelField("X", GUILayout.Width(15));
        currentMap.cameraData.rotationX = EditorGUILayout.FloatField(currentMap.cameraData.rotationX, GUILayout.Width(50));
        EditorGUILayout.LabelField("Y", GUILayout.Width(15));
        currentMap.cameraData.rotationY = EditorGUILayout.FloatField(currentMap.cameraData.rotationY, GUILayout.Width(50));
        EditorGUILayout.LabelField("Z", GUILayout.Width(15));
        currentMap.cameraData.rotationZ = EditorGUILayout.FloatField(currentMap.cameraData.rotationZ, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        currentMap.cameraData.fov = EditorGUILayout.FloatField("FOV", currentMap.cameraData.fov);

        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(currentMap);

        // Water Validation Summary
        EditorGUILayout.Space();
        DrawWaterValidationSummary();
    }

    private void ResizeGrid(int newWidth, int newHeight)
    {
        List<NodeData> newNodes = new();

        // Chỉ giữ lại các empty nodes nằm trong bounds mới
        foreach (var node in currentMap.emptyNodes)
        {
            if (node.position.x < newWidth && node.position.y < newHeight)
            {
                newNodes.Add(node);
            }
        }

        currentMap.emptyNodes = newNodes;
        currentMap.width = newWidth;
        currentMap.height = newHeight;
        EditorUtility.SetDirty(currentMap);
        Repaint();
    }

    private enum GridEditMode { EditMap, EditColorPath }
    private GridEditMode gridEditMode = GridEditMode.EditMap;
    private ColorEnum selectedPathColor = ColorEnum.Red; // ColorEnum thay vì int
    private int selectedPathIndex = -1;

    private void DrawResetMapButton()
    {
        EditorGUILayout.Space();
        
        // Fix Inner Block Colors button
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Fix Inner Block Colors (0 → -1)", GUILayout.Height(25)))
        {
            FixInnerBlockColors();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Edit Mode", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = (gridEditMode == GridEditMode.EditMap) ? Color.cyan : Color.white;
        if (GUILayout.Button("Edit Map", GUILayout.Height(25))) { gridEditMode = GridEditMode.EditMap; selectedPathIndex = -1; }

        GUI.backgroundColor = (gridEditMode == GridEditMode.EditColorPath) ? Color.green : Color.white;
        if (GUILayout.Button("Edit Color Paths", GUILayout.Height(25))) gridEditMode = GridEditMode.EditColorPath;

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        if (gridEditMode == GridEditMode.EditColorPath)
            DrawColorPathsEditor();
    }


    private void DrawColorPathsEditor()
    {
        EditorGUILayout.LabelField("Color Paths", EditorStyles.boldLabel);
        if (currentMap.colorPaths == null) currentMap.colorPaths = new List<ColorPathData>();

        EditorGUILayout.BeginHorizontal();
        selectedPathColor = (ColorEnum)EditorGUILayout.EnumPopup("New Path Color", selectedPathColor);
        if (GUILayout.Button("Add Path", GUILayout.Width(80)))
        {
            currentMap.colorPaths.Add(new ColorPathData { color = (int)selectedPathColor });
            selectedPathIndex = currentMap.colorPaths.Count - 1;
            EditorUtility.SetDirty(currentMap);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        for (int i = 0; i < currentMap.colorPaths.Count; i++)
        {
            var path = currentMap.colorPaths[i];
            GUI.backgroundColor = (selectedPathIndex == i) ? Color.cyan : Color.white;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"Path {i}: {path.color} ({path.positions.Count} points)", GUILayout.ExpandWidth(true)))
                selectedPathIndex = i;
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                currentMap.colorPaths.RemoveAt(i);
                if (selectedPathIndex >= currentMap.colorPaths.Count) selectedPathIndex = currentMap.colorPaths.Count - 1;
                EditorUtility.SetDirty(currentMap); Repaint(); return;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        if (selectedPathIndex >= 0 && selectedPathIndex < currentMap.colorPaths.Count)
        {
            EditorGUILayout.Space();
            var selectedPath = currentMap.colorPaths[selectedPathIndex];

            // Có thể sửa màu của path đã chọn
            EditorGUI.BeginChangeCheck();
            ColorEnum pathColor = (ColorEnum)selectedPath.color;
            pathColor = (ColorEnum)EditorGUILayout.EnumPopup("Path Color", pathColor);
            if (EditorGUI.EndChangeCheck())
            {
                selectedPath.color = (int)pathColor;
                EditorUtility.SetDirty(currentMap);
            }

            EditorGUILayout.LabelField("Positions (click Grid Map to add/remove):");
            for (int j = 0; j < selectedPath.positions.Count; j++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  [{j}] ({selectedPath.positions[j].x}, {selectedPath.positions[j].y})");
                if (GUILayout.Button("↑", GUILayout.Width(25)) && j > 0)
                {
                    (selectedPath.positions[j], selectedPath.positions[j - 1]) = (selectedPath.positions[j - 1], selectedPath.positions[j]);
                    EditorUtility.SetDirty(currentMap);
                }
                if (GUILayout.Button("↓", GUILayout.Width(25)) && j < selectedPath.positions.Count - 1)
                {
                    (selectedPath.positions[j], selectedPath.positions[j + 1]) = (selectedPath.positions[j + 1], selectedPath.positions[j]);
                    EditorUtility.SetDirty(currentMap);
                }
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    selectedPath.positions.RemoveAt(j);
                    EditorUtility.SetDirty(currentMap);
                    Repaint();
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Clear All Positions"))
            {
                selectedPath.positions.Clear();
                EditorUtility.SetDirty(currentMap);
            }
        }
        GUI.backgroundColor = Color.white;
    }

    // =====================================================================
    // 3 GRID VIEW (HORIZONTAL)
    // =====================================================================
    private void DrawThreeGrids()
    {
        mainScroll = EditorGUILayout.BeginScrollView(mainScroll, GUILayout.ExpandHeight(true));

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        DrawGridPanel("Grid Map", DrawCellMap);
        GUILayout.Space(20);
        DrawGridPanel("Grid Pipe", DrawCellPipe);
        GUILayout.Space(20);
        DrawGridPanel("Grid Block", DrawCellBlock);

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private void DrawGridPanel(string title, Action<int, int> drawCellFunc)
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        for (int y = currentMap.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < currentMap.width; x++) drawCellFunc(x, y);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    // =====================================================================
    // CELL RENDERERS
    // =====================================================================
    private void DrawCellMap(int x, int y)
    {
        bool isEmpty = currentMap.emptyNodes.Exists(n => n.position.x == x && n.position.y == y);

        if (gridEditMode == GridEditMode.EditColorPath)
        {
            // Edit Color Paths mode - hiển thị màu của color paths
            Color pathColor = new Color(0.7f, 0.7f, 0.7f); // Default gray
            foreach (var path in currentMap.colorPaths)
                if (path.positions.Contains(new Vector2Int(x, y)))
                {
                    if (blockColorData != null)
                        pathColor = blockColorData.GetColor((ColorEnum)path.color);
                    break;
                }
            GUI.backgroundColor = pathColor;
        }
        else
        {
            // Edit Map mode - hiển thị empty nodes
            // Empty nodes = màu đen (không spawn được)
            // Spawnable nodes = màu xám sáng (spawn được)
            GUI.backgroundColor = isEmpty ? Color.black : new Color(0.7f, 0.7f, 0.7f);
        }

        string buttonText = (gridEditMode == GridEditMode.EditMap && isEmpty) ? "X" : $"{x},{y}";

        if (GUILayout.Button(buttonText, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
        {
            if (currentMode == EditorMode.Grid)
            {
                if (gridEditMode == GridEditMode.EditColorPath && selectedPathIndex >= 0 && selectedPathIndex < currentMap.colorPaths.Count)
                {
                    // Edit Color Paths - thêm/xóa vị trí khỏi color path
                    var selectedPath = currentMap.colorPaths[selectedPathIndex];
                    var pos = new Vector2Int(x, y);
                    if (selectedPath.positions.Contains(pos))
                        selectedPath.positions.Remove(pos);
                    else
                        selectedPath.positions.Add(pos);
                    EditorUtility.SetDirty(currentMap);
                }
                else if (gridEditMode == GridEditMode.EditMap)
                {
                    // Edit Map - toggle empty nodes
                    var nodePos = new Vector2Int(x, y);
                    if (isEmpty)
                        currentMap.emptyNodes.RemoveAll(n => n.position == nodePos);
                    else
                        currentMap.emptyNodes.Add(new NodeData { position = nodePos });
                    EditorUtility.SetDirty(currentMap);
                }
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawCellPipe(int x, int y)
    {
        PipeData pipe = editorPipes.Find(p => p.position.x == x && p.position.y == y);
        GUI.backgroundColor = pipe != null ? Color.cyan : Color.gray;
        if (GUILayout.Button($"{x},{y}", GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
            if (currentMode == EditorMode.Pipe) OnClickPipeCell(x, y);
        GUI.backgroundColor = Color.white;
    }

    private void DrawCellBlock(int x, int y)
    {
        int cellColor = -1; // -1 = None

        foreach (var block in currentMap.blocks)
        {
            int step = RotationToStep(block.rotation);
            var cells = GetShapeCells(block.shapeType, step);
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                if (block.position.x + c.x == x && block.position.y + c.y == y)
                {
                    // Nếu là MixColor thì lấy màu từ mixColors
                    if (block.isMixColor && block.mixColors != null && i < block.mixColors.Count)
                        cellColor = block.mixColors[i].color;
                    else
                        cellColor = block.color;
                    break;
                }
            }
            if (cellColor != -1) break;
        }

        if (cellColor != -1 && blockColorData != null)
            GUI.backgroundColor = blockColorData.GetColor((ColorEnum)cellColor);
        else
            GUI.backgroundColor = Color.gray;

        if (GUILayout.Button($"{x},{y}", GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
        {
            if (currentMode == EditorMode.Block)
            {
                blockOrigin = new Vector2Int(x, y);
                var existing = currentMap.blocks.Find(b => b.position.x == x && b.position.y == y);
                if (existing != null) LoadBlockData(existing);
                else ResetBlockData();
                Repaint();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void LoadBlockData(BlockData block)
    {
        selectedShape = block.shapeType;
        selectedRotation = StepToRotation(RotationToStep(block.rotation));
        selectedColor = (ColorEnum)block.color; // Convert int to ColorEnum
        selectedDirection = block.direction;
        selectedHasStar = block.hasStar;
        selectedIceCount = block.iceCount;
        selectedIsCloseOpen = block.isCloseOpen;
        selectedIsStartOpen = block.isStartOpen;
        selectedCrateCount = block.crateCount;
        selectedKeyColorIndex = block.keyColorIndex;
        selectedBombTimeLimit = block.bombTimeLimit;
        selectedBombMoveLimit = block.bombMoveLimit;
        selectedScissorColor = block.scissorColor;
        selectedInnerBlockIndex = block.innerBlockColor;
        selectedIsStone = block.isStone;
        selectedIsMixColor = block.isMixColor;

        selectedRopeColors = new List<int>();
        if (block.ropeColors != null) foreach (var rc in block.ropeColors) selectedRopeColors.Add(rc);

        selectedMixColors = new List<ColorBlockData>();
        // Chỉ load mixColors khi block là isMixColor
        if (block.isMixColor && block.mixColors != null)
            foreach (var cc in block.mixColors) selectedMixColors.Add(new ColorBlockData { color = cc.color, colorCount = cc.colorCount });

        selectedLinkedBlockIndexes = new List<int>();
        if (block.linkedBlockIndexes != null) foreach (var li in block.linkedBlockIndexes) selectedLinkedBlockIndexes.Add(li);
    }

    private void ResetBlockData()
    {
        selectedRopeColors = new List<int>();
        selectedMixColors = new List<ColorBlockData>();
        selectedLinkedBlockIndexes = new List<int>();
        selectedInnerBlockIndex = -1;
        selectedHasStar = false;
        selectedIceCount = 0;
        selectedIsCloseOpen = false;
        selectedIsStartOpen = false;
        selectedCrateCount = 0;
        selectedKeyColorIndex = -1; // -1 = None
        selectedBombTimeLimit = 0f;
        selectedBombMoveLimit = 0;
        selectedScissorColor = -1; // -1 = None
        selectedIsStone = false;
        selectedIsMixColor = false;
    }


    // =====================================================================
    // PIPE EDITOR
    // =====================================================================
    private void OnClickPipeCell(int x, int y)
    {
        PipeData pipe = editorPipes.Find(p => p.position.x == x && p.position.y == y);
        if (pipe == null)
        {
            pipe = new PipeData
            {
                position = new Vector2Int(x, y),
                direction = Direction.Up,
                waterColors = new List<WaterColor>()
            };
            editorPipes.Add(pipe);
        }
        selectedPipeData = pipe;
        EditorUtility.SetDirty(currentMap);
        Repaint();
    }

    private Vector2 pipeEditorScroll;

    private void DrawPipeEditorPanel()
    {
        EditorGUILayout.LabelField("Pipe Editor", EditorStyles.boldLabel);
        if (selectedPipeData == null) { EditorGUILayout.HelpBox("Click một ô trong Grid Pipe để tạo/chọn Pipe.", MessageType.Info); return; }

        pipeEditorScroll = EditorGUILayout.BeginScrollView(pipeEditorScroll, GUILayout.MaxHeight(400));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Pipe at ({selectedPipeData.position.x},{selectedPipeData.position.y})", EditorStyles.boldLabel);

        // Direction
        selectedPipeData.direction = (Direction)EditorGUILayout.EnumPopup("Direction", selectedPipeData.direction);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pipe Properties", EditorStyles.boldLabel);

        selectedPipeData.hasStar = EditorGUILayout.Toggle("Has Star", selectedPipeData.hasStar);

        selectedPipeData.isLocked = EditorGUILayout.Toggle("Is Locked", selectedPipeData.isLocked);
        if (selectedPipeData.isLocked)
        {
            ColorEnum unlockColor = (ColorEnum)selectedPipeData.unlockColorId;
            unlockColor = (ColorEnum)EditorGUILayout.EnumPopup("Unlock Color", unlockColor);
            selectedPipeData.unlockColorId = (int)unlockColor;
        }

        selectedPipeData.hasOpenClose = EditorGUILayout.Toggle("Has Open/Close", selectedPipeData.hasOpenClose);
        if (selectedPipeData.hasOpenClose)
        {
            selectedPipeData.isClosed = EditorGUILayout.Toggle("Is Closed", selectedPipeData.isClosed);
        }

        selectedPipeData.iceCount = EditorGUILayout.IntField("Ice Count", selectedPipeData.iceCount);
        
        selectedPipeData.hasBarrier = EditorGUILayout.Toggle("Has Barrier", selectedPipeData.hasBarrier);
        if (selectedPipeData.hasBarrier)
        {
            selectedPipeData.isClockwise = EditorGUILayout.Toggle("Is Clockwise", selectedPipeData.isClockwise);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Water List", EditorStyles.boldLabel);

        if (selectedPipeData.waterColors == null)
            selectedPipeData.waterColors = new List<WaterColor>();

        for (int i = 0; i < selectedPipeData.waterColors.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Water {i + 1}", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Color:", GUILayout.Width(50));

            // Hiển thị color picker với ColorEnum
            ColorEnum currentColor = (ColorEnum)selectedPipeData.waterColors[i].color;
            ColorEnum newColor = (ColorEnum)EditorGUILayout.EnumPopup(currentColor, GUILayout.Width(100));
            selectedPipeData.waterColors[i].color = (int)newColor;

            // Preview màu
            if (blockColorData != null)
            {
                Color previewColor = blockColorData.GetColor(newColor);
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(30, 20), previewColor);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Value:", GUILayout.Width(50));
            selectedPipeData.waterColors[i].value = EditorGUILayout.IntField(selectedPipeData.waterColors[i].value, GUILayout.Width(60));

            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                selectedPipeData.waterColors.RemoveAt(i);
                GUI.backgroundColor = Color.white;
                break;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Water", GUILayout.Width(100), GUILayout.Height(25)))
        {
            selectedPipeData.waterColors.Add(new WaterColor(0, 4)); // Default: color 0, value 4
        }
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Delete This Pipe"))
        {
            editorPipes.Remove(selectedPipeData);
            selectedPipeData = null;
            GUI.backgroundColor = Color.white;
            EditorUtility.SetDirty(currentMap); Repaint(); return;
        }
        GUI.backgroundColor = Color.white;
        EditorUtility.SetDirty(currentMap);
    }

    private void DrawClearAllPipesButton()
    {
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("Clear All Pipes"))
            if (EditorUtility.DisplayDialog("Clear All Pipes", "Xóa TẤT CẢ Pipe trong map?", "Yes", "No"))
            {
                editorPipes.Clear();
                selectedPipeData = null;
                EditorUtility.SetDirty(currentMap); Repaint();
            }
        GUI.backgroundColor = Color.white;
    }

    // =====================================================================
    // BLOCK EDITOR
    // =====================================================================
    private void DrawBlockEditorPanel()
    {
        EditorGUILayout.LabelField("Block Editor", EditorStyles.boldLabel);

        // Show current block count
        if (currentMap != null)
        {
            EditorGUILayout.LabelField($"Current blocks in data: {currentMap.blocks.Count}", EditorStyles.helpBox);
        }

        // Shape buttons
        EditorGUILayout.LabelField("Block Shapes:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        foreach (BlockShape shape in System.Enum.GetValues(typeof(BlockShape)))
        {
            GUI.backgroundColor = (selectedShape == shape) ? new Color(0.5f, 0.7f, 1f) : Color.white;
            if (GUILayout.Button(shape.ToString(), GUILayout.Width(80), GUILayout.Height(30))) selectedShape = shape;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        if (selectedShape == null) { EditorGUILayout.HelpBox("1. Chọn 1 Block Shape ở trên", MessageType.Info); return; }
        if (blockOrigin == null) { EditorGUILayout.HelpBox("2. Click một ô trên Grid Block để đặt Origin", MessageType.Info); return; }

        blockEditorScroll = EditorGUILayout.BeginScrollView(blockEditorScroll, GUILayout.MaxHeight(500));

        EditorGUILayout.LabelField($"Origin: ({blockOrigin.Value.x}, {blockOrigin.Value.y})");
        selectedRotation = (BlockRotation)EditorGUILayout.EnumPopup("Rotation", selectedRotation);

        // Color picker với ColorEnum
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Block Color:", GUILayout.Width(80));
        selectedColor = (ColorEnum)EditorGUILayout.EnumPopup(selectedColor, GUILayout.Width(100));
        // Preview màu
        if (blockColorData != null)
        {
            Color blockPreviewColor = blockColorData.GetColor(selectedColor);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(30, 20), blockPreviewColor);
        }
        EditorGUILayout.EndHorizontal();

        selectedDirection = (MoveAxis)EditorGUILayout.EnumPopup("Move Direction", selectedDirection);

        // Properties
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Block Properties", EditorStyles.boldLabel);

        selectedIsStone = EditorGUILayout.Toggle("Is Stone", selectedIsStone);
        selectedHasStar = EditorGUILayout.Toggle("Has Star", selectedHasStar);
        selectedIceCount = EditorGUILayout.IntField("Ice Count", selectedIceCount);

        selectedIsCloseOpen = EditorGUILayout.Toggle("Has Open/Close", selectedIsCloseOpen);
        if (selectedIsCloseOpen)
        {
            selectedIsStartOpen = EditorGUILayout.Toggle("Start Open", selectedIsStartOpen);
        }

        selectedCrateCount = EditorGUILayout.IntField("Crate Count", selectedCrateCount);

        // Key Color với ColorEnum
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Key Color:", GUILayout.Width(80));
        if (selectedKeyColorIndex >= 0)
        {
            ColorEnum keyColor = (ColorEnum)selectedKeyColorIndex;
            keyColor = (ColorEnum)EditorGUILayout.EnumPopup(keyColor, GUILayout.Width(100));
            selectedKeyColorIndex = (int)keyColor;

            // Preview màu
            if (blockColorData != null)
            {
                Color keyPreviewColor = blockColorData.GetColor(keyColor);
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(30, 20), keyPreviewColor);
            }
        }
        else
        {
            EditorGUILayout.LabelField("None", GUILayout.Width(100));
        }

        if (GUILayout.Button(selectedKeyColorIndex >= 0 ? "Clear" : "Set", GUILayout.Width(50)))
        {
            selectedKeyColorIndex = selectedKeyColorIndex >= 0 ? -1 : (int)ColorEnum.Red;
        }
        EditorGUILayout.EndHorizontal();

        selectedBombTimeLimit = EditorGUILayout.FloatField("Bomb Time Limit", selectedBombTimeLimit);
        selectedBombMoveLimit = EditorGUILayout.IntField("Bomb Move Limit", selectedBombMoveLimit);

        // Scissor Color với ColorEnum
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Scissor Color:", GUILayout.Width(80));
        if (selectedScissorColor >= 0)
        {
            ColorEnum scissorColor = (ColorEnum)selectedScissorColor;
            scissorColor = (ColorEnum)EditorGUILayout.EnumPopup(scissorColor, GUILayout.Width(100));
            selectedScissorColor = (int)scissorColor;

            // Preview màu
            if (blockColorData != null)
            {
                Color scissorPreviewColor = blockColorData.GetColor(scissorColor);
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(30, 20), scissorPreviewColor);
            }
        }
        else
        {
            EditorGUILayout.LabelField("None", GUILayout.Width(100));
        }

        if (GUILayout.Button(selectedScissorColor >= 0 ? "Clear" : "Set", GUILayout.Width(50)))
        {
            selectedScissorColor = selectedScissorColor >= 0 ? -1 : (int)ColorEnum.Red;
        }
        EditorGUILayout.EndHorizontal();

        selectedInnerBlockIndex = EditorGUILayout.IntField("Inner Block Index (-1 = none)", selectedInnerBlockIndex);

        // Rope Colors
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rope Colors", EditorStyles.boldLabel);
        DrawRopeColorsEditor();

        // Mix Colors
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        selectedIsMixColor = EditorGUILayout.Toggle("Is Mix Color", selectedIsMixColor);
        if (EditorGUI.EndChangeCheck())
        {
            // Khi bỏ tick isMixColor, xóa hết data trong mixColors
            if (!selectedIsMixColor)
            {
                selectedMixColors.Clear();
                Debug.Log("Cleared mixColors data because isMixColor was unchecked");
            }
        }

        if (selectedIsMixColor)
        {
            DrawMixColorEditor();
        }

        // Linked Blocks
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Linked Block Indexes", EditorStyles.boldLabel);
        DrawLinkedBlocksEditor();

        // Preview
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);
        Color previewColor = blockColorData != null ? blockColorData.GetColor(selectedColor) : Color.white;
        DrawShapePreview((BlockShape)selectedShape, selectedRotation, previewColor);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply Block", GUILayout.Height(30)))
            ApplyBlockShape(blockOrigin.Value.x, blockOrigin.Value.y, (BlockShape)selectedShape, (int)selectedRotation);

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Delete Block", GUILayout.Height(30)))
        {
            var existing = currentMap.blocks.Find(b => b.position.x == blockOrigin.Value.x && b.position.y == blockOrigin.Value.y);
            if (existing != null)
            {
                currentMap.blocks.Remove(existing);
                EditorUtility.SetDirty(currentMap);
                Repaint();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void DrawRopeColorsEditor()
    {
        EditorGUILayout.LabelField("Rope Colors");
        if (selectedRopeColors == null) selectedRopeColors = new List<int>();

        for (int i = 0; i < selectedRopeColors.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Rope {i + 1}:", GUILayout.Width(60));

            // Color picker với ColorEnum
            ColorEnum ropeColor = (ColorEnum)selectedRopeColors[i];
            ropeColor = (ColorEnum)EditorGUILayout.EnumPopup(ropeColor, GUILayout.Width(100));
            selectedRopeColors[i] = (int)ropeColor;

            // Preview màu
            if (blockColorData != null)
            {
                Color previewColor = blockColorData.GetColor(ropeColor);
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(30, 20), previewColor);
            }

            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                selectedRopeColors.RemoveAt(i);
                GUI.backgroundColor = Color.white;
                break;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Rope Color", GUILayout.Width(120), GUILayout.Height(25)))
            selectedRopeColors.Add((int)ColorEnum.Red); // Default color Red
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawMixColorEditor()
    {
        if (selectedShape == null) return;

        var offsets = shapeOffsets[(BlockShape)selectedShape];
        int cellCount = offsets.Length;

        // Đảm bảo list đủ số lượng cell
        while (selectedMixColors.Count < cellCount)
            selectedMixColors.Add(new ColorBlockData { color = (int)selectedColor, colorCount = 4 });
        while (selectedMixColors.Count > cellCount)
            selectedMixColors.RemoveAt(selectedMixColors.Count - 1);

        EditorGUILayout.LabelField($"Mix Colors ({cellCount} cells)");
        for (int i = 0; i < cellCount; i++)
        {
            var offset = offsets[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Cell {i + 1} (Offset: {offset.x},{offset.y})", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Color:", GUILayout.Width(50));

            // Color picker với ColorEnum
            ColorEnum cellColor = (ColorEnum)selectedMixColors[i].color;
            cellColor = (ColorEnum)EditorGUILayout.EnumPopup(cellColor, GUILayout.Width(100));
            selectedMixColors[i].color = (int)cellColor;

            // Preview màu
            if (blockColorData != null)
            {
                Color previewColor = blockColorData.GetColor(cellColor);
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(30, 20), previewColor);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Count:", GUILayout.Width(50));
            selectedMixColors[i].colorCount = EditorGUILayout.IntField(selectedMixColors[i].colorCount, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }

    private void DrawLinkedBlocksEditor()
    {
        EditorGUILayout.LabelField("Linked Block Indexes");
        if (selectedLinkedBlockIndexes == null) selectedLinkedBlockIndexes = new List<int>();

        for (int i = 0; i < selectedLinkedBlockIndexes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Link {i}:", GUILayout.Width(60));
            selectedLinkedBlockIndexes[i] = EditorGUILayout.IntField(selectedLinkedBlockIndexes[i], GUILayout.Width(80));
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                selectedLinkedBlockIndexes.RemoveAt(i);
                break; // Thoát khỏi loop để tránh lỗi index
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add Linked Block", GUILayout.Width(120)))
            selectedLinkedBlockIndexes.Add(0);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }


    private void ApplyBlockShape(int originX, int originY, BlockShape shape, int rotateStep)
    {
        currentMap.blocks.RemoveAll(b => b.position.x == originX && b.position.y == originY);

        Debug.Log($"Creating block at ({originX}, {originY}) with shape {shape}, rotation {rotateStep * 90}");

        BlockData newBlock = new BlockData()
        {
            blockIndex = currentMap.blocks.Count, // Simple indexing
            position = new Vector2Int(originX, originY),
            rotation = rotateStep * 90,
            shapeType = shape,
            color = (int)selectedColor, // Convert ColorEnum to int
            isStone = selectedIsStone,
            innerBlockColor = selectedInnerBlockIndex,
            hasStar = selectedHasStar,
            linkedBlockIndexes = new List<int>(),
            iceCount = selectedIceCount,
            isCloseOpen = selectedIsCloseOpen,
            isStartOpen = selectedIsStartOpen,
            direction = selectedDirection,
            crateCount = selectedCrateCount,
            keyColorIndex = selectedKeyColorIndex,
            bombTimeLimit = selectedBombTimeLimit,
            bombMoveLimit = selectedBombMoveLimit,
            ropeColors = new List<int>(),
            scissorColor = selectedScissorColor,
            isMixColor = selectedIsMixColor,
            mixColors = new List<ColorBlockData>()
        };

        if (selectedRopeColors != null)
            foreach (var rc in selectedRopeColors) newBlock.ropeColors.Add(rc);

        // Chỉ copy mixColors khi isMixColor là true
        if (selectedIsMixColor && selectedMixColors != null)
            foreach (var cc in selectedMixColors) newBlock.mixColors.Add(new ColorBlockData { color = cc.color, colorCount = cc.colorCount });

        if (selectedLinkedBlockIndexes != null)
            foreach (var li in selectedLinkedBlockIndexes) newBlock.linkedBlockIndexes.Add(li);

        currentMap.blocks.Add(newBlock);
        Debug.Log($"Added block to currentMap.blocks. Total blocks: {currentMap.blocks.Count}");

        EditorUtility.SetDirty(currentMap);
        AssetDatabase.SaveAssets();
        Debug.Log("Block data saved!");
        Repaint();
    }

    private void DrawClearAllBlocksButton()
    {
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("Clear All Blocks"))
            if (EditorUtility.DisplayDialog("Clear All Blocks", "Xóa TẤT CẢ Block trong map?", "Yes", "No"))
            {
                currentMap.blocks.Clear();
                blockOrigin = null;
                selectedShape = null;
                EditorUtility.SetDirty(currentMap); Repaint();
            }
        GUI.backgroundColor = Color.white;
    }

    // =====================================================================
    // HELPERS
    // =====================================================================
    private List<Vector2Int> GetShapeCells(BlockShape shape, int rotateStep)
    {
        int rotation = rotateStep * 90; // Convert step to degrees

        // Special handling for Two, Three, and TwoSquare like in ShapeLibrary
        if (shape == BlockShape.Two)
        {
            return GetRotateTwoDiscrete(rotation);
        }
        else if (shape == BlockShape.Three)
        {
            return GetRotateThreeDiscrete(rotation);
        }
        else if (shape == BlockShape.TwoSquare)
        {
            return GetRotateTwoSquareCenter(rotation);
        }
        else
        {
            // Normal rotation for other shapes
            var baseOffsets = shapeOffsets[shape];
            List<Vector2Int> result = new();
            foreach (var o in baseOffsets)
            {
                Vector2 v = new Vector2(o.x, o.y);
                for (int i = 0; i < rotateStep; i++) v = new Vector2(-v.y, v.x);
                result.Add(new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y)));
            }
            return result;
        }
    }

    private List<Vector2Int> GetRotateTwoDiscrete(int rotation)
    {
        // Anchor always at (0,0) - first cell is always origin
        // 180° same as 0°, 270° same as 90° (symmetric shape)
        return rotation switch
        {
            0 => new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1) },
            90 => new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) },
            180 => new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1) }, // Same as 0°
            270 => new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) }, // Same as 90°
            _ => new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1) }
        };
    }

    private List<Vector2Int> GetRotateThreeDiscrete(int rotation)
    {
        // Anchor always at (0,0) - first cell is always origin
        // 180° same as 0°, 270° same as 90° (symmetric shape)
        return rotation switch
        {
            0 => new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) },
            90 => new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
            180 => new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }, // Same as 0°
            270 => new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }, // Same as 90°
            _ => new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }
        };
    }

    private List<Vector2Int> GetRotateTwoSquareCenter(int rotation)
    {
        var baseOffsets = shapeOffsets[BlockShape.TwoSquare];
        
        // Calculate bounding box
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var p in baseOffsets)
        {
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        // Center of shape
        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

        List<Vector2Int> result = new List<Vector2Int>();

        foreach (var src in baseOffsets)
        {
            // Move to center
            Vector2 p = (Vector2)src - center;

            // Rotate around (0,0)
            Vector2 r = rotation switch
            {
                0 => p,
                90 => new Vector2(-p.y, p.x),
                180 => new Vector2(-p.x, -p.y),
                270 => new Vector2(p.y, -p.x),
                _ => p
            };

            // Move back to grid space
            Vector2 final = r + center;

            // Snap to integer
            result.Add(new Vector2Int(Mathf.RoundToInt(final.x), Mathf.RoundToInt(final.y)));
        }

        return result;
    }

    private void DrawShapePreview(BlockShape shape, BlockRotation rotation, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        const int previewSize = 150;
        Rect rect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.Width(previewSize), GUILayout.Height(previewSize));
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

        float cell = rect.width / 5f;
        Handles.color = new Color(1, 1, 1, 0.08f);
        for (int i = 0; i <= 5; i++)
        {
            Handles.DrawLine(new Vector2(rect.x + i * cell, rect.y), new Vector2(rect.x + i * cell, rect.y + rect.height));
            Handles.DrawLine(new Vector2(rect.x, rect.y + i * cell), new Vector2(rect.x + rect.width, rect.y + i * cell));
        }

        var baseOffsets = shapeOffsets[shape];
        List<Vector2Int> cells = GetShapeCells(shape, (int)rotation);
        Vector2 center = new Vector2(2, 2);

        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];
            Vector2Int pos = c + Vector2Int.FloorToInt(center);
            if (pos.x < 0 || pos.x > 4 || pos.y < 0 || pos.y > 4) continue;

            // Lấy màu cho cell này
            Color cellColor = color;
            if (selectedIsMixColor && selectedMixColors != null && i < selectedMixColors.Count)
            {
                if (blockColorData != null)
                    cellColor = blockColorData.GetColor((ColorEnum)selectedMixColors[i].color);
            }

            Rect r = new Rect(rect.x + pos.x * cell, rect.y + (4 - pos.y) * cell, cell, cell);
            EditorGUI.DrawRect(r, cellColor);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private int RotationToStep(float rot)
    {
        int step = Mathf.RoundToInt(rot / 90f) % 4;
        if (step < 0) step += 4;
        return step;
    }

    private BlockRotation StepToRotation(int step)
    {
        step %= 4;
        if (step < 0) step += 4;
        return (BlockRotation)step;
    }

    // =====================================================================
    // WATER VALIDATION
    // =====================================================================
    private void DrawWaterValidationSummary()
    {
        EditorGUILayout.LabelField("Water Validation", EditorStyles.boldLabel);

        // Calculate required water values per color based on blocks
        Dictionary<int, int> requiredWaterPerColor = new Dictionary<int, int>();

        if (currentMap.blocks != null)
        {
            foreach (var block in currentMap.blocks)
            {

                if (block.isStone)
                    continue;
                int cellCount = GetBlockCellCount(block.shapeType);
                int valuePerBlock = cellCount * 4; // 1 cell = 4 value

                if (block.isMixColor && block.mixColors != null)
                {
                    // Mix color block - count each cell's color separately
                    foreach (var mixColor in block.mixColors)
                    {
                        int color = mixColor.color;
                        int value = mixColor.colorCount;
                        if (!requiredWaterPerColor.ContainsKey(color))
                            requiredWaterPerColor[color] = 0;
                        requiredWaterPerColor[color] += value;
                    }
                }
                else
                {
                    // Single color block (outer block)
                    int color = block.color;
                    if (!requiredWaterPerColor.ContainsKey(color))
                        requiredWaterPerColor[color] = 0;
                    requiredWaterPerColor[color] += valuePerBlock;
                }

                // Check for inner block
                if (block.innerBlockColor >= 0)
                {
                    // Inner block has same shape as outer block
                    int innerColor = block.innerBlockColor;
                    int innerValuePerBlock = cellCount * 4; // Same cell count, same value calculation
                    
                    if (!requiredWaterPerColor.ContainsKey(innerColor))
                        requiredWaterPerColor[innerColor] = 0;
                    requiredWaterPerColor[innerColor] += innerValuePerBlock;
                }
            }
        }

        // Calculate available water values per color from pipes
        Dictionary<int, int> availableWaterPerColor = new Dictionary<int, int>();

        if (editorPipes != null)
        {
            foreach (var pipe in editorPipes)
            {
                if (pipe.waterColors != null)
                {
                    foreach (var water in pipe.waterColors)
                    {
                        int color = water.color;
                        int value = water.value;
                        if (!availableWaterPerColor.ContainsKey(color))
                            availableWaterPerColor[color] = 0;
                        availableWaterPerColor[color] += value;
                    }
                }
            }
        }

        // Display summary
        EditorGUILayout.BeginVertical("box");

        // Get all unique colors
        HashSet<int> allColors = new HashSet<int>();
        foreach (var color in requiredWaterPerColor.Keys) allColors.Add(color);
        foreach (var color in availableWaterPerColor.Keys) allColors.Add(color);

        if (allColors.Count == 0)
        {
            EditorGUILayout.LabelField("No blocks or pipes yet", EditorStyles.helpBox);
        }
        else
        {
            bool allBalanced = true;

            foreach (var color in allColors)
            {
                int required = requiredWaterPerColor.ContainsKey(color) ? requiredWaterPerColor[color] : 0;
                int available = availableWaterPerColor.ContainsKey(color) ? availableWaterPerColor[color] : 0;
                int difference = available - required;

                EditorGUILayout.BeginHorizontal();

                // Color preview
                if (blockColorData != null)
                {
                    Color previewColor = blockColorData.GetColor((ColorEnum)color);
                    EditorGUI.DrawRect(GUILayoutUtility.GetRect(20, 20), previewColor);
                }

                // Color name
                ColorEnum colorEnum = (ColorEnum)color;
                EditorGUILayout.LabelField($"{colorEnum}:", GUILayout.Width(80));

                // Values
                if (difference == 0)
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField($"✓ Balanced ({available}/{required})", GUILayout.Width(150));
                    GUI.color = Color.white;
                }
                else if (difference > 0)
                {
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField($"⚠ Excess +{difference} ({available}/{required})", GUILayout.Width(150));
                    GUI.color = Color.white;
                    allBalanced = false;
                }
                else
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField($"✗ Missing {-difference} ({available}/{required})", GUILayout.Width(150));
                    GUI.color = Color.white;
                    allBalanced = false;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Overall status
            if (allBalanced)
            {
                GUI.backgroundColor = Color.green;
                EditorGUILayout.HelpBox("✓ All colors are balanced!", MessageType.Info);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = Color.yellow;
                EditorGUILayout.HelpBox("⚠ Some colors need adjustment", MessageType.Warning);
                GUI.backgroundColor = Color.white;
            }
        }

        EditorGUILayout.EndVertical();
    }

    private int GetBlockCellCount(BlockShape shape)
    {
        return shape switch
        {
            BlockShape.One => 1,
            BlockShape.Two => 2,
            BlockShape.Three => 3,
            BlockShape.ShortL => 3,
            BlockShape.L => 4,
            BlockShape.ReverseL => 4,
            BlockShape.ShortT => 4,
            BlockShape.TwoSquare => 4,
            BlockShape.Plus => 5,
            _ => 0
        };
    }

    // =====================================================================
    // SAVE + DELETE
    // =====================================================================
    private void DrawSaveDeleteButtons()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
        {
            SaveEditorData();
            EditorUtility.SetDirty(currentMap);
            AssetDatabase.SaveAssets();
            Debug.Log("Level data saved successfully!");
        }
        GUI.backgroundColor = Color.white;

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Delete Map File"))
            if (EditorUtility.DisplayDialog("Delete Map?", "Xóa file này?", "Yes", "No"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(currentMap));
                currentMap = null;
                selectedPipeData = null;
                editorPipes.Clear();
            }
        GUI.backgroundColor = Color.white;
    }

    // =====================================================================
    // UTILITY FUNCTIONS
    // =====================================================================
    private void FixInnerBlockColors()
    {
        if (currentMap == null || currentMap.blocks == null)
        {
            Debug.LogWarning("No map or blocks to fix!");
            return;
        }

        int fixedCount = 0;
        foreach (var block in currentMap.blocks)
        {
            if (block.innerBlockColor == 0)
            {
                block.innerBlockColor = -1;
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            EditorUtility.SetDirty(currentMap);
            Debug.Log($"Fixed {fixedCount} blocks: innerBlockColor changed from 0 to -1");
            Repaint();
        }
        else
        {
            Debug.Log("No blocks needed fixing (no innerBlockColor = 0 found)");
        }
    }
}