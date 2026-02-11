using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(RawImage))]
public class UVAnim : MonoBehaviour
{
    public RawImage img;
    public float speed;
    float xOffset = 0;
    Rect uvRect;
    private void Awake()
    {
        if (img == null)
        {
            img = GetComponent<RawImage>();
        }
        uvRect = img.uvRect;
    }
    private void Start()
    {

    }

    private void Update()
    {
        xOffset += speed * Time.unscaledDeltaTime;
        xOffset = xOffset > 10 ? -10 : xOffset;
        xOffset = xOffset < -10 ? 10 : xOffset;
        uvRect.x = xOffset;
        img.uvRect = uvRect;
    }
}
