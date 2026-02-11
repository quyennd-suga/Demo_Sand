using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FitWidthScale : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;

    [Header("Design Resolution")]
    public Vector2 designResolution = new Vector2(1024, 2048);

    Vector3 originalScale;

    RectTransform canvasRect;

    void Awake()
    {
        Cache();
        SetupCanvas();
        Apply();
    }

    void OnEnable()
    {
        Cache();
        SetupCanvas();
        Apply();
    }

    void OnValidate()
    {
        Cache();
        SetupCanvas();
        Apply();
    }

    void Update()
    {
        Apply();
    }

    void Cache()
    {
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
    }

    void SetupCanvas()
    {
        if (!canvas)
            canvas = GetComponentInParent<Canvas>();

        if (canvas)
            canvasRect = canvas.GetComponent<RectTransform>();
    }

    void Apply()
    {
        if (!canvasRect) return;

        Vector2 current = canvasRect.rect.size;

        if (current == Vector2.zero) return;

        float scale = current.x / designResolution.x;

        transform.localScale = originalScale * scale;
    }
}
