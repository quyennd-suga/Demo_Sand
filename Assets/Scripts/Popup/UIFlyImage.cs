using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFlyImage : MonoBehaviour
{
    [Header("Start and Target")]
    [SerializeField] private RectTransform fromRect; // where the image starts
    [SerializeField] private RectTransform[] toRect;   // where the image ends

    [Header("Where the flying image lives (recommended: Screen Space Overlay canvas)")]
    [SerializeField] private Canvas flyCanvas;          // Canvas chứa object bay
    [SerializeField] private RectTransform flyRoot;     // thường = flyCanvas.transform as RectTransform
    [SerializeField] private Image img;      // prefab Image (Raycast Target OFF)

    [Header("Anim")]
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    /// <summary>
    /// Create an Image that matches 'from', then animates to match 'to' (pos + size).
    /// </summary>
    public Coroutine Play(Sprite sprite, float duration,
                          bool matchRotation = false, int toIndex = 0)
    {
        gameObject.SetActive(true);
        return StartCoroutine(CoPlay(sprite, duration, matchRotation, toIndex));
    }

    private IEnumerator CoPlay(Sprite sprite, float duration,
                               bool matchRotation, int toIndex)
    {
        if (!flyCanvas) yield break;
        if (!flyRoot) flyRoot = flyCanvas.transform as RectTransform;

        // Spawn
        img.raycastTarget = false;
        img.sprite = sprite;

        RectTransform rt = img.rectTransform;

        // IMPORTANT: make fly-rect easy to control
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Compute start/end in flyRoot local space
        GetRectInLocalSpace(fromRect, flyRoot, out var startCenter, out var startSize, out var startRot);
        GetRectInLocalSpace(toRect[toIndex], flyRoot, out var endCenter, out var endSize, out var endRot);
        rt.anchoredPosition = startCenter;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, startSize.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, startSize.y);
        if (matchRotation) rt.localRotation = startRot;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            float k = ease.Evaluate(Mathf.Clamp01(t));

            rt.anchoredPosition = Vector2.LerpUnclamped(startCenter, endCenter, k);

            Vector2 size = Vector2.LerpUnclamped(startSize, endSize, k);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            if (matchRotation)
                rt.localRotation = Quaternion.SlerpUnclamped(startRot, endRot, k);

            yield return null;
        }

        // Snap end
        rt.anchoredPosition = endCenter;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, endSize.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, endSize.y);
        if (matchRotation) rt.localRotation = endRot;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Convert a RectTransform's world rect into local (targetRoot) center+size (+ rotation).
    /// </summary>
    private static void GetRectInLocalSpace(RectTransform source, RectTransform targetRoot,
                                           out Vector2 centerLocal, out Vector2 sizeLocal, out Quaternion rotLocal)
    {
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners); // 0:BL 1:TL 2:TR 3:BR

        // World -> Screen -> targetRoot local
        var cam = GetEventCameraFor(source);
        for (int i = 0; i < 4; i++)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRoot, sp, GetEventCameraFor(targetRoot), out var lp);
            corners[i] = lp;
        }

        // axis-aligned bounds in targetRoot local
        Vector2 min = corners[0];
        Vector2 max = corners[0];
        for (int i = 1; i < 4; i++)
        {
            min = Vector2.Min(min, corners[i]);
            max = Vector2.Max(max, corners[i]);
        }

        sizeLocal = max - min;
        centerLocal = (min + max) * 0.5f;

        // Optional: approximate rotation mapping
        // (good enough if you just want follow rotation, not perfect for skewed transforms)
        rotLocal = Quaternion.Inverse(targetRoot.rotation) * source.rotation;
    }

    private static Camera GetEventCameraFor(RectTransform rt)
    {
        var c = rt.GetComponentInParent<Canvas>();
        if (!c) return null;

        if (c.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return c.worldCamera != null ? c.worldCamera : Camera.main;
    }
}
