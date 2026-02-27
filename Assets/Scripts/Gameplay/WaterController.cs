using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterController : MonoBehaviour
{
    [SerializeField]
    private FillWaterController[] mixFillWater;
    [SerializeField]
    private FillWaterController fillWater;

    [SerializeField] private GameObject waterMask;

    public void ResetWater()
    {
        fillWater.ResetWater();
        foreach (var fill in mixFillWater)
        {
            fill.ResetWater();
        }
        waterMask.SetActive(false);
    }

    public void SetFillAmount(float amount, bool isComplete, Vector3 pourWorldPos)
    {
        waterMask.SetActive(true);
        fillWater.SetFillAmount(amount, isComplete, pourWorldPos);
    }

    public void SetMixFillAmount(float amount, int color, bool isComplete, Vector3 pourWorldPos)
    {
        waterMask.SetActive(true);
        foreach (var fill in mixFillWater)
        {
            if (fill.waterColor == color)
            {
                fill.SetFillAmount(amount, isComplete, pourWorldPos);
                break;
            }
        }
    }


    public void SetWaterColor(Color color, int blIndex, float duration, int _colorIndex)
    {
        fillWater.SetWaterColor(color, blIndex, duration, _colorIndex);
    }

    public void SetMixWaterColor(Color color, int blIndex, float duration, int _colorIndex, int id)
    {
        mixFillWater[id].SetWaterColor(color, blIndex, duration, _colorIndex);
    }
}
