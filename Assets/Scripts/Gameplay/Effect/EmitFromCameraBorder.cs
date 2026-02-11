using UnityEngine;

public class EmitFromCameraBorder : MonoBehaviour
{
    public Camera cam;
    public ParticleSystem ps;

    [Header("Spawn Rate")]
    public int emitPerSecond = 80;

    [Header("Spawn Depth")]
    public float depthFromCamera = 10f; // dùng cho camera perspective
    public float zPlaneWorld = 0f;      // dùng cho camera orthographic (2D)

    [Header("Inward Velocity")]
    public float speedMin = 1.5f;
    public float speedMax = 3.5f;
    public float inwardRandomAngle = 12f; // độ lệch hướng vào trong

    [Header("Inset")]
    [Range(-0.5f, 0.49f)]
    public float inset = 0.0f; // viewport inset (0..1)

    [Header("Safety")]
    public bool disableDefaultEmission = true; // <-- thêm

    float acc;

    void Reset()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!ps) ps = GetComponent<ParticleSystem>();

        if (ps && disableDefaultEmission)
        {
            // ✅ Tắt việc ParticleSystem tự spawn (nguyên nhân spawn ở giữa)
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;

            // Clear bursts nếu có set trong Inspector
            int burstCount = emission.burstCount;
            if (burstCount > 0)
            {
                var empty = new ParticleSystem.Burst[0];
                emission.SetBursts(empty);
            }

            // (Tuỳ chọn) Tắt Shape module để chắc chắn không có emit theo shape
            var shape = ps.shape;
            shape.enabled = false;
        }
    }

    void Update()
    {
        if (!cam || !ps) return;

        acc += Time.deltaTime * emitPerSecond;
        int count = Mathf.FloorToInt(acc);
        if (count <= 0) return;
        acc -= count;

        var ep = new ParticleSystem.EmitParams();

        for (int i = 0; i < count; i++)
        {
            GetSpawnAndVelocity(out Vector3 pos, out Vector3 vel);
            ep.position = pos;
            ep.velocity = vel;
            ps.Emit(ep, 1);
        }
    }

    void GetSpawnAndVelocity(out Vector3 worldPos, out Vector3 worldVel)
    {
        // 0=Left,1=Right,2=Bottom,3=Top
        int side = Random.Range(0, 4);
        float t = Random.value;

        Vector3 viewportPos = Vector3.zero;
        Vector3 viewportDir = Vector3.zero;

        float i = inset; // inset theo viewport (0..1)

        switch (side)
        {
            case 0: // Left
                viewportPos = new Vector3(0f + i, t, 0);
                viewportDir = Vector3.right;
                break;

            case 1: // Right
                viewportPos = new Vector3(1f - i, t, 0);
                viewportDir = Vector3.left;
                break;

            case 2: // Bottom
                viewportPos = new Vector3(t, 0f + i, 0);
                viewportDir = Vector3.up;
                break;

            default: // Top
                viewportPos = new Vector3(t, 1f - i, 0);
                viewportDir = Vector3.down;
                break;
        }

        // -------- world position --------
        if (cam.orthographic)
        {
            float camZ = cam.transform.position.z;
            float depth = Mathf.Abs(camZ - zPlaneWorld);
            viewportPos.z = depth;

            worldPos = cam.ViewportToWorldPoint(viewportPos);
            worldPos.z = zPlaneWorld;
        }
        else
        {
            viewportPos.z = depthFromCamera;
            worldPos = cam.ViewportToWorldPoint(viewportPos);
        }

        // -------- inward velocity --------
        Vector3 worldDir;

        if (cam.orthographic)
        {
            worldDir = (viewportDir.x * cam.transform.right + viewportDir.y * cam.transform.up).normalized;
        }
        else
        {
            Vector3 vp2 = viewportPos + (Vector3)(viewportDir * 0.01f);
            vp2.z = viewportPos.z;
            Vector3 w2 = cam.ViewportToWorldPoint(vp2);
            worldDir = (w2 - worldPos).normalized;
        }

        worldDir = Quaternion.AngleAxis(Random.Range(-inwardRandomAngle, inwardRandomAngle), cam.transform.forward) * worldDir;

        float speed = Random.Range(speedMin, speedMax);
        worldVel = worldDir * speed;
    }
}
