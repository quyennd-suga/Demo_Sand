using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class AutoFillBounds : MonoBehaviour
{
    MeshRenderer r;
    MaterialPropertyBlock mpb;

    void Apply()
    {
        if (!r) r = GetComponent<MeshRenderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();

        r.GetPropertyBlock(mpb);

        var bounds = r.bounds;

        float minY = transform.InverseTransformPoint(bounds.min).y;
        float maxY = transform.InverseTransformPoint(bounds.max).y;

        mpb.SetFloat("_MinY", minY);
        mpb.SetFloat("_MaxY", maxY);

        r.SetPropertyBlock(mpb);
    }

    void OnEnable() => Apply();
    void Start() => Apply();

#if UNITY_EDITOR
    void OnValidate() => Apply(); // khi scale / edit trong editor
#endif
}
