using UnityEngine;
using System.Collections;


public class WaterStreamStaticMeshController : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Shader property names")]
    [SerializeField] private string colorProp = "_Color";
    [SerializeField] private string headProp = "_HeadCut01";
    [SerializeField] private string tailProp = "_TailCut01";

    [Header("Timing")]
    public float waterSpeed = 1f;
    public float revealTime = 0.5f;
    public float holdTime = 0.3f;
    public float retractTime = 0.35f;

    private MaterialPropertyBlock mpb;
    private int colorId, headId, tailId;
    private Coroutine routine;

    private void Awake()
    {
        if (!meshRenderer) meshRenderer = GetComponentInChildren<MeshRenderer>();

        mpb = new MaterialPropertyBlock();
        colorId = Shader.PropertyToID(colorProp);
        headId = Shader.PropertyToID(headProp);
        tailId = Shader.PropertyToID(tailProp);

        SetHead(0f);
        SetTail(0f);
        meshRenderer.gameObject.SetActive(false);
    }

    public void ConfigWater(Direction dir)
    {
        SetWaterDirection(dir);
    }
    public void Show(Color col)
    {
        meshRenderer.gameObject.SetActive(true);

        if (routine != null) StopCoroutine(routine);

        SetColor(col);
        SetTail(0f);
        SetHead(0f);

        routine = StartCoroutine(Run());
    }

    public void Hide()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        meshRenderer.gameObject.SetActive(false);
    }

    private IEnumerator Run()
    {
        // Reveal
        float t = 0f;
        float rt = Mathf.Max(0.001f, revealTime);
        while (t < rt)
        {
            t += Time.deltaTime * waterSpeed;
            SetHead(Mathf.Clamp01(t / rt));
            yield return null;
        }
        SetHead(1f);

        // Hold
        if (holdTime > 0f) yield return new WaitForSeconds(holdTime);

        // Retract
        float r = 0f;
        float ct = Mathf.Max(0.001f, retractTime);
        while (r < ct)
        {
            r += Time.deltaTime * waterSpeed;
            SetTail(Mathf.Clamp01(r / ct));
            yield return null;
        }
        SetTail(1f);

        meshRenderer.gameObject.SetActive(false);
        routine = null;
    }

    private void SetColor(Color c)
    {
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(colorId, c);
        meshRenderer.SetPropertyBlock(mpb);
    }

    private void SetHead(float v01)
    {
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(headId, Mathf.Clamp01(v01));
        meshRenderer.SetPropertyBlock(mpb);
    }

    private void SetTail(float v01)
    {
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(tailId, Mathf.Clamp01(v01));
        meshRenderer.SetPropertyBlock(mpb);
    }


    public void SetWaterDirection(Direction dir)
    {
        Vector3 rot = Vector3.zero;
        Vector3 offset = Vector3.zero;
        switch (dir)
        {
            case Direction.Up:
                rot = Vector3.zero;
                break;
            case Direction.Right:
                rot = new Vector3(-180f, 0f, 270f);
                offset = new Vector3(-0.25f, 1f, 0f);
                break;
            case Direction.Down:
                rot = Vector3.zero;
                break;
            case Direction.Left:
                offset = new Vector3(0.25f, 1f, 0f);
                rot = new Vector3(0f, 0f, 90f);
                break;
        }
        transform.localPosition = offset;
        transform.localEulerAngles = rot;
    }    
}
