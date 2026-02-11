using UnityEngine;
using System.Collections;

/// <summary>
/// Thay thế FillWaterController bằng SandSimulation2D.
/// API hoàn toàn tương thích - chỉ cần swap component trong Inspector.
/// </summary>
[RequireComponent(typeof(SandSimulation2D), typeof(SandRenderer2D))]
public class FillSandController : MonoBehaviour
{
    [Header("Sand Components")]
    [SerializeField] private SandSimulation2D sandSim;
    [SerializeField] private SandRenderer2D sandRenderer;

    [Header("Fill Settings")]
    [Tooltip("Thời gian để fill từ 0→1 (tương đương maxTimeFillWater)")]
    public float maxTimeFillWater = 1f;

    [Tooltip("Nhân tốc độ fill")]
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Runtime Data - API tương thích FillWaterController")]
    public int blockIndex;
    public int waterColor; // Giữ tên để tương thích
    public bool isInnerWater; // Giữ tên để tương thích

    // Private state
    private float currentFillTarget = 0f;
    private float currentFillLevel = 0f;
    private Coroutine fillCoroutine;
    private MaterialPropertyBlock mpb;
    private int colorPropertyId;

    private void Awake()
    {
        if (sandSim == null) sandSim = GetComponent<SandSimulation2D>();
        if (sandRenderer == null) sandRenderer = GetComponent<SandRenderer2D>();

        mpb = new MaterialPropertyBlock();
        colorPropertyId = Shader.PropertyToID("_Color");
    }

    // ═══════════════════ API giống y chang FillWaterController ═══════════════════

    public void ResetWater()
    {
        StopFillCoroutine();
        sandSim.InitializeHeightField();
        currentFillLevel = 0f;
        currentFillTarget = 0f;
        gameObject.SetActive(false);
    }

    public void SetWaterColor(Color color, int blIndex, float duration, int _colorIndex, bool isInner = false)
    {
        maxTimeFillWater = duration;
        blockIndex = blIndex;
        waterColor = _colorIndex;
        isInnerWater = isInner;

        // Set màu material
        var renderer = sandRenderer.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor(colorPropertyId, color);
            renderer.SetPropertyBlock(mpb);
        }

        // Reset state
        sandSim.InitializeHeightField();
        currentFillLevel = 0f;
        currentFillTarget = 0f;
    }

    public void SetFillAmount(float newFill01, bool isComplete)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        float prevTarget = currentFillTarget;
        currentFillTarget = Mathf.Clamp01(currentFillTarget + newFill01);

        StartFillCoroutine(isComplete, prevTarget);
    }

    public void NotifyTransformChanged()
    {
        // Sand tự động theo transform, không cần xử lý
    }

    // ═══════════════════ Fill logic ═══════════════════

    private void StartFillCoroutine(bool isComplete, float prevTarget)
    {
        StopFillCoroutine();
        fillCoroutine = StartCoroutine(FillToTarget_TimeBased(isComplete, prevTarget));
    }

    private void StopFillCoroutine()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
    }

    private IEnumerator FillToTarget_TimeBased(bool isComplete, float prevTarget)
    {
        float start = currentFillLevel;
        float end = currentFillTarget;
        float delta = Mathf.Abs(end - start);

        if (delta <= 0.0001f)
        {
            currentFillLevel = end;
            NotifyComplete(isComplete);
            yield break;
        }

        float baseMax = Mathf.Max(0.01f, maxTimeFillWater);
        float duration = baseMax * delta / Mathf.Max(0.01f, speedMultiplier);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float targetLevel = Mathf.Lerp(start, end, t);
            float pumpAmount = targetLevel - currentFillLevel;

            if (pumpAmount > 0.0001f)
            {
                FillAllColumns(pumpAmount);
                currentFillLevel = targetLevel;
            }

            yield return null;
        }

        // Đảm bảo đạt target chính xác
        float finalPump = end - currentFillLevel;
        if (finalPump > 0.0001f)
        {
            FillAllColumns(finalPump);
        }
        currentFillLevel = end;

        fillCoroutine = null;
        NotifyComplete(isComplete);
    }

    private void FillAllColumns(float amount)
    {
        int colCount = sandSim.ColumnCount;
        for (int i = 0; i < colCount; i++)
        {
            sandSim.PumpSand(i, amount, SandSimulation2D.PumpDirection.Up);
        }
    }

    private void NotifyComplete(bool isComplete)
    {
        BlockView bl = GameController.Instance?.boardView?.GetBlockView(blockIndex);
        if (bl != null)
            bl.OnFillWaterComplete(isComplete, isInnerWater);
    }
}