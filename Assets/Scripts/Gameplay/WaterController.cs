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

    public void SetFillAmount(float amount, bool isComplete, float pourWorldX)
    {
        waterMask.SetActive(true);
        fillWater.SetFillAmount(amount, isComplete, pourWorldX);
    }

    public void SetMixFillAmount(float amount, int color, bool isComplete, float pourWorldX)
    {
        waterMask.SetActive(true);
        foreach (var fill in mixFillWater)
        {
            if (fill.waterColor == color)
            {
                fill.SetFillAmount(amount, isComplete, pourWorldX);
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
