using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FillWaterController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string waterLevelProp = "_WaterLevelY";
    [SerializeField] private string waterColorProp = "_SideColor";
    [SerializeField] private string waterSettleSpeed = "_SettleSpeed";
    
    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem sandParticle;


    [Header("Fill Mapping")]
    [SerializeField] private bool useWorldBoundsForFill = true;
    [SerializeField] private float levelOffset = 0f;


    [Header("Animation")]
    [Tooltip("Giữ lại để bạn có thể nhân thêm tốc độ nếu muốn (1 = đúng theo maxTimeFillWater).")]
    [SerializeField] private float speedMultiplier = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float fillAmount = 0f;

    public float maxTimeFillWater = 1f;

    private float currentFill;
    private float targetFill;

    public int blockIndex;

    public int waterColor;

    public bool isInnerWater;

    private MaterialPropertyBlock mpb;
    private int waterLevelId;
    private int waterColorId;
    private int waterSettleSpeedId;
    private bool markersReady;

    private Coroutine fillRoutine;

    private Color waterColorValue;

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


    public void ResetWater()
    {
        StopFillRoutine();
        HideParticle();
        currentFill = targetFill = 0f;
        gameObject.SetActive(false);
    }

    private void EnsureMPB()
    {
        if (mpb != null) return;

        mpb = new MaterialPropertyBlock();
        waterLevelId = Shader.PropertyToID(waterLevelProp);
        waterColorId = Shader.PropertyToID(waterColorProp);
        waterSettleSpeedId = Shader.PropertyToID(waterSettleSpeed);
    }

    public void SetWaterColor(Color color, int blIndex, float duration, int _colorIndex, bool isInner = false)
    {
        waterColorValue = color;
        maxTimeFillWater = duration;
        blockIndex = blIndex;
        waterColor = _colorIndex;
        isInnerWater = isInner;
        if (targetRenderer == null) return;

        EnsureMPB();
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(waterColorId, color);
        targetRenderer.SetPropertyBlock(mpb);
        currentFill = targetFill = 0f;
        ApplyFill(0);

        
    }

    /// <summary>
    /// Tăng thêm fill (additive). newFill01 là phần tăng thêm (0..1).
    /// </summary>
    public void SetFillAmount(float newFill01, bool isComplete)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        float prevTarget = targetFill;
        targetFill = Mathf.Clamp01(targetFill + newFill01);
        fillAmount = targetFill;
        // Sync 1 lần để tránh lệch visual nếu vừa spawn/enable
        ApplyFill(currentFill);

        StartFillRoutine(isComplete, prevTarget);
    }


    public void NotifyTransformChanged()
    {
        ApplyFill(currentFill);
    }

    private void StartFillRoutine(bool isComplete, float prevTarget)
    {
        StopFillRoutine();
        ShowParticle();
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
    /// Time-based fill:
    /// - Full 0 -> 1 mất maxTimeFillWater
    /// - Đoạn delta nhỏ sẽ ngắn theo tỷ lệ
    /// - Nếu duration truyền vào nhỏ/0, vẫn clamp để không chia 0
    /// </summary>
    private IEnumerator FillToTarget_TimeBased(bool isComplete, float prevTarget)
    {
        float start = currentFill;
        float end = targetFill;
        mpb.SetFloat(waterSettleSpeedId, 3f);
        targetRenderer.SetPropertyBlock(mpb);
        Debug.Log("=================================="+ mpb.GetFloat(waterSettleSpeedId));
        // delta cần chạy (ưu tiên dựa trên target mới - current hiện tại)
        float delta = Mathf.Abs(end - start);

        // nếu delta quá nhỏ thì snap
        if (delta <= 0.0001f)
        {
            currentFill = end;
            ApplyFill(currentFill);
            fillRoutine = null;
            mpb.SetFloat(waterSettleSpeedId, 0f);
            targetRenderer.SetPropertyBlock(mpb);
            Debug.Log("=================================="+ mpb.GetFloat(waterSettleSpeedId));
            NotifyComplete(isComplete);
            yield break;
        }

        float baseMax = Mathf.Max(0.01f, maxTimeFillWater);
        float duration = baseMax * delta;

        // cho phép tinh chỉnh nhanh/chậm
        float mul = Mathf.Max(0.01f, speedMultiplier);
        duration /= mul;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            currentFill = Mathf.LerpUnclamped(start, end, t);
            ApplyFill(currentFill);
            yield return null;
        }

        mpb.SetFloat(waterSettleSpeedId, 0f);
        Debug.Log("=================================="+ mpb.GetFloat(waterSettleSpeedId));
        targetRenderer.SetPropertyBlock(mpb);

        currentFill = end;
        ApplyFill(currentFill);

        fillRoutine = null;
        NotifyComplete(isComplete);
    }

    private void NotifyComplete(bool isComplete)
    {
        HideParticle();
        
        BlockView bl = GameController.Instance.boardView.GetBlockView(blockIndex);
        if (bl != null)
            bl.OnFillWaterComplete(isComplete, isInnerWater);
    }

    private void ApplyFill(float fill01)
    {

        Bounds b = targetRenderer.bounds; // WORLD AABB
        float bottomWorldY = b.min.y;
        float topWorldY = b.max.y;

        float surfaceWorldY = Mathf.Lerp(bottomWorldY, topWorldY, fill01) + levelOffset;

        float objY = transform.position.y;
        float waterLevelFromPivot = surfaceWorldY - objY;

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(waterLevelId, waterLevelFromPivot);
        targetRenderer.SetPropertyBlock(mpb);
        
        UpdateParticlePosition(surfaceWorldY);
    }
    
    // ========== PARTICLE HELPERS ==========
    
    private void ShowParticle()
    {
        if (sandParticle != null && !sandParticle.isPlaying)
        {
            SetParticleColor(waterColorValue);
            sandParticle.Play();
        }
    }
    
    private void HideParticle()
    {
        if (sandParticle != null && sandParticle.isPlaying)
            sandParticle.Stop();
    }
    
    private void UpdateParticlePosition(float surfaceWorldY)
    {
        if (sandParticle == null) return;
        
        Vector3 pos = sandParticle.transform.position;
        pos.y = surfaceWorldY;
        sandParticle.transform.position = pos;
    }
    private void SetParticleColor(Color color)
    {
        if (sandParticle == null) return;
        
        var main = sandParticle.main;
        main.startColor = color;
    }
}
