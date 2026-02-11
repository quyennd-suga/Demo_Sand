using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Hệ thống mô phỏng cát giả lập (fake sand) dựa trên height field 1D.
/// Không dùng physics, particle system, hay rigidbody.
/// Tối ưu cho mobile – chạy bằng Coroutine với fixed timestep.
/// </summary>
public class SandSimulation2D : MonoBehaviour
{
    // ───────────────────────── Enums ─────────────────────────
    public enum PumpDirection
    {
        Up,    // Bơm từ dưới lên  → tăng chiều cao
        Down,  // Bơm từ trên xuống → giảm chiều cao
        Left,  // Bơm từ trái      → đẩy cát sang phải
        Right  // Bơm từ phải      → đẩy cát sang trái
    }

    // ───────────────────── Inspector fields ──────────────────
    [Header("Grid")]
    [Tooltip("Số cột trong height field")]
    [SerializeField] private int columnCount = 32;

    [Header("Relaxation / Flow")]
    [Tooltip("Độ dốc tối đa trước khi cát bắt đầu chảy (angle of repose giả lập)")]
    [SerializeField] private float maxSlope = 0.015f;

    [Tooltip("Tốc độ lan cát mỗi step (0‒1). Giá trị cao = cát chảy nhanh hơn")]
    [Range(0.01f, 1f)]
    [SerializeField] private float flowSpeed = 0.55f;

    [Tooltip("Số lần relaxation mỗi simulation tick – cao hơn = mịn hơn nhưng tốn hơn")]
    [SerializeField] private int relaxIterationsPerTick = 5;

    [Header("Timing")]
    [Tooltip("Khoảng cách giữa mỗi tick mô phỏng (giây)")]
    [SerializeField] private float simulationInterval = 0.02f;

    [Header("Pump – Lateral")]
    [Tooltip("Hệ số xung lực khi bơm ngang (Left/Right). Quyết định lượng cát bị dồn sang cột kế bên")]
    [Range(0f, 1f)]
    [SerializeField] private float lateralPumpFactor = 0.6f;

    [Tooltip("Số cột bị ảnh hưởng khi bơm ngang (spread radius)")]
    [SerializeField] private int lateralSpreadColumns = 3;

    // ───────────────────── Runtime data ──────────────────────
    /// <summary>
    /// Height field chính – mỗi phần tử là chiều cao cát tại 1 cột, chuẩn hóa 0→1.
    /// </summary>
    private float[] _sandHeight;

    /// <summary>
    /// Buffer xung lực ngang tạm thời (lateral impulse).
    /// Được áp dụng trong relaxation step kế tiếp.
    /// </summary>
    private float[] _lateralImpulse;

    private Coroutine _simCoroutine;
    private bool _running;

    // ───────────────────── Properties ────────────────────────
    /// <summary>Mảng chiều cao cát (read‑only copy). Dùng cho renderer.</summary>
    public float[] SandHeight => _sandHeight;

    /// <summary>Số cột.</summary>
    public int ColumnCount => columnCount;

    /// <summary>Simulation đang chạy?</summary>
    public bool IsRunning => _running;

    // ───────────────────── Events ────────────────────────────
    /// <summary>Gọi mỗi khi height field thay đổi (sau mỗi tick). Renderer lắng nghe event này.</summary>
    public event Action OnHeightFieldChanged;

    // ═══════════════════════ Unity lifecycle ═════════════════
    private void Awake()
    {
        InitializeHeightField();
    }

    private void OnEnable()
    {
        StartSimulation();
    }

    private void OnDisable()
    {
        StopSimulation();
    }

    // ═══════════════════════ Public API ══════════════════════

    /// <summary>
    /// Khởi tạo (hoặc reset) height field với giá trị mặc định = 0.
    /// </summary>
    public void InitializeHeightField()
    {
        _sandHeight = new float[columnCount];
        _lateralImpulse = new float[columnCount];
    }

    /// <summary>
    /// Đặt chiều cao cát cho toàn bộ các cột cùng lúc (ví dụ load level).
    /// Mảng truyền vào sẽ được copy.
    /// </summary>
    public void SetHeightField(float[] heights)
    {
        if (heights == null || heights.Length != columnCount)
        {
            Debug.LogWarning("[SandSim] SetHeightField: array length mismatch.");
            return;
        }
        Array.Copy(heights, _sandHeight, columnCount);
        NotifyChanged();
    }

    /// <summary>
    /// Đặt chiều cao 1 cột.
    /// </summary>
    public void SetColumnHeight(int col, float height)
    {
        if (col < 0 || col >= columnCount) return;
        _sandHeight[col] = Mathf.Clamp01(height);
        NotifyChanged();
    }

    /// <summary>
    /// Bơm cát theo hướng bất kỳ.
    /// </summary>
    /// <param name="columnIndex">Cột mục tiêu (0‒columnCount‑1)</param>
    /// <param name="amount">Lượng cát (chuẩn hóa 0→1)</param>
    /// <param name="direction">Hướng bơm</param>
    public void PumpSand(int columnIndex, float amount, PumpDirection direction)
    {
        if (_sandHeight == null) return;
        if (columnIndex < 0 || columnIndex >= columnCount) return;
        if (amount <= 0f) return;

        switch (direction)
        {
            case PumpDirection.Up:
                PumpUp(columnIndex, amount);
                break;

            case PumpDirection.Down:
                PumpDown(columnIndex, amount);
                break;

            case PumpDirection.Left:
                PumpLateral(columnIndex, amount, -1); // đẩy sang trái → impulse âm? Không – "bơm TỪ trái" = đẩy cát sang PHẢI
                // "Pump Left" = nguồn từ bên trái → cát chảy sang phải
                break;

            case PumpDirection.Right:
                PumpLateral(columnIndex, amount, +1); // "Pump Right" = nguồn từ bên phải → cát chảy sang trái
                break;
        }
    }

