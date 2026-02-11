using System.Collections.Generic;
using UnityEngine;

public class EmitParticlesOnSpriteOutline : MonoBehaviour
{
    public ParticleSystem ps;
    public PolygonCollider2D poly;

    public int emitPerSecond = 80;
    public bool emitInWorldSpace = true;

    float acc;

    // cache segments
    struct Segment { public Vector2 a, b; public float len; }
    List<Segment> segs = new();
    float totalLen;

    void Reset()
    {
        ps = GetComponent<ParticleSystem>();
        poly = GetComponent<PolygonCollider2D>();
    }

    void Awake()
    {
        RebuildSegments();
    }

    // gọi lại khi bạn đổi sprite runtime
    public void RebuildSegments()
    {
        segs.Clear();
        totalLen = 0f;

        if (!poly || poly.pathCount == 0) return;

        for (int p = 0; p < poly.pathCount; p++)
        {
            var path = poly.GetPath(p);
            if (path.Length < 2) continue;

            for (int i = 0; i < path.Length; i++)
            {
                var a = path[i];
                var b = path[(i + 1) % path.Length];

                float len = Vector2.Distance(a, b);
                if (len <= 0.0001f) continue;

                segs.Add(new Segment { a = a, b = b, len = len });
                totalLen += len;
            }
        }
    }

    void Update()
    {
        if (!ps || segs.Count == 0) return;

        acc += Time.deltaTime * emitPerSecond;
        int count = Mathf.FloorToInt(acc);
        if (count <= 0) return;
        acc -= count;

        var emitParams = new ParticleSystem.EmitParams();

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetPointOnOutline();
            emitParams.position = pos;
            ps.Emit(emitParams, 1);
        }
    }

    Vector3 GetPointOnOutline()
    {
        // pick random by length
        float r = Random.value * totalLen;
        for (int i = 0; i < segs.Count; i++)
        {
            r -= segs[i].len;
            if (r <= 0f)
            {
                float t = Random.value;
                Vector2 local = Vector2.Lerp(segs[i].a, segs[i].b, t);

                if (emitInWorldSpace)
                    return transform.TransformPoint(local);

                return (Vector3)local;
            }
        }

        // fallback
        var last = segs[segs.Count - 1];
        Vector2 lf = Vector2.Lerp(last.a, last.b, Random.value);
        return emitInWorldSpace ? transform.TransformPoint(lf) : (Vector3)lf;
    }
}
