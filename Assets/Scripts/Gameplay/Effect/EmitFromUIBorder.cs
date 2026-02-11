using UnityEngine;

public class EmitFromUIBorder : MonoBehaviour
{
    public RectTransform rectTransform;
    public ParticleSystem ps;

    [Header("Match UIParticle 'Scale'")]
    public float uiParticleScale = 100f;   // <-- set đúng bằng UIParticle.Scale
    public Canvas canvas;                  // kéo Canvas vào (để lấy scaleFactor)

    [Header("Spawn Rate")]
    public int emitPerSecond = 80;

    [Header("Inset (pixels)")]
    public float insetPx = 0f;

    [Header("Inward Velocity (pixels/sec)")]
    public float speedMinPx = 40f;
    public float speedMaxPx = 90f;
    public float inwardRandomAngle = 12f;

    float acc;

    private bool isActive = false;

    void Awake()
    {
        if (!rectTransform) rectTransform = GetComponent<RectTransform>();
        if (!ps) ps = GetComponent<ParticleSystem>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.rateOverDistance = 0f;
    }
    public void StartEmitting()
    {
        isActive = true;
    }
    public void StopEmitting()
    {
        isActive = false;
    }
    void Update()
    {
        if (!rectTransform || !ps) return;
        if (!isActive) return;

        acc += Time.deltaTime * emitPerSecond;
        int count = Mathf.FloorToInt(acc);
        if (count <= 0) return;
        acc -= count;

        float canvasScale = canvas ? canvas.scaleFactor : 1f;
        float denom = Mathf.Max(0.0001f, uiParticleScale * canvasScale);

        var ep = new ParticleSystem.EmitParams();

        for (int i = 0; i < count; i++)
        {
            GetSpawnAndVelocityPx(out Vector2 posPx, out Vector2 velPx);

            // ✅ convert pixel -> particle units (Absolute mode)
            ep.position = new Vector3(posPx.x / denom, posPx.y / denom, 0f);
            ep.velocity = new Vector3(velPx.x / denom, velPx.y / denom, 0f);

            ps.Emit(ep, 1);
        }
    }

    void GetSpawnAndVelocityPx(out Vector2 posPx, out Vector2 velPx)
    {
        Rect r = rectTransform.rect;
        float halfW = r.width * 0.5f;
        float halfH = r.height * 0.5f;

        int side = Random.Range(0, 4);
        float t = Random.value;

        Vector2 dir;

        switch (side)
        {
            case 0: posPx = new Vector2(-halfW + insetPx, Mathf.Lerp(-halfH, halfH, t)); dir = Vector2.right; break;
            case 1: posPx = new Vector2(halfW - insetPx, Mathf.Lerp(-halfH, halfH, t)); dir = Vector2.left; break;
            case 2: posPx = new Vector2(Mathf.Lerp(-halfW, halfW, t), -halfH + insetPx); dir = Vector2.up; break;
            default: posPx = new Vector2(Mathf.Lerp(-halfW, halfW, t), halfH - insetPx); dir = Vector2.down; break;
        }

        float ang = Random.Range(-inwardRandomAngle, inwardRandomAngle);
        dir = (Quaternion.Euler(0, 0, ang) * dir).normalized;

        float sp = Random.Range(speedMinPx, speedMaxPx);
        velPx = dir * sp;
    }
}
