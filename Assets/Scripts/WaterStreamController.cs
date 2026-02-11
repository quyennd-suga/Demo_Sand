using UnityEngine;
using System.Collections;

public class WaterStreamController : MonoBehaviour
{
    [SerializeField] private WaterStreamVFX waterSream;

    [Header("Timing")]
    public float waterSpeed = 1f;
    public float revealTime = 0.3f;   // thời gian nước chạy ra tới target
    public float holdTime = 0.3f;     // đứng tại target bao lâu
    public float retractTime = 0.2f; // thời gian cut ngược từ from -> to

    private Coroutine routine;

    public void Show(Color col, Vector3 target)
    {
        if (waterSream == null) return;

        Vector3 from = transform.position;
        Vector3 to = target;

        waterSream.Config();
        waterSream.SetPath(from, to);
        waterSream.SetColor(col);

        // reset window
        waterSream.SetTailCut01(0f);
        waterSream.SetHeadCut01(0f);

        waterSream.gameObject.SetActive(true);

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(WaterDropRoutine());
    }

    private IEnumerator WaterDropRoutine()
    {
        // 1) Reveal head 0->1
        float t = 0f;
        float rt = Mathf.Max(0.001f, revealTime);
        while (t < rt)
        {
            t += Time.deltaTime * waterSpeed;
            waterSream.SetHeadCut01(Mathf.Clamp01(t / rt));
            yield return null;
        }
        waterSream.SetHeadCut01(1f);

        // 2) Hold
        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);

        // 3) Retract tail 0->1 (cut from from->to)
        float r = 0f;
        float ct = Mathf.Max(0.001f, retractTime);
        while (r < ct)
        {
            r += Time.deltaTime * waterSpeed;
            waterSream.SetTailCut01(Mathf.Clamp01(r / ct));
            yield return null;
        }
        waterSream.SetTailCut01(1f);

        // 4) Off
        waterSream.gameObject.SetActive(false);
        routine = null;
    }

    public void Hide()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        if (waterSream != null) waterSream.gameObject.SetActive(false);
    }
}
