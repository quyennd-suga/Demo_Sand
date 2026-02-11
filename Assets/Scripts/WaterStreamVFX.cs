using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterStreamVFX : MonoBehaviour
{
    [Header("Mesh")]
    [Range(4, 64)] public int segments = 18;
    public float width = 0.18f;
    [Tooltip("Độ cong Bezier: 0..1 (cao hơn => cong hơn)")]
    [Range(0f, 1f)] public float curve = 0.55f;

    [Header("Shader Props")]
    [SerializeField] private string headProp = "_Cutoff01";
    [SerializeField] private string tailProp = "_TailCut01";
    [SerializeField] private string colorProp = "_Color";

    private Mesh mesh;
    private Vector3[] verts;
    private Vector2[] uvs;
    private int[] tris;

    private MaterialPropertyBlock mpb;
    private int headId, tailId, colorId;

    private MeshRenderer mr;

    public void Config()
    {
        if (mr == null) mr = GetComponent<MeshRenderer>();

        // Create once (or recreate if missing)
        if (mesh == null)
        {
            mesh = new Mesh { name = "WaterStreamRibbon" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;

            mpb = new MaterialPropertyBlock();
            headId = Shader.PropertyToID(headProp);
            tailId = Shader.PropertyToID(tailProp);
            colorId = Shader.PropertyToID(colorProp);

            Alloc();

            // Static indices only once
            mesh.vertices = verts; // placeholder
            mesh.uv = uvs;         // placeholder
            mesh.triangles = tris; // ONCE
        }
        else
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            headId = Shader.PropertyToID(headProp);
            tailId = Shader.PropertyToID(tailProp);
            colorId = Shader.PropertyToID(colorProp);
        }

        // default window full hidden/revealed handled by controller
        SetHeadCut01(1f);
        SetTailCut01(0f);
    }

    private void Alloc()
    {
        int vCount = (segments + 1) * 2;
        int tCount = segments * 6;

        verts = new Vector3[vCount];
        uvs = new Vector2[vCount];
        tris = new int[tCount];

        int ti = 0;
        for (int i = 0; i < segments; i++)
        {
            int v = i * 2;
            tris[ti++] = v;
            tris[ti++] = v + 1;
            tris[ti++] = v + 2;

            tris[ti++] = v + 2;
            tris[ti++] = v + 1;
            tris[ti++] = v + 3;
        }
    }

    public void SetPath(Vector3 startWS, Vector3 endWS)
    {
        // Keep object at origin; vertices stored in world space effectively
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        Vector3 p0 = startWS;
        Vector3 p3 = endWS;

        Vector3 dir = (p3 - p0);
        float len = dir.magnitude;
        if (len < 1e-4f)
        {
            gameObject.SetActive(false);
            return;
        }

        Vector3 up = Vector3.up;
        Vector3 side = Vector3.Cross(dir.normalized, up);
        if (side.sqrMagnitude < 1e-4f) side = Vector3.right;

        Vector3 bend = up * (len * 0.35f * curve) + side * (len * 0.08f * curve);
        Vector3 p1 = p0 + dir * 0.33f + bend;
        Vector3 p2 = p0 + dir * 0.66f + bend;

        float halfW = width * 0.5f;

        Vector3 camFwd = Vector3.forward;
        //var cam = Camera.main;
        //if (cam) camFwd = cam.transform.forward;

        for (int i = 0; i <= segments; i++)
        {
            float tt = (float)i / segments;

            Vector3 pos = Bezier(p0, p1, p2, p3, tt);
            Vector3 pos2 = Bezier(p0, p1, p2, p3, Mathf.Min(1f, tt + (1f / segments)));

            Vector3 tangent = (pos2 - pos);
            if (tangent.sqrMagnitude < 1e-6f) tangent = dir;
            tangent.Normalize();

            Vector3 n = Vector3.Cross(tangent, camFwd);
            if (n.sqrMagnitude < 1e-6f) n = Vector3.Cross(tangent, Vector3.up);
            n.Normalize();

            Vector3 left = pos - n * halfW;
            Vector3 right = pos + n * halfW;

            int v = i * 2;
            verts[v] = left;
            verts[v + 1] = right;

            uvs[v] = new Vector2(tt, 0f);
            uvs[v + 1] = new Vector2(tt, 1f);
        }

        // Update dynamic data only
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);

        // Cheap bounds
        var center = (p0 + p3) * 0.5f;
        var ext = new Vector3(
            Mathf.Abs(p3.x - p0.x) * 0.5f + width,
            Mathf.Abs(p3.y - p0.y) * 0.5f + width,
            Mathf.Abs(p3.z - p0.z) * 0.5f + width
        );
        mesh.bounds = new Bounds(center, ext * 2f);

        gameObject.SetActive(true);
    }

    public void SetColor(Color c)
    {
        if (mr == null) mr = GetComponent<MeshRenderer>();
        mr.GetPropertyBlock(mpb);
        mpb.SetColor(colorId, c);
        mr.SetPropertyBlock(mpb);
    }

    public void SetHeadCut01(float v01)
    {
        if (mr == null) mr = GetComponent<MeshRenderer>();
        mr.GetPropertyBlock(mpb);
        mpb.SetFloat(headId, Mathf.Clamp01(v01));
        mr.SetPropertyBlock(mpb);
    }

    public void SetTailCut01(float v01)
    {
        if (mr == null) mr = GetComponent<MeshRenderer>();
        mr.GetPropertyBlock(mpb);
        mpb.SetFloat(tailId, Mathf.Clamp01(v01));
        mr.SetPropertyBlock(mpb);
    }

    private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1f - t;
        return (u * u * u) * a + (3f * u * u * t) * b + (3f * u * t * t) * c + (t * t * t) * d;
    }
}
