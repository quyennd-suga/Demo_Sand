using UnityEngine;
using System.Collections;

//[RequireComponent(typeof(MeshRenderer))]
public class WaterGraphTubeController : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Shader Props")]
    [SerializeField] private string headProp = "_Disolve";
    [SerializeField] private string tailProp = "_ReverseDisolve";
    [SerializeField] private string colorProp = "_BaseColor";

    [Header("Timing")]
    public float speed = 1f;
    public float revealTime = 0.5f;
    public float holdTime = 0.3f;
    public float retractTime = 0.35f;

    private MaterialPropertyBlock mpb;
    private int headId, tailId, colorId;
    private Coroutine routine;

    private void Awake()
    {
        if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();

        mpb = new MaterialPropertyBlock();
        headId = Shader.PropertyToID(headProp);
        tailId = Shader.PropertyToID(tailProp);
        colorId = Shader.PropertyToID(colorProp);

        SetHead(0f);
        SetTail(0f);
        meshRenderer.gameObject.SetActive(false);
    }

    public void Show(Color c)
    {
        meshRenderer.gameObject.SetActive(true);

        if (routine != null) StopCoroutine(routine);

        SetColor(c);
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
            t += Time.deltaTime * speed;
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
            r += Time.deltaTime * speed;
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
}
