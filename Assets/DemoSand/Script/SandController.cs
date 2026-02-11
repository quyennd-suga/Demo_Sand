using UnityEngine;

/// <summary>
/// Controller quản lý FillSandController, tương tự WaterController.
/// API hoàn toàn giống để dễ swap giữa water và sand.
/// </summary>
public class SandController : MonoBehaviour
{
    [Header("Fill Controllers")]
    [SerializeField] private FillSandController[] mixFillSand;
    [SerializeField] private FillSandController fillSand;

    [Header("Mask")]
    [SerializeField] private GameObject sandMask;

    // ═══════════════════ API giống WaterController ═══════════════════

    /// <summary>
    /// Reset toàn bộ cát về 0.
    /// </summary>
    public void ResetWater() // Giữ tên method để tương thích
    {
        fillSand.ResetWater();
        foreach (var fill in mixFillSand)
        {
            fill.ResetWater();
        }
        if (sandMask != null)
            sandMask.SetActive(false);
    }

    /// <summary>
    /// Fill cát chính (single color).
    /// </summary>
    public void SetFillAmount(float amount, bool isComplete)
    {
        if (sandMask != null)
            sandMask.SetActive(true);
        
        fillSand.SetFillAmount(amount, isComplete);
    }

    /// <summary>
    /// Fill cát mix theo màu cụ thể.
    /// </summary>
    public void SetMixFillAmount(float amount, int color, bool isComplete)
    {
        if (sandMask != null)
            sandMask.SetActive(true);

        foreach (var fill in mixFillSand)
        {
            if (fill.waterColor == color)
            {
                fill.SetFillAmount(amount, isComplete);
                break;
            }
        }
    }

    /// <summary>
    /// Set màu cát chính.
    /// </summary>
    public void SetWaterColor(Color color, int blIndex, float duration, int _colorIndex)
    {
        fillSand.SetWaterColor(color, blIndex, duration, _colorIndex);
    }

    /// <summary>
    /// Set màu cát mix theo index.
    /// </summary>
    public void SetMixWaterColor(Color color, int blIndex, float duration, int _colorIndex, int id)
    {
        if (id >= 0 && id < mixFillSand.Length)
        {
            mixFillSand[id].SetWaterColor(color, blIndex, duration, _colorIndex);
        }
    }

    // ═══════════════════ API bổ sung cho Sand ═══════════════════

    /// <summary>
    /// Lấy FillSandController chính.
    /// </summary>
    public FillSandController GetMainFillSand()
    {
        return fillSand;
    }

    /// <summary>
    /// Lấy FillSandController mix theo index.
    /// </summary>
    public FillSandController GetMixFillSand(int index)
    {
        if (index >= 0 && index < mixFillSand.Length)
            return mixFillSand[index];
        return null;
    }

    /// <summary>
    /// Pause/Resume tất cả simulation.
    /// </summary>
    public void SetSimulationActive(bool active)
    {
        if (active)
        {
            fillSand.GetComponent<SandSimulation2D>().StartSimulation();
            foreach (var fill in mixFillSand)
            {
                fill.GetComponent<SandSimulation2D>().StartSimulation();
            }
        }
        else
        {
            fillSand.GetComponent<SandSimulation2D>().StopSimulation();
            foreach (var fill in mixFillSand)
            {
                fill.GetComponent<SandSimulation2D>().StopSimulation();
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-find components nếu chưa assign
        if (fillSand == null)
            fillSand = GetComponentInChildren<FillSandController>();
        
        if (mixFillSand == null || mixFillSand.Length == 0)
            mixFillSand = GetComponentsInChildren<FillSandController>();
    }
#endif
}