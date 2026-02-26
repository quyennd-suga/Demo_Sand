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
    [SerializeField] private string sandScrollSpeedProp = "_SandScrollSpeed";
    [SerializeField] private string surfaceGrainSpeedProp = "_SurfaceGrainSpeed";
    
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
    private int sandScrollSpeedId;
    private int surfaceGrainSpeedId;
    private int boundsMinXId;
    private int boundsMaxXId;
    private int peakXId;
    private int peakHeightId;
    private bool markersReady;

    private float currentPeakHeight;
    private float defaultPeakHeight = -1f;

    private float pourWorldX;

    private Coroutine fillRoutine;
    private Coroutine grainSpeedResetRoutine;

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
        if (grainSpeedResetRoutine != null)
        {
            StopCoroutine(grainSpeedResetRoutine);
            grainSpeedResetRoutine = null;
        }
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
        sandScrollSpeedId = Shader.PropertyToID(sandScrollSpeedProp);
        surfaceGrainSpeedId = Shader.PropertyToID(surfaceGrainSpeedProp);
        boundsMinXId = Shader.PropertyToID("_BoundsMinX");
        boundsMaxXId = Shader.PropertyToID("_BoundsMaxX");
        peakXId = Shader.PropertyToID("_PeakX");
        peakHeightId = Shader.PropertyToID("_PeakHeight");

        if (defaultPeakHeight < 0f && targetRenderer != null && targetRenderer.sharedMaterial != null)
        {
            defaultPeakHeight = targetRenderer.sharedMaterial.GetFloat(peakHeightId);
            currentPeakHeight = defaultPeakHeight;
        }
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
    public void SetFillAmount(float newFill01, bool isComplete, float pourX)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        pourWorldX = pourX;

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
        if (grainSpeedResetRoutine != null)
        {
            StopCoroutine(grainSpeedResetRoutine);
            grainSpeedResetRoutine = null;
        }

        float start = currentFill;
        float end = targetFill;

        currentPeakHeight = defaultPeakHeight >= 0f ? defaultPeakHeight : 0.2f;

        mpb.SetFloat(waterSettleSpeedId, 15f);
        mpb.SetFloat(sandScrollSpeedId, 2f);
        mpb.SetFloat(surfaceGrainSpeedId, 5f);
        mpb.SetFloat(peakHeightId, currentPeakHeight);
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
            mpb.SetFloat(sandScrollSpeedId, 0f);
            targetRenderer.SetPropertyBlock(mpb);
            Debug.Log("=================================="+ mpb.GetFloat(waterSettleSpeedId));
            // Grain speed: delay 1s rồi mới tắt
            RestartGrainSpeedReset();
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
        mpb.SetFloat(sandScrollSpeedId, 0f);
        Debug.Log("=================================="+ mpb.GetFloat(waterSettleSpeedId));
        targetRenderer.SetPropertyBlock(mpb);
        // Grain speed: delay 1s rồi mới tắt
        RestartGrainSpeedReset();

        currentFill = end;
        ApplyFill(currentFill);

        fillRoutine = null;
        NotifyComplete(isComplete);
    }

    private void RestartGrainSpeedReset()
    {
        if (grainSpeedResetRoutine != null)
            StopCoroutine(grainSpeedResetRoutine);
        grainSpeedResetRoutine = StartCoroutine(DelayResetGrainSpeed(1.25f));
    }

    private IEnumerator DelayResetGrainSpeed(float delay)
    {
        // Animate _PeakHeight from current value down to a small unevenness over the delay duration
        float startHeight = currentPeakHeight;
        float basePeak = defaultPeakHeight >= 0f ? defaultPeakHeight : 0.2f;
        float targetHeight = basePeak * 0.25f; // Giữ lại 25% độ cao đỉnh để tạo sự mấp mô nhẹ
        float elapsed = 0f;

        while (elapsed < delay)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / delay);
            // Ease-out: cát san nhanh lúc đầu, chậm dần cuối
            float eased = 1f - (1f - t) * (1f - t);
            currentPeakHeight = Mathf.Lerp(startHeight, targetHeight, eased);

            EnsureMPB();
            targetRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(peakHeightId, currentPeakHeight);
            targetRenderer.SetPropertyBlock(mpb);

            yield return null;
        }

        // Kết thúc: reset hẳn grain speed và chốt lại độ mấp mô còn lại
        currentPeakHeight = targetHeight;
        EnsureMPB();
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(surfaceGrainSpeedId, 0f);
        mpb.SetFloat(peakHeightId, targetHeight);
        targetRenderer.SetPropertyBlock(mpb);
        grainSpeedResetRoutine = null;
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

        // Calculate _PeakX from pour position
        float rangeX = b.max.x - b.min.x;
        float peakX = rangeX > 0.001f ? Mathf.Clamp01((pourWorldX - b.min.x) / rangeX) : 0.5f;

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(waterLevelId, waterLevelFromPivot);
        mpb.SetFloat(boundsMinXId, b.min.x);
        mpb.SetFloat(boundsMaxXId, b.max.x);
        mpb.SetFloat(peakXId, peakX);
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
        
        // Set màu cho particle chính
        var main = sandParticle.main;
        main.startColor = color;
        
        // Set màu cho tất cả particle con
        ParticleSystem[] childParticles = sandParticle.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in childParticles)
        {
            var childMain = ps.main;
            childMain.startColor = color;
        }
    }
}
