using System.Collections;
using UnityEngine;

public class Fruit : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public int colorId;
    private Coroutine animCo;

    private static readonly int SandColor1Id = Shader.PropertyToID("_SandColor");
    private static readonly int SandColor2Id = Shader.PropertyToID("_SandColor2");
    private static readonly int SandColor3Id = Shader.PropertyToID("_SandColor3");
    private static readonly int SandColor4Id = Shader.PropertyToID("_SandColor4");
    private static readonly int WaterLevelYId = Shader.PropertyToID("_WaterLevelY");
    private static readonly int BoundsMinXId = Shader.PropertyToID("_BoundsMinX");
    private static readonly int BoundsMaxXId = Shader.PropertyToID("_BoundsMaxX");
    private static readonly int TurbulenceId = Shader.PropertyToID("_Turbulence");
    private static readonly int PeakHeightId = Shader.PropertyToID("_PeakHeight");
    private static readonly int FillVelocityId = Shader.PropertyToID("_FillVelocity");
    private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    private static readonly int FlowSpreadId = Shader.PropertyToID("_FlowSpread");
    private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
    private static readonly int SurfaceGrainSpeedId = Shader.PropertyToID("_SurfaceGrainSpeed");
    private static readonly int GrainStrengthId = Shader.PropertyToID("_GrainStrength");
    private static readonly int BottomDarknessId = Shader.PropertyToID("_BottomDarkness");
    private static readonly int SurfaceBandHeightId = Shader.PropertyToID("_SurfaceBandHeight");
    private static readonly int ScrollSpeedId = Shader.PropertyToID("_ScrollSpeed");
    private static readonly int ScrollDirXId = Shader.PropertyToID("_ScrollDirX");
    private static readonly int ScrollDirYId = Shader.PropertyToID("_ScrollDirY");

    private MaterialPropertyBlock mpb;

    /// <summary>
    /// Config fruit: set 4 màu cát + scroll theo hướng pipe đổ cát.
    /// Prefab Fruit cần có SpriteRenderer đã gán sẵn material SandEffect2.
    /// </summary>
    public void ConfigFruit(int color, Direction pipeDirection)
    {
        colorId = color;

        if (mpb == null) mpb = new MaterialPropertyBlock();

        // Tạo material instance riêng để phá SpriteRenderer dynamic batching.
        // Khi bị batch chung, MPB values bị leak giữa các sprites
        // → quả đứng im bị "nhiễm" _ScrollSpeed từ quả đang di chuyển.
        spriteRenderer.material = new Material(spriteRenderer.sharedMaterial);

        // Set 4 màu cát giống cách FillWaterController.SetWaterColor
        Color[] sandColors = DataContainer.Instance.blockColorData.GetSandColors((ColorEnum)color);
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(SandColor1Id, sandColors[0]);
        mpb.SetColor(SandColor2Id, sandColors[1]);
        mpb.SetColor(SandColor3Id, sandColors[2]);
        mpb.SetColor(SandColor4Id, sandColors[3]);

        // _WaterLevelY lớn để toàn bộ sprite được "fill đầy cát" (tránh clip)
        mpb.SetFloat(WaterLevelYId, 100f);

        // Bounds cho normalizedWorldX — dùng renderer bounds
        Bounds b = spriteRenderer.bounds;
        mpb.SetFloat(BoundsMinXId, b.min.x);
        mpb.SetFloat(BoundsMaxXId, b.max.x);

        // Tắt TẤT CẢ hiệu ứng động → cát hoàn toàn tĩnh
        mpb.SetFloat(TurbulenceId, 0f);         // grain push/swirl wave
        mpb.SetFloat(PeakHeightId, 0f);          // đỉnh tam giác bề mặt
        mpb.SetFloat(FillVelocityId, 0f);        // blend sang mountain wave
        mpb.SetFloat(NoiseStrengthId, 0f);       // sóng tĩnh bề mặt
        mpb.SetFloat(FlowSpreadId, 0f);          // cell offset magnitude
        mpb.SetFloat(WaveSpeedId, 0f);           // tốc độ sóng
        mpb.SetFloat(SurfaceGrainSpeedId, 0f);   // surface drifting voronoi grains
        mpb.SetFloat(GrainStrengthId, 0f);       // grain brightness variation
        mpb.SetFloat(BottomDarknessId, 0f);      // depth darkening → tránh jitter do d thay đổi
        mpb.SetFloat(SurfaceBandHeightId, 0.001f); // surface voronoi band → gần như tắt

        // Hướng scroll (set sẵn, nhưng speed = 0 → đứng im cho đến khi đổ)
        Vector2 scrollDir = GetScrollDirection(pipeDirection);
        mpb.SetFloat(ScrollSpeedId, 0f);
        mpb.SetFloat(ScrollDirXId, scrollDir.x);
        mpb.SetFloat(ScrollDirYId, scrollDir.y);

        spriteRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>
    /// Bật/tắt scroll. Gọi khi bắt đầu đổ (true) và khi đổ xong (false).
    /// </summary>
    public void SetScrolling(bool enabled)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(ScrollSpeedId, enabled ? 3.0f : 0f);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>
    /// Hướng scroll = hướng cát chảy = ngược hướng pipe nhìn vào board.
    /// Pipe Up → ống ở trên, cát đổ xuống → scroll (0, -1)
    /// Pipe Down → ống ở dưới, cát đổ lên → scroll (0, 1)
    /// Pipe Left → ống bên trái, cát đổ sang phải → scroll (1, 0)
    /// Pipe Right → ống bên phải, cát đổ sang trái → scroll (-1, 0)
    /// </summary>
    private static Vector2 GetScrollDirection(Direction dir)
    {
        return dir switch
        {
            Direction.Up    => new Vector2( 0f,  1f),
            Direction.Down  => new Vector2( 0f, -1f),
            Direction.Left  => new Vector2(-1f,  0f),
            Direction.Right => new Vector2( 1f,  0f),
            _ => Vector2.zero
        };
    }

    /// <summary>
    /// Fruit bị "đẩy ra" rồi despawn.
    /// </summary>
    public void SqueezeOut(Vector3 step, int orderIndex)
    {
        SetScrolling(true);
        Play(animCo, SqueezeOutCoroutine(step, orderIndex));
    }

    /// <summary>
    /// Fruit còn lại dịch chuyển để lấp chỗ trống (không despawn).
    /// </summary>
    public void Shift(Vector3 offset, int steps)
    {
        if (steps <= 0) return;
        Play(animCo, ShiftCoroutine(offset, steps));
    }

    private void Play(Coroutine current, IEnumerator next)
    {
        if (current != null) StopCoroutine(current);
        animCo = StartCoroutine(next);
    }

    private IEnumerator SqueezeOutCoroutine(Vector3 offset, int orderIndex)
    {

        Vector3 start = transform.localPosition;

        // Đẩy ra theo hướng ngược step một đoạn nhỏ
        // (tuỳ game bạn có thể đổi target = start - step * 1f hoặc +step)
        Vector3 target = start - offset * orderIndex;

        float dur = Mathf.Max(0.01f, GlobalValues.fruitSqueezeOutDuration * orderIndex);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.localPosition = Vector3.LerpUnclamped(start, target, t);
            yield return null;
        }

        transform.localPosition = target;

        // Despawn về pool
        animCo = null;
        PoolManager.Instance.Despawn(gameObject);
    }

    private IEnumerator ShiftCoroutine(Vector3 offset, int steps)
    {
        SetScrolling(true);

        Vector3 start = transform.localPosition;
        Vector3 target = start - offset * steps;

        float dur = Mathf.Max(0.01f, GlobalValues.fruitSqueezeOutDuration * steps);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            transform.localPosition = Vector3.LerpUnclamped(start, target, t);
            yield return null;
        }

        transform.localPosition = target;
        animCo = null;

        // Shift xong → dừng scroll
        SetScrolling(false);
    }

    private void OnDisable()
    {
        // an toàn khi object bị despawn trong lúc đang chạy coroutine
        if (animCo != null)
        {
            StopCoroutine(animCo);
            animCo = null;
        }
    }
}
