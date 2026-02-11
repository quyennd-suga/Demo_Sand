using System.Collections;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class WaterFlowSettings
{
    [Header("Timing")]
    public float fillDuration = 1.0f;
    public float holdDuration = 0.3f;
    public float drainDuration = 0.5f;
    public float flowSpeed = 2.0f;

    [Header("Visual")]
    public AnimationCurve fillCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve drainCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Color (for old shader fallback)")]
    public float borderColorMultiplier = 0.8f;
    public float highlightIntensity = 0.8f;
}

public class FruitMachine : MonoBehaviour
{
    public MeshRenderer innerMesh;
    public MeshRenderer outerMesh;

    public WaterStreamStaticMeshController waterStream;
    [Header("Pipe Water_Flow")]
    [SerializeField] private Renderer waterFlow_U;
    [SerializeField] private Renderer waterFlow_R;
    [SerializeField] private Renderer waterFlow_L;

    [Header("Water Flow Settings")]
    [SerializeField] private WaterFlowSettings waterFlowSettings = new WaterFlowSettings();

    [Header("Water Flow Material")]
    [Tooltip("Material template với shader Water_Flow_WaterFillStyle và settings đã setup")]
    public Material materialInstance;

    private Direction currentDirection;
    private Coroutine waterFlowCoroutine;

    // Stencil mask material
    private Material stencilMaskMaterial;
    private GameObject stencilMaskObject;
    private void Awake()
    {
        CreateStencilMask();
    }

    private void CreateStencilMask()
    {
        Shader maskShader = Shader.Find("Custom/Mask");
        if (maskShader == null)
        {
            return;
        }

        stencilMaskMaterial = new Material(maskShader);
        stencilMaskMaterial.SetInt("_StencilRef", 1);
        if (innerMesh != null)
        {
            stencilMaskObject = new GameObject("StencilMask");
            stencilMaskObject.transform.SetParent(transform);
            stencilMaskObject.transform.localPosition = innerMesh.transform.localPosition;
            stencilMaskObject.transform.localRotation = innerMesh.transform.localRotation;
            stencilMaskObject.transform.localScale = innerMesh.transform.localScale;

            // Copy mesh
            MeshFilter maskMeshFilter = stencilMaskObject.AddComponent<MeshFilter>();
            MeshFilter innerMeshFilter = innerMesh.GetComponent<MeshFilter>();
            if (innerMeshFilter != null)
            {
                maskMeshFilter.sharedMesh = innerMeshFilter.sharedMesh;
                Bounds maskBounds = innerMeshFilter.sharedMesh.bounds;
            }
            MeshRenderer maskRenderer = stencilMaskObject.AddComponent<MeshRenderer>();
            maskRenderer.material = stencilMaskMaterial;
        }
    }

    private void OnDestroy()
    {
        // Cleanup
        if (stencilMaskMaterial != null)
        {
            Destroy(stencilMaskMaterial);
        }
        if (stencilMaskObject != null)
        {
            Destroy(stencilMaskObject);
        }
    }

    public void Config(Direction dir)
    {
        currentDirection = dir;
        if (waterStream != null)
        {
            waterStream.ConfigWater(dir);
        }
        HideAllWaterFlows();
    }

    public void SetColor(Color color)
    {
        innerMesh.material.color = color;
        outerMesh.material.color = color;
    }

    public void RefreshMachineColor(Color color, float delay)
    {
        StartCoroutine(DelaySetColor(color, delay));
    }

    IEnumerator DelaySetColor(Color color, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetColor(color);
    }

    public void PlayWaterStream(Color color)
    {
        if (waterStream != null)
        {
            waterStream.Show(color);
        }
        ShowWaterFlow(color);
    }

    public void PlayWaterStream(Color color, int fruitCount)
    {
        if (waterStream != null)
        {
            waterStream.Show(color);
        }
        float totalDuration = fruitCount * GlobalValues.fruitSqueezeOutDuration;
        ShowWaterFlow(color, totalDuration);
    }

