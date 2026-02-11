using UnityEngine;

public class WaterFillRuntime : MonoBehaviour
{
    [Range(0, 1)] public float target = 1f;
    public float speed = 0.3f;
    public bool play = true;

    static readonly int FillID = Shader.PropertyToID("_Fill");

    Renderer r;
    MaterialPropertyBlock mpb;
    float fill;

    void Awake() { r = GetComponent<Renderer>(); mpb = new MaterialPropertyBlock(); }

    void Update()
    {
        if (!play || r == null) return;

        if (fill < target) fill = Mathf.Min(target, fill + speed * Time.deltaTime);

        r.GetPropertyBlock(mpb);
        mpb.SetFloat(FillID, fill);
        r.SetPropertyBlock(mpb);
    }
}
