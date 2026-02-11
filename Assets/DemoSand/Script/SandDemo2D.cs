using UnityEngine;

/// <summary>
/// Script demo để test hệ thống cát.
/// Gắn vào cùng GameObject với SandSimulation2D + SandRenderer2D.
/// Cho phép bơm cát bằng click/touch từ 4 hướng.
/// 
/// Cách dùng:
/// - Click/giữ chuột trái: Đổ cát tại vị trí chuột
/// - Click/giữ chuột phải: Hút cát tại vị trí chuột
/// - Phím Space: Toggle simulation on/off
/// - Phím R: Reset toàn bộ cát
/// - Phím F: Fill cát đều 50%
/// </summary>
[RequireComponent(typeof(SandSimulation2D), typeof(SandRenderer2D))]
public class SandDemo2D : MonoBehaviour
{
    [Header("Pump Settings")]
    [Tooltip("Lượng cát bơm mỗi frame khi giữ chuột")]
    [SerializeField] private float pumpAmountPerFrame = 0.02f;

    [Tooltip("Số cột ảnh hưởng khi bơm (brush width)")]
    [SerializeField] private int brushWidth = 3;

    [Header("Auto Demo")]
    [Tooltip("Bật để tự động bơm cát demo khi start")]
    [SerializeField] private bool autoDemoOnStart = true;

    [Tooltip("Lượng cát fill ban đầu (0→1)")]
    [SerializeField] private float initialFillLevel = 0.3f;

    private SandSimulation2D _sim;
    private SandRenderer2D _renderer;
    private Camera _cam;

    private void Awake()
    {
        _sim = GetComponent<SandSimulation2D>();
        _renderer = GetComponent<SandRenderer2D>();
        _cam = Camera.main;
    }

    private void Start()
    {
        if (autoDemoOnStart)
        {
            FillSand(initialFillLevel);
        }
    }

    private void Update()
    {
        HandleInput();
        HandleKeyboard();
    }

    // ═══════════════════════ Input ═══════════════════════════

    private void HandleInput()
    {
        bool leftDown = Input.GetMouseButton(0);
        bool rightDown = Input.GetMouseButton(1);

        if (!leftDown && !rightDown) return;

        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = Mathf.Abs(_cam.transform.position.z);
        Vector3 worldPos = _cam.ScreenToWorldPoint(mouseScreen);

        int centerCol = _renderer.WorldXToColumn(worldPos.x);

        // Click trái = đổ cát, click phải = hút cát
        SandSimulation2D.PumpDirection dir = leftDown
            ? SandSimulation2D.PumpDirection.Up
            : SandSimulation2D.PumpDirection.Down;

        int halfBrush = brushWidth / 2;
        int startCol = Mathf.Max(0, centerCol - halfBrush);
        int endCol = Mathf.Min(_sim.ColumnCount - 1, centerCol + halfBrush);

        _sim.PumpSandRange(startCol, endCol, pumpAmountPerFrame, dir);
    }

    private void HandleKeyboard()
    {
        // Space: toggle simulation
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_sim.IsRunning)
            {
                _sim.StopSimulation();
                Debug.Log("[SandDemo] Simulation PAUSED");
            }
            else
            {
                _sim.StartSimulation();
                Debug.Log("[SandDemo] Simulation RESUMED");
            }
        }

        // R: reset
        if (Input.GetKeyDown(KeyCode.R))
        {
            _sim.InitializeHeightField();
            Debug.Log("[SandDemo] Reset sand");
        }

        // F: fill đều
        if (Input.GetKeyDown(KeyCode.F))
        {
            FillSand(0.5f);
            Debug.Log("[SandDemo] Filled 50%");
        }
    }

    // ═══════════════════════ Helpers ═════════════════════════

    /// <summary>Fill toàn bộ cột cùng chiều cao.</summary>
    private void FillSand(float level)
    {
        float[] heights = new float[_sim.ColumnCount];
        for (int i = 0; i < heights.Length; i++)
            heights[i] = level;
        _sim.SetHeightField(heights);
    }

    // ═══════════════════════ Editor ══════════════════════════
#if UNITY_EDITOR
    [ContextMenu("Quick Setup - Create Material")]
    private void QuickSetupMaterial()
    {
        // Tìm shader SandUnlit2D
        Shader shader = Shader.Find("Custom/SandUnlit2D");
        if (shader == null)
        {
            Debug.LogError("Shader 'Custom/SandUnlit2D' not found! Make sure SandUnlit2D.shader exists.");
            return;
        }

        Material mat = new Material(shader);
        mat.name = "SandMaterial";

        // Lưu vào project
        string path = "Assets/DemoSand/Script/SandMaterial.mat";
        UnityEditor.AssetDatabase.CreateAsset(mat, path);
        UnityEditor.AssetDatabase.SaveAssets();

        // Gán cho renderer
        var renderer = GetComponent<SandRenderer2D>();
        if (renderer != null)
        {
            var serialized = new UnityEditor.SerializedObject(renderer);
            var prop = serialized.FindProperty("sandMaterial");
            prop.objectReferenceValue = mat;
            serialized.ApplyModifiedProperties();
        }

        Debug.Log($"Created and assigned material at {path}");
    }
#endif
}
