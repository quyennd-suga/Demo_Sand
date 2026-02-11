using UnityEngine;

/// <summary>
/// Renderer cho hệ thống cát giả lập.
/// Nhận sandHeight[] từ SandSimulation2D → dựng Mesh 2D mỗi khi dữ liệu thay đổi.
/// Hoàn toàn tách biệt khỏi logic simulation.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SandRenderer2D : MonoBehaviour
{
    // ───────────────────── Inspector ─────────────────────────
    [Header("References")]
    [Tooltip("Tham chiếu tới SandSimulation2D (có thể cùng GameObject hoặc khác)")]
    [SerializeField] private SandSimulation2D simulation;

    [Header("Dimensions")]
    [Tooltip("Chiều rộng tổng thể của vùng cát (world units) - Tự điều chỉnh cho khớp container")]
    [SerializeField] private float totalWidth = 6f;

    [Tooltip("Chiều cao tối đa khi sandHeight = 1 (world units) - Tự điều chỉnh cho khớp container")]
    [SerializeField] private float maxHeight = 4f;

    [Header("Mesh Mode")]
    [Tooltip("Dùng mesh có sẵn (assign trong MeshFilter) thay vì tạo mesh động")]
    [SerializeField] private bool usePremadeMesh = false;

    [Header("Visual")]
    [Tooltip("Material (nên dùng shader SandUnlit2D)")]
    [SerializeField] private Material sandMaterial;

    [Tooltip("Bật noise nhẹ trên đỉnh cát để tạo cảm giác hạt")]
    [SerializeField] private bool enableTopNoise = false;

    [Tooltip("Biên độ noise (world units)")]
    [SerializeField] private float noiseAmplitude = 0.03f;

    [Tooltip("Tần số noise")]
    [SerializeField] private float noiseFrequency = 8f;

    // ───────────────────── Runtime ───────────────────────────
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private Vector3[] _vertices;
    private Vector2[] _uvs;
    private int[] _triangles;
    private Color[] _colors;

    private int _colCount;
    private bool _meshInitialized;

    // ═══════════════════════ Unity lifecycle ═════════════════
    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        if (simulation == null)
            simulation = GetComponent<SandSimulation2D>();
    }

    private void OnEnable()
    {
        if (simulation != null)
            simulation.OnHeightFieldChanged += RebuildMesh;
    }

    private void OnDisable()
    {
        if (simulation != null)
            simulation.OnHeightFieldChanged -= RebuildMesh;
    }

    private void Start()
    {
        if (usePremadeMesh)
        {
            InitPremadeMesh();
        }
        else
        {
            InitMesh();
        }
        RebuildMesh();
    }

    // ═══════════════════════ Mesh setup ═════════════════════

    /// <summary>
    /// Khởi tạo mesh & buffers một lần.
    /// Topology: mỗi cột tạo 1 quad (2 triangles) → (colCount) quads.
    /// Vertices: 2 hàng × (colCount + 1) điểm.
    ///   - Hàng dưới (y = 0): index 0 → colCount
    ///   - Hàng trên (y = height): index (colCount+1) → 2*(colCount+1)−1
    /// </summary>
    private void InitMesh()
    {
        if (simulation == null)
        {
            Debug.LogError("[SandRenderer2D] Missing SandSimulation2D reference!");
            return;
        }

        _colCount = simulation.ColumnCount;
        int vertCountPerRow = _colCount + 1; // +1 vì mỗi quad cần cạnh trái + phải
        int totalVerts = vertCountPerRow * 2;

        _vertices = new Vector3[totalVerts];
        _uvs = new Vector2[totalVerts];
        _colors = new Color[totalVerts];
        _triangles = new int[_colCount * 6]; // 2 tri × 3 idx mỗi cột

        // Tính vị trí x cho mỗi cột
        float colWidth = totalWidth / _colCount;

        // Tạo vertices mặc định (y = 0 cho cả 2 hàng)
        for (int i = 0; i <= _colCount; i++)
        {
            float x = -totalWidth * 0.5f + i * colWidth;

            // Hàng dưới
            _vertices[i] = new Vector3(x, 0f, 0f);
            _uvs[i] = new Vector2((float)i / _colCount, 0f);
            _colors[i] = Color.white;

            // Hàng trên
            int topIdx = vertCountPerRow + i;
            _vertices[topIdx] = new Vector3(x, 0f, 0f);
            _uvs[topIdx] = new Vector2((float)i / _colCount, 0f);
            _colors[topIdx] = Color.white;
        }

        // Tạo triangles
        for (int col = 0; col < _colCount; col++)
        {
            int bl = col;                       // bottom-left
            int br = col + 1;                   // bottom-right
            int tl = vertCountPerRow + col;     // top-left
            int tr = vertCountPerRow + col + 1; // top-right

            int ti = col * 6;
            // Tri 1
            _triangles[ti + 0] = bl;
            _triangles[ti + 1] = tl;
            _triangles[ti + 2] = tr;
            // Tri 2
            _triangles[ti + 3] = bl;
            _triangles[ti + 4] = tr;
            _triangles[ti + 5] = br;
        }

        _mesh = new Mesh();
        _mesh.name = "SandMesh2D";
        _mesh.MarkDynamic(); // Tối ưu cho mesh cập nhật liên tục

        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.colors = _colors;
        _mesh.triangles = _triangles;

        _meshFilter.mesh = _mesh;

        // Gán material – tự tạo default nếu chưa gán
        if (sandMaterial == null)
        {
            Shader sh = Shader.Find("Custom/SandUnlit2D");
            if (sh == null)
                sh = Shader.Find("Sprites/Default");
            sandMaterial = new Material(sh);
            sandMaterial.color = new Color(0.86f, 0.72f, 0.52f, 1f);
            Debug.Log("[SandRenderer2D] Auto-created default sand material.");
        }
        _meshRenderer.material = sandMaterial;

        _meshInitialized = true;
    }

    // ═══════════════════════ Rebuild ═════════════════════════

    /// <summary>
    /// Cập nhật vertex y của hàng trên dựa trên sandHeight[].
    /// Gọi mỗi khi simulation thay đổi dữ liệu.
    /// </summary>
    public void RebuildMesh()
    {
        if (!_meshInitialized || simulation == null) return;

        float[] heights = simulation.SandHeight;
        if (heights == null || heights.Length != _colCount) return;

        if (usePremadeMesh)
        {
            RebuildPremadeMesh(heights);
        }
        else
        {
            RebuildDynamicMesh(heights);
        }
    }

    /// <summary>
    /// Rebuild cho mesh được tạo động (original mode).
    /// </summary>
    private void RebuildDynamicMesh(float[] heights)
    {
        int vertCountPerRow = _colCount + 1;
        float colWidth = totalWidth / _colCount;

        // Cập nhật hàng trên (top row vertices)
        for (int i = 0; i <= _colCount; i++)
        {
            // Chiều cao tại biên cột = trung bình 2 cột kề (mịn hơn)
            float h;
            if (i == 0)
                h = heights[0];
            else if (i == _colCount)
                h = heights[_colCount - 1];
            else
                h = (heights[i - 1] + heights[i]) * 0.5f;

            float y = h * maxHeight;

            // Optional noise nhẹ trên đỉnh cát
            if (enableTopNoise && h > 0.01f)
            {
                float noiseX = (float)i * noiseFrequency + Time.time * 0.3f;
                float n = Mathf.PerlinNoise(noiseX, 0.5f) * 2f - 1f; // −1 → 1
                y += n * noiseAmplitude;
            }

            int topIdx = vertCountPerRow + i;
            _vertices[topIdx].y = y;

            // UV.y = chiều cao chuẩn hóa (dùng trong shader cho gradient)
            float uvY = Mathf.Clamp01(y / maxHeight);
            _uvs[topIdx].y = uvY;

            // Vertex color: encode chiều cao để shader dùng
            _colors[topIdx] = new Color(1f, 1f, 1f, uvY);
        }

        // Cập nhật mesh buffers
        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.colors = _colors;
        _mesh.RecalculateBounds();
    }

    /// <summary>
    /// Rebuild cho premade mesh - chỉ update Y của vertices dựa trên sand height.
    /// Giữ nguyên mesh structure gốc.
    /// </summary>
    private void RebuildPremadeMesh(float[] heights)
    {
        Bounds bounds = _mesh.bounds;
        float colWidth = bounds.size.x / _colCount;
        float bottomY = bounds.min.y;

        // Cho mỗi vertex, tìm column tương ứng và update Y
        for (int i = 0; i < _vertices.Length; i++)
        {
            Vector3 v = _vertices[i];
            
            // Tìm column index dựa trên X position
            float normalizedX = (v.x - bounds.min.x) / bounds.size.x;
            int colIndex = Mathf.Clamp(Mathf.FloorToInt(normalizedX * _colCount), 0, _colCount - 1);
            
            // Lấy sand height tại column này
            float sandHeight = heights[colIndex];
            float targetY = bottomY + sandHeight * maxHeight;

            // Chỉ update vertices ở phía trên (Y > bottomY + threshold)
            // Giữ nguyên vertices ở bottom (đáy container)
            if (v.y > bottomY + 0.1f)
            {
                // Interpolate Y based on original Y position
                float originalHeightRatio = (v.y - bottomY) / bounds.size.y;
                
                // Nếu vertex này ở phía trên sand level → đặt về sand level
                // Nếu dưới sand level → giữ nguyên
                if (originalHeightRatio > sandHeight)
                {
                    _vertices[i].y = targetY;
                }
                else
                {
                    _vertices[i].y = bottomY + originalHeightRatio * sandHeight * maxHeight;
                }
            }
        }

        // Update mesh
        _mesh.vertices = _vertices;
        _mesh.RecalculateBounds();
    }

    // ═══════════════════════ Public helpers ══════════════════

    /// <summary>
    /// Chuyển world X position → column index gần nhất.
    /// Tiện cho input / pump logic.
    /// </summary>
    public int WorldXToColumn(float worldX)
    {
        float localX = worldX - transform.position.x;
        float leftEdge = -totalWidth * 0.5f;
        float colWidth = totalWidth / _colCount;
        int col = Mathf.FloorToInt((localX - leftEdge) / colWidth);
        return Mathf.Clamp(col, 0, _colCount - 1);
    }

    /// <summary>
    /// Column index → world X center.
    /// </summary>
    public float ColumnToWorldX(int col)
    {
        float leftEdge = -totalWidth * 0.5f;
        float colWidth = totalWidth / _colCount;
        return transform.position.x + leftEdge + (col + 0.5f) * colWidth;
    }

    public float TotalWidth => totalWidth;
    public float MaxHeight => maxHeight;

    // ═══════════════════════ Premade Mesh Mode ═══════════════

    /// <summary>
    /// Khởi tạo từ mesh có sẵn - không tầo mesh mới.
    /// Chỉ setup buffers để update vertex Y positions.
    /// </summary>
    private void InitPremadeMesh()
    {
        if (simulation == null)
        {
            Debug.LogError("[SandRenderer2D] Missing SandSimulation2D reference!");
            return;
        }

        Mesh originalMesh = _meshFilter.sharedMesh;
        if (originalMesh == null)
        {
            Debug.LogError("[SandRenderer2D] usePremadeMesh=true but no mesh assigned in MeshFilter!");
            return;
        }

        // Tạo readable copy nếu mesh không readable
        if (!originalMesh.isReadable)
        {
            Debug.LogWarning($"[SandRenderer2D] Mesh '{originalMesh.name}' is not readable. Creating readable copy...");
            _mesh = CreateReadableMeshCopy(originalMesh);
            _meshFilter.mesh = _mesh; // Assign copy vào MeshFilter
        }
        else
        {
            // Clone mesh để không modify original
            _mesh = Instantiate(originalMesh);
            _meshFilter.mesh = _mesh;
        }

        _colCount = simulation.ColumnCount;
        _vertices = _mesh.vertices;
        _uvs = _mesh.uv;
        _colors = _mesh.colors;
        _triangles = _mesh.triangles;

        // Auto-detect dimensions từ mesh bounds
        Bounds bounds = _mesh.bounds;
        totalWidth = bounds.size.x;
        maxHeight = bounds.size.y;

        Debug.Log($"[SandRenderer2D] Using premade mesh: {_mesh.name}, bounds: {totalWidth}x{maxHeight}, vertices: {_vertices.Length}");

        // Assign material nếu chưa có
        if (_meshRenderer.sharedMaterial == null && sandMaterial != null)
        {
            _meshRenderer.material = sandMaterial;
        }

        _meshInitialized = true;
    }

    /// <summary>
    /// Tạo readable copy của mesh không readable bằng Graphics.CopyBuffer.
    /// </summary>
    private Mesh CreateReadableMeshCopy(Mesh original)
    {
        Mesh readable = new Mesh();
        readable.name = original.name + "_Readable";

        // Copy vertex data
        readable.vertices = GetMeshVertices(original);
        readable.triangles = GetMeshTriangles(original);
        readable.normals = GetMeshNormals(original);
        readable.uv = GetMeshUV(original);
        readable.colors = GetMeshColors(original);
        readable.tangents = GetMeshTangents(original);

        readable.RecalculateBounds();
        return readable;
    }

    // Helper methods để đọc mesh data bằng GetVertices API (works with non-readable meshes)
    private Vector3[] GetMeshVertices(Mesh mesh)
    {
        var vertices = new System.Collections.Generic.List<Vector3>();
        mesh.GetVertices(vertices);
        return vertices.ToArray();
    }

    private int[] GetMeshTriangles(Mesh mesh)
    {
        var triangles = new System.Collections.Generic.List<int>();
        mesh.GetTriangles(triangles, 0);
        return triangles.ToArray();
    }

    private Vector3[] GetMeshNormals(Mesh mesh)
    {
        var normals = new System.Collections.Generic.List<Vector3>();
        mesh.GetNormals(normals);
        return normals.Count > 0 ? normals.ToArray() : null;
    }

    private Vector2[] GetMeshUV(Mesh mesh)
    {
        var uvs = new System.Collections.Generic.List<Vector2>();
        mesh.GetUVs(0, uvs);
        return uvs.Count > 0 ? uvs.ToArray() : null;
    }

    private Color[] GetMeshColors(Mesh mesh)
    {
        var colors = new System.Collections.Generic.List<Color>();
        mesh.GetColors(colors);
        return colors.Count > 0 ? colors.ToArray() : null;
    }

    private Vector4[] GetMeshTangents(Mesh mesh)
    {
        var tangents = new System.Collections.Generic.List<Vector4>();
        mesh.GetTangents(tangents);
        return tangents.Count > 0 ? tangents.ToArray() : null;
    }

    // ═══════════════════════ Editor ══════════════════════════
#if UNITY_EDITOR
    private void OnValidate()
    {
        totalWidth = Mathf.Max(0.1f, totalWidth);
        maxHeight = Mathf.Max(0.1f, maxHeight);
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ bounding box trong editor
        Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.3f);
        Vector3 center = transform.position + new Vector3(0f, maxHeight * 0.5f, 0f);
        Vector3 size = new Vector3(totalWidth, maxHeight, 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
