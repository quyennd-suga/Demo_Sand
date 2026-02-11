using System.Collections;
using UnityEngine;

/// <summary>
/// Fill controller cho shader "Sugame/URP/SandFill".
/// Hoạt động giống y hệt FillWaterController - set shader parameter _SandLevelY.
/// API tương thích để swap dễ dàng.
/// </summary>
[DisallowMultipleComponent]
public class FillSandShaderController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string sandLevelProp = "_SandLevelY";
    [SerializeField] private string sandColorProp = "_Color";

    [Header("Fill Mapping")]
    [SerializeField] private float levelOffset = 0f;

    [Header("Animation")]
    [Tooltip("Nhân tốc độ fill (1 = đúng theo maxTimeFillWater)")]
    [SerializeField] private float speedMultiplier = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float fillAmount = 0f;

    public float maxTimeFillWater = 1f;

    private float currentFill;
    private float targetFill;

    public int blockIndex;
    public int waterColor; // Giữ tên để tương thích
    public bool isInnerWater; // Giữ tên để tương thích

    private MaterialPropertyBlock mpb;
    private int sandLevelId;
    private int sandColorId;

    private Coroutine fillRoutine;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();

        EnsureMPB();

        currentFill = targetFill = Mathf.Clamp01(fillAmount);
        ApplyFill(currentFill);
    }

    private void OnDisable()
    {
        StopFillRoutine();
    }

    // ═══════════════════ API tương thích FillWaterController ═══════════════════

    public void ResetWater()
    {
        StopFillRoutine();
        currentFill = targetFill = 0f;
        ApplyFill(0f);
        gameObject.SetActive(false);
    }

    public void SetWaterColor(Color color, int blIndex, float duration, int _colorIndex, bool isInner = false)
    {
        maxTimeFillWater = duration;
        blockIndex = blIndex;
        waterColor = _colorIndex;
        isInnerWater = isInner;

        if (targetRenderer == null) return;

        EnsureMPB();
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(sandColorId, color);
        targetRenderer.SetPropertyBlock(mpb);

        currentFill = targetFill = 0f;
        ApplyFill(0f);
    }

    /// <summary>
    /// Tăng fill (additive). newFill01 là phần tăng thêm (0..1).
    /// </summary>
    public void SetFillAmount(float newFill01, bool isComplete)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        float prevTarget = targetFill;
        targetFill = Mathf.Clamp01(targetFill + newFill01);
        fillAmount = targetFill;

        // Sync visual
        ApplyFill(currentFill);

        StartFillRoutine(isComplete, prevTarget);
    }

    public void NotifyTransformChanged()
    {
        ApplyFill(currentFill);
    }

    // ═══════════════════ Fill logic ═══════════════════

    private void EnsureMPB()
    {
        if (mpb != null) return;

        mpb = new MaterialPropertyBlock();
        sandLevelId = Shader.PropertyToID(sandLevelProp);
        sandColorId = Shader.PropertyToID(sandColorProp);
    }

    private void StartFillRoutine(bool isComplete, float prevTarget)
    {
        StopFillRoutine();
        fillRoutine = StartCoroutine(FillToTarget_TimeBased(isComplete, prevTarget));
    }

    private void StopFillRoutine()
    {
        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }
    }

    /// <summary>
    /// Time-based fill: Full 0→1 mất maxTimeFillWater giây.
    /// </summary>
    private IEnumerator FillToTarget_TimeBased(bool isComplete, float prevTarget)
    {
        float start = currentFill;
        float end = targetFill;
        float delta = Mathf.Abs(end - start);

        if (delta <= 0.0001f)
        {
            currentFill = end;
            ApplyFill(currentFill);
            fillRoutine = null;
            NotifyComplete(isComplete);
            yield break;
        }

        float baseMax = Mathf.Max(0.01f, maxTimeFillWater);
        float duration = baseMax * delta / Mathf.Max(0.01f, speedMultiplier);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            currentFill = Mathf.LerpUnclamped(start, end, t);
            ApplyFill(currentFill);
            yield return null;
        }

        currentFill = end;
        ApplyFill(currentFill);

        fillRoutine = null;
        NotifyComplete(isComplete);
    }

    private void NotifyComplete(bool isComplete)
    {
        BlockView bl = GameController.Instance?.boardView?.GetBlockView(blockIndex);
        if (bl != null)
            bl.OnFillWaterComplete(isComplete, isInnerWater);
    }

    /// <summary>
    /// Apply fill level to shader parameter _SandLevelY.
    /// Tính toán giống y hệt FillWaterController.
    /// </summary>
    private void ApplyFill(float fill01)
    {
        if (targetRenderer == null) return;

        // World AABB bounds
        Bounds b = targetRenderer.bounds;
        float bottomWorldY = b.min.y;
        float topWorldY = b.max.y;

        // Surface Y trong world space
        float surfaceWorldY = Mathf.Lerp(bottomWorldY, topWorldY, fill01) + levelOffset;

        // Convert về local từ pivot
        float objY = transform.position.y;
        float sandLevelFromPivot = surfaceWorldY - objY;

        // Set shader parameter
        EnsureMPB();
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(sandLevelId, sandLevelFromPivot);
        targetRenderer.SetPropertyBlock(mpb);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && targetRenderer != null)
        {
            ApplyFill(fillAmount);
        }
    }
#endif
}
