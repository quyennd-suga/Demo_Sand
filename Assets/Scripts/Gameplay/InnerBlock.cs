using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InnerBlock : MonoBehaviour
{
    public MeshRenderer trayRender;
    public MeshRenderer glassRender;
    public FillWaterController fillWater; // optional (Image type = Filled)


    public void ResetInnerBlock()
    {
        gameObject.SetActive(false);
        transform.localScale = Vector3.one;
        fillWater.ResetWater();
    }
    public void SetInnerBlock(Color color, int blockIndex, float duration, int colorIndex)
    {
        gameObject.SetActive(true);
        if (trayRender != null)
        {
            trayRender.material.color = color;
        }
        if (glassRender != null)
        {
            glassRender.material.color = new Color(color.r, color.g, color.b, 0.6f);
        }
        fillWater.SetWaterColor(color, blockIndex, duration, colorIndex, true);
    }

    public void PourInner(float pourAmount, bool isComplete)
    {
        fillWater.SetFillAmount(pourAmount, isComplete);
    }

    public void BlockComplete()
    {
        transform.DOScale(Vector3.zero, 0.3f).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }    
}