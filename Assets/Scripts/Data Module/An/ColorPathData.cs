
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ColorPathData
{
    public int color;
    public List<Vector2Int> positions = new List<Vector2Int>();

    public ColorPathData() { }

    public ColorPathData(ColorPathData other)
    {
        color = other.color;
        positions = new List<Vector2Int>(other.positions);
    }
}