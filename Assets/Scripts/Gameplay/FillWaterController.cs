using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FillWaterController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string waterLevelProp = "_WaterLevelY";
    [SerializeField] private string waterColorProp = "_SideColor";
    [SerializeField] private string turbulence = "_Turbulence";
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
    private int turbulenceId;
    private int sandScrollSpeedId;
    private int surfaceGrainSpeedId;
    private int boundsMinXId;
    private int boundsMaxXId;
    private int peakXId;
    private int peakHeightId;
    private int fillDirXId;
    private int fillDirYId;
    private int fillPosXId;
    private int fillPosYId;
    private int sandColor1Id;
    private int sandColor2Id;
    private int sandColor3Id;
    private int sandColor4Id;
    private bool markersReady;

    private float currentPeakHeight;
    private float defaultPeakHeight = -1f;

    private Vector3 pourWorldPos;

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
        turbulenceId = Shader.PropertyToID(turbulence);
        sandScrollSpeedId = Shader.PropertyToID(sandScrollSpeedProp);
        surfaceGrainSpeedId = Shader.PropertyToID(surfaceGrainSpeedProp);
        boundsMinXId = Shader.PropertyToID("_BoundsMinX");
        boundsMaxXId = Shader.PropertyToID("_BoundsMaxX");
        peakXId = Shader.PropertyToID("_PeakX");
        peakHeightId = Shader.PropertyToID("_PeakHeight");
        fillDirXId = Shader.PropertyToID("_FillDirX");
        fillDirYId = Shader.PropertyToID("_FillDirY");
        fillPosXId = Shader.PropertyToID("_FillPosX");
        fillPosYId = Shader.PropertyToID("_FillPosY");
        sandColor1Id = Shader.PropertyToID("_SandColor");
        sandColor2Id = Shader.PropertyToID("_SandColor2");
        sandColor3Id = Shader.PropertyToID("_SandColor3");
        sandColor4Id = Shader.PropertyToID("_SandColor4");

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

        // Set 4 màu cát từ BlockColorData
        Color[] sandColors = DataContainer.Instance.blockColorData.GetSandColors((ColorEnum)_colorIndex);
        mpb.SetColor(sandColor1Id, sandColors[0]);
        mpb.SetColor(sandColor2Id, sandColors[1]);
        mpb.SetColor(sandColor3Id, sandColors[2]);
        mpb.SetColor(sandColor4Id, sandColors[3]);

        targetRenderer.SetPropertyBlock(mpb);
        currentFill = targetFill = 0f;
        ApplyFill(0);
    }

    /// <summary>
    /// Tăng thêm fill (additive). newFill01 là phần tăng thêm (0..1).
    /// </summary>
    public void SetFillAmount(float newFill01, bool isComplete, Vector3 pourPos)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        pourWorldPos = pourPos;

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

        // Tính hướng fill từ pourWorldPos đến center bounds
        Bounds b = targetRenderer.bounds;
        Vector3 center = b.center;
        Vector2 fillDir = new Vector2(pourWorldPos.x - center.x, pourWorldPos.y - center.y);
        if (fillDir.sqrMagnitude > 0.0001f)
            fillDir.Normalize();
        else
            fillDir = Vector2.up;

        // Fill entry point: clamp nozzle vào container bounds (cố định, không dùng surfaceWorldY)
        Vector3 objPos = transform.position;
        float entryX = Mathf.Clamp(pourWorldPos.x, b.min.x, b.max.x) - objPos.x;
        float entryY = Mathf.Clamp(pourWorldPos.y, b.min.y, b.max.y) - objPos.y;

        mpb.SetFloat(fillDirXId, fillDir.x);
        mpb.SetFloat(fillDirYId, fillDir.y);
        mpb.SetFloat(fillPosXId, entryX);
        mpb.SetFloat(fillPosYId, entryY);
        mpb.SetFloat(turbulenceId, 7f);
        mpb.SetFloat(sandScrollSpeedId, 2f);
        mpb.SetFloat(surfaceGrainSpeedId, 1.5f);
        targetRenderer.SetPropertyBlock(mpb);
        Debug.Log("==== FillDir: " + fillDir + " FillPos: (" + entryX + ", " + entryY + ") Turbulence: "+ mpb.GetFloat(turbulenceId));
        // delta cần chạy (ưu tiên dựa trên target mới - current hiện tại)
        float delta = Mathf.Abs(end - start);

        // nếu delta quá nhỏ thì snap
        if (delta <= 0.0001f)
        {
            currentFill = end;
            ApplyFill(currentFill);
            fillRoutine = null;
            ResetTurbulenceImmediate();
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

        ResetTurbulenceImmediate();
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

    /// <summary>
    /// Reset turbulence + fill params ngay lập tức (1 frame)
    /// </summary>
    private void ResetTurbulenceImmediate()
    {
        EnsureMPB();
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(turbulenceId, 0f);
        mpb.SetFloat(sandScrollSpeedId, 0f);
        mpb.SetFloat(fillDirXId, 0f);
        mpb.SetFloat(fillDirYId, 1f);
        mpb.SetFloat(fillPosXId, 0f);
        mpb.SetFloat(fillPosYId, 0.5f);
        targetRenderer.SetPropertyBlock(mpb);
    }

    private IEnumerator DelayResetGrainSpeed(float delay)
    {
        // Chỉ animate _PeakHeight — turbulence đã reset instant rồi
        float startHeight = currentPeakHeight;
        float basePeak = defaultPeakHeight >= 0f ? defaultPeakHeight : 0.2f;
        float targetHeight = basePeak * 0.25f;
        float elapsed = 0f;

        while (elapsed < delay)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / delay);
            float eased = 1f - (1f - t) * (1f - t);
            currentPeakHeight = Mathf.Lerp(startHeight, targetHeight, eased);

            ApplyFill(currentFill);
            yield return null;
        }

        // Kết thúc: reset grain speed
        currentPeakHeight = targetHeight;
        EnsureMPB();
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(surfaceGrainSpeedId, 0f);
        targetRenderer.SetPropertyBlock(mpb);
        ApplyFill(currentFill);
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
        float defaultPeakX = rangeX > 0.001f ? Mathf.Clamp01((pourWorldPos.x - b.min.x) / rangeX) : 0.5f;

        float peakX   = defaultPeakX;
        float height  = currentPeakHeight;

        // distanceY > 0  → vòi còn trên bề mặt cát (bình thường)
        // distanceY = 0  → vòi vừa chạm bề mặt (bắt đầu chìm)
        // distanceY < 0  → vòi bị cát bao phủ
        float distanceY = pourWorldPos.y - surfaceWorldY;

        if (distanceY < 0f && gameObject.activeInHierarchy)
        {
            // ── Pha 2: vòi vừa chìm (0 → -pushZone) ─────────────────────────
            // Smooth push đỉnh sang đối diện + giảm height
            const float pushZone    = 0.55f;   // khoảng chìm để hoàn thành push
            const float flattenZone = 0.30f;   // khoảng tiếp theo để flatten về 0

            float pushT = Mathf.Clamp01(-distanceY / pushZone);
            pushT = Mathf.SmoothStep(0f, 1f, pushT);

            // Đỉnh dịch sang phía đối diện vòi (mượt hơn: lerp tới 0.75/0.25 thay vì 0.8/0.2 cứng)
            float oppositeX = defaultPeakX < 0.5f
                ? Mathf.Lerp(0.7f, 0.8f, defaultPeakX * 2f)   // vòi nghiêng trái → đỉnh phải
                : Mathf.Lerp(0.3f, 0.2f, (defaultPeakX - 0.5f) * 2f); // vòi nghiêng phải → đỉnh trái
            peakX = Mathf.Lerp(defaultPeakX, oppositeX, pushT);

            // Height giảm về mức "trải phẳng" khi push hoàn tất
            float mediumHeight = currentPeakHeight * 0.35f;
            height = Mathf.Lerp(currentPeakHeight, mediumHeight, pushT);

            // ── Pha 3: vòi chìm sâu thêm (−pushZone → −pushZone−flattenZone) ──
            // Tiếp tục flatten xuống ~0, bắt đầu sau khi push xong
            float flattenDepth = -distanceY - pushZone;
            if (flattenDepth > 0f)
            {
                float flatT = Mathf.Clamp01(flattenDepth / flattenZone);
                flatT  = Mathf.SmoothStep(0f, 1f, flatT);
                height = Mathf.Lerp(mediumHeight, 0.01f, flatT);
                // PeakX giữ nguyên vị trí đã push sang, không cần dịch thêm
            }
        }

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(waterLevelId, waterLevelFromPivot);
        mpb.SetFloat(boundsMinXId, b.min.x);
        mpb.SetFloat(boundsMaxXId, b.max.x);
        mpb.SetFloat(peakXId, peakX);
        mpb.SetFloat(peakHeightId, height);
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