    public void UpdateVisualWaterFlow(Color waterColor)
    {
        float z = Mathf.Round(transform.eulerAngles.z);
        Renderer targetRenderer = null;

        if (z == 0f)        // Water_U
        {
            targetRenderer = waterFlow_U;
        }
        else if (z == 90f)  // Water_L
        {
            targetRenderer = waterFlow_R;
        }
        else if (z == 270f) // Water_R
        {
            targetRenderer = waterFlow_L;
        }

        if (targetRenderer != null)
        {
            Material matInstance = new Material(targetRenderer.material);
            targetRenderer.material = matInstance;

            matInstance.SetColor("_WaterColor", waterColor);
            matInstance.SetColor("_BorderColor", waterColor * waterFlowSettings.borderColorMultiplier);
            MeshFilter meshFilter = targetRenderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds bounds = meshFilter.sharedMesh.bounds;
                MeshFilter innerMeshFilter = innerMesh.GetComponent<MeshFilter>();
                if (innerMeshFilter != null && innerMeshFilter.sharedMesh != null)
                {
                    Bounds innerBounds = innerMeshFilter.sharedMesh.bounds;
                    float clampedMinY = Mathf.Max(bounds.min.y, innerBounds.min.y);
                    float clampedMaxY = Mathf.Min(bounds.max.y, innerBounds.max.y);

                    matInstance.SetFloat("_MinValue", clampedMinY);
                    matInstance.SetFloat("_MaxValue", clampedMaxY);
                }
                else
                {
                    matInstance.SetFloat("_MinValue", bounds.min.y);
                    matInstance.SetFloat("_MaxValue", bounds.max.y);
                }
            }
            else
            {
                matInstance.SetFloat("_MinValue", -1.0f);
                matInstance.SetFloat("_MaxValue", 1.0f);
            }

            SetFillAmountToMaterial(matInstance, 0f);
            targetRenderer.gameObject.SetActive(true);
            float fillAmountBefore = 0f;
            float fillAmountAfter = 1f;

            DOTween.To(() => fillAmountBefore, x =>
            {
                fillAmountBefore = x;
                SetFillAmountToMaterial(matInstance, fillAmountBefore);
            }, fillAmountAfter, waterFlowSettings.fillDuration).SetEase(waterFlowSettings.fillCurve);
        }
    }

    private void HideAllWaterFlows()
    {
        if (waterFlow_U != null) waterFlow_U.gameObject.SetActive(false);
        if (waterFlow_R != null) waterFlow_R.gameObject.SetActive(false);
        if (waterFlow_L != null) waterFlow_L.gameObject.SetActive(false);
    }

    private void ShowWaterFlow(Color color)
    {
        if (waterFlowCoroutine != null)
        {
            StopCoroutine(waterFlowCoroutine);
        }
        float z = Mathf.Round(transform.eulerAngles.z);
        Renderer targetRenderer = GetWaterFlowRendererByRotation(z);

        if (targetRenderer != null)
        {
            waterFlowCoroutine = StartCoroutine(PlayWaterFlowEffect(targetRenderer, color, z, -1f));
        }
    }

    private void ShowWaterFlow(Color color, float customDuration)
    {
        if (waterFlowCoroutine != null)
        {
            StopCoroutine(waterFlowCoroutine);
        }
        float z = Mathf.Round(transform.eulerAngles.z);
        Renderer targetRenderer = GetWaterFlowRendererByRotation(z);

        if (targetRenderer != null)
        {
            waterFlowCoroutine = StartCoroutine(PlayWaterFlowEffect(targetRenderer, color, z, customDuration));
        }
    }

    private Renderer GetWaterFlowRendererByRotation(float zRotation)
    {
        if (zRotation == 0f)
            return null;
        else if (zRotation == 90f)
            return waterFlow_R;
        else if (zRotation == 180f)
            return waterFlow_U;
        else if (zRotation == 270f || zRotation == -90f)
            return waterFlow_L;
        else
            return waterFlow_U;
    }

    private IEnumerator PlayWaterFlowEffect(Renderer renderer, Color color, float zRotation, float customDuration = -1f)
    {
        if (renderer == null) yield break;
        renderer.gameObject.SetActive(true);
        float actualFillDuration;
        float actualDrainDuration;
        float actualHoldDuration;

        if (customDuration > 0f)
        {
            actualFillDuration = customDuration * 0.7f;
            actualHoldDuration = customDuration * 0.1f;
            actualDrainDuration = customDuration * 0.2f;
        }
        else
        {
            actualFillDuration = waterFlowSettings.fillDuration;
            actualHoldDuration = waterFlowSettings.holdDuration;
            actualDrainDuration = waterFlowSettings.drainDuration;
        }
        Material matInstance = null;

        if (this.materialInstance != null)
        {
            matInstance = Object.Instantiate(this.materialInstance);
        }
        else if (renderer.sharedMaterial != null)
        {
            matInstance = Object.Instantiate(renderer.sharedMaterial);
        }
        else
        {
            yield break;
        }

        renderer.material = matInstance;

        if (matInstance.shader == null)
        {
            yield break;
        }

        if (matInstance.HasProperty("_SideColor"))
        {
            matInstance.SetColor("_SideColor", color);

            Color topColor = new Color(
                Mathf.Min(color.r * 1.3f, 1f),
                Mathf.Min(color.g * 1.3f, 1f),
                Mathf.Min(color.b * 1.3f, 1f),
                Mathf.Min(color.a * 1.2f, 1f)
            );
            matInstance.SetColor("_TopColor", topColor);
        }
        else
        {
            matInstance.SetColor("_WaterColor", color);
            if (matInstance.HasProperty("_BorderColor"))
            {
                matInstance.SetColor("_BorderColor", color * waterFlowSettings.borderColorMultiplier);
            }
        }
        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Bounds bounds = meshFilter.sharedMesh.bounds;

            MeshFilter innerMeshFilter = innerMesh.GetComponent<MeshFilter>();
            if (innerMeshFilter != null && innerMeshFilter.sharedMesh != null)
            {
                Bounds innerBounds = innerMeshFilter.sharedMesh.bounds;
                float clampedMinY = Mathf.Max(bounds.min.y, innerBounds.min.y);
                float clampedMaxY = Mathf.Min(bounds.max.y, innerBounds.max.y);
                matInstance.SetFloat("_MinValue", clampedMinY);
                matInstance.SetFloat("_MaxValue", clampedMaxY);

            }
            else
            {
                matInstance.SetFloat("_MinValue", bounds.min.y);
                matInstance.SetFloat("_MaxValue", bounds.max.y);
            }
            float reverseFlag = (zRotation == 0f) ? 0.0f : 1.0f;
            matInstance.SetFloat("_ReverseFill", reverseFlag);

        }
        else
        {
            matInstance.SetFloat("_MinValue", -1.0f);
            matInstance.SetFloat("_MaxValue", 1.0f);
            float reverseFlag = (zRotation == 0f) ? 0.0f : 1.0f;
            matInstance.SetFloat("_ReverseFill", reverseFlag);
        }

        SetFillAmountToMaterial(matInstance, 0f);
        float fillAmountBefore = 0f;
        float fillAmountAfter = 1f;

        var tween = DOTween.To(() => fillAmountBefore, x =>
        {
            fillAmountBefore = x;
            SetFillAmountToMaterial(matInstance, fillAmountBefore);
        }, fillAmountAfter, actualFillDuration).SetEase(waterFlowSettings.fillCurve);

        yield return tween.WaitForCompletion();
        yield return new WaitForSeconds(actualHoldDuration);
        fillAmountBefore = 1f;
        fillAmountAfter = 0f;

        var drainTween = DOTween.To(() => fillAmountBefore, x =>
        {
            fillAmountBefore = x;
            SetFillAmountToMaterial(matInstance, fillAmountBefore);
        }, fillAmountAfter, actualDrainDuration).SetEase(waterFlowSettings.drainCurve);

        yield return drainTween.WaitForCompletion();

        renderer.gameObject.SetActive(false);
        if (matInstance != null)
        {
            DestroyImmediate(matInstance);
        }
        waterFlowCoroutine = null;
    }

    private Vector3 GetFlowDirectionByRotation(float zRotation)
    {
        if (zRotation == 0f)           // Water_U
            return Vector3.down;       // (0, -1, 0) - từ trên xuống
        else if (zRotation == 90f)     // Water_L  
            return Vector3.left;       // (-1, 0, 0) - từ phải sang trái
        else if (zRotation == 270f || zRotation == -90f)  // Water_R
            return Vector3.right;      // (1, 0, 0) - từ trái sang phải
        else
            return Vector3.down;       // Default
    }

    private void SetFillAmountToMaterial(Material material, float fillAmount)
    {
        material.SetFloat("_FillAmount", fillAmount);
    }

    public void StopWaterFlow()
    {
        if (waterFlowCoroutine != null)
        {
            StopCoroutine(waterFlowCoroutine);
            waterFlowCoroutine = null;
        }
        HideAllWaterFlows();
    }

}
