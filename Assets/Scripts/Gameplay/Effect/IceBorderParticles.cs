using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class IceBorderParticles : MonoBehaviour
{
    [Header("Camera / Plane")]
    public Camera targetCamera;
    [Tooltip("Khoảng cách từ camera tới mặt phẳng particles (đặt trước camera).")]
    public float distanceFromCamera = 3f;

    [Header("Border Ring (Screen)")]
    [Tooltip("Độ dày viền theo % chiều ngắn màn hình (0.05 = 5%).")]
    [Range(0.01f, 0.35f)] public float borderThickness01 = 0.12f;

    [Header("Particles Look")]
    public int maxParticles = 180;
    public float spawnRate = 20f;
    public Vector2 startSize = new Vector2(0.08f, 0.22f);
    public float startLifetime = 6f;
    public float sizeOverLife = 0.6f;   // 1 -> giữ size, <1 -> nhỏ dần

    [Header("Motion")]
    public float driftSpeed = 0.05f;     // trôi nhẹ
    public float noiseStrength = 0.15f;  // rung nhẹ kiểu sương/băng

    [Header("Soft Fade")]
    [Tooltip("Dùng Material có shader Additive/AlphaBlend + texture đốm tròn mềm sẽ giống ảnh hơn.")]
    public Material particleMaterial;

    ParticleSystem ps;
    ParticleSystem.MainModule main;
    ParticleSystem.EmissionModule emission;

    // ring bounds (world space)
    float outerW, outerH;
    float innerW, innerH;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (!targetCamera) targetCamera = Camera.main;

        ConfigureParticleSystem();
        RebuildBounds();
    }

    void OnEnable()
    {
        if (ps != null) ps.Play();
    }

    void LateUpdate()
    {
        if (!targetCamera) return;

        // Giữ hệ particles luôn nằm trước camera, không bị lệch khi camera move/rotate
        transform.position = targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera;
        transform.rotation = targetCamera.transform.rotation;

        // Nếu đổi aspect/resolution (hoặc bạn muốn responsive), rebuild lại bounds
        // (Đơn giản: rebuild mỗi frame; nhẹ vì chỉ là vài phép tính)
        RebuildBounds();

        // Gán ring bounds cho spawn
        ClampParticlesToRing();
    }

    void ConfigureParticleSystem()
    {
        // Render
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = short.MaxValue; // luôn nằm trên
        if (particleMaterial) renderer.material = particleMaterial;

        main = ps.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.startLifetime = startLifetime;
        main.startSpeed = driftSpeed;
        main.startSize = Random.Range(startSize.x, startSize.y);
        main.startRotation = 0f;
        main.startColor = new Color(1f, 1f, 1f, 0.9f);

        // Emission
        emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = spawnRate;

        // Shape: box full-screen (outer rect). Ring sẽ do code lọc spawn & clamp.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.boxThickness = Vector3.zero;

        // Noise (rung nhẹ)
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = noiseStrength;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.15f;
        noise.octaveCount = 1;

        // Size over lifetime (cho mềm)
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        var curve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, sizeOverLife)
        );
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // Optional: giảm tốc dần để nhìn “đóng băng” hơn
        var limitVel = ps.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.dampen = 0.4f;
    }

    void RebuildBounds()
    {
        // Tính kích thước khung world-space tương ứng với màn hình tại distanceFromCamera
        float h = 2f * Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * distanceFromCamera;
        float w = h * targetCamera.aspect;

        outerW = w;
        outerH = h;

        // borderThickness theo chiều ngắn để “nhìn đều” trên mọi aspect
        float shortSide = Mathf.Min(outerW, outerH);
        float t = shortSide * borderThickness01;

        innerW = Mathf.Max(0.01f, outerW - 2f * t);
        innerH = Mathf.Max(0.01f, outerH - 2f * t);

        // set shape box = outer rect
        var shape = ps.shape;
        shape.scale = new Vector3(outerW, outerH, 0.01f);
    }

    void ClampParticlesToRing()
    {
        // Lấy particles ra, rồi ép vị trí của chúng nằm trong ring nếu lỡ bay vào giữa
        int count = ps.particleCount;
        if (count <= 0) return;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[count];
        int got = ps.GetParticles(particles);

        Vector3 center = transform.position;
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        float outerHalfW = outerW * 0.5f;
        float outerHalfH = outerH * 0.5f;
        float innerHalfW = innerW * 0.5f;
        float innerHalfH = innerH * 0.5f;

        for (int i = 0; i < got; i++)
        {
            Vector3 p = particles[i].position;

            // chuyển sang local (2D trên plane)
            Vector3 d = p - center;
            float x = Vector3.Dot(d, right);
            float y = Vector3.Dot(d, up);

            // nếu nằm trong inner rect -> đẩy ra border gần nhất
            bool insideInner = Mathf.Abs(x) < innerHalfW && Mathf.Abs(y) < innerHalfH;
            if (insideInner)
            {
                // chọn hướng đẩy ra theo cạnh gần nhất
                float dx = innerHalfW - Mathf.Abs(x);
                float dy = innerHalfH - Mathf.Abs(y);

                if (dx < dy)
                    x = Mathf.Sign(x) * innerHalfW;
                else
                    y = Mathf.Sign(y) * innerHalfH;

                // cộng thêm chút random để không bị “đường thẳng”
                x += Random.Range(-0.08f, 0.08f);
                y += Random.Range(-0.08f, 0.08f);

                // clamp trong outer rect
                x = Mathf.Clamp(x, -outerHalfW, outerHalfW);
                y = Mathf.Clamp(y, -outerHalfH, outerHalfH);

                particles[i].position = center + right * x + up * y;
            }

            // scale size random nhẹ theo thời gian sống để “đốm” tự nhiên
            float life01 = 1f - (particles[i].remainingLifetime / particles[i].startLifetime);
            particles[i].startSize = Mathf.Lerp(startSize.y, startSize.x, life01);
        }

        ps.SetParticles(particles, got);
    }
}