    /// <summary>
    /// Bơm cát vào một loạt cột liên tiếp (tiện cho vòi lớn).
    /// </summary>
    public void PumpSandRange(int startCol, int endCol, float amountPerCol, PumpDirection direction)
    {
        for (int i = startCol; i <= endCol; i++)
            PumpSand(i, amountPerCol, direction);
    }

    /// <summary>Bắt đầu simulation loop.</summary>
    public void StartSimulation()
    {
        if (_running) return;
        _running = true;
        _simCoroutine = StartCoroutine(SimulationLoop());
    }

    /// <summary>Dừng simulation (pause puzzle).</summary>
    public void StopSimulation()
    {
        _running = false;
        if (_simCoroutine != null)
        {
            StopCoroutine(_simCoroutine);
            _simCoroutine = null;
        }
    }

    // ═══════════════════════ Pump implementations ═══════════

    /// <summary>Bơm từ dưới lên – tăng chiều cao cột.</summary>
    private void PumpUp(int col, float amount)
    {
        _sandHeight[col] = Mathf.Clamp01(_sandHeight[col] + amount);
    }

    /// <summary>Bơm từ trên xuống – giảm chiều cao (hút cát ra).</summary>
    private void PumpDown(int col, float amount)
    {
        _sandHeight[col] = Mathf.Clamp01(_sandHeight[col] - amount);
    }

    /// <summary>
    /// Bơm ngang – thêm cát tại cột rồi tạo impulse khiến cát dồn sang hướng đối diện.
    /// <paramref name="dirSign"/>: –1 = bơm từ trái (đẩy phải), +1 = bơm từ phải (đẩy trái).
    /// </summary>
    private void PumpLateral(int col, float amount, int dirSign)
    {
        // Thêm cát vào cột nguồn
        _sandHeight[col] = Mathf.Clamp01(_sandHeight[col] + amount);

        // Tạo impulse lan ra các cột theo hướng đối diện nguồn
        // dirSign = -1 → pump from left → cát dồn sang phải → spread direction = +1
        // dirSign = +1 → pump from right → cát dồn sang trái → spread direction = -1
        int spreadDir = -dirSign;

        for (int i = 1; i <= lateralSpreadColumns; i++)
        {
            int targetCol = col + i * spreadDir;
            if (targetCol < 0 || targetCol >= columnCount) break;

            float factor = lateralPumpFactor * (1f - (float)(i - 1) / lateralSpreadColumns);
            float impulse = amount * factor;
            _lateralImpulse[targetCol] += impulse;
        }
    }

    // ═══════════════════════ Simulation loop ════════════════

    private IEnumerator SimulationLoop()
    {
        var wait = new WaitForSeconds(simulationInterval);

        while (_running)
        {
            SimulationTick();
            yield return wait;
        }
    }

    /// <summary>Một tick mô phỏng: áp impulse → relaxation → notify.</summary>
    private void SimulationTick()
    {
        // 1) Áp lateral impulse tích lũy
        ApplyLateralImpulse();

        // 2) Relaxation nhiều lần để cát chảy mịn
        for (int iter = 0; iter < relaxIterationsPerTick; iter++)
            RelaxationStep();

        // 3) Clamp toàn bộ
        ClampAll();

        // 4) Thông báo renderer
        NotifyChanged();
    }

    /// <summary>Áp dụng xung lực ngang đã tích lũy từ PumpLateral.</summary>
    private void ApplyLateralImpulse()
    {
        for (int i = 0; i < columnCount; i++)
        {
            if (_lateralImpulse[i] > 0f)
            {
                _sandHeight[i] = Mathf.Clamp01(_sandHeight[i] + _lateralImpulse[i]);
                _lateralImpulse[i] = 0f;
            }
        }
    }

    /// <summary>
    /// Relaxation: so sánh cột kề → san bớt cát từ cột cao sang cột thấp.
    /// Đây là "angle of repose" giả lập.
    /// </summary>
    private void RelaxationStep()
    {
        // Duyệt trái → phải
        for (int i = 0; i < columnCount - 1; i++)
        {
            float diff = _sandHeight[i] - _sandHeight[i + 1];
            if (diff > maxSlope)
            {
                float transfer = (diff - maxSlope) * 0.5f * flowSpeed;
                _sandHeight[i] -= transfer;
                _sandHeight[i + 1] += transfer;
            }
            else if (-diff > maxSlope)
            {
                float transfer = (-diff - maxSlope) * 0.5f * flowSpeed;
                _sandHeight[i] += transfer;
                _sandHeight[i + 1] -= transfer;
            }
        }
    }

    private void ClampAll()
    {
        for (int i = 0; i < columnCount; i++)
            _sandHeight[i] = Mathf.Clamp01(_sandHeight[i]);
    }

    private void NotifyChanged()
    {
        OnHeightFieldChanged?.Invoke();
    }

    // ═══════════════════════ Editor helpers ══════════════════
#if UNITY_EDITOR
    private void OnValidate()
    {
        columnCount = Mathf.Max(4, columnCount);
        relaxIterationsPerTick = Mathf.Max(1, relaxIterationsPerTick);
        simulationInterval = Mathf.Max(0.005f, simulationInterval);
    }
#endif
}
